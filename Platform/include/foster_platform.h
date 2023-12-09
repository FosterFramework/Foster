#ifndef FOSTER_H
#define FOSTER_H

#include <stddef.h>
#include <stdint.h>

// Export API
#if defined _WIN32 || defined __CYGWIN__
	#define FOSTER_API __declspec(dllexport)
	#define FOSTER_CALL __cdecl
#elif __GNUC__
	#define FOSTER_API __attribute__((__visibility__("default")))
	#define FOSTER_CALL
#else
	#define FOSTER_API
	#define FOSTER_CALL
#endif

#define FOSTER_MAX_TARGET_ATTACHMENTS 8
#define FOSTER_MAX_VERTEX_FORMAT_ELEMENTS 16
#define FOSTER_MAX_UNIFORM_NAME 64
#define FOSTER_MAX_UNIFORM_TEXTURES 32
#define FOSTER_MAX_CONTROLLERS 32

typedef uint8_t FosterBool;

typedef enum FosterRenderers
{
	FOSTER_RENDERER_NONE,
	FOSTER_RENDERER_D3D11,
	FOSTER_RENDERER_OPENGL,
} FosterRenderers;

typedef enum FosterFlags
{
	FOSTER_FLAG_FULLSCREEN    = 1 << 0,
	FOSTER_FLAG_VSYNC         = 1 << 1,
	FOSTER_FLAG_RESIZABLE     = 1 << 2,
	FOSTER_FLAG_MOUSE_VISIBLE = 1 << 3,
} FosterFlags;

