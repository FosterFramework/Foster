using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Foster.Framework.SDL3;

namespace Foster.Framework;

internal static unsafe partial class Renderer
{
	private struct TextureResource
	{
		public nint Device;
		public nint Texture;
		public nint TransferBuffer;
		public int Width;
		public int Height;
		public SDL_GPUTextureFormat Format;
	}

	private struct MeshResource
	{
		public nint Device;
		public BufferResource Index;
		public BufferResource Vertex;
		public BufferResource Instance;
		public IndexFormat IndexFormat;
		public VertexFormat VertexFormat;
	}

	private struct BufferResource
	{
		public nint Buffer;
		public nint TransferBuffer;
		public int  Capacity;
	}

	private struct ShaderResource
	{
		public nint Device;
		public nint VertexShader;
		public nint FragmentShader;
	}

	private struct ClearInfo
	{
		public Color? Color;
		public float? Depth;
		public int? Stencil;
	}

	private static nint cmd;
	private static nint renderPass;
	private static nint copyPass;
	private static TextureResource swapchain;
	private static Target? renderPassTarget;
	private static bool supportsD24S8;
	private static readonly Dictionary<int, nint> graphicsPipelines = [];
	private static readonly Dictionary<TextureSampler, nint> samplers = [];
	
	public static void Startup()
	{
		cmd = nint.Zero;
		renderPass = nint.Zero;
		copyPass = nint.Zero;
		swapchain = default;
		renderPassTarget = null;
		graphicsPipelines.Clear();

		supportsD24S8 = SDL_GPUTextureSupportsFormat(
			Platform.Device,
			SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
			SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
			SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) != 0;

		AcquireCommandBuffers();
	}

	public static void Shutdown()
	{
		Flush(true);
	}

	public static void Present()
	{
		Flush(false);
		AcquireCommandBuffers();
	}

	public static nint TextureCreate(int width, int height, TextureFormat format, bool isTarget)
	{
		SDL_GPUTextureCreateInfo info = new()
		{
			type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
			format = format switch
			{
				TextureFormat.R8G8B8A8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
				TextureFormat.R8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM,
				TextureFormat.Depth24Stencil8 => supportsD24S8
					? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT
					: SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT,
				_ => throw new InvalidEnumArgumentException()
			},
			usageFlags = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
			width = (uint)width,
			height = (uint)height,
			layerCountOrDepth = 1,
			levelCount = 1,
			sampleCount = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
		};

		if (isTarget)
		{
			if (format == TextureFormat.Depth24Stencil8)
				info.usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
			else
				info.usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
		}

		nint texture = SDL_CreateGPUTexture(Platform.Device, &info);
		if (texture == nint.Zero)
			throw new Exception($"Failed to create Texture: {Platform.GetSDLError()}");

		TextureResource* res = (TextureResource*)Marshal.AllocHGlobal(sizeof(TextureResource));
		*res = new TextureResource()
		{
			Device = Platform.Device,
			Texture = texture,
			TransferBuffer = nint.Zero,
			Width = width,
			Height = height,
			Format = info.format
		};
		return new nint(res);
	}

	public static void TextureSetData(nint texture, void* data, int length)
	{
		// get texture
		TextureResource* props = (TextureResource*)texture;

		// make sure transfer buffer exists
		if (props->TransferBuffer == nint.Zero)
		{
			SDL_GPUTransferBufferCreateInfo info = new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				sizeInBytes = (uint)length,
			};
			props->TransferBuffer = SDL_CreateGPUTransferBuffer(Platform.Device, &info);
		}

		// copy data
		{
			var dst = SDL_MapGPUTransferBuffer(Platform.Device, props->TransferBuffer, 1);
			Buffer.MemoryCopy(data, dst, length, length);
			SDL_UnmapGPUTransferBuffer(Platform.Device, props->TransferBuffer);
		}

