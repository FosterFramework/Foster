#ifdef FOSTER_OPENGL_ENABLED

#include "foster_renderer.h"
#include "foster_internal.h"
#include <stdio.h>
#include <string.h>
#include <stddef.h>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#else
#define APIENTRY
#endif

// OpenGL Value Types
typedef ptrdiff_t        GLintptr;
typedef ptrdiff_t        GLsizeiptr;
typedef unsigned int     GLenum;
typedef unsigned char    GLboolean;
typedef unsigned int     GLbitfield;
typedef void             GLvoid;
typedef signed char      GLbyte;      /* 1-byte signed */
typedef short            GLshort;     /* 2-byte signed */
typedef int              GLint;       /* 4-byte signed */
typedef unsigned char    GLubyte;     /* 1-byte unsigned */
typedef unsigned short   GLushort;    /* 2-byte unsigned */
typedef unsigned int     GLuint;      /* 4-byte unsigned */
typedef int              GLsizei;     /* 4-byte signed */
typedef float            GLfloat;     /* single precision float */
typedef float            GLclampf;    /* single precision float in [0,1] */
typedef double           GLdouble;    /* double precision float */
typedef double           GLclampd;    /* double precision float in [0,1] */
typedef char             GLchar;

// OpenGL Constants
#define GL_DONT_CARE 0x1100
#define GL_ZERO 0x0000
#define GL_ONE 0x0001
#define GL_BYTE 0x1400
#define GL_UNSIGNED_BYTE 0x1401
#define GL_SHORT 0x1402
#define GL_UNSIGNED_SHORT 0x1403
#define GL_INT 0x1404
#define GL_UNSIGNED_INT 0x1405
#define GL_FLOAT 0x1406
#define GL_HALF_FLOAT 0x140B
#define GL_UNSIGNED_SHORT_4_4_4_4_REV 0x8365
#define GL_UNSIGNED_SHORT_5_5_5_1_REV 0x8366
#define GL_UNSIGNED_INT_2_10_10_10_REV 0x8368
#define GL_UNSIGNED_SHORT_5_6_5 0x8363
#define GL_UNSIGNED_INT_24_8 0x84FA
#define GL_VENDOR 0x1F00
#define GL_RENDERER 0x1F01
#define GL_VERSION 0x1F02
#define GL_EXTENSIONS 0x1F03
#define GL_COLOR_BUFFER_BIT 0x4000
#define GL_DEPTH_BUFFER_BIT 0x0100
#define GL_STENCIL_BUFFER_BIT 0x0400
#define GL_SCISSOR_TEST 0x0C11
#define GL_DEPTH_TEST 0x0B71
#define GL_STENCIL_TEST 0x0B90
#define GL_LINE 0x1B01
#define GL_FILL 0x1B02
#define GL_CW 0x0900
#define GL_CCW 0x0901
#define GL_FRONT 0x0404
#define GL_BACK 0x0405
#define GL_FRONT_AND_BACK 0x0408
#define GL_CULL_FACE 0x0B44
#define GL_POLYGON_OFFSET_FILL 0x8037
#define GL_TEXTURE_2D 0x0DE1
#define GL_TEXTURE_3D 0x806F
#define GL_TEXTURE_CUBE_MAP 0x8513
#define GL_TEXTURE_CUBE_MAP_POSITIVE_X 0x8515
#define GL_BLEND 0x0BE2
#define GL_SRC_COLOR 0x0300
#define GL_ONE_MINUS_SRC_COLOR 0x0301
#define GL_SRC_ALPHA 0x0302
#define GL_ONE_MINUS_SRC_ALPHA 0x0303
#define GL_DST_ALPHA 0x0304
#define GL_ONE_MINUS_DST_ALPHA 0x0305
#define GL_DST_COLOR 0x0306
#define GL_ONE_MINUS_DST_COLOR 0x0307
#define GL_SRC_ALPHA_SATURATE 0x0308
#define GL_CONSTANT_COLOR 0x8001
#define GL_ONE_MINUS_CONSTANT_COLOR 0x8002
#define GL_CONSTANT_ALPHA 0x8003
#define GL_ONE_MINUS_CONSTANT_ALPHA 0x8004
#define GL_SRC1_ALPHA 0x8589
#define GL_SRC1_COLOR 0x88F9
#define GL_ONE_MINUS_SRC1_COLOR 0x88FA
#define GL_ONE_MINUS_SRC1_ALPHA 0x88FB
#define GL_MIN 0x8007
#define GL_MAX 0x8008
#define GL_FUNC_ADD 0x8006
#define GL_FUNC_SUBTRACT 0x800A
#define GL_FUNC_REVERSE_SUBTRACT 0x800B
#define GL_NEVER 0x0200
#define GL_LESS 0x0201
#define GL_EQUAL 0x0202
#define GL_LEQUAL 0x0203
#define GL_GREATER 0x0204
#define GL_NOTEQUAL 0x0205
#define GL_GEQUAL 0x0206
#define GL_ALWAYS 0x0207
#define GL_INVERT 0x150A
#define GL_KEEP 0x1E00
#define GL_REPLACE 0x1E01
#define GL_INCR 0x1E02
#define GL_DECR 0x1E03
#define GL_INCR_WRAP 0x8507
#define GL_DECR_WRAP 0x8508
#define GL_REPEAT 0x2901
#define GL_CLAMP_TO_EDGE 0x812F
#define GL_MIRRORED_REPEAT 0x8370
#define GL_NEAREST 0x2600
#define GL_LINEAR 0x2601
#define GL_NEAREST_MIPMAP_NEAREST 0x2700
#define GL_NEAREST_MIPMAP_LINEAR 0x2702
#define GL_LINEAR_MIPMAP_NEAREST 0x2701
#define GL_LINEAR_MIPMAP_LINEAR 0x2703
#define GL_COLOR_ATTACHMENT0 0x8CE0
#define GL_DEPTH_ATTACHMENT 0x8D00
#define GL_STENCIL_ATTACHMENT 0x8D20
#define GL_DEPTH_STENCIL_ATTACHMENT 0x821A
#define GL_RED 0x1903
#define GL_RGB 0x1907
#define GL_RGBA 0x1908
#define GL_LUMINANCE 0x1909
#define GL_RGB8 0x8051
#define GL_RGBA8 0x8058
#define GL_RGBA4 0x8056
#define GL_RGB5_A1 0x8057
#define GL_RGB10_A2_EXT 0x8059
#define GL_RGBA16 0x805B
#define GL_BGRA 0x80E1
#define GL_DEPTH_COMPONENT16 0x81A5
#define GL_DEPTH_COMPONENT24 0x81A6
#define GL_RG 0x8227
#define GL_RG8 0x822B
#define GL_RG16 0x822C
#define GL_R16F 0x822D
#define GL_R32F 0x822E
#define GL_RG16F 0x822F
#define GL_RG32F 0x8230
#define GL_RGBA32F 0x8814
#define GL_RGBA16F 0x881A
#define GL_DEPTH24_STENCIL8 0x88F0
#define GL_COMPRESSED_TEXTURE_FORMATS 0x86A3
#define GL_COMPRESSED_RGBA_S3TC_DXT1_EXT 0x83F1
#define GL_COMPRESSED_RGBA_S3TC_DXT3_EXT 0x83F2
#define GL_COMPRESSED_RGBA_S3TC_DXT5_EXT 0x83F3
#define GL_DEPTH_COMPONENT 0x1902
#define GL_DEPTH_STENCIL 0x84F9
#define GL_TEXTURE_WRAP_S 0x2802
#define GL_TEXTURE_WRAP_T 0x2803
#define GL_TEXTURE_WRAP_R 0x8072
#define GL_TEXTURE_MAG_FILTER 0x2800
#define GL_TEXTURE_MIN_FILTER 0x2801
#define GL_TEXTURE_MAX_ANISOTROPY_EXT 0x84FE
#define GL_TEXTURE_BASE_LEVEL 0x813C
#define GL_TEXTURE_MAX_LEVEL 0x813D
#define GL_TEXTURE_LOD_BIAS 0x8501
#define GL_PACK_ALIGNMENT 0x0D05
#define GL_UNPACK_ALIGNMENT 0x0CF5
#define GL_TEXTURE0 0x84C0
#define GL_MAX_TEXTURE_IMAGE_UNITS 0x8872
#define GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS 0x8B4C
#define GL_ARRAY_BUFFER 0x8892
#define GL_ELEMENT_ARRAY_BUFFER 0x8893
#define GL_STREAM_DRAW 0x88E0
#define GL_STATIC_DRAW 0x88E4
#define GL_DYNAMIC_DRAW 0x88E8
#define GL_MAX_VERTEX_ATTRIBS 0x8869
#define GL_FRAMEBUFFER 0x8D40
#define GL_READ_FRAMEBUFFER 0x8CA8
#define GL_DRAW_FRAMEBUFFER 0x8CA9
#define GL_RENDERBUFFER 0x8D41
#define GL_MAX_DRAW_BUFFERS 0x8824
#define GL_POINTS 0x0000
#define GL_LINES 0x0001
#define GL_LINE_STRIP 0x0003
#define GL_TRIANGLES 0x0004
#define GL_TRIANGLE_STRIP 0x0005
#define GL_QUERY_RESULT 0x8866
#define GL_QUERY_RESULT_AVAILABLE 0x8867
#define GL_SAMPLES_PASSED 0x8914
#define GL_MULTISAMPLE 0x809D
#define GL_MAX_SAMPLES 0x8D57
#define GL_SAMPLE_MASK 0x8E51
#define GL_FRAGMENT_SHADER 0x8B30
#define GL_VERTEX_SHADER 0x8B31
#define GL_ACTIVE_UNIFORMS 0x8B86
#define GL_ACTIVE_ATTRIBUTES 0x8B89
#define GL_FLOAT_VEC2 0x8B50
#define GL_FLOAT_VEC3 0x8B51
#define GL_FLOAT_VEC4 0x8B52
#define GL_SAMPLER_2D 0x8B5E
#define GL_FLOAT_MAT3x2 0x8B67
#define GL_FLOAT_MAT4 0x8B5C
#define GL_NUM_EXTENSIONS 0x821D
#define GL_DEBUG_SOURCE_API 0x8246
#define GL_DEBUG_SOURCE_WINDOW_SYSTEM 0x8247
#define GL_DEBUG_SOURCE_SHADER_COMPILER 0x8248
#define GL_DEBUG_SOURCE_THIRD_PARTY 0x8249
#define GL_DEBUG_SOURCE_APPLICATION 0x824A
#define GL_DEBUG_SOURCE_OTHER 0x824B
#define GL_DEBUG_TYPE_ERROR 0x824C
#define GL_DEBUG_TYPE_PUSH_GROUP 0x8269
#define GL_DEBUG_TYPE_POP_GROUP 0x826A
#define GL_DEBUG_TYPE_MARKER 0x8268
#define GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR 0x824D
#define GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR 0x824E
#define GL_DEBUG_TYPE_PORTABILITY 0x824F
#define GL_DEBUG_TYPE_PERFORMANCE 0x8250
#define GL_DEBUG_TYPE_OTHER 0x8251
#define GL_DEBUG_SEVERITY_HIGH 0x9146
#define GL_DEBUG_SEVERITY_MEDIUM 0x9147
#define GL_DEBUG_SEVERITY_LOW 0x9148
#define GL_DEBUG_SEVERITY_NOTIFICATION 0x826B
#define GL_DEBUG_OUTPUT 0x92E0
#define GL_DEBUG_OUTPUT_SYNCHRONOUS 0x8242
#define GL_COMPILE_STATUS 0x8B81
#define GL_LINK_STATUS 0x8B82

