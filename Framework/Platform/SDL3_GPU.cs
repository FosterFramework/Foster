using System.Runtime.InteropServices;

using SDL_WindowPtr = nint;
using SDL_bool = System.Byte;
using SDL_PropertiesID = System.UInt32;

using SDL_GPUDevicePtr = nint;
using SDL_GPUBufferPtr = nint;
using SDL_GPUTransferBufferPtr = nint;
using SDL_GPUTexturePtr = nint;
using SDL_GPUSamplerPtr = nint;
using SDL_GPUShaderPtr = nint;
using SDL_GPUComputePipelinePtr = nint;
using SDL_GPUGraphicsPipelinePtr = nint;
using SDL_GPUCommandBufferPtr = nint;
using SDL_GPURenderPassPtr = nint;
using SDL_GPUComputePassPtr = nint;
using SDL_GPUCopyPassPtr = nint;
using SDL_GPUFencePtr = nint;

namespace Foster.Framework;

internal static partial class SDL3
{
	public enum SDL_GPUColorComponentFlags : UInt32
	{
		SDL_GPU_COLORCOMPONENT_R = (1u << 0),
		SDL_GPU_COLORCOMPONENT_G = (1u << 1),
		SDL_GPU_COLORCOMPONENT_B = (1u << 2),
		SDL_GPU_COLORCOMPONENT_A = (1u << 3),
	}

	public enum SDL_GPUShaderFormat : UInt32
	{
		SDL_GPU_SHADERFORMAT_PRIVATE  = (1u << 0),
		SDL_GPU_SHADERFORMAT_SPIRV    = (1u << 1),
		SDL_GPU_SHADERFORMAT_DXBC     = (1u << 2),
		SDL_GPU_SHADERFORMAT_DXIL     = (1u << 3),
		SDL_GPU_SHADERFORMAT_MSL      = (1u << 4),
		SDL_GPU_SHADERFORMAT_METALLIB = (1u << 5),
	}

	public enum SDL_GPUBufferUsageFlags : UInt32
	{
		SDL_GPU_BUFFERUSAGE_VERTEX                = (1u << 0),
		SDL_GPU_BUFFERUSAGE_INDEX                 = (1u << 1),
		SDL_GPU_BUFFERUSAGE_INDIRECT              = (1u << 2),
		SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ = (1u << 3),
		SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ  = (1u << 4),
		SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE = (1u << 5),
	}

	public enum SDL_GPUTextureUsageFlags : UInt32
	{
		SDL_GPU_TEXTUREUSAGE_SAMPLER               = (1u << 0),
		SDL_GPU_TEXTUREUSAGE_COLOR_TARGET          = (1u << 1),
		SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET  = (1u << 2),
		SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ = (1u << 3),
		SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ  = (1u << 4),
		SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE = (1u << 5),
	}

	public const string SDL_PROP_GPU_CREATETEXTURE_D3D12_CLEAR_R_FLOAT        = "SDL.gpu.createtexture.d3d12.clear.r";
	public const string SDL_PROP_GPU_CREATETEXTURE_D3D12_CLEAR_G_FLOAT        = "SDL.gpu.createtexture.d3d12.clear.g";
	public const string SDL_PROP_GPU_CREATETEXTURE_D3D12_CLEAR_B_FLOAT        = "SDL.gpu.createtexture.d3d12.clear.b";
	public const string SDL_PROP_GPU_CREATETEXTURE_D3D12_CLEAR_A_FLOAT        = "SDL.gpu.createtexture.d3d12.clear.a";
	public const string SDL_PROP_GPU_CREATETEXTURE_D3D12_CLEAR_DEPTH_FLOAT    = "SDL.gpu.createtexture.d3d12.clear.depth";
	public const string SDL_PROP_GPU_CREATETEXTURE_D3D12_CLEAR_STENCIL_byte   = "SDL.gpu.createtexture.d3d12.clear.stencil";
	public const string SDL_PROP_GPU_DEVICE_CREATE_DEBUGMODE_BOOL             = "SDL.gpu.device.create.debugmode";
	public const string SDL_PROP_GPU_DEVICE_CREATE_PREFERLOWPOWER_BOOL        = "SDL.gpu.device.create.preferlowpower";
	public const string SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING                = "SDL.gpu.device.create.name";
	public const string SDL_PROP_GPU_DEVICE_CREATE_SHADERS_PRIVATE_BOOL       = "SDL.gpu.device.create.shaders.private";
	public const string SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOL         = "SDL.gpu.device.create.shaders.spirv";
	public const string SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXBC_BOOL          = "SDL.gpu.device.create.shaders.dxbc";
	public const string SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXIL_BOOL          = "SDL.gpu.device.create.shaders.dxil";
	public const string SDL_PROP_GPU_DEVICE_CREATE_SHADERS_MSL_BOOL           = "SDL.gpu.device.create.shaders.msl";
	public const string SDL_PROP_GPU_DEVICE_CREATE_SHADERS_METALLIB_BOOL      = "SDL.gpu.device.create.shaders.metallib";
	public const string SDL_PROP_GPU_DEVICE_CREATE_D3D12_SEMANTIC_NAME_STRING = "SDL.gpu.device.create.d3d12.semantic";