typedef enum FosterKeys
{
	FOSTER_KEY_UNKNOWN = 0,

	FOSTER_KEY_A = 4,
	FOSTER_KEY_B = 5,
	FOSTER_KEY_C = 6,
	FOSTER_KEY_D = 7,
	FOSTER_KEY_E = 8,
	FOSTER_KEY_F = 9,
	FOSTER_KEY_G = 10,
	FOSTER_KEY_H = 11,
	FOSTER_KEY_I = 12,
	FOSTER_KEY_J = 13,
	FOSTER_KEY_K = 14,
	FOSTER_KEY_L = 15,
	FOSTER_KEY_M = 16,
	FOSTER_KEY_N = 17,
	FOSTER_KEY_O = 18,
	FOSTER_KEY_P = 19,
	FOSTER_KEY_Q = 20,
	FOSTER_KEY_R = 21,
	FOSTER_KEY_S = 22,
	FOSTER_KEY_T = 23,
	FOSTER_KEY_U = 24,
	FOSTER_KEY_V = 25,
	FOSTER_KEY_W = 26,
	FOSTER_KEY_X = 27,
	FOSTER_KEY_Y = 28,
	FOSTER_KEY_Z = 29,

	FOSTER_KEY_D1 = 30,
	FOSTER_KEY_D2 = 31,
	FOSTER_KEY_D3 = 32,
	FOSTER_KEY_D4 = 33,
	FOSTER_KEY_D5 = 34,
	FOSTER_KEY_D6 = 35,
	FOSTER_KEY_D7 = 36,
	FOSTER_KEY_D8 = 37,
	FOSTER_KEY_D9 = 38,
	FOSTER_KEY_D0 = 39,

	FOSTER_KEY_ENTER = 40,
	FOSTER_KEY_ESCAPE = 41,
	FOSTER_KEY_BACKSPACE = 42,
	FOSTER_KEY_TAB = 43,
	FOSTER_KEY_SPACE = 44,

	FOSTER_KEY_MINUS = 45,
	FOSTER_KEY_EQUALS = 46,
	FOSTER_KEY_LEFTBRACKET = 47,
	FOSTER_KEY_RIGHTBRACKET = 48,
	FOSTER_KEY_BACKSLASH = 49,
	FOSTER_KEY_SEMICOLON = 51,
	FOSTER_KEY_APOSTROPHE = 52,
	FOSTER_KEY_TILDE = 53,
	FOSTER_KEY_COMMA = 54,
	FOSTER_KEY_PERIOD = 55,
	FOSTER_KEY_SLASH = 56,

	FOSTER_KEY_CAPSLOCK = 57,

	FOSTER_KEY_F1 = 58,
	FOSTER_KEY_F2 = 59,
	FOSTER_KEY_F3 = 60,
	FOSTER_KEY_F4 = 61,
	FOSTER_KEY_F5 = 62,
	FOSTER_KEY_F6 = 63,
	FOSTER_KEY_F7 = 64,
	FOSTER_KEY_F8 = 65,
	FOSTER_KEY_F9 = 66,
	FOSTER_KEY_F10 = 67,
	FOSTER_KEY_F11 = 68,
	FOSTER_KEY_F12 = 69,
	FOSTER_KEY_F13 = 104,
	FOSTER_KEY_F14 = 105,
	FOSTER_KEY_F15 = 106,
	FOSTER_KEY_F16 = 107,
	FOSTER_KEY_F17 = 108,
	FOSTER_KEY_F18 = 109,
	FOSTER_KEY_F19 = 110,
	FOSTER_KEY_F20 = 111,
	FOSTER_KEY_F21 = 112,
	FOSTER_KEY_F22 = 113,
	FOSTER_KEY_F23 = 114,
	FOSTER_KEY_F24 = 115,

	FOSTER_KEY_PRINTSCREEN = 70,
	FOSTER_KEY_SCROLLLOCK = 71,
	FOSTER_KEY_PAUSE = 72,
	FOSTER_KEY_INSERT = 73,
	FOSTER_KEY_HOME = 74,
	FOSTER_KEY_PAGEUP = 75,
	FOSTER_KEY_DELETE = 76,
	FOSTER_KEY_END = 77,
	FOSTER_KEY_PAGEDOWN = 78,
	FOSTER_KEY_RIGHT = 79,
	FOSTER_KEY_LEFT = 80,
	FOSTER_KEY_DOWN = 81,
	FOSTER_KEY_UP = 82,

	FOSTER_KEY_NUMLOCK = 83,

	FOSTER_KEY_APPLICATION = 101,

	FOSTER_KEY_EXECUTE = 116,
	FOSTER_KEY_HELP = 117,
	FOSTER_KEY_MENU = 118,
	FOSTER_KEY_SELECT = 119,
	FOSTER_KEY_STOP = 120,
	FOSTER_KEY_REDO = 121,
	FOSTER_KEY_UNDO = 122,
	FOSTER_KEY_CUT = 123,
	FOSTER_KEY_COPY = 124,
	FOSTER_KEY_PASTE = 125,
	FOSTER_KEY_FIND = 126,
	FOSTER_KEY_MUTE = 127,
	FOSTER_KEY_VOLUMEUP = 128,
	FOSTER_KEY_VOLUMEDOWN = 129,

	FOSTER_KEY_ALTERASE = 153,
	FOSTER_KEY_SYSREQ = 154,
	FOSTER_KEY_CANCEL = 155,
	FOSTER_KEY_CLEAR = 156,
	FOSTER_KEY_PRIOR = 157,
	FOSTER_KEY_ENTER2 = 158,
	FOSTER_KEY_SEPARATOR = 159,
	FOSTER_KEY_OUT = 160,
	FOSTER_KEY_OPER = 161,
	FOSTER_KEY_CLEARAGAIN = 162,

	FOSTER_KEY_KEYPAD_A = 188,
	FOSTER_KEY_KEYPAD_B = 189,
	FOSTER_KEY_KEYPAD_C = 190,
	FOSTER_KEY_KEYPAD_D = 191,
	FOSTER_KEY_KEYPAD_E = 192,
	FOSTER_KEY_KEYPAD_F = 193,
	FOSTER_KEY_KEYPAD_0 = 98,
	FOSTER_KEY_KEYPAD_00 = 176,
	FOSTER_KEY_KEYPAD_000 = 177,
	FOSTER_KEY_KEYPAD_1 = 89,
	FOSTER_KEY_KEYPAD_2 = 90,
	FOSTER_KEY_KEYPAD_3 = 91,
	FOSTER_KEY_KEYPAD_4 = 92,
	FOSTER_KEY_KEYPAD_5 = 93,
	FOSTER_KEY_KEYPAD_6 = 94,
	FOSTER_KEY_KEYPAD_7 = 95,
	FOSTER_KEY_KEYPAD_8 = 96,
	FOSTER_KEY_KEYPAD_9 = 97,
	FOSTER_KEY_KEYPAD_DIVIDE = 84,
	FOSTER_KEY_KEYPAD_MULTIPLY = 85,
	FOSTER_KEY_KEYPAD_MINUS = 86,
	FOSTER_KEY_KEYPAD_PLUS = 87,
	FOSTER_KEY_KEYPAD_ENTER = 88,
	FOSTER_KEY_KEYPAD_PEROID = 99,
	FOSTER_KEY_KEYPAD_EQUALS = 103,
	FOSTER_KEY_KEYPAD_COMMA = 133,
	FOSTER_KEY_KEYPAD_LEFT_PAREN = 182,
	FOSTER_KEY_KEYPAD_RIGHT_PAREN = 183,
	FOSTER_KEY_KEYPAD_LEFT_BRACE = 184,
	FOSTER_KEY_KEYPAD_RIGHT_BRACE = 185,
	FOSTER_KEY_KEYPAD_TAB = 186,
	FOSTER_KEY_KEYPAD_BACKSPACE = 187,
	FOSTER_KEY_KEYPAD_XOR = 194,
	FOSTER_KEY_KEYPAD_POWER = 195,
	FOSTER_KEY_KEYPAD_PERCENT = 196,
	FOSTER_KEY_KEYPAD_LESS = 197,
	FOSTER_KEY_KEYPAD_GREATER = 198,
	FOSTER_KEY_KEYPAD_AMPERSAND = 199,
	FOSTER_KEY_KEYPAD_COLON = 203,
	FOSTER_KEY_KEYPAD_HASH = 204,
	FOSTER_KEY_KEYPAD_SPACE = 205,
	FOSTER_KEY_KEYPAD_CLEAR = 216,

	FOSTER_KEY_LEFT_CONTROL = 224,
	FOSTER_KEY_LEFT_SHIFT = 225,
	FOSTER_KEY_LEFT_ALT = 226,
	FOSTER_KEY_LEFT_OS = 227,
	FOSTER_KEY_RIGHT_CONTROL = 228,
	FOSTER_KEY_RIGHT_SHIFT = 229,
	FOSTER_KEY_RIGHT_ALT = 230,
	FOSTER_KEY_RIGHT_OS = 231,
} FosterKeys;