		// upload to the GPU
		{
			CopyPassBegin();

			SDL_GPUTextureTransferInfo info = new()
			{
				transferBuffer = props->TransferBuffer,
				offset = 0,
				imagePitch = (uint)props->Width,
				imageHeight = (uint)props->Height,
			};

			SDL_GPUTextureRegion region = new()
			{
				texture = props->Texture,
				w = (uint)props->Width,
				h = (uint)props->Height
			};

			SDL_UploadToGPUTexture(copyPass, &info, &region, 1);
		}
	}

	public static void TextureGetData(nint texture, void* data, int length)
	{
		throw new NotImplementedException();
	}

	public static void TextureDestroy(nint texture)
	{
		TextureResource* res = (TextureResource*)texture;
		if (res->Device != Platform.Device)
			return;

		if (res->TransferBuffer != nint.Zero)
			SDL_ReleaseGPUTransferBuffer(Platform.Device, res->TransferBuffer);

		SDL_ReleaseGPUTexture(Platform.Device, res->Texture);
		Marshal.FreeHGlobal(texture);
	}

	public static nint MeshCreate()
	{
		MeshResource* res = (MeshResource*)Marshal.AllocHGlobal(sizeof(MeshResource));
		*res = new MeshResource()
		{
			Device = Platform.Device
		};
		return new nint(res);
	}

	public static void MeshSetVertexData(nint mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format)
	{
		MeshResource* res = (MeshResource*)mesh;
		res->VertexFormat = format;
		MeshUploadBuffer(&res->Vertex, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX);
	}

	public static void MeshSetIndexData(nint mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format)
	{
		MeshResource* res = (MeshResource*)mesh;
		res->IndexFormat = format;
		MeshUploadBuffer(&res->Index, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX);
	}

	public static void MeshDestroy(nint mesh)
	{
		MeshResource* res = (MeshResource*)mesh;
		if (res->Device != Platform.Device)
			return;

		MeshDestroyBuffer(&res->Vertex);
		MeshDestroyBuffer(&res->Index);
		MeshDestroyBuffer(&res->Instance);

		Marshal.FreeHGlobal(mesh);
	}

	private static void MeshUploadBuffer(BufferResource* res, nint data, int dataSize, int dataDestOffset, SDL_GPUBufferUsageFlags usage)
	{
		// recreate buffer
		var required = dataSize + dataDestOffset;
		if (required > res->Capacity ||
			res->Buffer == nint.Zero)
		{
			if (res->Buffer != nint.Zero)
			{
				SDL_ReleaseGPUBuffer(Platform.Device, res->Buffer);
				SDL_ReleaseGPUTransferBuffer(Platform.Device, res->TransferBuffer);
				res->Buffer = nint.Zero;
				res->TransferBuffer = nint.Zero;
			}

			var size = Math.Max(res->Capacity, 8);
			while (size < required)
				size *= 2;

			SDL_GPUBufferCreateInfo info = new()
			{
				usageFlags = usage,
				sizeInBytes = (uint)size
			};
			
			res->Buffer = SDL_CreateGPUBuffer(Platform.Device, &info);
			if (res->Buffer == nint.Zero)
				throw new Exception($"Failed to create Mesh: {Platform.GetSDLError()}");
			res->Capacity = size;
		}

		// make sure transfer buffer exists
		if (res->TransferBuffer == nint.Zero)
		{
			SDL_GPUTransferBufferCreateInfo info = new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				sizeInBytes = (uint)res->Capacity,
				props = 0
			};
			res->TransferBuffer = SDL_CreateGPUTransferBuffer(Platform.Device, &info);
		}

		// copy data
		{
			byte* dst = (byte*)SDL_MapGPUTransferBuffer(Platform.Device, res->TransferBuffer, 1);
			Buffer.MemoryCopy((void*)data, dst + dataDestOffset, dataSize, dataSize);
			SDL_UnmapGPUTransferBuffer(Platform.Device, res->TransferBuffer);
		}

		// submit to the GPU
		{
			CopyPassBegin();

			SDL_GPUTransferBufferLocation location = new()
			{
				offset = 0,
				transferBuffer = res->TransferBuffer
			};

			SDL_GPUBufferRegion region = new()
			{
				buffer = res->Buffer,
				offset = 0,
				size = (uint)res->Capacity
			};

			SDL_UploadToGPUBuffer(copyPass, &location, &region, cycle: 1);
		}
	}

	private static void MeshDestroyBuffer(BufferResource* res)
	{
		if (res->Buffer != nint.Zero)
			SDL_ReleaseGPUBuffer(Platform.Device, res->Buffer);

		if (res->TransferBuffer != nint.Zero)
			SDL_ReleaseGPUTransferBuffer(Platform.Device, res->TransferBuffer);

		*res = new();
	}

	public static nint ShaderCreate(in ShaderCreateInfo shaderInfo)
	{
		var entryPoint = "main"u8;
		nint vertexProgram;
		nint fragmentProgram;

		// create vertex shader
		fixed (byte* entryPointPtr = entryPoint)
		fixed (byte* vertexCode = shaderInfo.VertexShader)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				codeSize = (uint)shaderInfo.VertexShader.Length,
				code = vertexCode,
				entryPointName = entryPointPtr,
				format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
				samplerCount = 0,
				storageTextureCount = 0,
				storageBufferCount = 0,
				uniformBufferCount = 1
			};

			vertexProgram = SDL_CreateGPUShader(Platform.Device, &info);
			if (vertexProgram == nint.Zero)
				throw new Exception($"Failed to create Shader [Vertex]: {Platform.GetSDLError()}");
		}

		// create fragment program
		fixed (byte* entryPointPtr = entryPoint)
		fixed (byte* fragmentCode = shaderInfo.FragmentShader)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				codeSize = (uint)shaderInfo.FragmentShader.Length,
				code = fragmentCode,
				entryPointName = entryPointPtr,
				format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
				samplerCount = 1,
				storageTextureCount = 0,
				storageBufferCount = 0,
				uniformBufferCount = 1
			};

			fragmentProgram = SDL_CreateGPUShader(Platform.Device, &info);
			if (fragmentProgram == nint.Zero)
				throw new Exception($"Failed to create Shader [Fragment]: {Platform.GetSDLError()}");
		}

		ShaderResource* res = (ShaderResource*)Marshal.AllocHGlobal(sizeof(ShaderResource));
		*res = new ShaderResource()
		{
			Device = Platform.Device,
			VertexShader = vertexProgram,
			FragmentShader = fragmentProgram,
		};

		return new nint(res);
	}

	public static void ShaderDestroy(nint shader)
	{
		ShaderResource* res = (ShaderResource*)shader;
		if (res->Device != Platform.Device)
			return;
		
		SDL_ReleaseGPUShader(Platform.Device, res->VertexShader);
		SDL_ReleaseGPUShader(Platform.Device, res->FragmentShader);

		Marshal.FreeHGlobal(shader);
	}

	public static void Draw(in DrawCommand command)
	{
		RenderPassBegin(command.Target, default);

		// set scissor
		if (command.Scissor.HasValue)
		{
			SDL_Rect scissor = new()
			{
				x = command.Scissor.Value.X, y = command.Scissor.Value.Y,
				w = command.Scissor.Value.Width, h = command.Scissor.Value.Height,
			};
			SDL_SetGPUScissor(renderPass, &scissor);
		}
		else
			SDL_SetGPUScissor(renderPass, null);

		// set viewport
		if (command.Viewport.HasValue)
		{
			SDL_GPUViewport viewport = new()
			{
				x = command.Viewport.Value.X, y = command.Viewport.Value.Y,
				w = command.Viewport.Value.Width, h = command.Viewport.Value.Height,
				minDepth = 0.1f, maxDepth = 1.0f
			};
			SDL_SetGPUViewport(renderPass, &viewport);
		}
		else
			SDL_SetGPUViewport(renderPass, null);

		// figure out graphics pipeline, potentially create a new one
		var pipeline = ResolveGraphicsPipeline(command);
		SDL_BindGPUGraphicsPipeline(renderPass, pipeline);

		// bind mesh buffers
		{
			var meshRes = (MeshResource*)command.Mesh.resource;

			// bind index buffer
			SDL_GPUBufferBinding indexBinding = new()
			{
				buffer = meshRes->Index.Buffer,
				offset = 0
			};
			SDL_BindGPUIndexBuffer(renderPass, &indexBinding, meshRes->IndexFormat switch
			{
				IndexFormat.Sixteen => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT,
				IndexFormat.ThirtyTwo => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT,
				_ => throw new NotImplementedException()
			});

			// bind vertex buffer
			SDL_GPUBufferBinding vertexBinding = new()
			{
				buffer = meshRes->Vertex.Buffer,
				offset = 0
			};
			SDL_BindGPUVertexBuffers(renderPass, 0, &vertexBinding, 1);
		}

		// TODO:
		// still crashes past this point
		return;

		// bind samplers
		{
			var samplers = stackalloc SDL_GPUTextureSamplerBinding[1]
			{
				new(){
					texture = nint.Zero,
					sampler = GetSampler(new TextureSampler())
				}
			};
			SDL_BindGPUFragmentSamplers(renderPass, 0, samplers, 1);
		}

		// perform draw
		SDL_DrawGPUIndexedPrimitives(renderPass, 
			indexCount: (uint)command.MeshIndexCount,
			instanceCount: 0,
			firstIndex: (uint)command.MeshIndexStart,
			vertexOffset: 0,
			firstInstance: 0
		);
	}

	public static void Clear(Target? target, Color color, float depth, int stencil, ClearMask mask)
	{
		if (mask != ClearMask.None)
		{
			RenderPassBegin(target, new()
			{
				Color = mask.Has(ClearMask.Color) ? color : null,
				Depth = mask.Has(ClearMask.Depth) ? depth : null,
				Stencil = mask.Has(ClearMask.Stencil) ? stencil : null
			});
		}
	}

	private static void AcquireCommandBuffers()
	{
		cmd = SDL_AcquireGPUCommandBuffer(Platform.Device);

		uint w, h;
		nint swapchainTexture = SDL_AcquireGPUSwapchainTexture(cmd, Platform.Window, &w, &h);

		swapchain = new()
		{
			Texture = swapchainTexture,
			Format = SDL_GetGPUSwapchainTextureFormat(Platform.Device, Platform.Window),
			Width = (int)w,
			Height = (int)h
		};
	}

	private static void Flush(bool wait)
	{
		CopyPassEnd();
		RenderPassEnd();

		if (wait)
		{
			var fence = SDL_SubmitGPUCommandBufferAndAcquireFence(cmd);
			var fences = stackalloc nint[] { fence };
			SDL_WaitForGPUFences(Platform.Device, 1, fences, 1);
			SDL_ReleaseGPUFence(Platform.Device, fence);
		}
		else
		{
			SDL_SubmitGPUCommandBuffer(cmd);
		}

		cmd = nint.Zero;
	}

	private static void CopyPassBegin()
	{
		if (copyPass != nint.Zero)
			return;

		RenderPassEnd();
		copyPass = SDL_BeginGPUCopyPass(cmd);
	}

	private static void CopyPassEnd()
	{
		if (copyPass != nint.Zero)
			SDL_EndGPUCopyPass(copyPass);
		copyPass = nint.Zero;
	}

	private static void RenderPassBegin(Target? target, ClearInfo clear)
	{
		// only begin if we're not already in a render pass that is matching
		if (renderPass != nint.Zero &&
			renderPassTarget == target &&
			!clear.Color.HasValue && 
			!clear.Depth.HasValue &&
			!clear.Stencil.HasValue)
			return;

		RenderPassEnd();
		CopyPassEnd();

		StackList4<nint> colorTargets = new();
		nint depthStencilTarget = default;

		if (target != null)
		{
			foreach (var it in target.Attachments)
			{
				var res = (TextureResource*)it.resource;
				if (it.Format == TextureFormat.Depth24Stencil8)
					depthStencilTarget = res->Texture;
				else
					colorTargets.Add(res->Texture);
			}
		}
		else
		{
			colorTargets.Add(swapchain.Texture);
		}

		var colorInfo = stackalloc SDL_GPUColorAttachmentInfo[colorTargets.Count];
		var depthStencilInfo = new SDL_GPUDepthStencilAttachmentInfo();

		// get color infos
		for (int i = 0; i < colorTargets.Count; i ++)
		{
			colorInfo[i] = new()
			{
				texture = colorTargets[i],
				clearColor = GetColor(clear.Color ?? Color.Transparent),
				loadOp = clear.Color.HasValue ? 
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR : 
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				storeOp = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE
			};
		}

		// get depth info
		if (depthStencilTarget != nint.Zero)
		{
			depthStencilInfo = new()
			{
				texture = depthStencilTarget,
				depthStencilClearValue = new() {
					depth = clear.Depth ?? 0,
					stencil = (byte)(clear.Stencil ?? 0),
				},
				loadOp = clear.Depth.HasValue ?
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR :
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				storeOp = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
				stencilLoadOp = clear.Stencil.HasValue ?
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR :
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				stencilStoreOp = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE
			};
		}

		// begin pass
		renderPass = SDL_BeginGPURenderPass(
			cmd,
			colorInfo,
			(uint)colorTargets.Count,
			depthStencilTarget != nint.Zero ? &depthStencilInfo : null
		);
		renderPassTarget = target;
	}

	private static void RenderPassEnd()
	{
		if (renderPass != nint.Zero)
			SDL_EndGPURenderPass(renderPass);
		renderPass = nint.Zero;
		renderPassTarget = null;
	}

	private static SDL_FColor GetColor(Color color)
	{
		var vec4 = color.ToVector4();
		return new() { r = vec4.X, g = vec4.Y, b = vec4.Z, a = vec4.W, };
	}

	private static nint ResolveGraphicsPipeline(in DrawCommand command)
	{
		var target = command.Target;
		var mesh = command.Mesh;
		var material = command.Material;
		var shader = material.Shader!;
		var shaderRes = (ShaderResource*)shader.resource;
		var vertexFormat = mesh.VertexFormat!.Value;

		// build a big hashcode of everything in use
		var hash = HashCode.Combine(
			target,
			shader.resource,
			mesh.VertexFormat,
			command.CullMode,
			command.DepthCompare,
			command.DepthTestEnabled,
			command.DepthWriteEnabled,
			command.BlendMode
		);

		// try to find an existing pipeline
		if (!graphicsPipelines.TryGetValue(hash, out var pipeline))
		{
			var colorBlendState = GetBlendState(command.BlendMode);
			var colorAttachments = stackalloc SDL_GPUColorAttachmentDescription[4];
			var colorAttachmentCount = 0;
			var depthStencilAttachment = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;
			var vertexBindings = stackalloc SDL_GPUVertexBinding[1];
			var vertexAttributes = stackalloc SDL_GPUVertexAttribute[vertexFormat.Elements.Count];
			var vertexOffset = 0;

			if (target != null)
			{
				foreach (var it in target.Attachments)
				{
					if (it.Format == TextureFormat.Depth24Stencil8)
					{
						depthStencilAttachment = ((TextureResource*)it.resource)->Format;
					}
					else
					{
						colorAttachments[colorAttachmentCount] = new()
						{
							format = ((TextureResource*)it.resource)->Format,
							blendState = colorBlendState
						};
						colorAttachmentCount++;
					}
				}
			}
			else
			{
				colorAttachments[0] = new()
				{
					format = swapchain.Format,
					blendState = colorBlendState
				};
				colorAttachmentCount = 1;
			}

			vertexBindings[0] = new() {
				binding = 0,
				stride = (uint)vertexFormat.Stride,
				inputRate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
				instanceStepRate = 0
			};

			for (int i = 0; i < vertexFormat.Elements.Count; i ++)
			{
				var it = vertexFormat.Elements[i];
				GetVertexFormat(it.Type, out var format, out var size);
				vertexAttributes[i] = new()
				{
					location = (uint)it.Index,
					binding = 0,
					format = format,
					offset = (uint)vertexOffset
				};
				vertexOffset += size; 
			}
			
			SDL_GPUGraphicsPipelineCreateInfo info = new()
			{
				vertexShader = shaderRes->VertexShader,
				fragmentShader = shaderRes->FragmentShader,
				vertexInputState = new()
				{
					vertexBindings = vertexBindings,
					vertexBindingCount = 1,
					vertexAttributes = vertexAttributes,
					vertexAttributeCount = (uint)vertexFormat.Elements.Count
				},
				primitiveType = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
				rasterizerState = new()
				{
					fillMode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
					cullMode = command.CullMode switch
					{
						CullMode.None => SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
						CullMode.Front => SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT,
						CullMode.Back => SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
						_ => throw new NotImplementedException()
					},
					frontFace = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
					depthBiasEnable = 0
				},
				multisampleState = new()
				{
					sampleCount = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
					sampleMask = 0
				},
				depthStencilState = new()
				{
					depthTestEnable = command.DepthTestEnabled ? 1 : 0,
					depthWriteEnable = command.DepthWriteEnabled ? 1 : 0,
					stencilTestEnable = 0, // TODO: allow this
					compareOp = command.DepthCompare switch
					{
						DepthCompare.Always => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_ALWAYS,
						DepthCompare.Never => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NEVER,
						DepthCompare.Less => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS,
						DepthCompare.Equal => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_EQUAL,
						DepthCompare.LessOrEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
						DepthCompare.Greater => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER,
						DepthCompare.NotEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NOT_EQUAL,
						DepthCompare.GreatorOrEqual => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_GREATER_OR_EQUAL,
						_ => SDL_GPUCompareOp.SDL_GPU_COMPAREOP_NEVER
					},
				},
				attachmentInfo = new()
				{
					colorAttachmentDescriptions = colorAttachments,
					colorAttachmentCount = (uint)colorAttachmentCount,
					hasDepthStencilAttachment = depthStencilAttachment == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID ? 0 : 1,
					depthStencilFormat = depthStencilAttachment
				}
			};

			pipeline = SDL_CreateGPUGraphicsPipeline(Platform.Device, &info);
			if (pipeline == nint.Zero)
				throw new Exception($"Failed to create Graphics Pipeline for drawing: {Platform.GetSDLError()}");

			graphicsPipelines[hash] = pipeline;
		}

		return pipeline;
	}

	private static SDL_GPUColorAttachmentBlendState GetBlendState(BlendMode blend)
	{
		return default;
	}

	private static nint GetSampler(in TextureSampler sampler)
	{
		static SDL_GPUSamplerAddressMode GetWrapMode(TextureWrap wrap) => wrap switch
		{
			TextureWrap.Repeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
			TextureWrap.MirroredRepeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
			TextureWrap.ClampToEdge => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
			TextureWrap.ClampToBorder => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
			_ => throw new NotImplementedException()
		};

		if (!samplers.TryGetValue(sampler, out var result))
		{
			var filter = sampler.Filter switch
			{
				TextureFilter.Nearest => SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
				TextureFilter.Linear => SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
				_ => throw new NotImplementedException()
			};

			SDL_GPUSamplerCreateInfo info = new()
			{
				minFilter = filter,
				magFilter = filter,
				addressModeU = GetWrapMode(sampler.WrapX),
				addressModeV = GetWrapMode(sampler.WrapY),
				addressModeW = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
			};
			result = SDL_CreateGPUSampler(Platform.Device, &info);
			if (result == nint.Zero)
				throw new Exception($"Failed to create Texture Sampler: {Platform.GetSDLError()}");
			samplers[sampler] = result;
		}

		return result;
	}

	private static void GetVertexFormat(VertexType type, out SDL_GPUVertexElementFormat format, out int size)
	{
		format = type switch
		{
			VertexType.Float => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
			VertexType.Float2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
			VertexType.Float3 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
			VertexType.Float4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
			VertexType.Byte4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,
			VertexType.UByte4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,
			VertexType.Short2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
			VertexType.UShort2 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2,
			VertexType.Short4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,
			VertexType.UShort4 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4,
			_ => throw new NotImplementedException(),
		};

		size = type switch
		{
			VertexType.Float => 4,
			VertexType.Float2 => 8,
			VertexType.Float3 => 12,
			VertexType.Float4 => 16,
			VertexType.Byte4 => 4,
			VertexType.UByte4 => 4,
			VertexType.Short2 => 4,
			VertexType.UShort2 => 4,
			VertexType.Short4 => 8,
			VertexType.UShort4 => 8,
			_ => 0,
		};
	}
}