	public enum SDL_GPUPrimitiveType
	{
		SDL_GPU_PRIMITIVETYPE_POINTLIST,
		SDL_GPU_PRIMITIVETYPE_LINELIST,
		SDL_GPU_PRIMITIVETYPE_LINESTRIP,
		SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
		SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP
	}

	public enum SDL_GPULoadOp
	{
		SDL_GPU_LOADOP_LOAD,
		SDL_GPU_LOADOP_CLEAR,
		SDL_GPU_LOADOP_DONT_CARE
	}

	public enum SDL_GPUStoreOp
	{
		SDL_GPU_STOREOP_STORE,
		SDL_GPU_STOREOP_DONT_CARE
	}

	public enum SDL_GPUIndexElementSize
	{
		SDL_GPU_INDEXELEMENTSIZE_16BIT,
		SDL_GPU_INDEXELEMENTSIZE_32BIT
	}

	public enum SDL_GPUTextureFormat
	{
		SDL_GPU_TEXTUREFORMAT_INVALID = -1,

		/* Unsigned Normalized Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_A8_UNORM,
		SDL_GPU_TEXTUREFORMAT_R8_UNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8_UNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
		SDL_GPU_TEXTUREFORMAT_R16_UNORM,
		SDL_GPU_TEXTUREFORMAT_R16G16_UNORM,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM,
		SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM,
		SDL_GPU_TEXTUREFORMAT_B5G6R5_UNORM,
		SDL_GPU_TEXTUREFORMAT_B5G5R5A1_UNORM,
		SDL_GPU_TEXTUREFORMAT_B4G4R4A4_UNORM,
		SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM,
		/* Compressed Unsigned Normalized Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM,
		/* Compressed Signed Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_BC6H_RGB_FLOAT,
		/* Compressed Unsigned Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_BC6H_RGB_UFLOAT,
		/* Signed Normalized Float Color Formats  */
		SDL_GPU_TEXTUREFORMAT_R8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16G16_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM,
		/* Signed Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_R16_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT,
		/* Unsigned Float Color Formats */
		SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT,
		/* Unsigned Integer Color Formats */
		SDL_GPU_TEXTUREFORMAT_R8_UINT,
		SDL_GPU_TEXTUREFORMAT_R8G8_UINT,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT,
		SDL_GPU_TEXTUREFORMAT_R16_UINT,
		SDL_GPU_TEXTUREFORMAT_R16G16_UINT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT,
		/* Signed Integer Color Formats */
		SDL_GPU_TEXTUREFORMAT_R8_INT,
		SDL_GPU_TEXTUREFORMAT_R8G8_INT,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT,
		SDL_GPU_TEXTUREFORMAT_R16_INT,
		SDL_GPU_TEXTUREFORMAT_R16G16_INT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT,
		/* SRGB Unsigned Normalized Color Formats */
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB,
		/* Compressed SRGB Unsigned Normalized Color Formats */
		SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB,
		/* Depth Formats */
		SDL_GPU_TEXTUREFORMAT_D16_UNORM,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM,
		SDL_GPU_TEXTUREFORMAT_D32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
		SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT
	}

	public enum SDL_GPUTextureType
	{
		SDL_GPU_TEXTURETYPE_2D,
		SDL_GPU_TEXTURETYPE_2D_ARRAY,
		SDL_GPU_TEXTURETYPE_3D,
		SDL_GPU_TEXTURETYPE_CUBE
	}

	public enum SDL_GPUSampleCount
	{
		SDL_GPU_SAMPLECOUNT_1,
		SDL_GPU_SAMPLECOUNT_2,
		SDL_GPU_SAMPLECOUNT_4,
		SDL_GPU_SAMPLECOUNT_8
	}

