using System.Runtime.InteropServices;

using SDL_WindowPtr = nint;
using SDL_GpuDevicePtr = nint;
using SDL_GpuBufferPtr = nint;
using SDL_GpuTransferBufferPtr = nint;
using SDL_GpuTexturePtr = nint;
using SDL_GpuSamplerPtr = nint;
using SDL_GpuShaderPtr = nint;
using SDL_GpuComputePipelinePtr = nint;
using SDL_GpuGraphicsPipelinePtr = nint;
using SDL_GpuCommandBufferPtr = nint;
using SDL_GpuRenderPassPtr = nint;
using SDL_GpuComputePassPtr = nint;
using SDL_GpuCopyPassPtr = nint;
using SDL_GpuFencePtr = nint;

using SDL_bool = System.Int32;

namespace Foster.Framework;

internal static partial class SDL3
{
	public enum SDL_GpuPrimitiveType
	{
		SDL_GPU_PRIMITIVETYPE_POINTLIST,
		SDL_GPU_PRIMITIVETYPE_LINELIST,
		SDL_GPU_PRIMITIVETYPE_LINESTRIP,
		SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
		SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP
	}

	public enum SDL_GpuLoadOp
	{
		SDL_GPU_LOADOP_LOAD,
		SDL_GPU_LOADOP_CLEAR,
		SDL_GPU_LOADOP_DONT_CARE
	}

	public enum SDL_GpuStoreOp
	{
		SDL_GPU_STOREOP_STORE,
		SDL_GPU_STOREOP_DONT_CARE
	}

	public enum SDL_GpuIndexElementSize
	{
		SDL_GPU_INDEXELEMENTSIZE_16BIT,
		SDL_GPU_INDEXELEMENTSIZE_32BIT
	}

	public enum SDL_GpuTextureFormat
	{
		SDL_GPU_TEXTUREFORMAT_INVALID = -1,

		/* Unsigned Normalized Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8,
		SDL_GPU_TEXTUREFORMAT_B8G8R8A8,
		SDL_GPU_TEXTUREFORMAT_B5G6R5,
		SDL_GPU_TEXTUREFORMAT_B5G5R5A1,
		SDL_GPU_TEXTUREFORMAT_B4G4R4A4,
		SDL_GPU_TEXTUREFORMAT_R10G10B10A2,
		SDL_GPU_TEXTUREFORMAT_R16G16,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16,
		SDL_GPU_TEXTUREFORMAT_R8,
		SDL_GPU_TEXTUREFORMAT_A8,
		/* Compressed Unsigned Normalized Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_BC1,
		SDL_GPU_TEXTUREFORMAT_BC2,
		SDL_GPU_TEXTUREFORMAT_BC3,
		SDL_GPU_TEXTUREFORMAT_BC7,
		/* Signed Normalized Float Color Formats  */
		SDL_GPU_TEXTUREFORMAT_R8G8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM,
		/* Signed Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_R16_SFLOAT,
		SDL_GPU_TEXTUREFORMAT_R16G16_SFLOAT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SFLOAT,
		SDL_GPU_TEXTUREFORMAT_R32_SFLOAT,
		SDL_GPU_TEXTUREFORMAT_R32G32_SFLOAT,
		SDL_GPU_TEXTUREFORMAT_R32G32B32A32_SFLOAT,
		/* Unsigned Integer Color Formats */
		SDL_GPU_TEXTUREFORMAT_R8_UINT,
		SDL_GPU_TEXTUREFORMAT_R8G8_UINT,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT,
		SDL_GPU_TEXTUREFORMAT_R16_UINT,
		SDL_GPU_TEXTUREFORMAT_R16G16_UINT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT,
		/* SRGB Color Formats */
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SRGB,
		SDL_GPU_TEXTUREFORMAT_B8G8R8A8_SRGB,
		/* Compressed SRGB Color Formats */
		SDL_GPU_TEXTUREFORMAT_BC3_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC7_SRGB,
		/* Depth Formats */
		SDL_GPU_TEXTUREFORMAT_D16_UNORM,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM,
		SDL_GPU_TEXTUREFORMAT_D32_SFLOAT,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
		SDL_GPU_TEXTUREFORMAT_D32_SFLOAT_S8_UINT
	}