// OpenGL Functions
#define GL_FUNCTIONS \
	GL_FUNC(DebugMessageCallback, void, DEBUGPROC callback, const void* userParam) \
	GL_FUNC(GetString, const GLubyte*, GLenum name) \
	GL_FUNC(Flush, void, void) \
	GL_FUNC(Enable, void, GLenum mode) \
	GL_FUNC(Disable, void, GLenum mode) \
	GL_FUNC(Clear, void, GLenum mask) \
	GL_FUNC(ClearColor, void, GLfloat red, GLfloat green, GLfloat blue, GLfloat alpha) \
	GL_FUNC(ClearDepth, void, GLdouble depth) \
	GL_FUNC(ClearStencil, void, GLint stencil) \
	GL_FUNC(DepthMask, void, GLboolean enabled) \
	GL_FUNC(DepthFunc, void, GLenum func) \
	GL_FUNC(Viewport, void, GLint x, GLint y, GLint width, GLint height) \
	GL_FUNC(Scissor, void, GLint x, GLint y, GLint width, GLint height) \
	GL_FUNC(CullFace, void, GLenum mode) \
	GL_FUNC(BlendEquation, void, GLenum eq) \
	GL_FUNC(BlendEquationSeparate, void, GLenum modeRGB, GLenum modeAlpha) \
	GL_FUNC(BlendFunc, void, GLenum sfactor, GLenum dfactor) \
	GL_FUNC(BlendFuncSeparate, void, GLenum srcRGB, GLenum dstRGB, GLenum srcAlpha, GLenum dstAlpha) \
	GL_FUNC(BlendColor, void, GLfloat red, GLfloat green, GLfloat blue, GLfloat alpha) \
	GL_FUNC(ColorMask, void, GLboolean red, GLboolean green, GLboolean blue, GLboolean alpha) \
	GL_FUNC(GetIntegerv, void, GLenum name, GLint* data) \
	GL_FUNC(GenTextures, void, GLint n, void* textures) \
	GL_FUNC(GenRenderbuffers, void, GLint n, void* textures) \
	GL_FUNC(GenFramebuffers, void, GLint n, void* textures) \
	GL_FUNC(ActiveTexture, void, GLuint id) \
	GL_FUNC(BindTexture, void, GLenum target, GLuint id) \
	GL_FUNC(BindRenderbuffer, void, GLenum target, GLuint id) \
	GL_FUNC(BindFramebuffer, void, GLenum target, GLuint id) \
	GL_FUNC(TexImage2D, void, GLenum target, GLint level, GLenum internalFormat, GLint width, GLint height, GLint border, GLenum format, GLenum type, const void* data) \
	GL_FUNC(FramebufferRenderbuffer, void, GLenum target, GLenum attachment, GLenum renderbuffertarget, GLuint renderbuffer) \
	GL_FUNC(FramebufferTexture2D, void, GLenum target, GLenum attachment, GLenum textarget, GLuint texture, GLint level) \
	GL_FUNC(TexParameteri, void, GLenum target, GLenum name, GLint param) \
	GL_FUNC(RenderbufferStorage, void, GLenum target, GLenum internalformat, GLint width, GLint height) \
	GL_FUNC(GetTexImage, void, GLenum target, GLint level, GLenum format, GLenum type, void* data) \
	GL_FUNC(DrawElements, void, GLenum mode, GLint count, GLenum type, void* indices) \
	GL_FUNC(DrawElementsInstanced, void, GLenum mode, GLint count, GLenum type, void* indices, GLint amount) \
	GL_FUNC(DrawBuffers, void, GLsizei n, const GLenum* bufs) \
	GL_FUNC(DeleteTextures, void, GLint n, GLuint* textures) \
	GL_FUNC(DeleteRenderbuffers, void, GLint n, GLuint* renderbuffers) \
	GL_FUNC(DeleteFramebuffers, void, GLint n, GLuint* textures) \
	GL_FUNC(GenVertexArrays, void, GLint n, GLuint* arrays) \
	GL_FUNC(BindVertexArray, void, GLuint id) \
	GL_FUNC(GenBuffers, void, GLint n, GLuint* arrays) \
	GL_FUNC(BindBuffer, void, GLenum target, GLuint buffer) \
	GL_FUNC(BufferData, void, GLenum target, GLsizeiptr size, const void* data, GLenum usage) \
	GL_FUNC(BufferSubData, void, GLenum target, GLintptr offset, GLsizeiptr size, const void* data) \
	GL_FUNC(DeleteBuffers, void, GLint n, GLuint* buffers) \
	GL_FUNC(DeleteVertexArrays, void, GLint n, GLuint* arrays) \
	GL_FUNC(EnableVertexAttribArray, void, GLuint location) \
	GL_FUNC(DisableVertexAttribArray, void, GLuint location) \
	GL_FUNC(VertexAttribPointer, void, GLuint index, GLint size, GLenum type, GLboolean normalized, GLint stride, const void* pointer) \
	GL_FUNC(VertexAttribDivisor, void, GLuint index, GLuint divisor) \
	GL_FUNC(CreateShader, GLuint, GLenum type) \
	GL_FUNC(AttachShader, void, GLuint program, GLuint shader) \
	GL_FUNC(DetachShader, void, GLuint program, GLuint shader) \
	GL_FUNC(DeleteShader, void, GLuint shader) \
	GL_FUNC(ShaderSource, void, GLuint shader, GLsizei count, const GLchar** string, const GLint* length) \
	GL_FUNC(CompileShader, void, GLuint shader) \
	GL_FUNC(GetShaderiv, void, GLuint shader, GLenum pname, GLint* result) \
	GL_FUNC(GetShaderInfoLog, void, GLuint shader, GLint maxLength, GLsizei* length, GLchar* infoLog) \
	GL_FUNC(CreateProgram, GLuint, ) \
	GL_FUNC(DeleteProgram, void, GLuint program) \
	GL_FUNC(LinkProgram, void, GLuint program) \
	GL_FUNC(GetProgramiv, void, GLuint program, GLenum pname, GLint* result) \
	GL_FUNC(GetProgramInfoLog, void, GLuint program, GLint maxLength, GLsizei* length, GLchar* infoLog) \
	GL_FUNC(GetActiveUniform, void, GLuint program, GLuint index, GLint bufSize, GLsizei* length, GLint* size, GLenum* type, GLchar* name) \
	GL_FUNC(GetActiveAttrib, void, GLuint program, GLuint index, GLint bufSize, GLsizei* length, GLint* size, GLenum* type, GLchar* name) \
	GL_FUNC(UseProgram, void, GLuint program) \
	GL_FUNC(GetUniformLocation, GLint, GLuint program, const GLchar* name) \
	GL_FUNC(GetAttribLocation, GLint, GLuint program, const GLchar* name) \
	GL_FUNC(Uniform1f, void, GLint location, GLfloat v0) \
	GL_FUNC(Uniform2f, void, GLint location, GLfloat v0, GLfloat v1) \
	GL_FUNC(Uniform3f, void, GLint location, GLfloat v0, GLfloat v1, GLfloat v2) \
	GL_FUNC(Uniform4f, void, GLint location, GLfloat v0, GLfloat v1, GLfloat v2, GLfloat v3) \
	GL_FUNC(Uniform1fv, void, GLint location, GLint count, const GLfloat* value) \
	GL_FUNC(Uniform2fv, void, GLint location, GLint count, const GLfloat* value) \
	GL_FUNC(Uniform3fv, void, GLint location, GLint count, const GLfloat* value) \
	GL_FUNC(Uniform4fv, void, GLint location, GLint count, const GLfloat* value) \
	GL_FUNC(Uniform1i, void, GLint location, GLint v0) \
	GL_FUNC(Uniform2i, void, GLint location, GLint v0, GLint v1) \
	GL_FUNC(Uniform3i, void, GLint location, GLint v0, GLint v1, GLint v2) \
	GL_FUNC(Uniform4i, void, GLint location, GLint v0, GLint v1, GLint v2, GLint v3) \
	GL_FUNC(Uniform1iv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform2iv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform3iv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform4iv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform1ui, void, GLint location, GLuint v0) \
	GL_FUNC(Uniform2ui, void, GLint location, GLuint v0, GLuint v1) \
	GL_FUNC(Uniform3ui, void, GLint location, GLuint v0, GLuint v1, GLuint v2) \
	GL_FUNC(Uniform4ui, void, GLint location, GLuint v0, GLuint v1, GLuint v2, GLuint v3) \
	GL_FUNC(Uniform1uiv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform2uiv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform3uiv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(Uniform4uiv, void, GLint location, GLint count, const GLint* value) \
	GL_FUNC(UniformMatrix2fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix3fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix4fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix2x3fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix3x2fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix2x4fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix4x2fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix3x4fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(UniformMatrix4x3fv, void, GLint location, GLint count, GLboolean transpose, const GLfloat* value) \
	GL_FUNC(PixelStorei, void, GLenum pname, GLint param)

// Debug Function Delegate
typedef void (APIENTRY* DEBUGPROC)(GLenum source,
	GLenum type,
	GLuint id,
	GLenum severity,
	GLsizei length,
	const GLchar* message,
	const void* userParam);

// GL Function Typedefs
#define GL_FUNC(name, ret, ...) typedef ret (*gl ## name ## Fn) (__VA_ARGS__);
GL_FUNCTIONS
#undef GL_FUNC

#define FOSTER_RECT_EQUAL(a, b) ((a).x == (b).x && (a).y == (b).y && (a).w == (b).w && (a).h == (b).h)

typedef struct FosterTexure_OpenGL
{
	GLuint id;
	int width;
	int height;
	FosterTextureFormat format;
	GLenum glInternalFormat;
	GLenum glFormat;
	GLenum glType;
	GLenum glAttachment;
	FosterTextureSampler sampler;
	
	// Because Shader uniforms assign textures, it's possible for the user to
	// dispose of a texture but still have it assigned in a shader. Thus we use
	// a simple ref counter to determine when it's safe to delete the wrapping
	// texture class.
	int refCount;
	int disposed;
} FosterTexture_OpenGL;

typedef struct FosterTarget_OpenGL
{
	GLuint id;
	int width;
	int height;
	int attachmentCount;
	int colorAttachmentCount;
	FosterTexture_OpenGL* attachments[FOSTER_MAX_TARGET_ATTACHMENTS];
} FosterTarget_OpenGL;

typedef struct FosterUniform_OpenGL
{
	char* name;
	char* samplerName;
	GLint glLocation;
	GLsizei glSize;
	GLenum glType;
	int samplerIndex;
} FosterUniform_OpenGL;

typedef struct FosterShader_OpenGL
{
	GLuint id;
	GLint uniformCount;
	GLint samplerCount;
	FosterUniform_OpenGL* uniforms;
	FosterTexture_OpenGL* textures[FOSTER_MAX_UNIFORM_TEXTURES];
	FosterTextureSampler samplers[FOSTER_MAX_UNIFORM_TEXTURES];
} FosterShader_OpenGL;

typedef struct FosterMesh_OpenGL
{
	GLuint id;
	GLuint indexBuffer;
	GLuint vertexBuffer;
	GLuint instanceBuffer;
	int vertexAttributesEnabled;
	int instanceAttributesEnabled;
	GLuint vertexAttributes[32];
	GLuint instanceAttributes[32];
	GLenum indexFormat;
	int indexSize;
	int vertexBufferSize;
	int indexBufferSize;
} FosterMesh_OpenGL;

typedef struct
{
	// GL function pointers
	#define GL_FUNC(name, ret, ...) gl ## name ## Fn gl ## name;
	GL_FUNCTIONS
	#undef GL_FUNC

	// GL context
	void* context;

	// stored OpenGL state
	int stateInitializing;
	int stateActiveTextureSlot;
	GLuint stateTextureSlots[FOSTER_MAX_UNIFORM_TEXTURES];
	GLuint stateProgram;
	GLuint stateFrameBuffer;
	GLuint stateVertexArray;
	int stateFrameBufferWidth;
	int stateFrameBufferHeight;
	int stateHasScissor;
	FosterRect stateViewport;
	FosterRect stateScissor;
	FosterCompare stateCompare;
	FosterCull stateCull;
	FosterBlend stateBlend;
	int stateDepthMask;

	// info
	int max_color_attachments;
	int max_element_indices;
	int max_element_vertices;
	int max_renderbuffer_size;
	int max_samples;
	int max_texture_image_units;
	int max_texture_size;

} FosterOpenGLState;
static FosterOpenGLState fgl;

// debug callback
void APIENTRY FosterMessage_OpenGL(GLenum source, GLenum type, GLuint id, GLenum severity, GLsizei length, const GLchar* message, const void* userParam)
{
	if (FosterGetState()->desc.logging != FOSTER_LOGGING_ALL)
	{
		if (severity == GL_DEBUG_SEVERITY_NOTIFICATION &&
			type == GL_DEBUG_TYPE_OTHER)
			return;
	}

	const char* typeName = "";
	const char* severityName = "";

	switch (type)
	{
	case GL_DEBUG_TYPE_ERROR: typeName = "ERROR"; break;
	case GL_DEBUG_TYPE_DEPRECATED_BEHAVIOR: typeName = "DEPRECATED BEHAVIOR"; break;
	case GL_DEBUG_TYPE_MARKER: typeName = "MARKER"; break;
	case GL_DEBUG_TYPE_OTHER: typeName = "OTHER"; break;
	case GL_DEBUG_TYPE_PERFORMANCE: typeName = "PEROFRMANCE"; break;
	case GL_DEBUG_TYPE_POP_GROUP: typeName = "POP GROUP"; break;
	case GL_DEBUG_TYPE_PORTABILITY: typeName = "PORTABILITY"; break;
	case GL_DEBUG_TYPE_PUSH_GROUP: typeName = "PUSH GROUP"; break;
	case GL_DEBUG_TYPE_UNDEFINED_BEHAVIOR: typeName = "UNDEFINED BEHAVIOR"; break;
	}

	switch (severity)
	{
	case GL_DEBUG_SEVERITY_HIGH: severityName = "HIGH"; break;
	case GL_DEBUG_SEVERITY_MEDIUM: severityName = "MEDIUM"; break;
	case GL_DEBUG_SEVERITY_LOW: severityName = "LOW"; break;
	case GL_DEBUG_SEVERITY_NOTIFICATION: severityName = "NOTIFICATION"; break;
	}

	if (type == GL_DEBUG_TYPE_ERROR)
		FosterLogError("GL (%s:%s) %s", typeName, severityName, message);
	else if (severity != GL_DEBUG_SEVERITY_NOTIFICATION)
		FosterLogWarn("GL (%s:%s) %s", typeName, severityName, message);
	else
		FosterLogInfo("GL (%s) %s", typeName, message);
}

// conversion methods
GLenum FosterWrapToGL(FosterTextureWrap wrap)
{
	switch (wrap)
	{
		case FOSTER_TEXTURE_WRAP_REPEAT: return GL_REPEAT;
		case FOSTER_TEXTURE_WRAP_MIRRORED_REPEAT: return GL_MIRRORED_REPEAT;
		case FOSTER_TEXTURE_WRAP_CLAMP_TO_BORDER: return GL_CLAMP_TO_EDGE;
		case FOSTER_TEXTURE_WRAP_CLAMP_TO_EDGE: return GL_CLAMP_TO_EDGE;
		default: return GL_REPEAT;
	}
}

GLenum FosterFilterToGL(FosterTextureFilter filter)
{
	switch (filter)
	{
		case FOSTER_TEXTURE_FILTER_NEAREST: return GL_NEAREST;
		case FOSTER_TEXTURE_FILTER_LINEAR: return GL_LINEAR;
		default: return GL_NEAREST;
	}
}

GLenum FosterBlendOpToGL(FosterBlendOp operation)
{
	switch (operation)
	{
	case FOSTER_BLEND_OP_ADD: return GL_FUNC_ADD;
	case FOSTER_BLEND_OP_SUBTRACT: return GL_FUNC_SUBTRACT;
	case FOSTER_BLEND_OP_REVERSE_SUBTRACT: return GL_FUNC_REVERSE_SUBTRACT;
	case FOSTER_BLEND_OP_MIN: return GL_MIN;
	case FOSTER_BLEND_OP_MAX: return GL_MAX;
	default: return GL_FUNC_ADD;
	};
}

GLenum FosterBlendFactorToGL(FosterBlendFactor factor)
{
	switch (factor)
	{
	case FOSTER_BLEND_FACTOR_Zero: return GL_ZERO;
	case FOSTER_BLEND_FACTOR_One: return GL_ONE;
	case FOSTER_BLEND_FACTOR_SrcColor: return GL_SRC_COLOR;
	case FOSTER_BLEND_FACTOR_OneMinusSrcColor: return GL_ONE_MINUS_SRC_COLOR;
	case FOSTER_BLEND_FACTOR_DstColor: return GL_DST_COLOR;
	case FOSTER_BLEND_FACTOR_OneMinusDstColor: return GL_ONE_MINUS_DST_COLOR;
	case FOSTER_BLEND_FACTOR_SrcAlpha: return GL_SRC_ALPHA;
	case FOSTER_BLEND_FACTOR_OneMinusSrcAlpha: return GL_ONE_MINUS_SRC_ALPHA;
	case FOSTER_BLEND_FACTOR_DstAlpha: return GL_DST_ALPHA;
	case FOSTER_BLEND_FACTOR_OneMinusDstAlpha: return GL_ONE_MINUS_DST_ALPHA;
	case FOSTER_BLEND_FACTOR_ConstantColor: return GL_CONSTANT_COLOR;
	case FOSTER_BLEND_FACTOR_OneMinusConstantColor: return GL_ONE_MINUS_CONSTANT_COLOR;
	case FOSTER_BLEND_FACTOR_ConstantAlpha: return GL_CONSTANT_ALPHA;
	case FOSTER_BLEND_FACTOR_OneMinusConstantAlpha: return GL_ONE_MINUS_CONSTANT_ALPHA;
	case FOSTER_BLEND_FACTOR_SrcAlphaSaturate: return GL_SRC_ALPHA_SATURATE;
	case FOSTER_BLEND_FACTOR_Src1Color: return GL_SRC1_COLOR;
	case FOSTER_BLEND_FACTOR_OneMinusSrc1Color: return GL_ONE_MINUS_SRC1_COLOR;
	case FOSTER_BLEND_FACTOR_Src1Alpha: return GL_SRC1_ALPHA;
	case FOSTER_BLEND_FACTOR_OneMinusSrc1Alpha: return GL_ONE_MINUS_SRC1_ALPHA;
	};

	return GL_ZERO;
}

FosterUniformType FosterUniformTypeFromGL(GLenum value)
{
	switch (value)
	{
	case GL_FLOAT: return FOSTER_UNIFORM_TYPE_FLOAT;
	case GL_FLOAT_VEC2: return FOSTER_UNIFORM_TYPE_FLOAT2;
	case GL_FLOAT_VEC3: return FOSTER_UNIFORM_TYPE_FLOAT3;
	case GL_FLOAT_VEC4: return FOSTER_UNIFORM_TYPE_FLOAT4;
	case GL_FLOAT_MAT3x2: return FOSTER_UNIFORM_TYPE_MAT3X2;
	case GL_FLOAT_MAT4: return FOSTER_UNIFORM_TYPE_MAT4X4;
	case GL_SAMPLER_2D: return FOSTER_UNIFORM_TYPE_SAMPLER2D;
	};

	return FOSTER_UNIFORM_TYPE_NONE;
}

GLuint FosterMeshAssignAttributes_OpenGL(GLuint buffer, GLenum bufferType, FosterVertexFormat* format, GLint divisor)
{
	// bind
	fgl.glBindBuffer(bufferType, buffer);

	// TODO: disable existing enabled attributes?
	// ...

	// enable attributes
	size_t ptr = 0;
	for (int n = 0; n < format->elementCount; n++)
	{
		FosterVertexFormatElement element = format->elements[n];
		GLenum type = GL_UNSIGNED_BYTE;
		size_t componentSize = 0;
		int components = 1;

		switch (element.type)
		{
		case FOSTER_VERTEX_TYPE_FLOAT:
			type = GL_FLOAT;
			componentSize = 4;
			components = 1;
			break;
		case FOSTER_VERTEX_TYPE_FLOAT2:
			type = GL_FLOAT;
			componentSize = 4;
			components = 2;
			break;
		case FOSTER_VERTEX_TYPE_FLOAT3:
			type = GL_FLOAT;
			componentSize = 4;
			components = 3;
			break;
		case FOSTER_VERTEX_TYPE_FLOAT4:
			type = GL_FLOAT;
			componentSize = 4;
			components = 4;
			break;
		case FOSTER_VERTEX_TYPE_BYTE4:
			type = GL_BYTE;
			componentSize = 1;
			components = 4;
			break;
		case FOSTER_VERTEX_TYPE_UBYTE4:
			type = GL_UNSIGNED_BYTE;
			componentSize = 1;
			components = 4;
			break;
		case FOSTER_VERTEX_TYPE_SHORT2:
			type = GL_SHORT;
			componentSize = 2;
			components = 2;
			break;
		case FOSTER_VERTEX_TYPE_USHORT2:
			type = GL_UNSIGNED_SHORT;
			componentSize = 2;
			components = 2;
			break;
		case FOSTER_VERTEX_TYPE_SHORT4:
			type = GL_SHORT;
			componentSize = 2;
			components = 4;
			break;
		case FOSTER_VERTEX_TYPE_USHORT4:
			type = GL_UNSIGNED_SHORT;
			componentSize = 2;
			components = 4;
			break;
		}

		GLuint location = (GLuint)(element.index);
		fgl.glEnableVertexAttribArray(location);
		fgl.glVertexAttribPointer(location, components, type, element.normalized, format->stride, (void*)ptr);
		fgl.glVertexAttribDivisor(location, divisor);
		ptr += components * componentSize;
	}

	return format->stride;
}

void FosterBindFrameBuffer(FosterTarget_OpenGL* target)
{
	GLenum framebuffer = 0;

	if (target == NULL)
	{
		framebuffer = 0;
		FosterGetSizeInPixels(&fgl.stateFrameBufferWidth, &fgl.stateFrameBufferHeight);
	}
	else
	{
		framebuffer = target->id;
		fgl.stateFrameBufferWidth = target->width;
		fgl.stateFrameBufferHeight = target->height;
	}

	if (fgl.stateInitializing || fgl.stateFrameBuffer != framebuffer)
	{
		GLenum attachments[4];
		fgl.glBindFramebuffer(GL_FRAMEBUFFER, framebuffer);

		// figure out draw buffers
		if (target == NULL)
		{
			attachments[0] = GL_BACK;
			fgl.glDrawBuffers(1, attachments);
		}
		else
		{
			for (int i = 0; i < target->colorAttachmentCount; i ++)
				attachments[i] = GL_COLOR_ATTACHMENT0 + i;
			fgl.glDrawBuffers(target->colorAttachmentCount, attachments);
		}

	}
	fgl.stateFrameBuffer = framebuffer;
}

void FosterBindProgram(GLuint id)
{
	if (fgl.stateInitializing || fgl.stateProgram != id)
		fgl.glUseProgram(id);
	fgl.stateProgram = id;
}

void FosterBindArray(GLuint id)
{
	if (fgl.stateInitializing || fgl.stateVertexArray != id)
		fgl.glBindVertexArray(id);
	fgl.stateVertexArray = id;
}

void FosterBindTexture(int slot, GLuint id)
{
	if (fgl.stateActiveTextureSlot != slot)
	{
		fgl.glActiveTexture(GL_TEXTURE0);
		fgl.stateActiveTextureSlot = slot;
	}

	if (fgl.stateTextureSlots[slot] != id)
		fgl.glBindTexture(GL_TEXTURE_2D, id);
}

// Same as FosterBindTexture, except it the resulting global state doesn't
// necessarily have the slot active or texture bound, if no changes were required.
void FosterEnsureTextureSlotIs(int slot, GLuint id)
{
	if (fgl.stateTextureSlots[slot] != id)
	{
		if (fgl.stateActiveTextureSlot != slot)
		{
			fgl.glActiveTexture(GL_TEXTURE0 + slot);
			fgl.stateActiveTextureSlot = slot;
		}

		fgl.glBindTexture(GL_TEXTURE_2D, id);
	}
}

void FosterSetTextureSampler(FosterTexture_OpenGL* tex, FosterTextureSampler sampler)
{
	if (!tex->disposed && (
		tex->sampler.filter != sampler.filter ||
		tex->sampler.wrapX != sampler.wrapX ||
		tex->sampler.wrapY != sampler.wrapY))
	{
		FosterBindTexture(0, tex->id);

		if (tex->sampler.filter != sampler.filter)
		{
			fgl.glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, FosterFilterToGL(sampler.filter));
			fgl.glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, FosterFilterToGL(sampler.filter));
		}

		if (tex->sampler.wrapX != sampler.wrapX)
			fgl.glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, FosterWrapToGL(sampler.wrapX));

		if (tex->sampler.wrapY != sampler.wrapY)
			fgl.glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, FosterWrapToGL(sampler.wrapY));
		
		tex->sampler = sampler;
	}
}

