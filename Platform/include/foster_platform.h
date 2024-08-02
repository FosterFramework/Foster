#ifndef FOSTER_H
#define FOSTER_H

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

typedef unsigned char FosterBool;
typedef struct FosterFont FosterFont;
typedef void (FOSTER_CALL * FosterWriteFn)(void *context, void *data, int size);

typedef enum FosterImageWriteFormat
{
	FOSTER_IMAGE_WRITE_FORMAT_PNG,
	FOSTER_IMAGE_WRITE_FORMAT_QOI,
} FosterImageWriteFormat;

#if __cplusplus
extern "C" {
#endif

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

#if __cplusplus
}
#endif

#endif
