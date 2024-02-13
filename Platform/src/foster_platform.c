#include "foster_platform.h"
#include "foster_renderer.h"
#include "foster_internal.h"
#include <SDL.h>

#define STBTT_STATIC
#define STB_TRUETYPE_IMPLEMENTATION
#include "third_party/stb_truetype.h"

#define FOSTER_MAX_MESSAGE_SIZE 1024

#define FOSTER_CHECK(flags, flag) \
	(((flags) & (flag)) != 0)

#define FOSTER_ASSERT_RUNNING_RET(func, ret) \
	do { if (!fstate.running) { FOSTER_LOG_ERROR("Failed '%s', Foster is not running", #func); return ret; } } while(0)

#define FOSTER_ASSERT_RUNNING(func) \
	do { if (!fstate.running) { FOSTER_LOG_ERROR("Failed '%s', Foster is not running", #func); return; } } while(0)

FosterKeys FosterGetKeyFromSDL(SDL_Scancode key);
FosterButtons FosterGetButtonFromSDL(SDL_GameControllerButton button);
FosterMouse FosterGetMouseFromSDL(uint8_t button);
FosterAxis FosterGetAxisFromSDL(int axis);
int FosterFindJoystickIndexSDL(SDL_Joystick** joysticks, SDL_JoystickID instanceID);
int FosterFindGamepadIndexSDL(SDL_GameController** gamepads, SDL_JoystickID instanceID);

static FosterState fstate;

FosterState* FosterGetState()
{
	return &fstate;
}

void FosterLog_SDL(void *userdata, int category, SDL_LogPriority priority, const char *message)
{
	switch (priority)
	{
		case SDL_LOG_PRIORITY_VERBOSE:
		case SDL_LOG_PRIORITY_DEBUG:
			if (fstate.logFilter == FOSTER_LOG_FILTER_VERBOSE)
				FOSTER_LOG_INFO("%s", message);
			break;
		case SDL_LOG_PRIORITY_INFO:
			FOSTER_LOG_INFO("%s", message);
			break;
		case SDL_LOG_PRIORITY_WARN:
			FOSTER_LOG_WARN("%s", message);
			break;
		case SDL_LOG_PRIORITY_ERROR:
		case SDL_LOG_PRIORITY_CRITICAL:
			FOSTER_LOG_ERROR("%s", message);
			break;
	}
}

void FosterStartup(FosterDesc desc)
{
	fstate.desc = desc;
	fstate.flags = 0;
	fstate.window = NULL;
	fstate.windowCreateFlags = SDL_WINDOW_ALLOW_HIGHDPI | SDL_WINDOW_HIDDEN;
	fstate.running = false;
	fstate.device.renderer = FOSTER_RENDERER_NONE;
	fstate.clipboardText = NULL;
	fstate.userPath = NULL;

	if (fstate.desc.width <= 0 || fstate.desc.height <= 0)
	{
		FOSTER_LOG_ERROR("Foster invalid application width/height (%i, %i)", desc.width, desc.height);
		return;
	}

	// Get SDL version
	SDL_version version;
	SDL_GetVersion(&version);
	FOSTER_LOG_INFO("SDL: v%i.%i.%i", version.major, version.minor, version.patch);

	// track SDL output
	if (fstate.logFilter != FOSTER_LOG_FILTER_IGNORE_ALL && fstate.logFn)
	{
		SDL_LogSetOutputFunction(FosterLog_SDL, NULL);
	}

	// Make us DPI aware on Windows
	SDL_SetHint(SDL_HINT_WINDOWS_DPI_AWARENESS, "permonitorv2");

	// use physical button layout, not labels
	SDL_SetHintWithPriority(SDL_HINT_GAMECONTROLLER_USE_BUTTON_LABELS, "0", SDL_HINT_OVERRIDE);

	// by default allow controller presses while unfocused, let game decide if it should handle them
	SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");

	// initialize SDL
	int sdl_init_flags = SDL_INIT_VIDEO | SDL_INIT_TIMER | SDL_INIT_EVENTS | SDL_INIT_JOYSTICK | SDL_INIT_GAMECONTROLLER;
	if (SDL_Init(sdl_init_flags) != 0)
	{
		FOSTER_LOG_ERROR("Foster SDL_Init Failed: %s", SDL_GetError());
		return;
	}

	// determine renderer type
	if (!FosterGetDevice(fstate.desc.renderer, &fstate.device))
	{
		FOSTER_LOG_ERROR("Foster Failed to get Renderer Device");
		return;
	}

	// let renderer run any prep
	if (fstate.device.prepare)
		fstate.device.prepare();

	// create the Window
	fstate.window = SDL_CreateWindow(
		(fstate.desc.windowTitle == NULL ? "Foster Application" : fstate.desc.windowTitle),
		SDL_WINDOWPOS_CENTERED,
		SDL_WINDOWPOS_CENTERED,
		fstate.desc.width,
		fstate.desc.height,
		fstate.windowCreateFlags);

	if (fstate.window == NULL)
	{
		FOSTER_LOG_ERROR("Foster SDL_CreateWindow Failed: %s", SDL_GetError());
		return;
	}

	fstate.running = true;

	// initialize renderer
	if (fstate.device.initialize)
	{
		if (!fstate.device.initialize())
		{
			FOSTER_LOG_ERROR("Foster Failed to initialize Renderer Device");
			fstate.running = false;
			SDL_DestroyWindow(fstate.window);
			return;
		}
	}

	// toggle flags & show window
	FosterSetFlags(fstate.desc.flags);
	SDL_ShowWindow(fstate.window);
}

void FosterSetLogCallback(FosterLogFn logFn, FosterLogFilter logFiler)
{
	fstate.logFn = logFn;
	fstate.logFilter = logFiler;
}

void FosterBeginFrame()
{
	FOSTER_ASSERT_RUNNING(FosterBeginFrame);

	if (fstate.device.frameBegin)
		fstate.device.frameBegin();
}

FosterBool FosterPollEvents(FosterEvent* output)
{
	FOSTER_ASSERT_RUNNING_RET(FosterPollEvents, 0);

	SDL_Event event;
	*output = (FosterEvent){ 0 };
	output->eventType = FOSTER_EVENT_TYPE_NONE;

	// Mouse Movement is always polled every time we poll events
	// This way we track it even if SDL hasn't sent new mouse motion events
	// TODO: should this just change to a getter?
	if (!fstate.polledMouseMovement)
	{
		int mouseX, mouseY;
		SDL_GetMouseState(&mouseX, &mouseY);
		output->eventType = FOSTER_EVENT_TYPE_MOUSE_MOVE;
		output->mouse.x = (float)mouseX;
		output->mouse.y = (float)mouseY;
		fstate.polledMouseMovement = 1;
		return 1;
	}

	// get next SDL event, or return false if no more found
NEXT_EVENT:
	if (!SDL_PollEvent(&event))
	{
		fstate.polledMouseMovement = 0;
		return 0;
	}

	// System Events
	if (event.type == SDL_QUIT)
	{
		output->eventType = FOSTER_EVENT_TYPE_EXIT_REQUESTED;
	}
	// Mouse
	else if (event.type == SDL_MOUSEBUTTONDOWN || event.type == SDL_MOUSEBUTTONUP)
	{
		output->eventType = FOSTER_EVENT_TYPE_MOUSE_BUTTON;
		output->mouse.button = FosterGetMouseFromSDL(event.button.button);
		output->mouse.buttonPressed = event.type == SDL_MOUSEBUTTONDOWN;
	}
	else if (event.type == SDL_MOUSEWHEEL)
	{
		output->eventType = FOSTER_EVENT_TYPE_MOUSE_WHEEL;
		output->mouse.x = (float)event.wheel.x;
		output->mouse.y = (float)event.wheel.y;
	}
	// Keyboard
	else if (event.type == SDL_KEYDOWN || event.type == SDL_KEYUP)
	{
		if (event.key.repeat != 0)
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_KEYBOARD_KEY;
		output->keyboard.key = FosterGetKeyFromSDL(event.key.keysym.scancode);
		output->keyboard.keyPressed = event.type == SDL_KEYDOWN;
	}
	else if (event.type == SDL_TEXTINPUT)
	{
		output->eventType = FOSTER_EVENT_TYPE_KEYBOARD_INPUT;
		for (int i = 0; i < SDL_TEXTINPUTEVENT_TEXT_SIZE; i ++)
			output->keyboard.text[i] = event.text.text[i];
	}
	// Joystick Controller
	else if (event.type == SDL_JOYDEVICEADDED)
	{
		int index = event.jdevice.which;

		if (SDL_IsGameController(index) || index < 0 || index >= FOSTER_MAX_CONTROLLERS)
			goto NEXT_EVENT;

		SDL_Joystick* ptr = fstate.joysticks[index] = SDL_JoystickOpen(index);

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_CONNECT;
		output->controller.index = index;
		output->controller.name = SDL_JoystickName(ptr);
		output->controller.isGamepad = 0;
		output->controller.gamepadType = FOSTER_GAMEPAD_TYPE_UNKNOWN;
		output->controller.buttonCount = SDL_JoystickNumButtons(ptr);
		output->controller.axisCount = SDL_JoystickNumAxes(ptr);
		output->controller.vendor = SDL_JoystickGetVendor(ptr);
		output->controller.product = SDL_JoystickGetProduct(ptr);
		output->controller.version = SDL_JoystickGetProductVersion(ptr);
	}
	else if (event.type == SDL_JOYDEVICEREMOVED)
	{
		int index = FosterFindJoystickIndexSDL(fstate.joysticks, event.jdevice.which);
		if (index < 0 || SDL_IsGameController(index))
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_DISCONNECT;
		output->controller.index = index;
		SDL_JoystickClose(fstate.joysticks[index]);
	}
	else if (event.type == SDL_JOYBUTTONDOWN || event.type == SDL_JOYBUTTONUP)
	{
		int index = FosterFindJoystickIndexSDL(fstate.joysticks, event.jdevice.which);
		if (index < 0 || SDL_IsGameController(index))
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_BUTTON;
		output->controller.index = index;
		output->controller.button = event.jbutton.button;
		output->controller.buttonPressed = event.type == SDL_JOYBUTTONDOWN;
	}
	else if (event.type == SDL_JOYAXISMOTION)
	{
		int index = FosterFindJoystickIndexSDL(fstate.joysticks, event.jdevice.which);
		if (index < 0 || SDL_IsGameController(index))
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_AXIS;
		output->controller.index = index;
		output->controller.axis = event.jaxis.axis;
		if (event.jaxis.value >= 0)
			output->controller.axisValue = event.jaxis.value / 32767.0f;
		else
			output->controller.axisValue = event.jaxis.value / 32768.0f;
	}
	// Gamepad Controller
	else if (event.type == SDL_CONTROLLERDEVICEADDED)
	{
		int index = event.cdevice.which;
		if (index < 0 || index >= FOSTER_MAX_CONTROLLERS)
			goto NEXT_EVENT;

		SDL_GameController* ptr = fstate.gamepads[index] = SDL_GameControllerOpen(index);
		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_CONNECT;
		output->controller.index = index;
		output->controller.name = SDL_GameControllerName(ptr);
		output->controller.buttonCount = 15;
		output->controller.axisCount = 6;
		output->controller.isGamepad = 1;
		output->controller.gamepadType = (FosterGamepadTypes)SDL_GameControllerGetType(ptr);
		output->controller.vendor = SDL_GameControllerGetVendor(ptr);
		output->controller.product = SDL_GameControllerGetProduct(ptr);
		output->controller.version = SDL_GameControllerGetProductVersion(ptr);
	}
	else if (event.type == SDL_CONTROLLERDEVICEREMOVED)
	{
		int index = FosterFindGamepadIndexSDL(fstate.gamepads, event.cdevice.which);
		if (index < 0)
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_DISCONNECT;
		output->controller.index = index;
		SDL_GameControllerClose(fstate.gamepads[index]);
	}
	else if (event.type == SDL_CONTROLLERBUTTONDOWN || event.type == SDL_CONTROLLERBUTTONUP)
	{
		int index = FosterFindGamepadIndexSDL(fstate.gamepads, event.cdevice.which);
		if (index < 0)
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_BUTTON;
		output->controller.index = index;
		output->controller.button = FOSTER_BUTTON_NONE;
		if (event.cbutton.button >= 0 && event.cbutton.button < 15)
			output->controller.button = FosterGetButtonFromSDL(event.cbutton.button);
		output->controller.buttonPressed = event.type == SDL_CONTROLLERBUTTONDOWN;
	}
	else if (event.type == SDL_CONTROLLERAXISMOTION)
	{
		int index = FosterFindGamepadIndexSDL(fstate.gamepads, event.cdevice.which);
		if (index < 0)
			goto NEXT_EVENT;

		output->eventType = FOSTER_EVENT_TYPE_CONTROLLER_AXIS;
		output->controller.index = index;
		output->controller.axis = FOSTER_AXIS_NONE;
		if (event.caxis.axis >= 0 && event.caxis.axis < 6)
			output->controller.axis  = FosterGetAxisFromSDL(event.caxis.axis);
		if (event.jaxis.value >= 0)
			output->controller.axisValue = event.jaxis.value / 32767.0f;
		else
			output->controller.axisValue = event.jaxis.value / 32768.0f;
	}
	else
	{
		goto NEXT_EVENT;
	}

	return 1;
}

void FosterEndFrame()
{
	FOSTER_ASSERT_RUNNING(FosterEndFrame);

	if (fstate.device.frameEnd)
		fstate.device.frameEnd();
}

void FosterShutdown()
{
	if (!fstate.running)
		return;
	if (fstate.device.shutdown)
		fstate.device.shutdown();
	if (fstate.clipboardText != NULL)
		SDL_free(fstate.clipboardText);
	if (fstate.userPath != NULL)
		SDL_free(fstate.userPath);
	fstate.clipboardText = NULL;
	fstate.running = false;
	SDL_DestroyWindow(fstate.window);
	SDL_Quit();
}

FosterBool FosterIsRunning()
{
	return fstate.running;
}

void FosterSetTitle(const char* title)
{
	FOSTER_ASSERT_RUNNING(FosterSetTitle);
	SDL_SetWindowTitle(fstate.window, title);
}

void FosterSetSize(int width, int height)
{
	FOSTER_ASSERT_RUNNING(FosterSetSize);
	SDL_SetWindowSize(fstate.window, width, height);
}

void FosterGetSize(int* width, int* height)
{
	FOSTER_ASSERT_RUNNING(FosterGetSize);
	SDL_GetWindowSize(fstate.window, width, height);
}

void FosterGetSizeInPixels(int* width, int* height)
{
	FOSTER_ASSERT_RUNNING(FosterGetSizeInPixels);
	SDL_GetWindowSizeInPixels(fstate.window, width, height);
}

void FosterGetDisplaySize(int* width, int* height)
{
	FOSTER_ASSERT_RUNNING(FosterGetDisplaySize);

	int index = SDL_GetWindowDisplayIndex(fstate.window);

	SDL_DisplayMode mode;
	SDL_GetCurrentDisplayMode(index, &mode);

	*width  = mode.w;
	*height = mode.h;
}

void FosterSetFlags(FosterFlags flags)
{
	FOSTER_ASSERT_RUNNING(FosterSetFlags);

	if (flags != fstate.flags)
	{
		// fullscreen
		SDL_SetWindowFullscreen(fstate.window,
			FOSTER_CHECK(flags, FOSTER_FLAG_FULLSCREEN) ? SDL_WINDOW_FULLSCREEN_DESKTOP : 0);

		// resizable
		SDL_SetWindowResizable(fstate.window,
			FOSTER_CHECK(flags, FOSTER_FLAG_RESIZABLE) ? SDL_TRUE : SDL_FALSE);

		// mouse visible
		SDL_ShowCursor(FOSTER_CHECK(flags, FOSTER_FLAG_MOUSE_VISIBLE) ? SDL_ENABLE : SDL_DISABLE);

		// vsync
		if (fstate.device.renderer == FOSTER_RENDERER_OPENGL)
		{
			int result = SDL_GL_SetSwapInterval(FOSTER_CHECK(flags, FOSTER_FLAG_VSYNC) ? 1 : 0);
			if (result != 0)
				FOSTER_LOG_WARN("Setting V-Sync Failed: %s", SDL_GetError());
		}

		fstate.flags = flags;
	}
}

void FosterSetCentered()
{
	FOSTER_ASSERT_RUNNING(FosterSetCentered);
	SDL_SetWindowPosition(fstate.window, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
}

const char* FosterGetUserPath()
{
	FOSTER_ASSERT_RUNNING_RET(FosterGetUserPath, NULL);

	if (fstate.userPath == NULL)
		fstate.userPath = SDL_GetPrefPath(NULL, fstate.desc.applicationName);

	return fstate.userPath;
}

void FosterSetClipboard(const char* cstr)
{
	FOSTER_ASSERT_RUNNING(FosterSetClipboard);
	SDL_SetClipboardText(cstr);
}

const char* FosterGetClipboard()
{
	FOSTER_ASSERT_RUNNING_RET(FosterGetClipboard, NULL);

	// free previous clipboard text
	if (fstate.clipboardText != NULL)
	{
		SDL_free(fstate.clipboardText);
		fstate.clipboardText = NULL;
	}

	fstate.clipboardText = SDL_GetClipboardText();
	return fstate.clipboardText;
}

FosterBool FosterGetFocused()
{
	FOSTER_ASSERT_RUNNING_RET(FosterGetClipboard, false);
	Uint32 flags = SDL_GetWindowFlags(fstate.window);
	return (flags & (SDL_WINDOW_INPUT_FOCUS | SDL_WINDOW_MOUSE_FOCUS)) != 0;
}

FosterFont* FosterFontInit(unsigned char* data, int length)
{
	if (stbtt_GetNumberOfFonts(data) <= 0)
	{
		FOSTER_LOG_ERROR("Unable to parse Font File");
		return NULL;
	}

	stbtt_fontinfo* info = (stbtt_fontinfo*)SDL_malloc(sizeof(stbtt_fontinfo));

	if (stbtt_InitFont(info, data, 0) == 0)
	{
		FOSTER_LOG_ERROR("Unable to parse Font File");
		SDL_free(info);
		return NULL;
	}

	return (FosterFont*)info;
}

void FosterFontGetMetrics(FosterFont* font, int* ascent, int* descent, int* linegap)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;
	stbtt_GetFontVMetrics(info, ascent, descent, linegap);
}

int FosterFontGetGlyphIndex(FosterFont* font, int codepoint)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;
	return stbtt_FindGlyphIndex(info, codepoint);
}

float FosterFontGetScale(FosterFont* font, float size)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;
	return stbtt_ScaleForMappingEmToPixels(info, size);
}

float FosterFontGetKerning(FosterFont* font, int glyph1, int glyph2, float scale)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;
	return stbtt_GetGlyphKernAdvance(info, glyph1, glyph2) * scale;
}

void FosterFontGetCharacter(FosterFont* font, int glyph, float scale, int* width, int* height, float* advance, float* offsetX, float* offsetY, int* visible)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;

	int adv, ox, x0, y0, x1, y1;

	stbtt_GetGlyphHMetrics(info, glyph, &adv, &ox);
	stbtt_GetGlyphBitmapBox(info, glyph, scale, scale, &x0, &y0, &x1, &y1);

	*width = (x1 - x0);
	*height = (y1 - y0);
	*advance = adv * scale;
	*offsetX = ox * scale;
	*offsetY = (float)y0;
	*visible = *width > 0 && *height > 0 && stbtt_IsGlyphEmpty(info, glyph) == 0;
}

void FosterFontGetPixels(FosterFont* font, unsigned char* dest, int glyph, int width, int height, float scale)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;

	// parse it directly into the dest buffer
	stbtt_MakeGlyphBitmap(info, dest, width, height, width, scale, scale, glyph);

	// convert the buffer to RGBA data by working backwards, overwriting data
	int len = width * height;
	for (int a = (len - 1) * 4, b = (len - 1); b >= 0; a -= 4, b -= 1)
	{
		dest[a + 0] = dest[b];
		dest[a + 1] = dest[b];
		dest[a + 2] = dest[b];
		dest[a + 3] = dest[b];
	}
}

void FosterFontFree(FosterFont* font)
{
	stbtt_fontinfo* info = (stbtt_fontinfo*)font;
	SDL_free(info);
}

FosterRenderers FosterGetRenderer()
{
	FOSTER_ASSERT_RUNNING_RET(FosterGetRenderer, FOSTER_RENDERER_NONE);
	return fstate.device.renderer;
}

FosterTexture* FosterTextureCreate(int width, int height, FosterTextureFormat format)
{
	FOSTER_ASSERT_RUNNING_RET(FosterTextureCreate, NULL);
	return fstate.device.textureCreate(width, height, format);
}

void FosterTextureSetData(FosterTexture* texture, void* data, int length)
{
	FOSTER_ASSERT_RUNNING(FosterTextureSetData);
	fstate.device.textureSetData(texture, data, length);
}

void FosterTextureGetData(FosterTexture* texture, void* data, int length)
{
	FOSTER_ASSERT_RUNNING(FosterTextureGetData);
	fstate.device.textureGetData(texture, data, length);
}

void FosterTextureDestroy(FosterTexture* texture)
{
	FOSTER_ASSERT_RUNNING(FosterTextureDestroy);
	fstate.device.textureDestroy(texture);
}

FosterTarget* FosterTargetCreate(int width, int height, FosterTextureFormat* attachments, int attachmentCount)
{
	FOSTER_ASSERT_RUNNING_RET(FosterTargetCreate, NULL);
	return fstate.device.targetCreate(width, height, attachments, attachmentCount);
}

FosterTexture* FosterTargetGetAttachment(FosterTarget* target, int index)
{
	FOSTER_ASSERT_RUNNING_RET(FosterTargetGetAttachment, NULL);

	if (index < 0 || index >= FOSTER_MAX_TARGET_ATTACHMENTS)
		return NULL;

	return fstate.device.targetGetAttachment(target, index);
}

void FosterTargetDestroy(FosterTarget* target)
{
	FOSTER_ASSERT_RUNNING(FosterTargetDestroy);
	fstate.device.targetDestroy(target);
}

FosterShader* FosterShaderCreate(FosterShaderData* data)
{
	FOSTER_ASSERT_RUNNING_RET(FosterShaderCreate, NULL);
	return fstate.device.shaderCreate(data);
}

void FosterShaderGetUniforms(FosterShader* shader, FosterUniformInfo* output, int* count, int max)
{
	FOSTER_ASSERT_RUNNING(FosterShaderGetUniforms);
	fstate.device.shaderGetUniforms(shader, output, count, max);
}

void FosterShaderSetUniform(FosterShader* shader, int index, float* values)
{
	FOSTER_ASSERT_RUNNING(FosterShaderSetUniform);
	fstate.device.shaderSetUniform(shader, index, values);
}

void FosterShaderSetTexture(FosterShader* shader, int index, FosterTexture** values)
{
	FOSTER_ASSERT_RUNNING(FosterShaderSetTexture);
	fstate.device.shaderSetTexture(shader, index, values);
}

void FosterShaderSetSampler(FosterShader* shader, int index, FosterTextureSampler* values)
{
	FOSTER_ASSERT_RUNNING(FosterShaderSetSampler);
	fstate.device.shaderSetSampler(shader, index, values);
}

void FosterShaderDestroy(FosterShader* shader)
{
	FOSTER_ASSERT_RUNNING(FosterShaderDestroy);
	fstate.device.shaderDestroy(shader);
}

FosterMesh* FosterMeshCreate()
{
	FOSTER_ASSERT_RUNNING_RET(FosterMeshCreate, NULL);
	return fstate.device.meshCreate();
}

void FosterMeshSetVertexFormat(FosterMesh* mesh, FosterVertexFormat* format)
{
	FOSTER_ASSERT_RUNNING(FosterMeshSetVertexFormat);
	fstate.device.meshSetVertexFormat(mesh, format);
}

void FosterMeshSetVertexData(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset)
{
	FOSTER_ASSERT_RUNNING(FosterMeshSetVertexData);
	fstate.device.meshSetVertexData(mesh, data, dataSize, dataDestOffset);
}

void FosterMeshSetIndexFormat(FosterMesh* mesh, FosterIndexFormat format)
{
	FOSTER_ASSERT_RUNNING(FosterMeshSetIndexFormat);
	fstate.device.meshSetIndexFormat(mesh, format);
}

void FosterMeshSetIndexData(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset)
{
	FOSTER_ASSERT_RUNNING(FosterMeshSetIndexData);
	fstate.device.meshSetIndexData(mesh, data, dataSize, dataDestOffset);
}

void FosterMeshDestroy(FosterMesh* mesh)
{
	FOSTER_ASSERT_RUNNING(FosterMeshDestroy);
	fstate.device.meshDestroy(mesh);
}

void FosterDraw(FosterDrawCommand* command)
{
	FOSTER_ASSERT_RUNNING(FosterDraw);
	fstate.device.draw(command);
}

void FosterClear(FosterClearCommand* clear)
{
	FOSTER_ASSERT_RUNNING(FosterClear);
	fstate.device.clear(clear);
}

void FosterLog(FosterLogLevel level, const char* fmt, ...)
{
	if (fstate.logFilter == FOSTER_LOG_FILTER_IGNORE_ALL ||
		fstate.logFn == NULL)
		return;

	char msg[FOSTER_MAX_MESSAGE_SIZE];
	va_list ap;
	va_start(ap, fmt);
	SDL_vsnprintf(msg, sizeof(msg), fmt, ap);
	va_end(ap);

	fstate.logFn(msg, level);
}

int FosterFindJoystickIndexSDL(SDL_Joystick** joysticks, SDL_JoystickID instanceID)
{
	for (int i = 0; i < FOSTER_MAX_CONTROLLERS; i++)
		if (joysticks[i] != NULL && SDL_JoystickInstanceID(joysticks[i]) == instanceID)
			return i;
	return -1;
}

int FosterFindGamepadIndexSDL(SDL_GameController** gamepads, SDL_JoystickID instanceID)
{
	for (int i = 0; i < FOSTER_MAX_CONTROLLERS; i++)
	{
		if (gamepads[i] != NULL)
		{
			SDL_Joystick* joystick = SDL_GameControllerGetJoystick(gamepads[i]);
			if (SDL_JoystickInstanceID(joystick) == instanceID)
				return i;
		}
	}
	return -1;
}

FosterKeys FosterGetKeyFromSDL(SDL_Scancode key)
{
	switch (key)
	{
	case SDL_SCANCODE_UNKNOWN: return FOSTER_KEY_UNKNOWN;
	case SDL_SCANCODE_A: return FOSTER_KEY_A;
	case SDL_SCANCODE_B: return FOSTER_KEY_B;
	case SDL_SCANCODE_C: return FOSTER_KEY_C;
	case SDL_SCANCODE_D: return FOSTER_KEY_D;
	case SDL_SCANCODE_E: return FOSTER_KEY_E;
	case SDL_SCANCODE_F: return FOSTER_KEY_F;
	case SDL_SCANCODE_G: return FOSTER_KEY_G;
	case SDL_SCANCODE_H: return FOSTER_KEY_H;
	case SDL_SCANCODE_I: return FOSTER_KEY_I;
	case SDL_SCANCODE_J: return FOSTER_KEY_J;
	case SDL_SCANCODE_K: return FOSTER_KEY_K;
	case SDL_SCANCODE_L: return FOSTER_KEY_L;
	case SDL_SCANCODE_M: return FOSTER_KEY_M;
	case SDL_SCANCODE_N: return FOSTER_KEY_N;
	case SDL_SCANCODE_O: return FOSTER_KEY_O;
	case SDL_SCANCODE_P: return FOSTER_KEY_P;
	case SDL_SCANCODE_Q: return FOSTER_KEY_Q;
	case SDL_SCANCODE_R: return FOSTER_KEY_R;
	case SDL_SCANCODE_S: return FOSTER_KEY_S;
	case SDL_SCANCODE_T: return FOSTER_KEY_T;
	case SDL_SCANCODE_U: return FOSTER_KEY_U;
	case SDL_SCANCODE_V: return FOSTER_KEY_V;
	case SDL_SCANCODE_W: return FOSTER_KEY_W;
	case SDL_SCANCODE_X: return FOSTER_KEY_X;
	case SDL_SCANCODE_Y: return FOSTER_KEY_Y;
	case SDL_SCANCODE_Z: return FOSTER_KEY_Z;
	case SDL_SCANCODE_1: return FOSTER_KEY_D1;
	case SDL_SCANCODE_2: return FOSTER_KEY_D2;
	case SDL_SCANCODE_3: return FOSTER_KEY_D3;
	case SDL_SCANCODE_4: return FOSTER_KEY_D4;
	case SDL_SCANCODE_5: return FOSTER_KEY_D5;
	case SDL_SCANCODE_6: return FOSTER_KEY_D6;
	case SDL_SCANCODE_7: return FOSTER_KEY_D7;
	case SDL_SCANCODE_8: return FOSTER_KEY_D8;
	case SDL_SCANCODE_9: return FOSTER_KEY_D9;
	case SDL_SCANCODE_0: return FOSTER_KEY_D0;
	case SDL_SCANCODE_RETURN: return FOSTER_KEY_ENTER;
	case SDL_SCANCODE_ESCAPE: return FOSTER_KEY_ESCAPE;
	case SDL_SCANCODE_BACKSPACE: return FOSTER_KEY_BACKSPACE;
	case SDL_SCANCODE_TAB: return FOSTER_KEY_TAB;
	case SDL_SCANCODE_SPACE: return FOSTER_KEY_SPACE;
	case SDL_SCANCODE_MINUS: return FOSTER_KEY_MINUS;
	case SDL_SCANCODE_EQUALS: return FOSTER_KEY_EQUALS;
	case SDL_SCANCODE_LEFTBRACKET: return FOSTER_KEY_LEFTBRACKET;
	case SDL_SCANCODE_RIGHTBRACKET: return FOSTER_KEY_RIGHTBRACKET;
	case SDL_SCANCODE_BACKSLASH: return FOSTER_KEY_BACKSLASH;
	case SDL_SCANCODE_SEMICOLON: return FOSTER_KEY_SEMICOLON;
	case SDL_SCANCODE_APOSTROPHE: return FOSTER_KEY_APOSTROPHE;
	case SDL_SCANCODE_GRAVE: return FOSTER_KEY_TILDE;
	case SDL_SCANCODE_COMMA: return FOSTER_KEY_COMMA;
	case SDL_SCANCODE_PERIOD: return FOSTER_KEY_PERIOD;
	case SDL_SCANCODE_SLASH: return FOSTER_KEY_SLASH;
	case SDL_SCANCODE_CAPSLOCK: return FOSTER_KEY_CAPSLOCK;
	case SDL_SCANCODE_F1: return FOSTER_KEY_F1;
	case SDL_SCANCODE_F2: return FOSTER_KEY_F2;
	case SDL_SCANCODE_F3: return FOSTER_KEY_F3;
	case SDL_SCANCODE_F4: return FOSTER_KEY_F4;
	case SDL_SCANCODE_F5: return FOSTER_KEY_F5;
	case SDL_SCANCODE_F6: return FOSTER_KEY_F6;
	case SDL_SCANCODE_F7: return FOSTER_KEY_F7;
	case SDL_SCANCODE_F8: return FOSTER_KEY_F8;
	case SDL_SCANCODE_F9: return FOSTER_KEY_F9;
	case SDL_SCANCODE_F10: return FOSTER_KEY_F10;
	case SDL_SCANCODE_F11: return FOSTER_KEY_F11;
	case SDL_SCANCODE_F12: return FOSTER_KEY_F12;
	case SDL_SCANCODE_PRINTSCREEN: return FOSTER_KEY_PRINTSCREEN;
	case SDL_SCANCODE_SCROLLLOCK: return FOSTER_KEY_SCROLLLOCK;
	case SDL_SCANCODE_PAUSE: return FOSTER_KEY_PAUSE;
	case SDL_SCANCODE_INSERT: return FOSTER_KEY_INSERT;
	case SDL_SCANCODE_HOME: return FOSTER_KEY_HOME;
	case SDL_SCANCODE_PAGEUP: return FOSTER_KEY_PAGEUP;
	case SDL_SCANCODE_DELETE: return FOSTER_KEY_DELETE;
	case SDL_SCANCODE_END: return FOSTER_KEY_END;
	case SDL_SCANCODE_PAGEDOWN: return FOSTER_KEY_PAGEDOWN;
	case SDL_SCANCODE_RIGHT: return FOSTER_KEY_RIGHT;
	case SDL_SCANCODE_LEFT: return FOSTER_KEY_LEFT;
	case SDL_SCANCODE_DOWN: return FOSTER_KEY_DOWN;
	case SDL_SCANCODE_UP: return FOSTER_KEY_UP;
	case SDL_SCANCODE_KP_DIVIDE: return FOSTER_KEY_KEYPAD_DIVIDE;
	case SDL_SCANCODE_KP_MULTIPLY: return FOSTER_KEY_KEYPAD_MULTIPLY;
	case SDL_SCANCODE_KP_MINUS: return FOSTER_KEY_KEYPAD_MINUS;
	case SDL_SCANCODE_KP_PLUS: return FOSTER_KEY_KEYPAD_PLUS;
	case SDL_SCANCODE_KP_ENTER: return FOSTER_KEY_KEYPAD_ENTER;
	case SDL_SCANCODE_KP_1: return FOSTER_KEY_KEYPAD_1;
	case SDL_SCANCODE_KP_2: return FOSTER_KEY_KEYPAD_2;
	case SDL_SCANCODE_KP_3: return FOSTER_KEY_KEYPAD_3;
	case SDL_SCANCODE_KP_4: return FOSTER_KEY_KEYPAD_4;
	case SDL_SCANCODE_KP_5: return FOSTER_KEY_KEYPAD_5;
	case SDL_SCANCODE_KP_6: return FOSTER_KEY_KEYPAD_6;
	case SDL_SCANCODE_KP_7: return FOSTER_KEY_KEYPAD_7;
	case SDL_SCANCODE_KP_8: return FOSTER_KEY_KEYPAD_8;
	case SDL_SCANCODE_KP_9: return FOSTER_KEY_KEYPAD_9;
	case SDL_SCANCODE_KP_0: return FOSTER_KEY_KEYPAD_0;
	case SDL_SCANCODE_APPLICATION: return FOSTER_KEY_APPLICATION;
	case SDL_SCANCODE_KP_EQUALS: return FOSTER_KEY_KEYPAD_EQUALS;
	case SDL_SCANCODE_F13: return FOSTER_KEY_F13;
	case SDL_SCANCODE_F14: return FOSTER_KEY_F14;
	case SDL_SCANCODE_F15: return FOSTER_KEY_F15;
	case SDL_SCANCODE_F16: return FOSTER_KEY_F16;
	case SDL_SCANCODE_F17: return FOSTER_KEY_F17;
	case SDL_SCANCODE_F18: return FOSTER_KEY_F18;
	case SDL_SCANCODE_F19: return FOSTER_KEY_F19;
	case SDL_SCANCODE_F20: return FOSTER_KEY_F20;
	case SDL_SCANCODE_F21: return FOSTER_KEY_F21;
	case SDL_SCANCODE_F22: return FOSTER_KEY_F22;
	case SDL_SCANCODE_F23: return FOSTER_KEY_F23;
	case SDL_SCANCODE_F24: return FOSTER_KEY_F24;
	case SDL_SCANCODE_EXECUTE: return FOSTER_KEY_EXECUTE;
	case SDL_SCANCODE_HELP: return FOSTER_KEY_HELP;
	case SDL_SCANCODE_MENU: return FOSTER_KEY_MENU;
	case SDL_SCANCODE_SELECT: return FOSTER_KEY_SELECT;
	case SDL_SCANCODE_STOP: return FOSTER_KEY_STOP;
	case SDL_SCANCODE_UNDO: return FOSTER_KEY_UNDO;
	case SDL_SCANCODE_CUT: return FOSTER_KEY_CUT;
	case SDL_SCANCODE_COPY: return FOSTER_KEY_COPY;
	case SDL_SCANCODE_PASTE: return FOSTER_KEY_PASTE;
	case SDL_SCANCODE_FIND: return FOSTER_KEY_FIND;
	case SDL_SCANCODE_MUTE: return FOSTER_KEY_MUTE;
	case SDL_SCANCODE_VOLUMEUP: return FOSTER_KEY_VOLUMEUP;
	case SDL_SCANCODE_VOLUMEDOWN: return FOSTER_KEY_VOLUMEDOWN;
	case SDL_SCANCODE_KP_COMMA: return FOSTER_KEY_KEYPAD_COMMA;
	case SDL_SCANCODE_ALTERASE: return FOSTER_KEY_ALTERASE;
	case SDL_SCANCODE_SYSREQ: return FOSTER_KEY_SYSREQ;
	case SDL_SCANCODE_CANCEL: return FOSTER_KEY_CANCEL;
	case SDL_SCANCODE_CLEAR: return FOSTER_KEY_CLEAR;
	case SDL_SCANCODE_PRIOR: return FOSTER_KEY_PRIOR;
	case SDL_SCANCODE_RETURN2: return FOSTER_KEY_ENTER2;
	case SDL_SCANCODE_SEPARATOR: return FOSTER_KEY_SEPARATOR;
	case SDL_SCANCODE_OUT: return FOSTER_KEY_OUT;
	case SDL_SCANCODE_OPER: return FOSTER_KEY_OPER;
	case SDL_SCANCODE_CLEARAGAIN: return FOSTER_KEY_CLEARAGAIN;
	case SDL_SCANCODE_KP_00: return FOSTER_KEY_KEYPAD_00;
	case SDL_SCANCODE_KP_000: return FOSTER_KEY_KEYPAD_000;
	case SDL_SCANCODE_KP_LEFTPAREN: return FOSTER_KEY_KEYPAD_LEFT_PAREN;
	case SDL_SCANCODE_KP_RIGHTPAREN: return FOSTER_KEY_KEYPAD_RIGHT_PAREN;
	case SDL_SCANCODE_KP_LEFTBRACE: return FOSTER_KEY_KEYPAD_LEFT_BRACE;
	case SDL_SCANCODE_KP_RIGHTBRACE: return FOSTER_KEY_KEYPAD_RIGHT_BRACE;
	case SDL_SCANCODE_KP_TAB: return FOSTER_KEY_KEYPAD_TAB;
	case SDL_SCANCODE_KP_BACKSPACE: return FOSTER_KEY_KEYPAD_BACKSPACE;
	case SDL_SCANCODE_KP_A: return FOSTER_KEY_KEYPAD_A;
	case SDL_SCANCODE_KP_B: return FOSTER_KEY_KEYPAD_B;
	case SDL_SCANCODE_KP_C: return FOSTER_KEY_KEYPAD_C;
	case SDL_SCANCODE_KP_D: return FOSTER_KEY_KEYPAD_D;
	case SDL_SCANCODE_KP_E: return FOSTER_KEY_KEYPAD_E;
	case SDL_SCANCODE_KP_F: return FOSTER_KEY_KEYPAD_F;
	case SDL_SCANCODE_KP_XOR: return FOSTER_KEY_KEYPAD_XOR;
	case SDL_SCANCODE_KP_POWER: return FOSTER_KEY_KEYPAD_POWER;
	case SDL_SCANCODE_KP_PERCENT: return FOSTER_KEY_KEYPAD_PERCENT;
	case SDL_SCANCODE_KP_LESS: return FOSTER_KEY_KEYPAD_LESS;
	case SDL_SCANCODE_KP_GREATER: return FOSTER_KEY_KEYPAD_GREATER;
	case SDL_SCANCODE_KP_AMPERSAND: return FOSTER_KEY_KEYPAD_AMPERSAND;
	case SDL_SCANCODE_KP_COLON: return FOSTER_KEY_KEYPAD_COLON;
	case SDL_SCANCODE_KP_HASH: return FOSTER_KEY_KEYPAD_HASH;
	case SDL_SCANCODE_KP_SPACE: return FOSTER_KEY_KEYPAD_SPACE;
	case SDL_SCANCODE_KP_CLEAR: return FOSTER_KEY_KEYPAD_CLEAR;
	case SDL_SCANCODE_LCTRL: return FOSTER_KEY_LEFT_CONTROL;
	case SDL_SCANCODE_LSHIFT: return FOSTER_KEY_LEFT_SHIFT;
	case SDL_SCANCODE_LALT: return FOSTER_KEY_LEFT_ALT;
	case SDL_SCANCODE_LGUI: return FOSTER_KEY_LEFT_OS;
	case SDL_SCANCODE_RCTRL: return FOSTER_KEY_RIGHT_CONTROL;
	case SDL_SCANCODE_RSHIFT: return FOSTER_KEY_RIGHT_SHIFT;
	case SDL_SCANCODE_RALT: return FOSTER_KEY_RIGHT_ALT;
	case SDL_SCANCODE_RGUI: return FOSTER_KEY_RIGHT_OS;
	}

	return FOSTER_KEY_UNKNOWN;
}

FosterButtons FosterGetButtonFromSDL(SDL_GameControllerButton button)
{
	switch (button)
	{
	case SDL_CONTROLLER_BUTTON_INVALID: return FOSTER_BUTTON_NONE;
	case SDL_CONTROLLER_BUTTON_A: return FOSTER_BUTTON_SOUTH;
	case SDL_CONTROLLER_BUTTON_B: return FOSTER_BUTTON_EAST;
	case SDL_CONTROLLER_BUTTON_X: return FOSTER_BUTTON_WEST;
	case SDL_CONTROLLER_BUTTON_Y: return FOSTER_BUTTON_NORTH;
	case SDL_CONTROLLER_BUTTON_BACK: return FOSTER_BUTTON_BACK;
	case SDL_CONTROLLER_BUTTON_GUIDE: return FOSTER_BUTTON_SELECT;
	case SDL_CONTROLLER_BUTTON_START: return FOSTER_BUTTON_START;
	case SDL_CONTROLLER_BUTTON_LEFTSTICK: return FOSTER_BUTTON_LEFTSTICK;
	case SDL_CONTROLLER_BUTTON_RIGHTSTICK: return FOSTER_BUTTON_RIGHTSTICK;
	case SDL_CONTROLLER_BUTTON_LEFTSHOULDER: return FOSTER_BUTTON_LEFTSHOULDER;
	case SDL_CONTROLLER_BUTTON_RIGHTSHOULDER: return FOSTER_BUTTON_RIGHTSHOULDER;
	case SDL_CONTROLLER_BUTTON_DPAD_UP: return FOSTER_BUTTON_UP;
	case SDL_CONTROLLER_BUTTON_DPAD_DOWN: return FOSTER_BUTTON_DOWN;
	case SDL_CONTROLLER_BUTTON_DPAD_LEFT: return FOSTER_BUTTON_LEFT;
	case SDL_CONTROLLER_BUTTON_DPAD_RIGHT: return FOSTER_BUTTON_RIGHT;
	// case SDL_CONTROLLER_BUTTON_MISC1: return FOSTER_BUTTON_MISC1;
	// case SDL_CONTROLLER_BUTTON_PADDLE1: return FOSTER_BUTTON_PADDLE1;
	// case SDL_CONTROLLER_BUTTON_PADDLE2: return FOSTER_BUTTON_PADDLE2;
	// case SDL_CONTROLLER_BUTTON_PADDLE3: return FOSTER_BUTTON_PADDLE3;
	// case SDL_CONTROLLER_BUTTON_PADDLE4: return FOSTER_BUTTON_PADDLE4;
	// case SDL_CONTROLLER_BUTTON_TOUCHPAD: return FOSTER_BUTTON_TOUCHPAD;
	}

	return FOSTER_BUTTON_NONE;
}

FosterMouse FosterGetMouseFromSDL(uint8_t button)
{
	switch (button)
	{
	case SDL_BUTTON_LEFT: return FOSTER_MOUSE_LEFT;
	case SDL_BUTTON_RIGHT: return FOSTER_MOUSE_RIGHT;
	case SDL_BUTTON_MIDDLE: return FOSTER_MOUSE_MIDDLE;
	}

	return FOSTER_MOUSE_NONE;
}

FosterAxis FosterGetAxisFromSDL(int axis)
{
	switch (axis)
	{
	case SDL_CONTROLLER_AXIS_INVALID: return FOSTER_AXIS_NONE;
	case SDL_CONTROLLER_AXIS_LEFTX: return FOSTER_AXIS_LEFT_X;
	case SDL_CONTROLLER_AXIS_LEFTY: return FOSTER_AXIS_LEFT_Y;
	case SDL_CONTROLLER_AXIS_RIGHTX: return FOSTER_AXIS_RIGHT_X;
	case SDL_CONTROLLER_AXIS_RIGHTY: return FOSTER_AXIS_RIGHT_Y;
	case SDL_CONTROLLER_AXIS_TRIGGERLEFT: return FOSTER_AXIS_LEFT_TRIGGER;
	case SDL_CONTROLLER_AXIS_TRIGGERRIGHT: return FOSTER_AXIS_RIGHT_TRIGGER;
	}

	return FOSTER_AXIS_NONE;
}
