#ifndef FOSTER_INTERNAL_H
#define FOSTER_INTERNAL_H

#include "foster_platform.h"
#include "foster_renderer.h"
#include <SDL.h>

#define FOSTER_LOG_INFO(...) FosterLog(FOSTER_LOG_LEVEL_INFO, __VA_ARGS__)
#define FOSTER_LOG_WARN(...) FosterLog(FOSTER_LOG_LEVEL_WARNING, __VA_ARGS__)
#define FOSTER_LOG_ERROR(...) FosterLog(FOSTER_LOG_LEVEL_ERROR, __VA_ARGS__)

// foster global state
typedef struct
{
	FosterBool running;
	FosterDesc desc;
	FosterFlags flags;
	FosterRenderDevice device;
	int windowCreateFlags;
	SDL_Window* window;
	SDL_Joystick* joysticks[FOSTER_MAX_CONTROLLERS];
	SDL_GameController* gamepads[FOSTER_MAX_CONTROLLERS];
	char* clipboardText;
	char* userPath;
	FosterLogFn logFn;
	FosterLogFilter logFilter;
	FosterBool polledMouseMovement;
} FosterState;

FosterState* FosterGetState();

void FosterLog(FosterLogLevel level, const char* fmt, ...);

#endif