typedef enum FosterMouse
{
	FOSTER_MOUSE_NONE = 0,
	FOSTER_MOUSE_LEFT = 1,
	FOSTER_MOUSE_MIDDLE = 2,
	FOSTER_MOUSE_RIGHT = 3
} FosterMouse;

typedef enum FosterButtons
{
	FOSTER_BUTTON_NONE = -1,
	FOSTER_BUTTON_A = 0,
	FOSTER_BUTTON_B = 1,
	FOSTER_BUTTON_X = 2,
	FOSTER_BUTTON_Y = 3,
	FOSTER_BUTTON_BACK = 4,
	FOSTER_BUTTON_SELECT = 5,
	FOSTER_BUTTON_START = 6,
	FOSTER_BUTTON_LEFTSTICK = 7,
	FOSTER_BUTTON_RIGHTSTICK = 8,
	FOSTER_BUTTON_LEFTSHOULDER = 9,
	FOSTER_BUTTON_RIGHTSHOULDER = 10,
	FOSTER_BUTTON_UP = 11,
	FOSTER_BUTTON_DOWN = 12,
	FOSTER_BUTTON_LEFT = 13,
	FOSTER_BUTTON_RIGHT = 14
} FosterButtons;

typedef enum FosterAxis
{
	FOSTER_AXIS_NONE = -1,
	FOSTER_AXIS_LEFT_X = 0,
	FOSTER_AXIS_LEFT_Y = 1,
	FOSTER_AXIS_RIGHT_X = 2,
	FOSTER_AXIS_RIGHT_Y = 3,
	FOSTER_AXIS_LEFT_TRIGGER = 4,
	FOSTER_AXIS_RIGHT_TRIGGER = 5
} FosterAxis;