void FosterTextureReturnReference(FosterTexture_OpenGL* texture)
{
	if (texture != NULL)
	{
		texture->refCount--;
		if (texture->refCount <= 0)
		{
			if (!texture->disposed)
				FosterLogError("Texture is being free'd without deleting its GPU Texture Data");
			SDL_free(texture);
		}
	}
}

FosterTexture_OpenGL* FosterTextureRequestReference(FosterTexture_OpenGL* texture)
{
	if (texture != NULL)
		texture->refCount++;
	return texture;
}

void FosterSetViewport(int enabled, FosterRect rect)
{
	FosterRect viewport;

	if (enabled)
	{
		viewport = rect;
		viewport.y = fgl.stateFrameBufferHeight - viewport.y - viewport.h;
	}
	else
	{
		viewport.x = 0; viewport.y = 0;
		viewport.w = fgl.stateFrameBufferWidth;
		viewport.h = fgl.stateFrameBufferHeight;
	}

	if (fgl.stateInitializing || !FOSTER_RECT_EQUAL(viewport, fgl.stateViewport))
	{
		fgl.glViewport((GLint)viewport.x, (GLint)viewport.y, (GLint)viewport.w, (GLint)viewport.h);
		fgl.stateViewport = viewport;
	}
}

void FosterSetScissor(int enabled, FosterRect rect)
{
	// get input scissor first
	FosterRect scissor = rect;
	scissor.y = fgl.stateFrameBufferHeight - scissor.y - scissor.h;
	if (scissor.w < 0) scissor.w = 0;
	if (scissor.h < 0) scissor.h = 0;

	// toggle scissor
	if (fgl.stateInitializing ||
		enabled != fgl.stateHasScissor ||
		(enabled && !FOSTER_RECT_EQUAL(scissor, fgl.stateScissor)))
	{
		if (enabled)
		{
			if (!fgl.stateHasScissor)
				fgl.glEnable(GL_SCISSOR_TEST);
			fgl.glScissor((GLint)scissor.x, (GLint)scissor.y, (GLint)scissor.w, (GLint)scissor.h);
			fgl.stateScissor = scissor;
		}
		else
		{
			fgl.glDisable(GL_SCISSOR_TEST);
		}

		fgl.stateHasScissor = enabled;
	}
}

