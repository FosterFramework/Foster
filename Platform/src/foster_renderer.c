#include "foster_renderer.h"

bool FosterGetDevice(FosterRenderers preferred, FosterRenderDevice* device)
{
	if (preferred == FOSTER_RENDERER_NONE)
	{
		// TODO: 
		// once D3D11 renderer is re-implemented, this is the correct behavior:
		/*#if defined(_WIN32) && defined(FOSTER_D3D11_ENABLED)
			preferred = FOSTER_RENDERER_D3D11;
		#else
			preferred = FOSTER_RENDERER_OPENGL;
		#endif*/

		preferred = FOSTER_RENDERER_OPENGL;
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