typedef enum FosterCompare
{
	FOSTER_COMPARE_NONE,
	FOSTER_COMPARE_ALWAYS,
	FOSTER_COMPARE_NEVER,
	FOSTER_COMPARE_LESS,
	FOSTER_COMPARE_EQUAL,
	FOSTER_COMPARE_LESS_OR_EQUAL,
	FOSTER_COMPARE_GREATER,
	FOSTER_COMPARE_NOT_EQUAL,
	FOSTER_COMPARE_GREATOR_OR_EQUAL
} FosterCompare;

typedef enum FosterCull
{
	FOSTER_CULL_NONE = 0,
	FOSTER_CULL_FRONT = 1,
	FOSTER_CULL_BACK = 2,
} FosterCull;

typedef enum FosterBlendOp
{
	FOSTER_BLEND_OP_ADD,
	FOSTER_BLEND_OP_SUBTRACT,
	FOSTER_BLEND_OP_REVERSE_SUBTRACT,
	FOSTER_BLEND_OP_MIN,
	FOSTER_BLEND_OP_MAX
} FosterBlendOp;

typedef enum FosterBlendFactor
{
	FOSTER_BLEND_FACTOR_Zero,
	FOSTER_BLEND_FACTOR_One,
	FOSTER_BLEND_FACTOR_SrcColor,
	FOSTER_BLEND_FACTOR_OneMinusSrcColor,
	FOSTER_BLEND_FACTOR_DstColor,
	FOSTER_BLEND_FACTOR_OneMinusDstColor,
	FOSTER_BLEND_FACTOR_SrcAlpha,
	FOSTER_BLEND_FACTOR_OneMinusSrcAlpha,
	FOSTER_BLEND_FACTOR_DstAlpha,
	FOSTER_BLEND_FACTOR_OneMinusDstAlpha,
	FOSTER_BLEND_FACTOR_ConstantColor,
	FOSTER_BLEND_FACTOR_OneMinusConstantColor,
	FOSTER_BLEND_FACTOR_ConstantAlpha,
	FOSTER_BLEND_FACTOR_OneMinusConstantAlpha,
	FOSTER_BLEND_FACTOR_SrcAlphaSaturate,
	FOSTER_BLEND_FACTOR_Src1Color,
	FOSTER_BLEND_FACTOR_OneMinusSrc1Color,
	FOSTER_BLEND_FACTOR_Src1Alpha,
	FOSTER_BLEND_FACTOR_OneMinusSrc1Alpha
} FosterBlendFactor;

typedef enum FosterBlendMask
{
	FOSTER_BLEND_MASK_N = 0,
	FOSTER_BLEND_MASK_R = 1,
	FOSTER_BLEND_MASK_G = 2,
	FOSTER_BLEND_MASK_B = 4,
	FOSTER_BLEND_MASK_A = 8,
} FosterBlendMask;

typedef enum FosterTextureFilter
{
	FOSTER_TEXTURE_FILTER_NEAREST,
	FOSTER_TEXTURE_FILTER_LINEAR
} FosterTextureFilter;