	[Flags]
	public enum SDL_GpuTextureUsageFlagBits
	{
		SDL_GPU_TEXTUREUSAGE_SAMPLER_BIT = 0x00000001,
		SDL_GPU_TEXTUREUSAGE_COLOR_TARGET_BIT = 0x00000002,
		SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET_BIT = 0x00000004,
		SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ_BIT = 0x00000008,
		SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ_BIT = 0x00000020,
		SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE_BIT = 0x00000040
	}

	public enum SDL_GpuTextureType
	{
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTURETYPE_3D,
		SDL_GPU_TEXTURETYPE_CUBE,
	}

	public enum SDL_GpuSampleCount
	{
		SDL_GPU_SAMPLECOUNT_1,
		SDL_GPU_SAMPLECOUNT_2,
		SDL_GPU_SAMPLECOUNT_4,
		SDL_GPU_SAMPLECOUNT_8
	}

	public enum SDL_GpuCubeMapFace
	{
		SDL_GPU_CUBEMAPFACE_POSITIVEX,
		SDL_GPU_CUBEMAPFACE_NEGATIVEX,
		SDL_GPU_CUBEMAPFACE_POSITIVEY,
		SDL_GPU_CUBEMAPFACE_NEGATIVEY,
		SDL_GPU_CUBEMAPFACE_POSITIVEZ,
		SDL_GPU_CUBEMAPFACE_NEGATIVEZ
	}

	[Flags]
	public enum SDL_GpuBufferUsageFlagBits
	{
		SDL_GPU_BUFFERUSAGE_VERTEX_BIT = 0x00000001,
		SDL_GPU_BUFFERUSAGE_INDEX_BIT = 0x00000002,
		SDL_GPU_BUFFERUSAGE_INDIRECT_BIT = 0x00000004,
		SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ_BIT = 0x00000008,
		SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ_BIT = 0x00000020,
		SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE_BIT = 0x00000040
	}

	public enum SDL_GpuTransferBufferUsage
	{
		SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
		SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD
	}

	public enum SDL_GpuShaderStage
	{
		SDL_GPU_SHADERSTAGE_VERTEX,
		SDL_GPU_SHADERSTAGE_FRAGMENT
	}

	public enum SDL_GpuShaderFormat
	{
		SDL_GPU_SHADERFORMAT_INVALID,
		SDL_GPU_SHADERFORMAT_SECRET,
		SDL_GPU_SHADERFORMAT_SPIRV,
		SDL_GPU_SHADERFORMAT_HLSL,
		SDL_GPU_SHADERFORMAT_DXBC,
		SDL_GPU_SHADERFORMAT_DXIL,
		SDL_GPU_SHADERFORMAT_MSL,
		SDL_GPU_SHADERFORMAT_METALLIB,
	}

	public enum SDL_GpuVertexElementFormat
	{
		SDL_GPU_VERTEXELEMENTFORMAT_UINT,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
		SDL_GPU_VERTEXELEMENTFORMAT_VECTOR2,
		SDL_GPU_VERTEXELEMENTFORMAT_VECTOR3,
		SDL_GPU_VERTEXELEMENTFORMAT_VECTOR4,
		SDL_GPU_VERTEXELEMENTFORMAT_COLOR,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,
		SDL_GPU_VERTEXELEMENTFORMAT_NORMALIZEDSHORT2,
		SDL_GPU_VERTEXELEMENTFORMAT_NORMALIZEDSHORT4,
		SDL_GPU_VERTEXELEMENTFORMAT_HALFVECTOR2,
		SDL_GPU_VERTEXELEMENTFORMAT_HALFVECTOR4
	}

	public enum SDL_GpuVertexInputRate
	{
		SDL_GPU_VERTEXINPUTRATE_VERTEX = 0,
		SDL_GPU_VERTEXINPUTRATE_INSTANCE = 1
	}

	public enum SDL_GpuFillMode
	{
		SDL_GPU_FILLMODE_FILL,
		SDL_GPU_FILLMODE_LINE
	}