void FosterSetBlend(const FosterBlend* blend)
{
	if (fgl.stateInitializing ||
		fgl.stateBlend.colorOp != blend->colorOp ||
		fgl.stateBlend.alphaOp != blend->alphaOp)
	{
		GLenum colorOp = FosterBlendOpToGL(blend->colorOp);
		GLenum alphaOp = FosterBlendOpToGL(blend->alphaOp);
		fgl.glBlendEquationSeparate(colorOp, alphaOp);
	}

	if (fgl.stateInitializing ||
		fgl.stateBlend.colorSrc != blend->colorSrc ||
		fgl.stateBlend.colorDst != blend->colorDst ||
		fgl.stateBlend.alphaSrc != blend->alphaSrc ||
		fgl.stateBlend.alphaDst != blend->alphaDst)
	{
		GLenum colorSrc = FosterBlendFactorToGL(blend->colorSrc);
		GLenum colorDst = FosterBlendFactorToGL(blend->colorDst);
		GLenum alphaSrc = FosterBlendFactorToGL(blend->alphaSrc);
		GLenum alphaDst = FosterBlendFactorToGL(blend->alphaDst);
		fgl.glBlendFuncSeparate(colorSrc, colorDst, alphaSrc, alphaDst);
	}

	if (fgl.stateInitializing || fgl.stateBlend.mask != blend->mask)
	{
		fgl.glColorMask(
			((int)blend->mask & (int)FOSTER_BLEND_MASK_R),
			((int)blend->mask & (int)FOSTER_BLEND_MASK_G),
			((int)blend->mask & (int)FOSTER_BLEND_MASK_B),
			((int)blend->mask & (int)FOSTER_BLEND_MASK_A));
	}

	if (fgl.stateInitializing || fgl.stateBlend.rgba != blend->rgba)
	{
		unsigned char r = blend->rgba >> 24;
		unsigned char g = blend->rgba >> 16;
		unsigned char b = blend->rgba >> 8;
		unsigned char a = blend->rgba;

		fgl.glBlendColor(
			r / 255.0f,
			g / 255.0f,
			b / 255.0f,
			a / 255.0f);
	}

	fgl.stateBlend = *blend;
}