typedef enum FosterTextureWrap
{
	FOSTER_TEXTURE_WRAP_REPEAT,
	FOSTER_TEXTURE_WRAP_MIRRORED_REPEAT,
	FOSTER_TEXTURE_WRAP_CLAMP_TO_EDGE,
	FOSTER_TEXTURE_WRAP_CLAMP_TO_BORDER
} FosterTextureWrap;

typedef enum FosterTextureFormat
{
	FOSTER_TEXTURE_FORMAT_R8G8B8A8,
	FOSTER_TEXTURE_FORMAT_R8,
	FOSTER_TEXTURE_FORMAT_DEPTH24_STENCIL8,
} FosterTextureFormat;

typedef enum FosterClearMask
{
	FOSTER_CLEAR_MASK_NONE    = 0,
	FOSTER_CLEAR_MASK_COLOR   = 1 << 0,
	FOSTER_CLEAR_MASK_DEPTH   = 1 << 1,
	FOSTER_CLEAR_MASK_STENCIL = 1 << 2,
	FOSTER_CLEAR_MASK_All     = FOSTER_CLEAR_MASK_COLOR | FOSTER_CLEAR_MASK_DEPTH | FOSTER_CLEAR_MASK_STENCIL
} FosterClearMask;

typedef enum FosterUniformType
{
	FOSTER_UNIFORM_TYPE_NONE,
	FOSTER_UNIFORM_TYPE_FLOAT,
	FOSTER_UNIFORM_TYPE_FLOAT2,
	FOSTER_UNIFORM_TYPE_FLOAT3,
	FOSTER_UNIFORM_TYPE_FLOAT4,
	FOSTER_UNIFORM_TYPE_MAT3X2,
	FOSTER_UNIFORM_TYPE_MAT4X4,
	FOSTER_UNIFORM_TYPE_TEXTURE2D,
	FOSTER_UNIFORM_TYPE_SAMPLER2D
} FosterUniformType;

typedef enum FosterVertexType
{
	FOSTER_VERTEX_TYPE_NONE,
	FOSTER_VERTEX_TYPE_FLOAT,
	FOSTER_VERTEX_TYPE_FLOAT2,
	FOSTER_VERTEX_TYPE_FLOAT3,
	FOSTER_VERTEX_TYPE_FLOAT4,
	FOSTER_VERTEX_TYPE_BYTE4,
	FOSTER_VERTEX_TYPE_UBYTE4,
	FOSTER_VERTEX_TYPE_SHORT2,
	FOSTER_VERTEX_TYPE_USHORT2,
	FOSTER_VERTEX_TYPE_SHORT4,
	FOSTER_VERTEX_TYPE_USHORT4
} FosterVertexType;

typedef enum FosterIndexFormat
{
	FOSTER_INDEX_FORMAT_SIXTEEN,
	FOSTER_INDEX_FORMAT_THIRTY_TWO
} FosterIndexFormat;

typedef enum FosterLogging
{
	FOSTER_LOGGING_DEFAULT,
	FOSTER_LOGGING_ALL,
	FOSTER_LOGGING_NONE
} FosterLogging;

typedef enum FosterImageWriteFormat
{
	FOSTER_IMAGE_WRITE_FORMAT_PNG,
	FOSTER_IMAGE_WRITE_FORMAT_QOI,
} FosterImageWriteFormat;

