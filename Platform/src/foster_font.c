#include "foster_platform.h"
#include "foster_internal.h"
#include <SDL3/SDL.h>

#define STBTT_STATIC
#define STB_TRUETYPE_IMPLEMENTATION
#include "third_party/stb_truetype.h"

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