void FosterSetCompare(FosterCompare compare)
{
	if (fgl.stateInitializing || compare != fgl.stateCompare)
	{
		if (compare == FOSTER_COMPARE_NONE)
		{
			fgl.glDisable(GL_DEPTH_TEST);
		}
		else
		{
			if (fgl.stateCompare == FOSTER_COMPARE_NONE)
				fgl.glEnable(GL_DEPTH_TEST);

			switch (compare)
			{
			case FOSTER_COMPARE_NONE: break;
			case FOSTER_COMPARE_ALWAYS: fgl.glDepthFunc(GL_ALWAYS); break;
			case FOSTER_COMPARE_EQUAL: fgl.glDepthFunc(GL_EQUAL); break;
			case FOSTER_COMPARE_GREATER: fgl.glDepthFunc(GL_GREATER); break;
			case FOSTER_COMPARE_GREATOR_OR_EQUAL: fgl.glDepthFunc(GL_GEQUAL); break;
			case FOSTER_COMPARE_LESS: fgl.glDepthFunc(GL_LESS); break;
			case FOSTER_COMPARE_LESS_OR_EQUAL: fgl.glDepthFunc(GL_LEQUAL); break;
			case FOSTER_COMPARE_NEVER: fgl.glDepthFunc(GL_NEVER); break;
			case FOSTER_COMPARE_NOT_EQUAL: fgl.glDepthFunc(GL_NOTEQUAL); break;
			}
		}
	}

	fgl.stateCompare = compare;
}

void FosterSetDepthMask(int depthMask)
{
	if (fgl.stateInitializing || depthMask != fgl.stateDepthMask)
	{
		if (depthMask)
			fgl.glDepthMask(1);
		else
			fgl.glDepthMask(0);
	}

	fgl.stateDepthMask = depthMask;
}

void FosterSetCull(FosterCull cull)
{
	if (fgl.stateInitializing || cull != fgl.stateCull)
	{
		if (cull == FOSTER_CULL_NONE)
		{
			fgl.glDisable(GL_CULL_FACE);
		}
		else
		{
			if (fgl.stateCull == FOSTER_CULL_NONE)
				fgl.glEnable(GL_CULL_FACE);

			switch (cull)
			{
			case FOSTER_CULL_NONE: break;
			case FOSTER_CULL_BACK: fgl.glCullFace(GL_BACK); break;
			case FOSTER_CULL_FRONT: fgl.glCullFace(GL_FRONT); break;
			}
		}
	}

	fgl.stateCull = cull;
}

void FosterPrepare_OpenGL()
{
	FosterState* state = FosterGetState();
	state->windowCreateFlags |= SDL_WINDOW_OPENGL;

	#ifdef __EMSCRIPTEN__
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 0);
	#else
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 3);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 3);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_CORE);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_FLAGS, SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
		SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);

		// TODO:
		// This should be controlled via the gfx api somehow?
		SDL_GL_SetAttribute(SDL_GL_DEPTH_SIZE, 24);
		SDL_GL_SetAttribute(SDL_GL_STENCIL_SIZE, 8);
		SDL_GL_SetAttribute(SDL_GL_MULTISAMPLEBUFFERS, 1);
		SDL_GL_SetAttribute(SDL_GL_MULTISAMPLESAMPLES, 4);
	#endif
}

bool FosterInitialize_OpenGL()
{
	FosterState* state = FosterGetState();

	// create gl context
	fgl.context = SDL_GL_CreateContext(state->window);
	if (fgl.context == NULL)
	{
		FosterLogError("Failed to create OpenGL Context: %s", SDL_GetError());
		return false;
	}
	SDL_GL_MakeCurrent(state->window, fgl.context);

	// bind opengl functions
	#define GL_FUNC(name, ...) fgl.gl ## name = (gl ## name ## Fn)(SDL_GL_GetProcAddress("gl" #name));
	GL_FUNCTIONS
	#undef GL_FUNC

	// bind debug message callback
	if (fgl.glDebugMessageCallback != NULL && state->desc.logging != FOSTER_LOGGING_NONE)
	{
		fgl.glEnable(GL_DEBUG_OUTPUT);
		fgl.glEnable(GL_DEBUG_OUTPUT_SYNCHRONOUS);
		fgl.glDebugMessageCallback(FosterMessage_OpenGL, NULL);
	}

	// get opengl info
	fgl.glGetIntegerv(0x8CDF, &fgl.max_color_attachments);
	fgl.glGetIntegerv(0x80E9, &fgl.max_element_indices);
	fgl.glGetIntegerv(0x80E8, &fgl.max_element_vertices);
	fgl.glGetIntegerv(0x84E8, &fgl.max_renderbuffer_size);
	fgl.glGetIntegerv(0x8D57, &fgl.max_samples);
	fgl.glGetIntegerv(0x8872, &fgl.max_texture_image_units);
	fgl.glGetIntegerv(0x0D33, &fgl.max_texture_size);

	// don't include row padding
	fgl.glPixelStorei(GL_PACK_ALIGNMENT, 1);
	fgl.glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

	// assign info
	// info.type = RendererType::OpenGL;
	// info.instancing = true;
	// info.origin_bottom_left = true;
	// info.max_texture_size = max_texture_size;

	// Blend State is always enabled
	fgl.glEnable(GL_BLEND);

	// set default starting state
	fgl.stateInitializing = 1;
	FosterRect zeroRect = { 0 };
	FosterBlend zeroBlend = { 0 };
	FosterBindProgram(0);
	FosterBindFrameBuffer(NULL);
	FosterBindArray(0);
	FosterSetViewport(0, zeroRect);
	FosterSetScissor(0, zeroRect);
	FosterSetBlend(&zeroBlend);
	FosterSetCull(FOSTER_CULL_NONE);
	FosterSetCompare(FOSTER_COMPARE_NONE);
	FosterSetDepthMask(0);
	fgl.stateInitializing = 0;

	// zero out texture state
	fgl.stateActiveTextureSlot = 0;
	fgl.glActiveTexture(GL_TEXTURE0);
	for (int i = 0; i < FOSTER_MAX_UNIFORM_TEXTURES; i ++)
		fgl.stateTextureSlots[i] = 0;

	// log
	FosterLogInfo("OpenGL: v%s, %s", fgl.glGetString(GL_VERSION), fgl.glGetString(GL_RENDERER));
	return true;
}