typedef void (FOSTER_CALL * FosterLogFn)(const char *msg);
typedef void (FOSTER_CALL * FosterExitRequestFn)();
typedef void (FOSTER_CALL * FosterOnTextFn)(const char* txt);
typedef void (FOSTER_CALL * FosterOnKeyFn)(int key, FosterBool pressed);
typedef void (FOSTER_CALL * FosterOnMouseButtonFn)(int button, FosterBool pressed);
typedef void (FOSTER_CALL * FosterOnMouseMoveFn)(float posX, float posY);
typedef void (FOSTER_CALL * FosterOnMouseWheelFn)(float offsetX, float offsetY);
typedef void (FOSTER_CALL * FosterOnControllerConnectFn)(int index, const char* name, int buttonCount, int axisCount, FosterBool isGamepad, uint16_t vendor, uint16_t product, uint16_t version);
typedef void (FOSTER_CALL * FosterOnControllerDisconnectFn)(int index);
typedef void (FOSTER_CALL * FosterOnControllerButtonFn)(int index, int button, FosterBool pressed);
typedef void (FOSTER_CALL * FosterOnControllerAxisFn)(int index, int axis, float value);
typedef void (FOSTER_CALL * FosterWriteFn)(void *context, void *data, int size);

typedef struct FosterTexture FosterTexture; 
typedef struct FosterTarget FosterTarget; 
typedef struct FosterShader FosterShader; 
typedef struct FosterMesh FosterMesh; 

typedef struct FosterDesc
{
	const char* windowTitle;
	const char* applicationName;
	int width;
	int height;
	FosterRenderers renderer;
	FosterFlags flags;
	FosterLogFn onLogInfo;
	FosterLogFn onLogWarn;
	FosterLogFn onLogError;
	FosterExitRequestFn onExitRequest;
	FosterOnTextFn onText;
	FosterOnKeyFn onKey;
	FosterOnMouseButtonFn onMouseButton;
	FosterOnMouseMoveFn onMouseMove;
	FosterOnMouseWheelFn onMouseWheel;
	FosterOnControllerConnectFn onControllerConnect;
	FosterOnControllerDisconnectFn onControllerDisconnect;
	FosterOnControllerButtonFn onControllerButton;
	FosterOnControllerAxisFn onControllerAxis;
	FosterLogging logging;
} FosterDesc;

typedef struct FosterRect
{
	int x, y, w, h;
} FosterRect;

typedef struct FosterColor
{
	unsigned char r, g, b, a;
} FosterColor;

typedef struct FosterShaderData
{
	void* vertexShader;
	void* fragmentShader;
} FosterShaderData;

typedef struct FosterTextureSampler
{
	FosterTextureFilter filter;
	FosterTextureWrap wrapX;
	FosterTextureWrap wrapY;
} FosterTextureSampler;

typedef struct FosterVertexFormatElement
{
	int index;
	FosterVertexType type;
	int normalized;
} FosterVertexFormatElement;

typedef struct FosterVertexFormat
{
	FosterVertexFormatElement* elements;
	int elementCount;
	int stride;
} FosterVertexFormat;

typedef struct FosterUniformInfo
{
	int index;
	const char* name;
	FosterUniformType type;
	int arrayElements;
} FosterUniformInfo;

typedef struct FosterBlend
{
	FosterBlendOp colorOp;
	FosterBlendFactor colorSrc;
	FosterBlendFactor colorDst;
	FosterBlendOp alphaOp;
	FosterBlendFactor alphaSrc;
	FosterBlendFactor alphaDst;
	FosterBlendMask mask;
	uint32_t rgba;
} FosterBlend;

typedef struct FosterDrawCommand
{
	FosterTarget* target;
	FosterMesh* mesh;
	FosterShader* shader;
	int hasViewport;
	int hasScissor;
	FosterRect viewport;
	FosterRect scissor;
	int indexStart;
	int indexCount;
	int instanceCount;
	FosterCompare compare;
	FosterCull cull;
	FosterBlend blend;
} FosterDrawCommand;

typedef struct FosterClearCommand
{
	FosterTarget* target;
	FosterRect clip;
	FosterColor color;
	float depth;
	int stencil;
	FosterClearMask mask;
} FosterClearCommand;

typedef struct FosterFont FosterFont;