	public enum SDL_GpuCullMode
	{
		SDL_GPU_CULLMODE_NONE,
		SDL_GPU_CULLMODE_FRONT,
		SDL_GPU_CULLMODE_BACK
	}

	public enum SDL_GpuFrontFace
	{
		SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE,
		SDL_GPU_FRONTFACE_CLOCKWISE
	}

	public enum SDL_GpuCompareOp
	{
		SDL_GPU_COMPAREOP_NEVER,
		SDL_GPU_COMPAREOP_LESS,
		SDL_GPU_COMPAREOP_EQUAL,
		SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
		SDL_GPU_COMPAREOP_GREATER,
		SDL_GPU_COMPAREOP_NOT_EQUAL,
		SDL_GPU_COMPAREOP_GREATER_OR_EQUAL,
		SDL_GPU_COMPAREOP_ALWAYS
	}

	public enum SDL_GpuStencilOp
	{
		SDL_GPU_STENCILOP_KEEP,
		SDL_GPU_STENCILOP_ZERO,
		SDL_GPU_STENCILOP_REPLACE,
		SDL_GPU_STENCILOP_INCREMENT_AND_CLAMP,
		SDL_GPU_STENCILOP_DECREMENT_AND_CLAMP,
		SDL_GPU_STENCILOP_INVERT,
		SDL_GPU_STENCILOP_INCREMENT_AND_WRAP,
		SDL_GPU_STENCILOP_DECREMENT_AND_WRAP
	}

	public enum SDL_GpuBlendOp
	{
		SDL_GPU_BLENDOP_ADD,
		SDL_GPU_BLENDOP_SUBTRACT,
		SDL_GPU_BLENDOP_REVERSE_SUBTRACT,
		SDL_GPU_BLENDOP_MIN,
		SDL_GPU_BLENDOP_MAX
	}

	public enum SDL_GpuBlendFactor
	{
		SDL_GPU_BLENDFACTOR_ZERO,
		SDL_GPU_BLENDFACTOR_ONE,
		SDL_GPU_BLENDFACTOR_SRC_COLOR,
		SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
		SDL_GPU_BLENDFACTOR_DST_COLOR,
		SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
		SDL_GPU_BLENDFACTOR_SRC_ALPHA,
		SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
		SDL_GPU_BLENDFACTOR_DST_ALPHA,
		SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
		SDL_GPU_BLENDFACTOR_CONSTANT_COLOR,
		SDL_GPU_BLENDFACTOR_ONE_MINUS_CONSTANT_COLOR,
		SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE
	}

	[Flags]
	public enum SDL_GpuColorComponentFlagBits
	{
		SDL_GPU_COLORCOMPONENT_R_BIT = 0x00000001,
		SDL_GPU_COLORCOMPONENT_G_BIT = 0x00000002,
		SDL_GPU_COLORCOMPONENT_B_BIT = 0x00000004,
		SDL_GPU_COLORCOMPONENT_A_BIT = 0x00000008
	}

	public enum SDL_GpuFilter
	{
		SDL_GPU_FILTER_NEAREST,
		SDL_GPU_FILTER_LINEAR
	}

	public enum SDL_GpuSamplerMipmapMode
	{
		SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
		SDL_GPU_SAMPLERMIPMAPMODE_LINEAR
	}

	public enum SDL_GpuSamplerAddressMode
	{
		SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
		SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE
	}

	public enum SDL_GpuPresentMode
	{
		SDL_GPU_PRESENTMODE_VSYNC,
		SDL_GPU_PRESENTMODE_IMMEDIATE,
		SDL_GPU_PRESENTMODE_MAILBOX
	}

	public enum SDL_GpuSwapchainComposition
	{
		SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
		SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR,
		SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR,
		SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2048
	}