void FosterShutdown_OpenGL()
{
	SDL_GL_DeleteContext(fgl.context);
	fgl.context = NULL;
}

void FosterFrameBegin_OpenGL()
{

}

void FosterFrameEnd_OpenGL()
{
	FosterState* state = FosterGetState();

	// bind 0 to the frame buffer as per SDL's suggestion for macOS:
	// https://wiki.libsdl.org/SDL2/SDL_GL_SwapWindow#remarks
	FosterBindFrameBuffer(NULL);

	SDL_GL_SwapWindow(state->window);
}

FosterTexture* FosterTextureCreate_OpenGL(int width, int height, FosterTextureFormat format)
{
	FosterTexture_OpenGL result;
	FosterTexture_OpenGL* tex = NULL;

	result.id = 0;
	result.width = width;
	result.height = height;
	result.format = format;
	result.glInternalFormat = GL_RED;
	result.glFormat = GL_RED;
	result.glType = GL_UNSIGNED_BYTE;
	result.refCount = 1;
	result.disposed = 0;
	result.sampler.filter = -1;
	result.sampler.wrapX = -1;
	result.sampler.wrapY = -1;

	if (width > fgl.max_texture_size || height > fgl.max_texture_size)
	{
		FosterLogError("Exceeded Max Texture Size of %i", fgl.max_texture_size);
		return NULL;
	}

	switch (format)
	{
	case FOSTER_TEXTURE_FORMAT_R8:
		result.glInternalFormat = GL_RED;
		result.glFormat = GL_RED;
		result.glType = GL_UNSIGNED_BYTE;
		break;
	case FOSTER_TEXTURE_FORMAT_R8G8B8A8:
		result.glInternalFormat = GL_RGBA;
		result.glFormat = GL_RGBA;
		result.glType = GL_UNSIGNED_BYTE;
		break;
	case FOSTER_TEXTURE_FORMAT_DEPTH24_STENCIL8:
		result.glInternalFormat = GL_DEPTH24_STENCIL8;
		result.glFormat = GL_DEPTH_STENCIL;
		result.glType = GL_UNSIGNED_INT_24_8;
		break;
	default:
		FosterLogError("Invalid Texture Format (%i)", format);
		return NULL;
	}

	fgl.glGenTextures(1, &result.id);
	if (result.id == 0)
	{
		FosterLogError("Failed to create Texture");
		return NULL;
	}

	FosterBindTexture(0, result.id);
	fgl.glTexImage2D(GL_TEXTURE_2D, 0, result.glInternalFormat, width, height, 0, result.glFormat, result.glType, NULL);

	tex = (FosterTexture_OpenGL*)SDL_malloc(sizeof(FosterTexture_OpenGL));
	*tex = result;
	return (FosterTexture*)tex;
}

void FosterTextureSetData_OpenGL(FosterTexture* texture, void* data, int length)
{
	FosterTexture_OpenGL* tex = (FosterTexture_OpenGL*)texture;
	FosterBindTexture(0, tex->id);
	fgl.glTexImage2D(GL_TEXTURE_2D, 0, tex->glInternalFormat, tex->width, tex->height, 0, tex->glFormat, tex->glType, data);
}

void FosterTextureGetData_OpenGL(FosterTexture* texture, void* data, int length)
{
	FosterTexture_OpenGL* tex = (FosterTexture_OpenGL*)texture;
	FosterBindTexture(0, tex->id);
	fgl.glGetTexImage(GL_TEXTURE_2D, 0, tex->glInternalFormat, tex->glType, data);
}

void FosterTextureDestroy_OpenGL(FosterTexture* texture)
{
	FosterTexture_OpenGL* tex = (FosterTexture_OpenGL*)texture;

	if (!tex->disposed)
	{
		tex->disposed = 1;
		fgl.glDeleteTextures(1, &tex->id);
		FosterTextureReturnReference(tex);
	}
}

FosterTarget* FosterTargetCreate_OpenGL(int width, int height, FosterTextureFormat* attachments, int attachmentCount)
{
	FosterTarget_OpenGL result;
	result.id = 0;
	result.width = width;
	result.height = height;
	result.attachmentCount = attachmentCount;
	result.colorAttachmentCount = 0;
	for (int i = 0; i < FOSTER_MAX_TARGET_ATTACHMENTS; i ++)
		result.attachments[i] = NULL;

	fgl.glGenFramebuffers(1, &result.id);
	fgl.glBindFramebuffer(GL_FRAMEBUFFER, result.id);

	for (int i = 0; i < attachmentCount; i++)
	{
		FosterTexture_OpenGL* tex = (FosterTexture_OpenGL*)FosterTextureCreate_OpenGL(width, height, attachments[i]);

		if (tex == NULL)
		{
			for (int j = 0; j < i; j ++)
				FosterTextureDestroy_OpenGL((FosterTexture*)tex);
			FosterLogError("Failed to create Target Attachment");
			FosterBindFrameBuffer(NULL);
			return NULL;
		}

		result.attachments[i] = tex;

		if (attachments[i] == FOSTER_TEXTURE_FORMAT_DEPTH24_STENCIL8)
		{
			tex->glAttachment = GL_DEPTH_STENCIL_ATTACHMENT;
		}
		else
		{
			tex->glAttachment = GL_COLOR_ATTACHMENT0 + result.colorAttachmentCount;
			result.colorAttachmentCount++;
		}

		fgl.glFramebufferTexture2D(GL_FRAMEBUFFER, tex->glAttachment, GL_TEXTURE_2D, tex->id, 0);
	}

	// since we manually set the framebuffer above, clear buffer assignment to maintain correct state
	FosterBindFrameBuffer(NULL);

	// create result
	FosterTarget_OpenGL* tar = (FosterTarget_OpenGL*)SDL_malloc(sizeof(FosterTarget_OpenGL));
	*tar = result;
	return (FosterTarget*)tar;
}

FosterTexture* FosterTargetGetAttachment_OpenGL(FosterTarget* target, int index)
{
	FosterTarget_OpenGL* tar = (FosterTarget_OpenGL*)target;
	return (FosterTexture*)tar->attachments[index];
}

void FosterTargetDestroy_OpenGL(FosterTarget* target)
{
	FosterTarget_OpenGL* tar = (FosterTarget_OpenGL*)target;

	for (int i = 0; i < FOSTER_MAX_TARGET_ATTACHMENTS; i ++)
	{
		if (tar->attachments[i] != NULL)
			FosterTextureDestroy_OpenGL((FosterTexture*)tar->attachments[i]);
	}

	fgl.glDeleteFramebuffers(1, &tar->id);
	SDL_free(tar);
}

