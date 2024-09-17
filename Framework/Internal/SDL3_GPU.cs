using System.Runtime.InteropServices;

using SDL_WindowPtr = nint;
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

internal static unsafe partial class SDL3
{
	public enum SDL_GPUPrimitiveType
	{
		SDL_GPU_PRIMITIVETYPE_TRIANGLELIST, 
		SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP, 
		SDL_GPU_PRIMITIVETYPE_LINELIST, 
		SDL_GPU_PRIMITIVETYPE_LINESTRIP,  
		SDL_GPU_PRIMITIVETYPE_POINTLIST 
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
		SDL_GPU_STOREOP_DONT_CARE,  
		SDL_GPU_STOREOP_RESOLVE,  
		SDL_GPU_STOREOP_RESOLVE_AND_STORE 
	}
	public enum SDL_GPUIndexElementSize
	{
		SDL_GPU_INDEXELEMENTSIZE_16BIT, 
		SDL_GPU_INDEXELEMENTSIZE_32BIT 
	}
	public enum SDL_GPUTextureFormat
	{
		SDL_GPU_TEXTUREFORMAT_INVALID,
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
		SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM,
		SDL_GPU_TEXTUREFORMAT_BC6H_RGB_FLOAT,
		SDL_GPU_TEXTUREFORMAT_BC6H_RGB_UFLOAT,
		SDL_GPU_TEXTUREFORMAT_R8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16G16_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM,
		SDL_GPU_TEXTUREFORMAT_R16_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT,
		SDL_GPU_TEXTUREFORMAT_R8_UINT,
		SDL_GPU_TEXTUREFORMAT_R8G8_UINT,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT,
		SDL_GPU_TEXTUREFORMAT_R16_UINT,
		SDL_GPU_TEXTUREFORMAT_R16G16_UINT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT,
		SDL_GPU_TEXTUREFORMAT_R8_INT,
		SDL_GPU_TEXTUREFORMAT_R8G8_INT,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT,
		SDL_GPU_TEXTUREFORMAT_R16_INT,
		SDL_GPU_TEXTUREFORMAT_R16G16_INT,
		SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT,
		SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB,
		SDL_GPU_TEXTUREFORMAT_D16_UNORM,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM,
		SDL_GPU_TEXTUREFORMAT_D32_FLOAT,
		SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
		SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT
	}
	[Flags]
	public enum SDL_GPUTextureUsageFlags : UInt32
	{
		SDL_GPU_TEXTUREUSAGE_SAMPLER  = (1u << 0),
		SDL_GPU_TEXTUREUSAGE_COLOR_TARGET = (1u << 1),
		SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET = (1u << 2),
		SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ = (1u << 3),
		SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ = (1u << 4),
		SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE = (1u << 5),
	}
	public enum SDL_GPUTextureType
	{
		SDL_GPU_TEXTURETYPE_2D,  
		SDL_GPU_TEXTURETYPE_2D_ARRAY,  
		SDL_GPU_TEXTURETYPE_3D,  
		SDL_GPU_TEXTURETYPE_CUBE,  
		SDL_GPU_TEXTURETYPE_CUBE_ARRAY 
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
	[Flags]
	public enum SDL_GPUBufferUsageFlags : UInt32
	{
		SDL_GPU_BUFFERUSAGE_VERTEX = (1u << 0),
		SDL_GPU_BUFFERUSAGE_INDEX  = (1u << 1),
		SDL_GPU_BUFFERUSAGE_INDIRECT = (1u << 2),
		SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ = (1u << 3),
		SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ = (1u << 4),
		SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE = (1u << 5),
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
	[Flags]
	public enum SDL_GPUShaderFormat : UInt32
	{
		SDL_GPU_SHADERFORMAT_INVALID = 0,
		SDL_GPU_SHADERFORMAT_PRIVATE = (1u << 0),
		SDL_GPU_SHADERFORMAT_SPIRV = (1u << 1),
		SDL_GPU_SHADERFORMAT_DXBC  = (1u << 2),
		SDL_GPU_SHADERFORMAT_DXIL  = (1u << 3),
		SDL_GPU_SHADERFORMAT_MSL = (1u << 4),
		SDL_GPU_SHADERFORMAT_METALLIB = (1u << 5),
	}
	public enum SDL_GPUVertexElementFormat
	{
		SDL_GPU_VERTEXELEMENTFORMAT_INVALID,
		SDL_GPU_VERTEXELEMENTFORMAT_INT,
		SDL_GPU_VERTEXELEMENTFORMAT_INT2,
		SDL_GPU_VERTEXELEMENTFORMAT_INT3,
		SDL_GPU_VERTEXELEMENTFORMAT_INT4,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT2,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT3,
		SDL_GPU_VERTEXELEMENTFORMAT_UINT4,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
		SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE2,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2,
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_BYTE4_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT2,
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT4,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_SHORT4_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT2_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_USHORT4_NORM,
		SDL_GPU_VERTEXELEMENTFORMAT_HALF2,
		SDL_GPU_VERTEXELEMENTFORMAT_HALF4
	}
	public enum SDL_GPUVertexInputRate
	{
		SDL_GPU_VERTEXINPUTRATE_VERTEX,  
		SDL_GPU_VERTEXINPUTRATE_INSTANCE 
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
		SDL_GPU_COMPAREOP_INVALID,
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
		SDL_GPU_STENCILOP_INVALID,
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
		SDL_GPU_BLENDOP_INVALID,
		SDL_GPU_BLENDOP_ADD,  
		SDL_GPU_BLENDOP_SUBTRACT, 
		SDL_GPU_BLENDOP_REVERSE_SUBTRACT, 
		SDL_GPU_BLENDOP_MIN,  
		SDL_GPU_BLENDOP_MAX 
	}
	public enum SDL_GPUBlendFactor
	{
		SDL_GPU_BLENDFACTOR_INVALID,
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
	public enum SDL_GPUColorComponentFlags : byte
	{
		SDL_GPU_COLORCOMPONENT_R = (byte)(1u << 0),
		SDL_GPU_COLORCOMPONENT_G = (byte)(1u << 1),
		SDL_GPU_COLORCOMPONENT_B = (byte)(1u << 2),
		SDL_GPU_COLORCOMPONENT_A = (byte)(1u << 3),
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
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUViewport
	{
		public float x; 
		public float y; 
		public float w; 
		public float h; 
		public float min_depth; 
		public float max_depth; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTextureTransferInfo
	{
		public SDL_GPUTransferBufferPtr transfer_buffer; 
		public UInt32 offset;  
		public UInt32 pixels_per_row;  
		public UInt32 rows_per_layer;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTransferBufferLocation
	{
		public SDL_GPUTransferBufferPtr transfer_buffer; 
		public UInt32 offset;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTextureLocation
	{
		public SDL_GPUTexturePtr texture; 
		public UInt32 mip_level;  
		public UInt32 layer;  
		public UInt32 x;  
		public UInt32 y;  
		public UInt32 z;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTextureRegion
	{
		public SDL_GPUTexturePtr texture; 
		public UInt32 mip_level;  
		public UInt32 layer;  
		public UInt32 x;  
		public UInt32 y;  
		public UInt32 z;  
		public UInt32 w;  
		public UInt32 h;  
		public UInt32 d;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUBlitRegion
	{
		public SDL_GPUTexturePtr texture; 
		public UInt32 mip_level;  
		public UInt32 layer_or_depth_plane; 
		public UInt32 x;  
		public UInt32 y;  
		public UInt32 w;  
		public UInt32 h;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUBufferLocation
	{
		public SDL_GPUBufferPtr buffer; 
		public UInt32 offset; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUBufferRegion
	{
		public SDL_GPUBufferPtr buffer; 
		public UInt32 offset; 
		public UInt32 size; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUIndirectDrawCommand
	{
		public UInt32 num_vertices;  
		public UInt32 num_instances; 
		public UInt32 first_vertex;  
		public UInt32 first_instance; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUIndexedIndirectDrawCommand
	{
		public UInt32 num_indices; 
		public UInt32 num_instances; 
		public UInt32 first_index; 
		public Int32 vertex_offset; 
		public UInt32 first_instance; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUIndirectDispatchCommand
	{
		public UInt32 groupcount_x; 
		public UInt32 groupcount_y; 
		public UInt32 groupcount_z; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUSamplerCreateInfo
	{
		public SDL_GPUFilter min_filter; 
		public SDL_GPUFilter mag_filter; 
		public SDL_GPUSamplerMipmapMode mipmap_mode; 
		public SDL_GPUSamplerAddressMode address_mode_u; 
		public SDL_GPUSamplerAddressMode address_mode_v; 
		public SDL_GPUSamplerAddressMode address_mode_w; 
		public float mip_lod_bias; 
		public float max_anisotropy; 
		public SDL_GPUCompareOp compare_op;  
		public float min_lod;  
		public float max_lod;  
		public bool enable_anisotropy; 
		public bool enable_compare;  
		public byte padding1;
		public byte padding2;
		public SDL_PropertiesID props; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUVertexBufferDescription
	{
		public UInt32 slot; 
		public UInt32 pitch;  
		public SDL_GPUVertexInputRate input_rate; 
		public UInt32 instance_step_rate; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUVertexAttribute
	{
		public UInt32 location; 
		public UInt32 buffer_slot;  
		public SDL_GPUVertexElementFormat format; 
		public UInt32 offset; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUVertexInputState
	{
		public SDL_GPUVertexBufferDescription* vertex_buffer_descriptions; 
		public UInt32 num_vertex_buffers; 
		public SDL_GPUVertexAttribute* vertex_attributes; 
		public UInt32 num_vertex_attributes;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUStencilOpState
	{
		public SDL_GPUStencilOp fail_op; 
		public SDL_GPUStencilOp pass_op; 
		public SDL_GPUStencilOp depth_fail_op; 
		public SDL_GPUCompareOp compare_op;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUColorTargetBlendState
	{
		public SDL_GPUBlendFactor src_color_blendfactor;  
		public SDL_GPUBlendFactor dst_color_blendfactor;  
		public SDL_GPUBlendOp color_blend_op; 
		public SDL_GPUBlendFactor src_alpha_blendfactor;  
		public SDL_GPUBlendFactor dst_alpha_blendfactor;  
		public SDL_GPUBlendOp alpha_blend_op; 
		public SDL_GPUColorComponentFlags color_write_mask; 
		public bool enable_blend; 
		public bool enable_color_write_mask;  
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUShaderCreateInfo
	{
		public UIntPtr code_size;  
		public byte* code; 
		public nint entrypoint;  
		public SDL_GPUShaderFormat format;  
		public SDL_GPUShaderStage stage;  
		public UInt32 num_samplers; 
		public UInt32 num_storage_textures; 
		public UInt32 num_storage_buffers;  
		public UInt32 num_uniform_buffers;  
		public SDL_PropertiesID props;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTextureCreateInfo
	{
		public SDL_GPUTextureType type; 
		public SDL_GPUTextureFormat format; 
		public SDL_GPUTextureUsageFlags usage;  
		public UInt32 width;  
		public UInt32 height; 
		public UInt32 layer_count_or_depth; 
		public UInt32 num_levels; 
		public SDL_GPUSampleCount sample_count; 
		public SDL_PropertiesID props;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUBufferCreateInfo
	{
		public SDL_GPUBufferUsageFlags usage; 
		public UInt32 size; 
		public SDL_PropertiesID props;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTransferBufferCreateInfo
	{
		public SDL_GPUTransferBufferUsage usage; 
		public UInt32 size;  
		public SDL_PropertiesID props; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPURasterizerState
	{
		public SDL_GPUFillMode fill_mode;  
		public SDL_GPUCullMode cull_mode;  
		public SDL_GPUFrontFace front_face;  
		public float depth_bias_constant_factor; 
		public float depth_bias_clamp; 
		public float depth_bias_slope_factor;  
		public bool enable_depth_bias; 
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUMultisampleState
	{
		public SDL_GPUSampleCount sample_count; 
		public UInt32 sample_mask;  
		public bool enable_mask;  
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUDepthStencilState
	{
		public SDL_GPUCompareOp compare_op; 
		public SDL_GPUStencilOpState back_stencil_state;  
		public SDL_GPUStencilOpState front_stencil_state; 
		public byte compare_mask;  
		public byte write_mask;  
		public bool enable_depth_test;  
		public bool enable_depth_write; 
		public bool enable_stencil_test;  
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUColorTargetDescription
	{
		public SDL_GPUTextureFormat format;  
		public SDL_GPUColorTargetBlendState blend_state; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUGraphicsPipelineTargetInfo
	{
		public SDL_GPUColorTargetDescription* color_target_descriptions; 
		public UInt32 num_color_targets; 
		public SDL_GPUTextureFormat depth_stencil_format;  
		public bool has_depth_stencil_target;  
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUGraphicsPipelineCreateInfo
	{
		public SDL_GPUShaderPtr vertex_shader;  
		public SDL_GPUShaderPtr fragment_shader;  
		public SDL_GPUVertexInputState vertex_input_state;  
		public SDL_GPUPrimitiveType primitive_type; 
		public SDL_GPURasterizerState rasterizer_state; 
		public SDL_GPUMultisampleState multisample_state; 
		public SDL_GPUDepthStencilState depth_stencil_state;  
		public SDL_GPUGraphicsPipelineTargetInfo target_info; 
		public SDL_PropertiesID props;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUComputePipelineCreateInfo
	{
		public UIntPtr code_size;  
		public byte* code; 
		public nint entrypoint;  
		public SDL_GPUShaderFormat format;  
		public UInt32 num_samplers; 
		public UInt32 num_readonly_storage_textures;  
		public UInt32 num_readonly_storage_buffers; 
		public UInt32 num_writeonly_storage_textures; 
		public UInt32 num_writeonly_storage_buffers;  
		public UInt32 num_uniform_buffers;  
		public UInt32 threadcount_x;  
		public UInt32 threadcount_y;  
		public UInt32 threadcount_z;  
		public SDL_PropertiesID props;  
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUColorTargetInfo
	{
		public SDL_GPUTexturePtr texture;  
		public UInt32 mip_level; 
		public UInt32 layer_or_depth_plane;  
		public SDL_FColor clear_color; 
		public SDL_GPULoadOp load_op;  
		public SDL_GPUStoreOp store_op;  
		public SDL_GPUTexturePtr resolve_texture; 
		public UInt32 resolve_mip_level; 
		public UInt32 resolve_layer; 
		public bool cycle; 
		public bool cycle_resolve_texture; 
		public byte padding1;
		public byte padding2;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUDepthStencilTargetInfo
	{
		public SDL_GPUTexturePtr texture;  
		public float clear_depth;  
		public SDL_GPULoadOp load_op;  
		public SDL_GPUStoreOp store_op;  
		public SDL_GPULoadOp stencil_load_op;  
		public SDL_GPUStoreOp stencil_store_op;  
		public bool cycle; 
		public byte clear_stencil;  
		public byte padding1;
		public byte padding2;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUBlitInfo {
		public SDL_GPUBlitRegion source;  
		public SDL_GPUBlitRegion destination; 
		public SDL_GPULoadOp load_op; 
		public SDL_FColor clear_color;  
		public SDL_FlipMode flip_mode;  
		public SDL_GPUFilter filter;  
		public bool cycle;  
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUBufferBinding
	{
		public SDL_GPUBufferPtr buffer; 
		public UInt32 offset; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUTextureSamplerBinding
	{
		public SDL_GPUTexturePtr texture; 
		public SDL_GPUSamplerPtr sampler; 
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUStorageBufferWriteOnlyBinding
	{
		public SDL_GPUBufferPtr buffer; 
		public bool cycle;  
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GPUStorageTextureWriteOnlyBinding
	{
		public SDL_GPUTexturePtr texture; 
		public UInt32 mip_level;  
		public UInt32 layer;  
		public bool cycle;  
		public byte padding1;
		public byte padding2;
		public byte padding3;
	}

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_GPUSupportsShaderFormats(
		SDL_GPUShaderFormat format_flags,
		nint name);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_GPUSupportsProperties(
		SDL_PropertiesID props);

	[LibraryImport(DLL)]
	public static partial SDL_GPUDevicePtr SDL_CreateGPUDevice(
		SDL_GPUShaderFormat format_flags,
		[MarshalAs(UnmanagedType.U1)] bool debug_mode,
		nint name);

	[LibraryImport(DLL)]
	public static partial SDL_GPUDevicePtr SDL_CreateGPUDeviceWithProperties(
		SDL_PropertiesID props);

	[LibraryImport(DLL)]
	public static partial void SDL_DestroyGPUDevice(SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static partial int SDL_GetNumGPUDrivers();

	[LibraryImport(DLL)]
	public static partial nint SDL_GetGPUDriver(int index);

	[LibraryImport(DLL)]
	public static partial nint SDL_GetGPUDeviceDriver(SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static partial SDL_GPUShaderFormat SDL_GetGPUShaderFormats(SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static partial SDL_GPUComputePipelinePtr SDL_CreateGPUComputePipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUComputePipelineCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial SDL_GPUGraphicsPipelinePtr SDL_CreateGPUGraphicsPipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUGraphicsPipelineCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial SDL_GPUSamplerPtr SDL_CreateGPUSampler(
		SDL_GPUDevicePtr device,
		SDL_GPUSamplerCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial SDL_GPUShaderPtr SDL_CreateGPUShader(
		SDL_GPUDevicePtr device,
		SDL_GPUShaderCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial SDL_GPUTexturePtr SDL_CreateGPUTexture(
		SDL_GPUDevicePtr device,
		SDL_GPUTextureCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial SDL_GPUBufferPtr SDL_CreateGPUBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUBufferCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial SDL_GPUTransferBufferPtr SDL_CreateGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferCreateInfo* createinfo);

	[LibraryImport(DLL)]
	public static partial void SDL_SetGPUBufferName(
		SDL_GPUDevicePtr device,
		SDL_GPUBufferPtr buffer,
		nint text);

	[LibraryImport(DLL)]
	public static partial void SDL_SetGPUTextureName(
		SDL_GPUDevicePtr device,
		SDL_GPUTexturePtr texture,
		nint text);

	[LibraryImport(DLL)]
	public static partial void SDL_InsertGPUDebugLabel(
		SDL_GPUCommandBufferPtr command_buffer,
		nint text);

	[LibraryImport(DLL)]
	public static partial void SDL_PushGPUDebugGroup(
		SDL_GPUCommandBufferPtr command_buffer,
		nint name);

	[LibraryImport(DLL)]
	public static partial void SDL_PopGPUDebugGroup(
		SDL_GPUCommandBufferPtr command_buffer);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUTexture(
		SDL_GPUDevicePtr device,
		SDL_GPUTexturePtr texture);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUSampler(
		SDL_GPUDevicePtr device,
		SDL_GPUSamplerPtr sampler);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUBufferPtr buffer);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferPtr transfer_buffer);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUComputePipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUComputePipelinePtr compute_pipeline);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUShader(
		SDL_GPUDevicePtr device,
		SDL_GPUShaderPtr shader);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUGraphicsPipeline(
		SDL_GPUDevicePtr device,
		SDL_GPUGraphicsPipelinePtr graphics_pipeline);

	[LibraryImport(DLL)]
	public static partial SDL_GPUCommandBufferPtr SDL_AcquireGPUCommandBuffer(
		SDL_GPUDevicePtr device);
	[LibraryImport(DLL)]
	public static partial void SDL_PushGPUVertexUniformData(
		SDL_GPUCommandBufferPtr command_buffer,
		UInt32 slot_index,
		void* data,
		UInt32 length);

	[LibraryImport(DLL)]
	public static partial void SDL_PushGPUFragmentUniformData(
		SDL_GPUCommandBufferPtr command_buffer,
		UInt32 slot_index,
		void* data,
		UInt32 length);

	[LibraryImport(DLL)]
	public static partial void SDL_PushGPUComputeUniformData(
		SDL_GPUCommandBufferPtr command_buffer,
		UInt32 slot_index,
		void* data,
		UInt32 length);
	[LibraryImport(DLL)]
	public static partial SDL_GPURenderPassPtr SDL_BeginGPURenderPass(
		SDL_GPUCommandBufferPtr command_buffer,
		SDL_GPUColorTargetInfo* color_target_infos,
		UInt32 num_color_targets,
		SDL_GPUDepthStencilTargetInfo* depth_stencil_target_info);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUGraphicsPipeline(
		SDL_GPURenderPassPtr render_pass,
		SDL_GPUGraphicsPipelinePtr graphics_pipeline);

	[LibraryImport(DLL)]
	public static partial void SDL_SetGPUViewport(
		SDL_GPURenderPassPtr render_pass,
		SDL_GPUViewport* viewport);

	[LibraryImport(DLL)]
	public static partial void SDL_SetGPUScissor(
		SDL_GPURenderPassPtr render_pass,
		SDL_Rect* scissor);

	[LibraryImport(DLL)]
	public static partial void SDL_SetGPUBlendConstants(
		SDL_GPURenderPassPtr render_pass,
		SDL_FColor blend_constants);

	[LibraryImport(DLL)]
	public static partial void SDL_SetGPUStencilReference(
		SDL_GPURenderPassPtr render_pass,
		byte reference);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUVertexBuffers(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUBufferBinding* bindings,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUIndexBuffer(
		SDL_GPURenderPassPtr render_pass,
		SDL_GPUBufferBinding* binding,
		SDL_GPUIndexElementSize index_element_size);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUVertexSamplers(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUTextureSamplerBinding* texture_sampler_bindings,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUVertexStorageTextures(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUTexturePtr* storage_textures,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUVertexStorageBuffers(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUBufferPtr* storage_buffers,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUFragmentSamplers(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUTextureSamplerBinding* texture_sampler_bindings,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUFragmentStorageTextures(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUTexturePtr* storage_textures,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUFragmentStorageBuffers(
		SDL_GPURenderPassPtr render_pass,
		UInt32 first_slot,
		SDL_GPUBufferPtr* storage_buffers,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_DrawGPUIndexedPrimitives(
		SDL_GPURenderPassPtr render_pass,
		UInt32 num_indices,
		UInt32 num_instances,
		UInt32 first_index,
		Int32 vertex_offset,
		UInt32 first_instance);

	[LibraryImport(DLL)]
	public static partial void SDL_DrawGPUPrimitives(
		SDL_GPURenderPassPtr render_pass,
		UInt32 num_vertices,
		UInt32 num_instances,
		UInt32 first_vertex,
		UInt32 first_instance);

	[LibraryImport(DLL)]
	public static partial void SDL_DrawGPUPrimitivesIndirect(
		SDL_GPURenderPassPtr render_pass,
		SDL_GPUBufferPtr buffer,
		UInt32 offset,
		UInt32 draw_count);

	[LibraryImport(DLL)]
	public static partial void SDL_DrawGPUIndexedPrimitivesIndirect(
		SDL_GPURenderPassPtr render_pass,
		SDL_GPUBufferPtr buffer,
		UInt32 offset,
		UInt32 draw_count);

	[LibraryImport(DLL)]
	public static partial void SDL_EndGPURenderPass(
		SDL_GPURenderPassPtr render_pass);

	[LibraryImport(DLL)]
	public static partial SDL_GPUComputePassPtr SDL_BeginGPUComputePass(
		SDL_GPUCommandBufferPtr command_buffer,
		SDL_GPUStorageTextureWriteOnlyBinding* storage_texture_bindings,
		UInt32 num_storage_texture_bindings,
		SDL_GPUStorageBufferWriteOnlyBinding* storage_buffer_bindings,
		UInt32 num_storage_buffer_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUComputePipeline(
		SDL_GPUComputePassPtr compute_pass,
		SDL_GPUComputePipelinePtr compute_pipeline);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUComputeSamplers(
		SDL_GPUComputePassPtr compute_pass,
		UInt32 first_slot,
		SDL_GPUTextureSamplerBinding* texture_sampler_bindings,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUComputeStorageTextures(
		SDL_GPUComputePassPtr compute_pass,
		UInt32 first_slot,
		SDL_GPUTexturePtr* storage_textures,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_BindGPUComputeStorageBuffers(
		SDL_GPUComputePassPtr compute_pass,
		UInt32 first_slot,
		SDL_GPUBufferPtr* storage_buffers,
		UInt32 num_bindings);

	[LibraryImport(DLL)]
	public static partial void SDL_DispatchGPUCompute(
		SDL_GPUComputePassPtr compute_pass,
		UInt32 groupcount_x,
		UInt32 groupcount_y,
		UInt32 groupcount_z);

	[LibraryImport(DLL)]
	public static partial void SDL_DispatchGPUComputeIndirect(
		SDL_GPUComputePassPtr compute_pass,
		SDL_GPUBufferPtr buffer,
		UInt32 offset);

	[LibraryImport(DLL)]
	public static partial void SDL_EndGPUComputePass(
		SDL_GPUComputePassPtr compute_pass);

	[LibraryImport(DLL)]
	public static partial void* SDL_MapGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferPtr transfer_buffer,
		[MarshalAs(UnmanagedType.U1)] bool cycle);

	[LibraryImport(DLL)]
	public static partial void SDL_UnmapGPUTransferBuffer(
		SDL_GPUDevicePtr device,
		SDL_GPUTransferBufferPtr transfer_buffer);

	[LibraryImport(DLL)]
	public static partial SDL_GPUCopyPassPtr SDL_BeginGPUCopyPass(
		SDL_GPUCommandBufferPtr command_buffer);

	[LibraryImport(DLL)]
	public static partial void SDL_UploadToGPUTexture(
		SDL_GPUCopyPassPtr copy_pass,
		SDL_GPUTextureTransferInfo* source,
		SDL_GPUTextureRegion* destination,
		[MarshalAs(UnmanagedType.U1)] bool cycle);

	[LibraryImport(DLL)]
	public static partial void SDL_UploadToGPUBuffer(
		SDL_GPUCopyPassPtr copy_pass,
		SDL_GPUTransferBufferLocation* source,
		SDL_GPUBufferRegion* destination,
		[MarshalAs(UnmanagedType.U1)] bool cycle);

	[LibraryImport(DLL)]
	public static partial void SDL_CopyGPUTextureToTexture(
		SDL_GPUCopyPassPtr copy_pass,
		SDL_GPUTextureLocation* source,
		SDL_GPUTextureLocation* destination,
		UInt32 w,
		UInt32 h,
		UInt32 d,
		[MarshalAs(UnmanagedType.U1)] bool cycle);

	[LibraryImport(DLL)]
	public static partial void SDL_CopyGPUBufferToBuffer(
		SDL_GPUCopyPassPtr copy_pass,
		SDL_GPUBufferLocation* source,
		SDL_GPUBufferLocation* destination,
		UInt32 size,
		[MarshalAs(UnmanagedType.U1)] bool cycle);

	[LibraryImport(DLL)]
	public static partial void SDL_DownloadFromGPUTexture(
		SDL_GPUCopyPassPtr copy_pass,
		SDL_GPUTextureRegion* source,
		SDL_GPUTextureTransferInfo* destination);

	[LibraryImport(DLL)]
	public static partial void SDL_DownloadFromGPUBuffer(
		SDL_GPUCopyPassPtr copy_pass,
		SDL_GPUBufferRegion* source,
		SDL_GPUTransferBufferLocation* destination);

	[LibraryImport(DLL)]
	public static partial void SDL_EndGPUCopyPass(
		SDL_GPUCopyPassPtr copy_pass);

	[LibraryImport(DLL)]
	public static partial void SDL_GenerateMipmapsForGPUTexture(
		SDL_GPUCommandBufferPtr command_buffer,
		SDL_GPUTexturePtr texture);

	[LibraryImport(DLL)]
	public static partial void SDL_BlitGPUTexture(
		SDL_GPUCommandBufferPtr command_buffer,
		SDL_GPUBlitInfo* info);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_WindowSupportsGPUSwapchainComposition(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window,
		SDL_GPUSwapchainComposition swapchain_composition);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_WindowSupportsGPUPresentMode(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window,
		SDL_GPUPresentMode present_mode);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_ClaimWindowForGPUDevice(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseWindowFromGPUDevice(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_SetGPUSwapchainParameters(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window,
		SDL_GPUSwapchainComposition swapchain_composition,
		SDL_GPUPresentMode present_mode);

	[LibraryImport(DLL)]
	public static partial SDL_GPUTextureFormat SDL_GetGPUSwapchainTextureFormat(
		SDL_GPUDevicePtr device,
		SDL_WindowPtr window);

	[LibraryImport(DLL)]
	public static partial SDL_GPUTexturePtr SDL_AcquireGPUSwapchainTexture(
		SDL_GPUCommandBufferPtr command_buffer,
		SDL_WindowPtr window,
		UInt32* w,
		UInt32* h);

	[LibraryImport(DLL)]
	public static partial void SDL_SubmitGPUCommandBuffer(
		SDL_GPUCommandBufferPtr command_buffer);

	[LibraryImport(DLL)]
	public static partial SDL_GPUFencePtr SDL_SubmitGPUCommandBufferAndAcquireFence(
		SDL_GPUCommandBufferPtr command_buffer);

	[LibraryImport(DLL)]
	public static partial void SDL_WaitForGPUIdle(
		SDL_GPUDevicePtr device);

	[LibraryImport(DLL)]
	public static partial void SDL_WaitForGPUFences(
		SDL_GPUDevicePtr device,
		[MarshalAs(UnmanagedType.U1)] bool wait_all,
		SDL_GPUFencePtr *fences,
		UInt32 num_fences);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_QueryGPUFence(
		SDL_GPUDevicePtr device,
		SDL_GPUFencePtr fence);

	[LibraryImport(DLL)]
	public static partial void SDL_ReleaseGPUFence(
		SDL_GPUDevicePtr device,
		SDL_GPUFencePtr fence);

	[LibraryImport(DLL)]
	public static partial UInt32 SDL_GPUTextureFormatTexelBlockSize(
		SDL_GPUTextureFormat format);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_GPUTextureSupportsFormat(
		SDL_GPUDevicePtr device,
		SDL_GPUTextureFormat format,
		SDL_GPUTextureType type,
		SDL_GPUTextureUsageFlags usage);

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.U1)] 
	public static partial bool SDL_GPUTextureSupportsSampleCount(
		SDL_GPUDevicePtr device,
		SDL_GPUTextureFormat format,
		SDL_GPUSampleCount sample_count);

}