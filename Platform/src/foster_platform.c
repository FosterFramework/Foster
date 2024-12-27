#include "foster_platform.h"

#define STBI_NO_STDIO
#define STB_IMAGE_IMPLEMENTATION
#include "third_party/stb_image.h"

#define STBI_WRITE_NO_STDIO
#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "third_party/stb_image_write.h"

#define STBTT_STATIC
#define STB_TRUETYPE_IMPLEMENTATION
#include "third_party/stb_truetype.h"

#define QOI_NO_STDIO
#define QOI_IMPLEMENTATION
#define QOI_MALLOC(sz) STBI_MALLOC(sz)
#define QOI_FREE(p) STBI_FREE(p) 
#include "third_party/qoi.h"

FosterBool FosterImage_TestQOI(const unsigned char* data, int length);
unsigned char* FosterImage_LoadQOI(const unsigned char* data, int length, int* w, int * h);
FosterBool FosterImage_WriteQOI(FosterWriteFn* func, void* context, int w, int h, const void* data);

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
	return 0;
}

FosterFont* FosterFontInit(unsigned char* data, int length)
{
	if (stbtt_GetNumberOfFonts(data) <= 0)
		return NULL;

	void* userdata;
	stbtt_fontinfo* info = (stbtt_fontinfo*)STBTT_malloc(sizeof(stbtt_fontinfo),userdata);

	if (stbtt_InitFont(info, data, 0) == 0)
	{
		STBI_FREE(info);
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
	void* userdata;
	STBTT_free(info, userdata);
}

// Internal Methods:

FosterBool FosterImage_TestQOI(const unsigned char* data, int length)
{
	if (length < QOI_HEADER_SIZE)
		return 0;

	int p = 0;
	unsigned int magic = qoi_read_32(data, &p);
	if (magic != QOI_MAGIC)
		return 0;

	return 1;
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

FosterBool FosterImage_WriteQOI(FosterWriteFn* func, void* context, int w, int h, const void* data)
{
	qoi_desc desc = { w, h, 4, 1 };
	int length;
	void* encoded = qoi_encode(data, &desc, &length);

	if (encoded != NULL)
	{
		((stbi_write_func*)func)(context, encoded, length);
		QOI_FREE(encoded);
		return 1;
	}

	return 0;
}