FosterShader* FosterShaderCreate_OpenGL(FosterShaderData* data)
{
	GLchar log[1024] = { 0 };
	GLsizei logLength = 0;
	GLuint vertexShader;
	GLuint fragmentShader;
	const GLchar* source;

	if (data->vertexShader == NULL)
	{
		FosterLogError("Invalid Vertex Shader");
		return NULL;
	}

	if (data->fragmentShader == NULL)
	{
		FosterLogError("Invalid Fragment Shader");
		return NULL;
	}

	vertexShader = fgl.glCreateShader(GL_VERTEX_SHADER);
	{
		source = (const GLchar*)data->vertexShader;
		fgl.glShaderSource(vertexShader, 1, &source, NULL);
		fgl.glCompileShader(vertexShader);
		fgl.glGetShaderInfoLog(vertexShader, 1024, &logLength, log);

		GLint params;
		fgl.glGetShaderiv(vertexShader, GL_COMPILE_STATUS, &params);

		// validate shader
		if (!params)
		{
			fgl.glDeleteShader(vertexShader);
			if (logLength > 0)
				FosterLogError("%s", log);
			return NULL;
		}
		else if (logLength > 0)
		{
			FosterLogInfo("%s", log);
		}
	}

	fragmentShader = fgl.glCreateShader(GL_FRAGMENT_SHADER);
	{
		source = (const GLchar*)data->fragmentShader;
		fgl.glShaderSource(fragmentShader, 1, &source, NULL);
		fgl.glCompileShader(fragmentShader);
		fgl.glGetShaderInfoLog(fragmentShader, 1024, &logLength, log);

		GLint params;
		fgl.glGetShaderiv(fragmentShader, GL_COMPILE_STATUS, &params);

		// validate shader
		if (!params)
		{
			fgl.glDeleteShader(vertexShader);
			fgl.glDeleteShader(fragmentShader);
			if (logLength > 0)
				FosterLogError("%s", log);
			return NULL;
		}
		else if (logLength > 0)
		{
			FosterLogInfo("%s", log);
		}
	}

	// create actual shader program
	GLuint id = fgl.glCreateProgram();
	fgl.glAttachShader(id, vertexShader);
	fgl.glAttachShader(id, fragmentShader);
	fgl.glLinkProgram(id);
	fgl.glGetProgramInfoLog(id, 1024, &logLength, log);
	fgl.glDetachShader(id, vertexShader);
	fgl.glDetachShader(id, fragmentShader);
	fgl.glDeleteShader(vertexShader);
	fgl.glDeleteShader(fragmentShader);

	// validate link status
	GLint linkResult;
	fgl.glGetProgramiv(id, GL_LINK_STATUS, &linkResult);

	if (!linkResult)
	{
		if (logLength > 0)
			FosterLogError("%s", log);
		return NULL;
	}
	else if (logLength > 0)
	{
		FosterLogInfo("%s", log);
	}

	FosterShader_OpenGL* shader = (FosterShader_OpenGL*)SDL_malloc(sizeof(FosterShader_OpenGL));
	shader->id = id;
	shader->samplerCount = 0;
	shader->uniformCount = 0;
	shader->uniforms = NULL;

	for (int i = 0; i < FOSTER_MAX_UNIFORM_TEXTURES; i ++)
	{
		shader->textures[i] = NULL;
		shader->samplers[i].filter = FOSTER_TEXTURE_FILTER_LINEAR;
		shader->samplers[i].wrapX = FOSTER_TEXTURE_WRAP_CLAMP_TO_EDGE;
		shader->samplers[i].wrapY = FOSTER_TEXTURE_WRAP_CLAMP_TO_EDGE;
	}

	// query uniforms and cache them
	fgl.glGetProgramiv(id, GL_ACTIVE_UNIFORMS, &shader->uniformCount);

	if (shader->uniformCount > 0)
	{
		shader->uniforms = (FosterUniform_OpenGL*)SDL_malloc(sizeof(FosterUniform_OpenGL) * shader->uniformCount);

		for (int i = 0; i < shader->uniformCount; i++)
		{
			FosterUniform_OpenGL* uniform = shader->uniforms + i;
			uniform->glLocation = 0;
			uniform->glSize = 0;
			uniform->glType = 0;
			uniform->name = NULL;
			uniform->samplerName = NULL;
			uniform->samplerIndex = 0;

			// get the name & properties
			GLsizei nameLen;
			char    nameBuf[256];
			fgl.glGetActiveUniform(id, i, 255, &nameLen, &uniform->glSize, &uniform->glType, nameBuf);

			// array names end with "[0]", and we don't want that
			for (int n = 0; n < nameLen - 2; n++)
				if (nameBuf[n] == '[' && nameBuf[n + 1] == '0' && nameBuf[n + 2] == ']')
				{
					nameBuf[n] = '\0';
					nameLen -= 3;
					break;
				}

			// allocate enough room for the name
			uniform->name = (char*)SDL_malloc(nameLen + 8);
			SDL_strlcpy(uniform->name, nameBuf, nameLen + 1);
			
			// get GL location
			uniform->glLocation = fgl.glGetUniformLocation(id, uniform->name);

			// if we're a sampler we need a unique sampler name + track what sampler index
			if (uniform->glType == GL_SAMPLER_2D)
			{
				uniform->samplerName = (char*)SDL_malloc(nameLen + 16);
				SDL_snprintf(uniform->samplerName, nameLen + 16, "%s_sampler", uniform->name);
				uniform->samplerIndex = shader->samplerCount;
				shader->samplerCount += uniform->glSize;
			}
		}
	}

	return (FosterShader*)shader;
}

void FosterShaderGetUniforms_OpenGL(FosterShader* shader, FosterUniformInfo* output, int* count, int max)
{
	FosterShader_OpenGL* it = (FosterShader_OpenGL*)shader;

	int t = 0;

	for (int i = 0; t < max && i < it->uniformCount; i ++)
	{
		FosterUniform_OpenGL* uniform = it->uniforms + i;

		// OpenGL doesn't have separate Sampler's and Texture's...
		// So we create an "extra" uniform and add a "_sampler" suffix
		if (uniform->glType == GL_SAMPLER_2D)
		{
			output[t].index = i;
			output[t].name = uniform->name;
			output[t].type = FOSTER_UNIFORM_TYPE_TEXTURE2D;
			output[t].arrayElements = uniform->glSize;
			t++;

			output[t].index = i;
			output[t].name = uniform->samplerName;
			output[t].type = FOSTER_UNIFORM_TYPE_SAMPLER2D;
			output[t].arrayElements = uniform->glSize;
			t++;
		}
		else
		{
			output[t].index = i;
			output[t].name = uniform->name;
			output[t].type = FosterUniformTypeFromGL(uniform->glType);
			output[t].arrayElements = uniform->glSize;
			t++;
		}
	}

	*count = t;
}

void FosterShaderSetUniform_OpenGL(FosterShader* shader, int index, float* values)
{
	FosterShader_OpenGL* it = (FosterShader_OpenGL*)shader;

	if (index < 0 || index > it->uniformCount)
	{
		FosterLogError("Failed to set uniform '%i': index out of bounds");
		return;
	}

	FosterBindProgram(it->id);

	FosterUniform_OpenGL* uniform = it->uniforms + index;

	switch (uniform->glType)
	{
	case GL_FLOAT:
		fgl.glUniform1fv(uniform->glLocation, (GLint)uniform->glSize, values);
		return;
	case GL_FLOAT_VEC2:
		fgl.glUniform2fv(uniform->glLocation, (GLint)uniform->glSize, values);
		return;
	case GL_FLOAT_VEC3:
		fgl.glUniform3fv(uniform->glLocation, (GLint)uniform->glSize, values);
		return;
	case GL_FLOAT_VEC4:
		fgl.glUniform4fv(uniform->glLocation, (GLint)uniform->glSize, values);
		return;
	case GL_FLOAT_MAT3x2:
		fgl.glUniformMatrix3x2fv(uniform->glLocation, (GLint)uniform->glSize, 0, values);
		return;
	case GL_FLOAT_MAT4:
		fgl.glUniformMatrix4fv(uniform->glLocation, (GLint)uniform->glSize, 0, values);
		return;
	}

	FosterLogError("Failed to set uniform '%s', unsupported type '%i'", uniform->name, uniform->glType);
}

void FosterShaderSetTexture_OpenGL(FosterShader* shader, int index, FosterTexture** values)
{
	FosterShader_OpenGL* it = (FosterShader_OpenGL*)shader;

	if (index < 0 || index > it->uniformCount)
	{
		FosterLogError("Failed to set uniform '%i': index out of bounds", index);
		return;
	}

	FosterUniform_OpenGL* uniform = it->uniforms + index;
	if (uniform->glType != GL_SAMPLER_2D)
	{
		FosterLogError("Failed to set uniform '%s': not a Texture", uniform->name);
		return;
	}

	for (int i = 0; i < uniform->glSize && uniform->samplerIndex + i < FOSTER_MAX_UNIFORM_TEXTURES; i ++)
	{
		int index = uniform->samplerIndex + i;
		FosterTextureReturnReference(it->textures[index]);
		it->textures[index] = FosterTextureRequestReference((FosterTexture_OpenGL*)values[i]);
	}
}

void FosterShaderSetSampler_OpenGL(FosterShader* shader, int index, FosterTextureSampler* values)
{
	FosterShader_OpenGL* it = (FosterShader_OpenGL*)shader;

	if (index < 0 || index > it->uniformCount)
	{
		FosterLogError("Failed to set uniform '%i': index out of bounds", index);
		return;
	}

	FosterUniform_OpenGL* uniform = it->uniforms + index;
	if (uniform->glType != GL_SAMPLER_2D)
	{
		FosterLogError("Failed to set uniform '%s': not a Sampler", uniform->name);
		return;
	}

	for (int i = 0; i < uniform->glSize && uniform->samplerIndex + i < FOSTER_MAX_UNIFORM_TEXTURES; i ++)
		it->samplers[uniform->samplerIndex + i] = values[i];
}

void FosterShaderDestroy_OpenGL(FosterShader* shader)
{
	FosterShader_OpenGL* it = (FosterShader_OpenGL*)shader;
	fgl.glDeleteProgram(it->id);

	for (int i = 0; i < FOSTER_MAX_UNIFORM_TEXTURES; i ++)
		FosterTextureReturnReference(it->textures[i]);

	for (int i = 0; i < it->uniformCount; i ++)
	{
		SDL_free(it->uniforms[i].name);
		SDL_free(it->uniforms[i].samplerName);
	}

	SDL_free(it->uniforms);
	SDL_free(it);
}

FosterMesh* FosterMeshCreate_OpenGL()
{
	FosterMesh_OpenGL result;
	result.id = 0;
	result.indexBuffer = 0;
	result.vertexBuffer = 0;
	result.instanceBuffer = 0;
	result.vertexAttributesEnabled = 0;
	result.instanceAttributesEnabled = 0;
	result.vertexBufferSize = 0;
	result.indexBufferSize = 0;

	fgl.glGenVertexArrays(1, &result.id);
	if (result.id == 0)
	{
		FosterLogError("%s", "Failed to create Mesh");
		return NULL;
	}

	FosterMesh_OpenGL* mesh = (FosterMesh_OpenGL*)SDL_malloc(sizeof(FosterMesh_OpenGL));
	*mesh = result;
	return (FosterMesh*)mesh;
}

void FosterMeshSetVertexFormat_OpenGL(FosterMesh* mesh, FosterVertexFormat* format)
{
	FosterMesh_OpenGL* it = (FosterMesh_OpenGL*)mesh;
	FosterBindArray(it->id);

	if (it->vertexBuffer == 0)
		fgl.glGenBuffers(1, &(it->vertexBuffer));
	FosterMeshAssignAttributes_OpenGL(it->vertexBuffer, GL_ARRAY_BUFFER, format, 0);
}