#if __cplusplus
extern "C" {
#endif

FOSTER_API void FosterStartup(FosterDesc desc);

FOSTER_API void FosterBeginFrame();

FOSTER_API void FosterPollEvents();

FOSTER_API void FosterEndFrame();

FOSTER_API void FosterShutdown();

FOSTER_API FosterBool FosterIsRunning();

FOSTER_API void FosterSetTitle(const char* title);

FOSTER_API void FosterSetSize(int width, int height);

FOSTER_API void FosterGetSize(int* width, int* height);

FOSTER_API void FosterGetSizeInPixels(int* width, int* height);

FOSTER_API void FosterSetFlags(FosterFlags flags);

FOSTER_API void FosterSetCentered();

FOSTER_API const char* FosterGetUserPath();

FOSTER_API void FosterSetClipboard(const char* cstr);

FOSTER_API const char* FosterGetClipboard();

FOSTER_API FosterBool FosterGetFocused();

FOSTER_API unsigned char* FosterImageLoad(const unsigned char* memory, int length, int* w, int* h);

FOSTER_API void FosterImageFree(unsigned char* data);

FOSTER_API FosterBool FosterImageWrite(FosterWriteFn* func, void* context, FosterImageWriteFormat format, int w, int h, const void* data);

FOSTER_API FosterFont* FosterFontInit(unsigned char* data, int length);

FOSTER_API void FosterFontGetMetrics(FosterFont* font, int* ascent, int* descent, int* linegap);

FOSTER_API int FosterFontGetGlyphIndex(FosterFont* font, int codepoint);

FOSTER_API float FosterFontGetScale(FosterFont* font, float size);

FOSTER_API float FosterFontGetKerning(FosterFont* font, int glyph1, int glyph2, float scale);

FOSTER_API void FosterFontGetCharacter(FosterFont* font, int glyph, float scale, int* width, int* height, float* advance, float* offsetX, float* offsetY, int* visible);

FOSTER_API void FosterFontGetPixels(FosterFont* font, unsigned char* dest, int glyph, int width, int height, float scale);

FOSTER_API void FosterFontFree(FosterFont* font);

FOSTER_API FosterRenderers FosterGetRenderer();

FOSTER_API FosterTexture* FosterTextureCreate(int width, int height, FosterTextureFormat format);

FOSTER_API void FosterTextureSetData(FosterTexture* texture, void* data, int length);

FOSTER_API void FosterTextureGetData(FosterTexture* texture, void* data, int length);

FOSTER_API void FosterTextureDestroy(FosterTexture* texture);

FOSTER_API FosterTarget* FosterTargetCreate(int width, int height, FosterTextureFormat* attachments, int attachmentCount);

FOSTER_API FosterTexture* FosterTargetGetAttachment(FosterTarget* target, int index);

FOSTER_API void FosterTargetDestroy(FosterTarget* target);

FOSTER_API FosterShader* FosterShaderCreate(FosterShaderData* data);

FOSTER_API void FosterShaderGetUniforms(FosterShader* shader, FosterUniformInfo* output, int* count, int max);

FOSTER_API void FosterShaderSetUniform(FosterShader* shader, int index, float* values);

FOSTER_API void FosterShaderSetTexture(FosterShader* shader, int index, FosterTexture** values);

FOSTER_API void FosterShaderSetSampler(FosterShader* shader, int index, FosterTextureSampler* values);

FOSTER_API void FosterShaderDestroy(FosterShader* shader);

FOSTER_API FosterMesh* FosterMeshCreate();

FOSTER_API void FosterMeshSetVertexFormat(FosterMesh* mesh, FosterVertexFormat* format);

FOSTER_API void FosterMeshSetVertexData(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset);

FOSTER_API void FosterMeshSetIndexFormat(FosterMesh* mesh, FosterIndexFormat format);

FOSTER_API void FosterMeshSetIndexData(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset);

FOSTER_API void FosterMeshDestroy(FosterMesh* mesh);

FOSTER_API void FosterDraw(FosterDrawCommand* command);

FOSTER_API void FosterClear(FosterClearCommand* clear);

#if __cplusplus
}
#endif

#endif