	public enum SDL_GPUCubeMapFace
	{
		SDL_GPU_CUBEMAPFACE_POSITIVEX,
		SDL_GPU_CUBEMAPFACE_NEGATIVEX,
		SDL_GPU_CUBEMAPFACE_POSITIVEY,
		SDL_GPU_CUBEMAPFACE_NEGATIVEY,
		SDL_GPU_CUBEMAPFACE_POSITIVEZ,
		SDL_GPU_CUBEMAPFACE_NEGATIVEZ
	}

	public enum SDL_GPUTransferBufferUsage
	{
		SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
		SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD
	}

	public enum SDL_GPUShaderStage
	{
		SDL_GPU_SHADERSTAGE_VERTEX,
		SDL_GPU_SHADERSTAGE_FRAGMENT
	}

	public enum SDL_GPUVertexElementFormat
	{
		/* 32-bit Signed Integers */
		SDL_GPU_VERTEXELEMENTFORMAT_INT,
		SDL_GPU_VERTEXELEMENTFORMAT_INT2,
		SDL_GPU_VERTEXELEMENTFORMAT_INT3,
		SDL_GPU_VERTEXELEMENTFORMAT_INT4,

		/* 32-bit Unsigned Integers */
		SDL_GPU_VERTEXELEMENTFORMAT_UINT,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT2,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT3,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT4,

		/* 32-bit Floats */
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,

		/* 8-bit Signed Integers */
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE2,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,

		/* 8-bit Unsigned Integers */
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2,
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,

		/* 8-bit Signed Normalized */
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE4_NORM,

		/* 8-bit Unsigned Normalized */
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,

		/* 16-bit Signed Integers */
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,

		/* 16-bit Unsigned Integers */
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT2,
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT4,

		/* 16-bit Signed Normalized */
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT4_NORM,

		/* 16-bit Unsigned Normalized */
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT4_NORM,

		/* 16-bit Floats */
		SDL_GPU_VERTEXELEMENTFORMAT_HALF2,
		SDL_GPU_VERTEXELEMENTFORMAT_HALF4
	}

	public enum SDL_GPUVertexInputRate
	{
		SDL_GPU_VERTEXINPUTRATE_VERTEX = 0,
		SDL_GPU_VERTEXINPUTRATE_INSTANCE = 1
	}

	public enum SDL_GPUFillMode
	{
		SDL_GPU_FILLMODE_FILL,
		SDL_GPU_FILLMODE_LINE
	}

	public enum SDL_GPUCullMode
	{
		SDL_GPU_CULLMODE_NONE,
		SDL_GPU_CULLMODE_FRONT,
		SDL_GPU_CULLMODE_BACK
	}

	public enum SDL_GPUFrontFace
	{
		SDL_GPU_FRONTFACE_COUNTER_CLOCKWISE,
		SDL_GPU_FRONTFACE_CLOCKWISE
	}

	public enum SDL_GPUCompareOp
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

	public enum SDL_GPUStencilOp
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

	public enum SDL_GPUBlendOp
	{
		SDL_GPU_BLENDOP_ADD,
		SDL_GPU_BLENDOP_SUBTRACT,
		SDL_GPU_BLENDOP_REVERSE_SUBTRACT,
		SDL_GPU_BLENDOP_MIN,
		SDL_GPU_BLENDOP_MAX
	}

	public enum SDL_GPUBlendFactor
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

	public enum SDL_GPUFilter
	{
		SDL_GPU_FILTER_NEAREST,
		SDL_GPU_FILTER_LINEAR
	}

	public enum SDL_GPUSamplerMipmapMode
	{
		SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
		SDL_GPU_SAMPLERMIPMAPMODE_LINEAR
	}

	public enum SDL_GPUSamplerAddressMode
	{
		SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
		SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE
	}

	public enum SDL_GPUPresentMode
	{
		SDL_GPU_PRESENTMODE_VSYNC,
		SDL_GPU_PRESENTMODE_IMMEDIATE,
		SDL_GPU_PRESENTMODE_MAILBOX
	}

	public enum SDL_GPUSwapchainComposition
	{
		SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
		SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR,
		SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR,
		SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2048
	}

	public enum SDL_GPUDriver
	{
		SDL_GPU_DRIVER_INVALID = -1,
		SDL_GPU_DRIVER_PRIVATE, /* NDA'd platforms */
		SDL_GPU_DRIVER_VULKAN,
		SDL_GPU_DRIVER_D3D11,
		SDL_GPU_DRIVER_D3D12,
		SDL_GPU_DRIVER_METAL
	}