void FosterMeshSetVertexData_OpenGL(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset)
{
	FosterMesh_OpenGL* it = (FosterMesh_OpenGL*)mesh;
	FosterBindArray(it->id);

	if (it->vertexBuffer == 0)
		fgl.glGenBuffers(1, &(it->vertexBuffer));
	fgl.glBindBuffer(GL_ARRAY_BUFFER, it->vertexBuffer);

	// expand vertex buffer if needed
	int totalSize = dataDestOffset + dataSize;
	if (totalSize > it->vertexBufferSize)
	{
		it->vertexBufferSize = totalSize;
		fgl.glBufferData(GL_ARRAY_BUFFER, totalSize, NULL, GL_DYNAMIC_DRAW);
	}

	// fill data at the offset
	fgl.glBufferSubData(GL_ARRAY_BUFFER, dataDestOffset, dataSize, data);
}

void FosterMeshSetIndexFormat_OpenGL(FosterMesh* mesh, FosterIndexFormat format)
{
	FosterMesh_OpenGL* it = (FosterMesh_OpenGL*)mesh;

	switch (format)
	{
	case FOSTER_INDEX_FORMAT_SIXTEEN:
		it->indexFormat = GL_UNSIGNED_SHORT;
		it->indexSize = 2;
		break;
	case FOSTER_INDEX_FORMAT_THIRTY_TWO:
		it->indexFormat = GL_UNSIGNED_INT;
		it->indexSize = 4;
		break;
	default:
		FosterLogError("Invalid Index Format '%i'", format);
		break;
	}
}

void FosterMeshSetIndexData_OpenGL(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset)
{
	FosterMesh_OpenGL* it = (FosterMesh_OpenGL*)mesh;
	FosterBindArray(it->id);

	if (it->indexBuffer == 0)
		fgl.glGenBuffers(1, &(it->indexBuffer));
	fgl.glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, it->indexBuffer);

	// expand buffer if needed
	int totalSize = dataDestOffset + dataSize;
	if (totalSize > it->indexBufferSize)
	{
		it->indexBufferSize = totalSize;
		fgl.glBufferData(GL_ELEMENT_ARRAY_BUFFER, totalSize, NULL, GL_DYNAMIC_DRAW);
	}

	// fill data from the offset
	fgl.glBufferSubData(GL_ELEMENT_ARRAY_BUFFER, dataDestOffset, dataSize, data);
}

void FosterMeshDestroy_OpenGL(FosterMesh* mesh)
{
	FosterMesh_OpenGL* it = (FosterMesh_OpenGL*)mesh;

	if (it->vertexBuffer != 0)
		fgl.glDeleteBuffers(1, &it->vertexBuffer);
	if (it->indexBuffer != 0)
		fgl.glDeleteBuffers(1, &it->indexBuffer);
	if (it->instanceBuffer != 0)
		fgl.glDeleteBuffers(1, &it->instanceBuffer);
	if (it->id != 0)
		fgl.glDeleteVertexArrays(1, &it->id);

	SDL_free(it);
}

void FosterDraw_OpenGL(FosterDrawCommand* command)
{
	FosterTarget_OpenGL* target = (FosterTarget_OpenGL*)command->target;
	FosterShader_OpenGL* shader = (FosterShader_OpenGL*)command->shader;
	FosterMesh_OpenGL* mesh = (FosterMesh_OpenGL*)command->mesh;

	// Set State
	FosterBindFrameBuffer(target);
	FosterBindProgram(shader->id);
	FosterBindArray(mesh->id);
	FosterSetBlend(&command->blend);
	FosterSetCompare(command->compare);
	FosterSetDepthMask(command->depthMask);
	FosterSetCull(command->cull);
	FosterSetViewport(command->hasViewport, command->viewport);
	FosterSetScissor(command->hasScissor, command->scissor);

	// Update Texture Uniforms & Samplers
	{
		GLuint textureSlots[FOSTER_MAX_UNIFORM_TEXTURES];

		// update samplers
		for (int i = 0; i < FOSTER_MAX_UNIFORM_TEXTURES; i ++)
		{
			FosterTexture_OpenGL* tex = shader->textures[i];
			if (shader->textures[i] != NULL)
				FosterSetTextureSampler(shader->textures[i], shader->samplers[i]);
		}

		// bind textures
		int slot = 0;
		for (int i = 0; i < shader->uniformCount; i ++)
		{
			FosterUniform_OpenGL* uniform = shader->uniforms + i;
			if (uniform->glType != GL_SAMPLER_2D)
				continue;

			// bind textures & update sampler state
			for (int n = 0; n < uniform->glSize && slot < FOSTER_MAX_UNIFORM_TEXTURES; n ++)
			{
				FosterTexture_OpenGL* tex = shader->textures[uniform->samplerIndex + n];

				if (tex != NULL && !tex->disposed)
				{
					FosterEnsureTextureSlotIs(slot, tex->id);
					textureSlots[n] = slot;
					slot++;
				}
				else
				{
					textureSlots[n] = 0;
				}
			}

			// bind texture slots for this uniform
			fgl.glUniform1iv(uniform->glLocation, (GLint)uniform->glSize, textureSlots);
		}
	}

	// Draw the Mesh
	{
		int64_t indexStartPtr = mesh->indexSize * command->indexStart;

		if (command->instanceCount > 0)
		{
			fgl.glDrawElementsInstanced(
				GL_TRIANGLES,
				(GLint)(command->indexCount),
				mesh->indexFormat,
				(void*)indexStartPtr,
				(GLint)command->instanceCount);
		}
		else
		{
			fgl.glDrawElements(
				GL_TRIANGLES,
				(GLint)(command->indexCount),
				mesh->indexFormat,
				(void*)indexStartPtr);
		}
	}
}

void FosterClear_OpenGL(FosterClearCommand* command)
{
	FosterBindFrameBuffer((FosterTarget_OpenGL*)command->target);
	FosterSetViewport(1, command->clip);
	FosterSetScissor(0, fgl.stateScissor);

	int clear = 0;

	if ((command->mask & FOSTER_CLEAR_MASK_COLOR) == FOSTER_CLEAR_MASK_COLOR)
	{
		clear |= GL_COLOR_BUFFER_BIT;
		fgl.glColorMask(true, true, true, true);
		fgl.glClearColor(command->color.r / 255.0f, command->color.g / 255.0f, command->color.b / 255.0f, command->color.a / 255.0f);
	}
		
	if ((command->mask & FOSTER_CLEAR_MASK_DEPTH) == FOSTER_CLEAR_MASK_DEPTH)
	{
		FosterSetDepthMask(1);

		clear |= GL_DEPTH_BUFFER_BIT;
		if (fgl.glClearDepth)
			fgl.glClearDepth(command->depth);
	}

	if ((command->mask & FOSTER_CLEAR_MASK_STENCIL) == FOSTER_CLEAR_MASK_STENCIL)
	{
		clear |= GL_STENCIL_BUFFER_BIT;
		if (fgl.glClearStencil)
			fgl.glClearStencil(command->stencil);
	}

	fgl.glClear(clear);
}

bool FosterGetDevice_OpenGL(FosterRenderDevice* device)
{
	device->renderer = FOSTER_RENDERER_OPENGL;
	device->prepare = FosterPrepare_OpenGL;
	device->initialize = FosterInitialize_OpenGL;
	device->shutdown = FosterShutdown_OpenGL;
	device->frameBegin = FosterFrameBegin_OpenGL;
	device->frameEnd = FosterFrameEnd_OpenGL;
	device->textureCreate = FosterTextureCreate_OpenGL;
	device->textureSetData = FosterTextureSetData_OpenGL;
	device->textureGetData = FosterTextureGetData_OpenGL;
	device->textureDestroy = FosterTextureDestroy_OpenGL;
	device->targetCreate = FosterTargetCreate_OpenGL;
	device->targetGetAttachment = FosterTargetGetAttachment_OpenGL;
	device->targetDestroy = FosterTargetDestroy_OpenGL;
	device->shaderCreate = FosterShaderCreate_OpenGL;
	device->shaderSetUniform = FosterShaderSetUniform_OpenGL;
	device->shaderSetTexture = FosterShaderSetTexture_OpenGL;
	device->shaderSetSampler = FosterShaderSetSampler_OpenGL;
	device->shaderGetUniforms = FosterShaderGetUniforms_OpenGL;
	device->shaderDestroy = FosterShaderDestroy_OpenGL;
	device->meshCreate = FosterMeshCreate_OpenGL;
	device->meshSetVertexFormat = FosterMeshSetVertexFormat_OpenGL;
	device->meshSetVertexData = FosterMeshSetVertexData_OpenGL;
	device->meshSetIndexFormat = FosterMeshSetIndexFormat_OpenGL;
	device->meshSetIndexData = FosterMeshSetIndexData_OpenGL;
	device->meshDestroy = FosterMeshDestroy_OpenGL;
	device->draw = FosterDraw_OpenGL;
	device->clear = FosterClear_OpenGL;
	return true;
}

#else // FOSTER_OPENGL_ENABLED

#include "foster_renderer.h"

bool FosterGetDevice_OpenGL(FosterRenderDevice* device)
{
	device->renderer = FOSTER_RENDERER_OPENGL;
	return false;
}

#endif


