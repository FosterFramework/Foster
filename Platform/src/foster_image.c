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
bool FosterImage_WriteQOI(FosterWriteFn* func, void* context, int w, int h, const void* data);

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

FosterBool FosterImageWrite(FosterWriteFn* func, void* context, FosterImageWriteFormat format, int w, int h, const void* data)
{
	// note: 'FosterWriteFn' and 'stbi_write_func' must be the same
	switch (format)
	{
	case FOSTER_IMAGE_WRITE_FORMAT_PNG:
		return stbi_write_png_to_func((stbi_write_func*)func, context, w, h, 4, data, w * 4) != 0;
	case FOSTER_IMAGE_WRITE_FORMAT_QOI:
		return FosterImage_WriteQOI(func, context, w, h, data);
	}
	return false;
}

bool FosterImage_TestQOI(const unsigned char* data, int length)
{
	if (length < QOI_HEADER_SIZE)
		return false;

	int p = 0;
	unsigned int magic = qoi_read_32(data, &p);
	if (magic != QOI_MAGIC)
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

bool FosterImage_WriteQOI(FosterWriteFn* func, void* context, int w, int h, const void* data)
{
	qoi_desc desc = { w, h, 4, 1 };
	int length;
	void* encoded = qoi_encode(data, &desc, &length);

	if (encoded != NULL)
	{
		((stbi_write_func*)func)(context, encoded, length);
		QOI_FREE(encoded);
		return true;
	}

	return false;
}
