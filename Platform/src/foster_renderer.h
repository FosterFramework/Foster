#ifndef FOSTER_RENDERER_H
#define FOSTER_RENDERER_H

#include "foster_platform.h"
#include <stdbool.h>

typedef struct FosterRenderDevice
{
	FosterRenderers renderer;

	void (*prepare)();
	bool (*initialize)();
	void (*shutdown)();
	void (*frameBegin)();
	void (*frameEnd)();
	
	FosterTexture* (*textureCreate)(int width, int height, FosterTextureFormat format);
	void (*textureSetData)(FosterTexture* texture, void* data, int length);
	void (*textureGetData)(FosterTexture* texture, void* data, int length);
	void (*textureDestroy)(FosterTexture* texture);

	FosterTarget* (*targetCreate)(int width, int height, FosterTextureFormat* formats, int format_count);
	FosterTexture* (*targetGetAttachment)(FosterTarget* target, int index);
	void (*targetDestroy)(FosterTarget* target);

	FosterShader* (*shaderCreate)(FosterShaderData* data);
	void (*shaderSetUniform)(FosterShader* shader, int index, float* values);
	void (*shaderSetTexture)(FosterShader* shader, int index, FosterTexture** values);
	void (*shaderSetSampler)(FosterShader* shader, int index, FosterTextureSampler* values);
	void (*shaderGetUniforms)(FosterShader* shader, FosterUniformInfo* output, int* count, int max);
	void (*shaderDestroy)(FosterShader* shader);

	FosterMesh* (*meshCreate)();
	void (*meshSetVertexFormat)(FosterMesh* mesh, FosterVertexFormat* format);
	void (*meshSetVertexData)(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset);
	void (*meshSetIndexFormat)(FosterMesh* mesh, FosterIndexFormat format);
	void (*meshSetIndexData)(FosterMesh* mesh, void* data, int dataSize, int dataDestOffset);
	void (*meshDestroy)(FosterMesh* mesh);

	void (*draw)(FosterDrawCommand* command);
	void (*clear)(FosterClearCommand* clear);
} FosterRenderDevice;

bool FosterGetDevice(FosterRenderers preferred, FosterRenderDevice* device);
bool FosterGetDevice_D3D11(FosterRenderDevice* device);
bool FosterGetDevice_OpenGL(FosterRenderDevice* device);

#endif