	/* Structures */

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUDepthStencilValue
	{
		public float depth;
		public byte stencil;
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUViewport
	{
		public float x;
		public float y;
		public float w;
		public float h;
		public float minDepth;
		public float maxDepth;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTextureTransferInfo
	{
		public SDL_GPUTransferBufferPtr transferBuffer;
		public UInt32 offset;	  /* starting location of the image data */
		public UInt32 imagePitch;  /* number of pixels from one row to the next */
		public UInt32 imageHeight; /* number of rows from one layer/depth-slice to the next */
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTransferBufferLocation
	{
		public SDL_GPUTransferBufferPtr transferBuffer;
		public UInt32 offset;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTextureLocation
	{
		public SDL_GPUTexturePtr texture;
		public UInt32 mipLevel;
		public UInt32 layer;
		public UInt32 x;
		public UInt32 y;
		public UInt32 z;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTextureRegion
	{
		public SDL_GPUTexturePtr texture;
		public UInt32 mipLevel;
		public UInt32 layer;
		public UInt32 x;
		public UInt32 y;
		public UInt32 z;
		public UInt32 w;
		public UInt32 h;
		public UInt32 d;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUBlitRegion
	{
		public SDL_GPUTexturePtr texture;
		public UInt32 mipLevel;
		public UInt32 layerOrDepthPlane;
		public UInt32 x;
		public UInt32 y;
		public UInt32 w;
		public UInt32 h;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUBufferLocation
	{
		public SDL_GPUBufferPtr buffer;
		public UInt32 offset;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUBufferRegion
	{
		public SDL_GPUBufferPtr buffer;
		public UInt32 offset;
		public UInt32 size;
	}

	/* Note that the `firstVertex` and `firstInstance` parameters are NOT compatible with
	* built-in vertex/instance ID variables in shaders (for example, SV_VertexID). If
	* your shader depends on these variables, the correlating draw call parameter MUST
	* be 0.
	*/
	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUIndirectDrawCommand
	{
		public UInt32 vertexCount;   /* number of vertices to draw */
		public UInt32 instanceCount; /* number of instances to draw */
		public UInt32 firstVertex;   /* index of the first vertex to draw */
		public UInt32 firstInstance; /* ID of the first instance to draw */
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUIndexedIndirectDrawCommand
	{
		public UInt32 indexCount;	/* number of vertices to draw per instance */
		public UInt32 instanceCount; /* number of instances to draw */
		public UInt32 firstIndex;	/* base index within the index buffer */
		public Int32 vertexOffset;  /* value added to vertex index before indexing into the vertex buffer */
		public UInt32 firstInstance; /* ID of the first instance to draw */
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUIndirectDispatchCommand
	{
		public UInt32 groupCountX;
		public UInt32 groupCountY;
		public UInt32 groupCountZ;
	}

	/* State structures */

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUSamplerCreateInfo
	{
		public SDL_GPUFilter minFilter;
		public SDL_GPUFilter magFilter;
		public SDL_GPUSamplerMipmapMode mipmapMode;
		public SDL_GPUSamplerAddressMode addressModeU;
		public SDL_GPUSamplerAddressMode addressModeV;
		public SDL_GPUSamplerAddressMode addressModeW;
		public float mipLodBias;
		public float maxAnisotropy;
		public SDL_bool anisotropyEnable;
		public SDL_bool compareEnable;
		public byte padding1;
		public byte padding2;
		public SDL_GPUCompareOp compareOp;
		public float minLod;
		public float maxLod;
		public SDL_PropertiesID props;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUVertexBinding
	{
		public UInt32 binding;
		public UInt32 stride;
		public SDL_GPUVertexInputRate inputRate;
		public UInt32 instanceStepRate; /* ignored unless inputRate is INSTANCE */
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUVertexAttribute
	{
		public UInt32 location;
		public UInt32 binding;
		public SDL_GPUVertexElementFormat format;
		public UInt32 offset;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUVertexInputState
	{
		public SDL_GPUVertexBinding* vertexBindings;
		public UInt32 vertexBindingCount;
		public SDL_GPUVertexAttribute* vertexAttributes;
		public UInt32 vertexAttributeCount;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUStencilOpState
	{
		public SDL_GPUStencilOp failOp;
		public SDL_GPUStencilOp passOp;
		public SDL_GPUStencilOp depthFailOp;
		public SDL_GPUCompareOp compareOp;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUColorAttachmentBlendState
	{
		public SDL_bool blendEnable;
		public byte padding1;
		public byte padding2;
		public byte padding3;
		public SDL_GPUBlendFactor srcColorBlendFactor;
		public SDL_GPUBlendFactor dstColorBlendFactor;
		public SDL_GPUBlendOp colorBlendOp;
		public SDL_GPUBlendFactor srcAlphaBlendFactor;
		public SDL_GPUBlendFactor dstAlphaBlendFactor;
		public SDL_GPUBlendOp alphaBlendOp;
		public SDL_GPUColorComponentFlags colorWriteMask;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUShaderCreateInfo
	{
		public UInt64 codeSize;
		public byte* code;
		public byte* entryPointName;
		public SDL_GPUShaderFormat format;
		public SDL_GPUShaderStage stage;
		public UInt32 samplerCount;
		public UInt32 storageTextureCount;
		public UInt32 storageBufferCount;
		public UInt32 uniformBufferCount;
		public SDL_PropertiesID props;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTextureCreateInfo
	{
		public SDL_GPUTextureType type;
		public SDL_GPUTextureFormat format;
		public SDL_GPUTextureUsageFlags usageFlags;
		public UInt32 width;
		public UInt32 height;
		public UInt32 layerCountOrDepth;
		public UInt32 levelCount;
		public SDL_GPUSampleCount sampleCount;
		public SDL_PropertiesID props;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUBufferCreateInfo
	{
		public SDL_GPUBufferUsageFlags usageFlags;
		public UInt32 sizeInBytes;
		public SDL_PropertiesID props;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTransferBufferCreateInfo
	{
		public SDL_GPUTransferBufferUsage usage;
		public UInt32 sizeInBytes;
		public SDL_PropertiesID props;
	}

	/* Pipeline state structures */

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPURasterizerState
	{
		public SDL_GPUFillMode fillMode;
		public SDL_GPUCullMode cullMode;
		public SDL_GPUFrontFace frontFace;
		public SDL_bool depthBiasEnable;
		public byte padding1;
		public byte padding2;
		public byte padding3;
		public float depthBiasConstantFactor;
		public float depthBiasClamp;
		public float depthBiasSlopeFactor;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUMultisampleState
	{
		public SDL_GPUSampleCount sampleCount;
		public UInt32 sampleMask;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUDepthStencilState
	{
		public SDL_bool depthTestEnable;
		public SDL_bool depthWriteEnable;
		public SDL_bool stencilTestEnable;
		public byte padding1;
		public SDL_GPUCompareOp compareOp;
		public SDL_GPUStencilOpState backStencilState;
		public SDL_GPUStencilOpState frontStencilState;
		public byte compareMask;
		public byte writeMask;
		public byte padding2;
		public byte padding3;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUColorAttachmentDescription
	{
		public SDL_GPUTextureFormat format;
		public SDL_GPUColorAttachmentBlendState blendState;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUGraphicsPipelineAttachmentInfo
	{
		public SDL_GPUColorAttachmentDescription* colorAttachmentDescriptions;
		public UInt32 colorAttachmentCount;
		public SDL_bool hasDepthStencilAttachment;
		public byte padding1;
		public byte padding2;
		public byte padding3;
		public SDL_GPUTextureFormat depthStencilFormat;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUGraphicsPipelineCreateInfo
	{
		public SDL_GPUShaderPtr vertexShader;
		public SDL_GPUShaderPtr fragmentShader;
		public SDL_GPUVertexInputState vertexInputState;
		public SDL_GPUPrimitiveType primitiveType;
		public SDL_GPURasterizerState rasterizerState;
		public SDL_GPUMultisampleState multisampleState;
		public SDL_GPUDepthStencilState depthStencilState;
		public SDL_GPUGraphicsPipelineAttachmentInfo attachmentInfo;
		public SDL_PropertiesID props;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUComputePipelineCreateInfo
	{
		public UInt64 codeSize;
		public byte* code;
		public char* entryPointName;
		public SDL_GPUShaderFormat format;
		public UInt32 readOnlyStorageTextureCount;
		public UInt32 readOnlyStorageBufferCount;
		public UInt32 writeOnlyStorageTextureCount;
		public UInt32 writeOnlyStorageBufferCount;
		public UInt32 uniformBufferCount;
		public UInt32 threadCountX;
		public UInt32 threadCountY;
		public UInt32 threadCountZ;
		public SDL_PropertiesID props;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUColorAttachmentInfo
	{
		public SDL_GPUTexturePtr texture;
		public UInt32 mipLevel;
		public UInt32 layerOrDepthPlane;
		public SDL_FColor clearColor;
		public SDL_GPULoadOp loadOp;
		public SDL_GPUStoreOp storeOp;
		public SDL_bool cycle;
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUDepthStencilAttachmentInfo
	{
		public SDL_GPUTexturePtr texture;
		public SDL_GPUDepthStencilValue depthStencilClearValue;
		public SDL_GPULoadOp loadOp;
		public SDL_GPUStoreOp storeOp;
		public SDL_GPULoadOp stencilLoadOp;
		public SDL_GPUStoreOp stencilStoreOp;
		public SDL_bool cycle;
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}

	/* Binding structs */

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUBufferBinding
	{
		public SDL_GPUBufferPtr buffer;
		public UInt32 offset;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUTextureSamplerBinding
	{
		public SDL_GPUTexturePtr texture;
		public SDL_GPUSamplerPtr sampler;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUStorageBufferWriteOnlyBinding
	{
		public SDL_GPUBufferPtr buffer;
		public SDL_bool cycle;
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}

	
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GPUStorageTextureWriteOnlyBinding
	{
		public SDL_GPUTexturePtr texture;
		public UInt32 mipLevel;
		public UInt32 layer;
		public SDL_bool cycle;
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}

	/* Functions */

	/* Device */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUDevicePtr SDL_CreateGPUDevice(
		SDL_GPUShaderFormat formatFlags,
		SDL_bool debugMode,
		char* name);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUDevicePtr SDL_CreateGPUDeviceWithProperties(
		SDL_PropertiesID props);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DestroyGPUDevice(SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUDriver SDL_GetGPUDriver(SDL_GPUDevicePtr device);

	/* State Creation */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUComputePipelinePtr SDL_CreateGPUComputePipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUComputePipelineCreateInfo* computePipelineCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUGraphicsPipelinePtr SDL_CreateGPUGraphicsPipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUGraphicsPipelineCreateInfo* pipelineCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUSamplerPtr SDL_CreateGPUSampler(
		SDL_GPUDevicePtr device,
		SDL_GPUSamplerCreateInfo* samplerCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUShaderPtr SDL_CreateGPUShader(
		SDL_GPUDevicePtr device,
		SDL_GPUShaderCreateInfo* shaderCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUTexturePtr SDL_CreateGPUTexture(
		SDL_GPUDevicePtr device,
		SDL_GPUTextureCreateInfo* textureCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUBufferPtr SDL_CreateGPUBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUBufferCreateInfo* bufferCreateInfo);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUTransferBufferPtr SDL_CreateGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferCreateInfo* transferBufferCreateInfo);

	/* Debug Naming */

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetGPUBufferName(
		SDL_GPUDevicePtr device,
		SDL_GPUBufferPtr buffer,
		char* text);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetGPUTextureName(
		SDL_GPUDevicePtr device,
		SDL_GPUTexturePtr texture,
		char* text);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_InsertGPUDebugLabel(
		SDL_GPUCommandBufferPtr commandBuffer,
		char* text);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_PushGPUDebugGroup(
		SDL_GPUCommandBufferPtr commandBuffer,
		char* name);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_PopGPUDebugGroup(
		SDL_GPUCommandBufferPtr commandBuffer);

	/* Disposal */

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUTexture(
		SDL_GPUDevicePtr device,
		SDL_GPUTexturePtr texture);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUSampler(
		SDL_GPUDevicePtr device,
		SDL_GPUSamplerPtr sampler);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUBufferPtr buffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferPtr transferBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUComputePipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUComputePipelinePtr computePipeline);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUShader(
		SDL_GPUDevicePtr device,
		SDL_GPUShaderPtr shader);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUGraphicsPipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUGraphicsPipelinePtr graphicsPipeline);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUCommandBufferPtr SDL_AcquireGPUCommandBuffer(
		SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_PushGPUVertexUniformData(
		SDL_GPUCommandBufferPtr commandBuffer,
		UInt32 slotIndex,
		void* data,
		UInt32 dataLengthInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_PushGPUFragmentUniformData(
		SDL_GPUCommandBufferPtr commandBuffer,
		UInt32 slotIndex,
		void* data,
		UInt32 dataLengthInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_PushGPUComputeUniformData(
		SDL_GPUCommandBufferPtr commandBuffer,
		UInt32 slotIndex,
		void* data,
		UInt32 dataLengthInBytes);

	/* Graphics State */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPURenderPassPtr SDL_BeginGPURenderPass(
		SDL_GPUCommandBufferPtr commandBuffer,
		SDL_GPUColorAttachmentInfo* colorAttachmentInfos,
		UInt32 colorAttachmentCount,
		SDL_GPUDepthStencilAttachmentInfo* depthStencilAttachmentInfo);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUGraphicsPipeline(
		SDL_GPURenderPassPtr renderPass,
		SDL_GPUGraphicsPipelinePtr graphicsPipeline);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetGPUViewport(
		SDL_GPURenderPassPtr renderPass,
		SDL_GPUViewport* viewport);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetGPUScissor(
		SDL_GPURenderPassPtr renderPass,
		SDL_Rect* scissor);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetGPUBlendConstants(
		SDL_GPURenderPassPtr renderPass,
		SDL_FColor blendConstants);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetGPUStencilReference(
		SDL_GPURenderPassPtr renderPass,
		byte reference);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUVertexBuffers(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstBinding,
		SDL_GPUBufferBinding* pBindings,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUIndexBuffer(
		SDL_GPURenderPassPtr renderPass,
		SDL_GPUBufferBinding* pBinding,
		SDL_GPUIndexElementSize indexElementSize);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUVertexSamplers(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GPUTextureSamplerBinding* textureSamplerBindings,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUVertexStorageTextures(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GPUTexturePtr* storageTextures,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUVertexStorageBuffers(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GPUBufferPtr* storageBuffers,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUFragmentSamplers(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GPUTextureSamplerBinding* textureSamplerBindings,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUFragmentStorageTextures(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GPUTexturePtr* storageTextures,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUFragmentStorageBuffers(
		SDL_GPURenderPassPtr renderPass,
		UInt32 firstSlot,
		SDL_GPUBufferPtr* storageBuffers,
		UInt32 bindingCount);

	/* Drawing */

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DrawGPUIndexedPrimitives(
		SDL_GPURenderPassPtr renderPass,
		UInt32 indexCount,
		UInt32 instanceCount,
		UInt32 firstIndex,
		Int32 vertexOffset,
		UInt32 firstInstance);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DrawGPUPrimitives(
		SDL_GPURenderPassPtr renderPass,
		UInt32 vertexCount,
		UInt32 instanceCount,
		UInt32 firstVertex,
		UInt32 firstInstance);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DrawGPUPrimitivesIndirect(
		SDL_GPURenderPassPtr renderPass,
		SDL_GPUBufferPtr buffer,
		UInt32 offsetInBytes,
		UInt32 drawCount,
		UInt32 stride);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DrawGPUIndexedPrimitivesIndirect(
		SDL_GPURenderPassPtr renderPass,
		SDL_GPUBufferPtr buffer,
		UInt32 offsetInBytes,
		UInt32 drawCount,
		UInt32 stride);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_EndGPURenderPass(
		SDL_GPURenderPassPtr renderPass);

	/* Compute Pass */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUComputePassPtr SDL_BeginGPUComputePass(
		SDL_GPUCommandBufferPtr commandBuffer,
		SDL_GPUStorageTextureWriteOnlyBinding* storageTextureBindings,
		UInt32 storageTextureBindingCount,
		SDL_GPUStorageBufferWriteOnlyBinding* storageBufferBindings,
		UInt32 storageBufferBindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUComputePipeline(
		SDL_GPUComputePassPtr computePass,
		SDL_GPUComputePipelinePtr computePipeline);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUComputeStorageTextures(
		SDL_GPUComputePassPtr computePass,
		UInt32 firstSlot,
		SDL_GPUTexturePtr* storageTextures,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BindGPUComputeStorageBuffers(
		SDL_GPUComputePassPtr computePass,
		UInt32 firstSlot,
		SDL_GPUBufferPtr* storageBuffers,
		UInt32 bindingCount);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DispatchGPUCompute(
		SDL_GPUComputePassPtr computePass,
		UInt32 groupCountX,
		UInt32 groupCountY,
		UInt32 groupCountZ);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DispatchGPUComputeIndirect(
		SDL_GPUComputePassPtr computePass,
		SDL_GPUBufferPtr buffer,
		UInt32 offsetInBytes);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_EndGPUComputePass(
		SDL_GPUComputePassPtr computePass);

	/* TransferBuffer Data */

	[LibraryImport(DLL)]
	public static unsafe partial void* SDL_MapGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferPtr transferBuffer,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_UnmapGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferPtr transferBuffer);

	/* Copy Pass */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUCopyPassPtr SDL_BeginGPUCopyPass(
		SDL_GPUCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_UploadToGPUTexture(
		SDL_GPUCopyPassPtr copyPass,
		SDL_GPUTextureTransferInfo* source,
		SDL_GPUTextureRegion* destination,
		SDL_bool cycle);

	/* Uploads data from a TransferBuffer to a Buffer. */

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_UploadToGPUBuffer(
		SDL_GPUCopyPassPtr copyPass,
		SDL_GPUTransferBufferLocation* source,
		SDL_GPUBufferRegion* destination,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_CopyGPUTextureToTexture(
		SDL_GPUCopyPassPtr copyPass,
		SDL_GPUTextureLocation* source,
		SDL_GPUTextureLocation* destination,
		UInt32 w,
		UInt32 h,
		UInt32 d,
		SDL_bool cycle);

	/* Copies data from a buffer to a buffer. */

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_CopyGPUBufferToBuffer(
		SDL_GPUCopyPassPtr copyPass,
		SDL_GPUBufferLocation* source,
		SDL_GPUBufferLocation* destination,
		UInt32 size,
		SDL_bool cycle);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DownloadFromGPUTexture(
		SDL_GPUCopyPassPtr copyPass,
		SDL_GPUTextureRegion* source,
		SDL_GPUTextureTransferInfo* destination);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_DownloadFromGPUBuffer(
		SDL_GPUCopyPassPtr copyPass,
		SDL_GPUBufferRegion* source,
		SDL_GPUTransferBufferLocation* destination);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_EndGPUCopyPass(
		SDL_GPUCopyPassPtr copyPass);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_GenerateMipmapsForGPUTexture(
		SDL_GPUCommandBufferPtr commandBuffer,
		SDL_GPUTexturePtr texture);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_BlitGPUTexture(
		SDL_GPUCommandBufferPtr commandBuffer,
		SDL_GPUBlitRegion* source,
		SDL_GPUBlitRegion* destination,
		SDL_FlipMode flipMode,
		SDL_GPUFilter filterMode,
		SDL_bool cycle);

	/* Submission/Presentation */

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_WindowSupportsGPUSwapchainComposition(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window,
		SDL_GPUSwapchainComposition swapchainComposition);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_WindowSupportsGPUPresentMode(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window,
		SDL_GPUPresentMode presentMode);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_ClaimWindowForGPUDevice(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseWindowFromGPUDevice(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_SetGPUSwapchainParameters(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window,
		SDL_GPUSwapchainComposition swapchainComposition,
		SDL_GPUPresentMode presentMode);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUTextureFormat SDL_GetGPUSwapchainTextureFormat(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUTexturePtr SDL_AcquireGPUSwapchainTexture(
		SDL_GPUCommandBufferPtr commandBuffer,
		SDL_WindowPtr window,
		UInt32* pWidth,
		UInt32* pHeight);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SubmitGPUCommandBuffer(
		SDL_GPUCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_GPUFencePtr SDL_SubmitGPUCommandBufferAndAcquireFence(
		SDL_GPUCommandBufferPtr commandBuffer);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_WaitForGPUIdle(
		SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_WaitForGPUFences(
		SDL_GPUDevicePtr device,
		SDL_bool waitAll,
		SDL_GPUFencePtr* pFences,
		UInt32 fenceCount);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_QueryGPUFence(
		SDL_GPUDevicePtr device,
		SDL_GPUFencePtr fence);

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_ReleaseGPUFence(
		SDL_GPUDevicePtr device,
		SDL_GPUFencePtr fence);

	/* Format Info */

	[LibraryImport(DLL)]
	public static unsafe partial UInt32 SDL_GPUTextureFormatTexelBlockSize(
		SDL_GPUTextureFormat textureFormat);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GPUTextureSupportsFormat(
		SDL_GPUDevicePtr device,
		SDL_GPUTextureFormat format,
		SDL_GPUTextureType type,
		SDL_GPUTextureUsageFlags usage);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_bool SDL_GPUTextureSupportsSampleCount(
		SDL_GPUDevicePtr device,
		SDL_GPUTextureFormat format,
		SDL_GPUSampleCount sampleCount);

}