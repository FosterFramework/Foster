#include "foster_renderer.h"

bool FosterGetDevice(FosterRenderers preferred, FosterRenderDevice* device)
{
	if (preferred == FOSTER_RENDERER_NONE)
	{
		#ifdef _WIN32
			preferred = FOSTER_RENDERER_D3D11;
		#else
			preferred = FOSTER_RENDERER_OPENGL;
		#endif
	}

	switch (preferred)
	{
		case FOSTER_RENDERER_NONE:
			return false;
		case FOSTER_RENDERER_OPENGL:
			return FosterGetDevice_OpenGL(device);
		case FOSTER_RENDERER_D3D11:
			return FosterGetDevice_D3D11(device);
	}

	return false;
}
