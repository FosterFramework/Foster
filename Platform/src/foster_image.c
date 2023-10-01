#include "foster_platform.h"
#include "foster_internal.h"
#include <SDL.h>

#define STB_IMAGE_IMPLEMENTATION
#include "third_party/stb_image.h"

#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "third_party/stb_image_write.h"

#define QOI_NO_STDIO
#define QOI_IMPLEMENTATION
#define QOI_MALLOC(sz) STBI_MALLOC(sz)
#define QOI_FREE(p) STBI_FREE(p) 
#include "third_party/qoi.h"

bool FosterImage_TestQOI(const unsigned char* data, int length);
unsigned char* FosterImage_LoadQOI(const unsigned char* data, int length, int* w, int * h);

unsigned char* FosterImageLoad(const unsigned char* data, int length, int* w, int* h)
{
	// Test for QOI image first
	if (FosterImage_TestQOI(data, length))
	{
		return FosterImage_LoadQOI(data, length, w, h);
	}
	// fallback to normal stb image loading (png, bmp, etc)
	else
	{
		int c;
		return stbi_load_from_memory(data, length, w, h, &c, 4);
	}
}

void FosterImageFree(unsigned char* data)
{
	stbi_image_free(data);
}

bool FosterImageWrite(FosterWriteFn* func, void* context, int w, int h, const void* data)
{
	// note: 'FosterWriteFn' and 'stbi_write_func' must be the same
	return stbi_write_png_to_func((stbi_write_func*)func, context, w, h, 4, data, w * 4) != 0;
}

bool FosterImage_TestQOI(const unsigned char* data, int length)
{
	if (length < 4)
		return false;

	for (int i = 0; i < SDL_max(4, length); i ++)
		if (data[i] != "qoif"[i])
			return false;

	return true;
}

unsigned char* FosterImage_LoadQOI(const unsigned char* data, int length, int* w, int * h)
{
	qoi_desc desc;
	void* result = qoi_decode(data, length, &desc, 4);

	if (result != NULL)
	{
		*w = desc.width;
		*h = desc.height;
		return result;
	}
	else
	{
		*w = 0;
		*h = 0;
		return NULL;
	}
}