	[Flags]
	public enum SDL_GpuBackendBits : UInt64
	{
		SDL_GPU_BACKEND_INVALID = 0,
		SDL_GPU_BACKEND_VULKAN = 0x0000000000000001,
		SDL_GPU_BACKEND_D3D11 = 0x0000000000000002,
		SDL_GPU_BACKEND_METAL = 0x0000000000000004,
		SDL_GPU_BACKEND_D3D12 = 0x0000000000000008,
		SDL_GPU_BACKEND_ALL = (SDL_GPU_BACKEND_VULKAN | SDL_GPU_BACKEND_D3D11 | SDL_GPU_BACKEND_METAL | SDL_GPU_BACKEND_D3D12)
	}

	/* Structures */

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuDepthStencilValue
	{
		public float depth;
		public UInt32 stencil;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuRect
	{
		public Int32 x;
		public Int32 y;
		public Int32 w;
		public Int32 h;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuColor
	{
		public float r;
		public float g;
		public float b;
		public float a;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuViewport
	{
		public float x;
		public float y;
		public float w;
		public float h;
		public float minDepth;
		public float maxDepth;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTextureTransferInfo
	{
		public SDL_GpuTransferBufferPtr transferBuffer;
		public UInt32 offset;
		public UInt32 imagePitch;
		public UInt32 imageHeight;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTransferBufferLocation
	{
		public SDL_GpuTransferBufferPtr transferBuffer;
		public UInt32 offset;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTransferBufferRegion
	{
		public SDL_GpuTransferBufferPtr transferBuffer;
		public UInt32 offset;
		public UInt32 size;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTextureSlice
	{
		public SDL_GpuTexturePtr texture;
		public UInt32 mipLevel;
		public UInt32 layer;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTextureLocation
	{
		public SDL_GpuTextureSlice textureSlice;
		public UInt32 x;
		public UInt32 y;
		public UInt32 z;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTextureRegion
	{
		public SDL_GpuTextureSlice textureSlice;
		public UInt32 x;
		public UInt32 y;
		public UInt32 z;
		public UInt32 w;
		public UInt32 h;
		public UInt32 d;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuBufferLocation
	{
		public SDL_GpuBufferPtr buffer;
		public UInt32 offset;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuBufferRegion
	{
		public SDL_GpuBufferPtr buffer;
		public UInt32 offset;
		public UInt32 size;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuIndirectDrawCommand
	{
		public UInt32 vertexCount;
		public UInt32 instanceCount;
		public UInt32 firstVertex;
		public UInt32 firstInstance;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuIndexedIndirectDrawCommand
	{
		public UInt32 indexCount;
		public UInt32 instanceCount;
		public UInt32 firstIndex;
		public UInt32 vertexOffset;
		public UInt32 firstInstance;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuIndirectDispatchCommand
	{
		public UInt32 groupCountX;
		public UInt32 groupCountY;
		public UInt32 groupCountZ;
	}

	/* State structures */

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuSamplerCreateInfo
	{
		public SDL_GpuFilter minFilter;
		public SDL_GpuFilter magFilter;
		public SDL_GpuSamplerMipmapMode mipmapMode;
		public SDL_GpuSamplerAddressMode addressModeU;
		public SDL_GpuSamplerAddressMode addressModeV;
		public SDL_GpuSamplerAddressMode addressModeW;
		public float mipLodBias;
		public SDL_bool anisotropyEnable;
		public float maxAnisotropy;
		public SDL_bool compareEnable;
		public SDL_GpuCompareOp compareOp;
		public float minLod;
		public float maxLod;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuVertexBinding
	{
		public UInt32 binding;
		public UInt32 stride;
		public SDL_GpuVertexInputRate inputRate;
		public UInt32 stepRate;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuVertexAttribute
	{
		public UInt32 location;
		public UInt32 binding;
		public SDL_GpuVertexElementFormat format;
		public UInt32 offset;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GpuVertexInputState
	{
		public SDL_GpuVertexBinding* vertexBindings;
		public UInt32 vertexBindingCount;
		public SDL_GpuVertexAttribute* vertexAttributes;
		public UInt32 vertexAttributeCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuStencilOpState
	{
		public SDL_GpuStencilOp failOp;
		public SDL_GpuStencilOp passOp;
		public SDL_GpuStencilOp depthFailOp;
		public SDL_GpuCompareOp compareOp;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuColorAttachmentBlendState
	{
		public SDL_bool blendEnable;
		public SDL_GpuBlendFactor srcColorBlendFactor;
		public SDL_GpuBlendFactor dstColorBlendFactor;
		public SDL_GpuBlendOp colorBlendOp;
		public SDL_GpuBlendFactor srcAlphaBlendFactor;
		public SDL_GpuBlendFactor dstAlphaBlendFactor;
		public SDL_GpuBlendOp alphaBlendOp;
		public SDL_GpuColorComponentFlagBits colorWriteMask;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GpuShaderCreateInfo
	{
		public nuint codeSize;
		public byte* code;
		public char* entryPointName;
		public SDL_GpuShaderFormat format;
		public SDL_GpuShaderStage stage;
		public UInt32 samplerCount;
		public UInt32 storageTextureCount;
		public UInt32 storageBufferCount;
		public UInt32 uniformBufferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTextureCreateInfo
	{
		public UInt32 width;
		public UInt32 height;
		public UInt32 depth;
		public SDL_bool isCube;
		public UInt32 layerCount;
		public UInt32 levelCount;
		public SDL_GpuSampleCount sampleCount;
		public SDL_GpuTextureFormat format;
		public SDL_GpuTextureUsageFlagBits usageFlags;
	}

	/* Pipeline state structures */

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuRasterizerState
	{
		public SDL_GpuFillMode fillMode;
		public SDL_GpuCullMode cullMode;
		public SDL_GpuFrontFace frontFace;
		public SDL_bool depthBiasEnable;
		public float depthBiasConstantFactor;
		public float depthBiasClamp;
		public float depthBiasSlopeFactor;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuMultisampleState
	{
		public SDL_GpuSampleCount multisampleCount;
		public UInt32 sampleMask;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuDepthStencilState
	{
		public SDL_bool depthTestEnable;
		public SDL_bool depthWriteEnable;
		public SDL_GpuCompareOp compareOp;
		public SDL_bool stencilTestEnable;
		public SDL_GpuStencilOpState backStencilState;
		public SDL_GpuStencilOpState frontStencilState;
		public UInt32 compareMask;
		public UInt32 writeMask;
		public UInt32 reference;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuColorAttachmentDescription
	{
		public SDL_GpuTextureFormat format;
		public SDL_GpuColorAttachmentBlendState blendState;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GpuGraphicsPipelineAttachmentInfo
	{
		public SDL_GpuColorAttachmentDescription* colorAttachmentDescriptions;
		public UInt32 colorAttachmentCount;
		public SDL_bool hasDepthStencilAttachment;
		public SDL_GpuTextureFormat depthStencilFormat;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GpuGraphicsPipelineCreateInfo
	{
		public SDL_GpuShaderPtr vertexShader;
		public SDL_GpuShaderPtr fragmentShader;
		public SDL_GpuVertexInputState vertexInputState;
		public SDL_GpuPrimitiveType primitiveType;
		public SDL_GpuRasterizerState rasterizerState;
		public SDL_GpuMultisampleState multisampleState;
		public SDL_GpuDepthStencilState depthStencilState;
		public SDL_GpuGraphicsPipelineAttachmentInfo attachmentInfo;
		public fixed float blendConstants[4];
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GpuComputePipelineCreateInfo
	{
		public nuint codeSize;
		public byte* code;
		public byte* entryPointName;
		public SDL_GpuShaderFormat format;
		public UInt32 readOnlyStorageTextureCount;
		public UInt32 readOnlyStorageBufferCount;
		public UInt32 readWriteStorageTextureCount;
		public UInt32 readWriteStorageBufferCount;
		public UInt32 uniformBufferCount;
		public UInt32 threadCountX;
		public UInt32 threadCountY;
		public UInt32 threadCountZ;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuColorAttachmentInfo
	{
		public SDL_GpuTextureSlice textureSlice;
		public SDL_GpuColor clearColor;
		public SDL_GpuLoadOp loadOp;
		public SDL_GpuStoreOp storeOp;
		public SDL_bool cycle;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuDepthStencilAttachmentInfo
	{
		public SDL_GpuTextureSlice textureSlice;
		public SDL_GpuDepthStencilValue depthStencilClearValue;
		public SDL_GpuLoadOp loadOp;
		public SDL_GpuStoreOp storeOp;
		public SDL_GpuLoadOp stencilLoadOp;
		public SDL_GpuStoreOp stencilStoreOp;
		public SDL_bool cycle;
	}

	/* Binding structs */

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuBufferBinding
	{
		public SDL_GpuBufferPtr buffer;
		public UInt32 offset;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuTextureSamplerBinding
	{
		public SDL_GpuTexturePtr texture;
		public SDL_GpuSamplerPtr sampler;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuStorageBufferReadWriteBinding
	{
		public SDL_GpuBufferPtr buffer;
		public SDL_bool cycle;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GpuStorageTextureReadWriteBinding
	{
		public SDL_GpuTextureSlice textureSlice;
		public SDL_bool cycle;
	}

	/* Functions */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuDevicePtr SDL_GpuCreateDevice(
		SDL_GpuBackendBits preferredBackends,
		SDL_bool debugMode,
		SDL_bool preferLowPower);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDestroyDevice(SDL_GpuDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuBackendBits SDL_GpuGetBackend(SDL_GpuDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuComputePipelinePtr SDL_GpuCreateComputePipeline(
		SDL_GpuDevicePtr device,
		SDL_GpuComputePipelineCreateInfo* computePipelineCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuGraphicsPipelinePtr SDL_GpuCreateGraphicsPipeline(
		SDL_GpuDevicePtr device,
		SDL_GpuGraphicsPipelineCreateInfo* pipelineCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuSamplerPtr SDL_GpuCreateSampler(
		SDL_GpuDevicePtr device,
		SDL_GpuSamplerCreateInfo* samplerCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuShaderPtr SDL_GpuCreateShader(
		SDL_GpuDevicePtr device,
		SDL_GpuShaderCreateInfo* shaderCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuTexturePtr SDL_GpuCreateTexture(
		SDL_GpuDevicePtr device,
		SDL_GpuTextureCreateInfo* textureCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuBufferPtr SDL_GpuCreateBuffer(
		SDL_GpuDevicePtr device,
		SDL_GpuBufferUsageFlagBits usageFlags,
		UInt32 sizeInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuTransferBufferPtr SDL_GpuCreateTransferBuffer(
		SDL_GpuDevicePtr device,
		SDL_GpuTransferBufferUsage usage,
		UInt32 sizeInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuSetBufferName(
		SDL_GpuDevicePtr device,
		SDL_GpuBufferPtr buffer,
		nint text);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuSetTextureName(
		SDL_GpuDevicePtr device,
		SDL_GpuTexturePtr texture,
		nint text);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuInsertDebugLabel(
		SDL_GpuCommandBufferPtr commandBuffer,
		nint text);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuPushDebugGroup(
		SDL_GpuCommandBufferPtr commandBuffer,
		nint name);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuPopDebugGroup(
		SDL_GpuCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseTexture(
		SDL_GpuDevicePtr device,
		SDL_GpuTexturePtr texture);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseSampler(
		SDL_GpuDevicePtr device,
		SDL_GpuSamplerPtr sampler);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseBuffer(
		SDL_GpuDevicePtr device,
		SDL_GpuBufferPtr buffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseTransferBuffer(
		SDL_GpuDevicePtr device,
		SDL_GpuTransferBufferPtr transferBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseComputePipeline(
		SDL_GpuDevicePtr device,
		SDL_GpuComputePipelinePtr computePipeline);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseShader(
		SDL_GpuDevicePtr device,
		SDL_GpuShaderPtr shader);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseGraphicsPipeline(
		SDL_GpuDevicePtr device,
		SDL_GpuGraphicsPipelinePtr graphicsPipeline);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuCommandBufferPtr SDL_GpuAcquireCommandBuffer(
		SDL_GpuDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuPushVertexUniformData(
		SDL_GpuCommandBufferPtr commandBuffer,
		UInt32 slotIndex,
		nint data,
		UInt32 dataLengthInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuPushFragmentUniformData(
		SDL_GpuCommandBufferPtr commandBuffer,
		UInt32 slotIndex,
		nint data,
		UInt32 dataLengthInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuPushComputeUniformData(
		SDL_GpuCommandBufferPtr commandBuffer,
		UInt32 slotIndex,
		nint data,
		UInt32 dataLengthInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuRenderPassPtr SDL_GpuBeginRenderPass(
		SDL_GpuCommandBufferPtr commandBuffer,
		SDL_GpuColorAttachmentInfo* colorAttachmentInfos,
		UInt32 colorAttachmentCount,
		SDL_GpuDepthStencilAttachmentInfo* depthStencilAttachmentInfo);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindGraphicsPipeline(
		SDL_GpuRenderPassPtr renderPass,
		SDL_GpuGraphicsPipelinePtr graphicsPipeline);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuSetViewport(
		SDL_GpuRenderPassPtr renderPass,
		SDL_GpuViewport* viewport);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuSetScissor(
		SDL_GpuRenderPassPtr renderPass,
		SDL_GpuRect* scissor);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindVertexBuffers(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstBinding,
		SDL_GpuBufferBinding* pBindings,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindIndexBuffer(
		SDL_GpuRenderPassPtr renderPass,
		SDL_GpuBufferBinding* pBinding,
		SDL_GpuIndexElementSize indexElementSize);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindVertexSamplers(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GpuTextureSamplerBinding* textureSamplerBindings,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindVertexStorageTextures(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GpuTextureSlice* storageTextureSlices,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindVertexStorageBuffers(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GpuBufferPtr *storageBuffers,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindFragmentSamplers(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GpuTextureSamplerBinding* textureSamplerBindings,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindFragmentStorageTextures(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GpuTextureSlice* storageTextureSlices,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindFragmentStorageBuffers(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GpuBufferPtr *storageBuffers,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDrawIndexedPrimitives(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 baseVertex,
		UInt32 startIndex,
		UInt32 primitiveCount,
		UInt32 instanceCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDrawPrimitives(
		SDL_GpuRenderPassPtr renderPass,
		UInt32 vertexStart,
		UInt32 primitiveCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDrawPrimitivesIndirect(
		SDL_GpuRenderPassPtr renderPass,
		SDL_GpuBufferPtr buffer,
		UInt32 offsetInBytes,
		UInt32 drawCount,
		UInt32 stride);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDrawIndexedPrimitivesIndirect(
		SDL_GpuRenderPassPtr renderPass,
		SDL_GpuBufferPtr buffer,
		UInt32 offsetInBytes,
		UInt32 drawCount,
		UInt32 stride);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuEndRenderPass(
		SDL_GpuRenderPassPtr renderPass);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuComputePassPtr SDL_GpuBeginComputePass(
		SDL_GpuCommandBufferPtr commandBuffer,
		SDL_GpuStorageTextureReadWriteBinding* storageTextureBindings,
		UInt32 storageTextureBindingCount,
		SDL_GpuStorageBufferReadWriteBinding* storageBufferBindings,
		UInt32 storageBufferBindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindComputePipeline(
		SDL_GpuComputePassPtr computePass,
		SDL_GpuComputePipelinePtr computePipeline);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindComputeStorageTextures(
		SDL_GpuComputePassPtr computePass,
		UInt32 firstSlot,
		SDL_GpuTextureSlice* storageTextureSlices,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBindComputeStorageBuffers(
		SDL_GpuComputePassPtr computePass,
		UInt32 firstSlot,
		SDL_GpuBufferPtr *storageBuffers,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDispatchCompute(
		SDL_GpuComputePassPtr computePass,
		UInt32 groupCountX,
		UInt32 groupCountY,
		UInt32 groupCountZ);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDispatchComputeIndirect(
		SDL_GpuComputePassPtr computePass,
		SDL_GpuBufferPtr buffer,
		UInt32 offsetInBytes
	);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuEndComputePass(
		SDL_GpuComputePassPtr computePass);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuMapTransferBuffer(
		SDL_GpuDevicePtr device,
		SDL_GpuTransferBufferPtr transferBuffer,
		SDL_bool cycle,
		byte** ppData);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuUnmapTransferBuffer(
		SDL_GpuDevicePtr device,
		SDL_GpuTransferBufferPtr transferBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuSetTransferData(
		SDL_GpuDevicePtr device,
		nint source,
		SDL_GpuTransferBufferRegion* destination,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuGetTransferData(
		SDL_GpuDevicePtr device,
		SDL_GpuTransferBufferRegion* source,
		nint destination);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuCopyPassPtr SDL_GpuBeginCopyPass(
		SDL_GpuCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuUploadToTexture(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuTextureTransferInfo* source,
		SDL_GpuTextureRegion* destination,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuUploadToBuffer(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuTransferBufferLocation* source,
		SDL_GpuBufferRegion* destination,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuCopyTextureToTexture(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuTextureLocation* source,
		SDL_GpuTextureLocation* destination,
		UInt32 w,
		UInt32 h,
		UInt32 d,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuCopyBufferToBuffer(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuBufferLocation* source,
		SDL_GpuBufferLocation* destination,
		UInt32 size,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuGenerateMipmaps(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuTexturePtr texture);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDownloadFromTexture(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuTextureRegion* source,
		SDL_GpuTextureTransferInfo* destination);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuDownloadFromBuffer(
		SDL_GpuCopyPassPtr copyPass,
		SDL_GpuBufferRegion* source,
		SDL_GpuTransferBufferLocation* destination);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuEndCopyPass(
		SDL_GpuCopyPassPtr copyPass);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuBlit(
		SDL_GpuCommandBufferPtr commandBuffer,
		SDL_GpuTextureRegion* source,
		SDL_GpuTextureRegion* destination,
		SDL_GpuFilter filterMode,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GpuSupportsSwapchainComposition(
		SDL_GpuDevicePtr device,
		SDL_WindowPtr window,
		SDL_GpuSwapchainComposition swapchainComposition);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GpuSupportsPresentMode(
		SDL_GpuDevicePtr device,
		SDL_WindowPtr window,
		SDL_GpuPresentMode presentMode);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GpuClaimWindow(
		SDL_GpuDevicePtr device,
		SDL_WindowPtr window,
		SDL_GpuSwapchainComposition swapchainComposition,
		SDL_GpuPresentMode presentMode);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuUnclaimWindow(
		SDL_GpuDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GpuSetSwapchainParameters(
		SDL_GpuDevicePtr device,
		SDL_WindowPtr window,
		SDL_GpuSwapchainComposition swapchainComposition,
		SDL_GpuPresentMode presentMode);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuTextureFormat SDL_GpuGetSwapchainTextureFormat(
		SDL_GpuDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuTexturePtr SDL_GpuAcquireSwapchainTexture(
		SDL_GpuCommandBufferPtr commandBuffer,
		SDL_WindowPtr window,
		UInt32* pWidth,
		UInt32* pHeight);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuSubmit(
		SDL_GpuCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuFencePtr SDL_GpuSubmitAndAcquireFence(
		SDL_GpuCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuWait(
		SDL_GpuDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuWaitForFences(
		SDL_GpuDevicePtr device,
		SDL_bool waitAll,
		SDL_GpuFencePtr *pFences,
		UInt32 fenceCount);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GpuQueryFence(
		SDL_GpuDevicePtr device,
		SDL_GpuFencePtr fence);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GpuReleaseFence(
		SDL_GpuDevicePtr device,
		SDL_GpuFencePtr fence);

	[LibraryImport(DLL)]
	public static unsafe partial UInt32 SDL_GpuTextureFormatTexelBlockSize(
		SDL_GpuTextureFormat textureFormat);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GpuIsTextureFormatSupported(
		SDL_GpuDevicePtr device,
		SDL_GpuTextureFormat format,
		SDL_GpuTextureType type,
		SDL_GpuTextureUsageFlagBits usage);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GpuSampleCount SDL_GpuGetBestSampleCount(
		SDL_GpuDevicePtr device,
		SDL_GpuTextureFormat format,
		SDL_GpuSampleCount desiredSampleCount);
}