using System.ComponentModel;
using System.Numerics;
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

	private static nint device;
	private static nint cmd;
	private static nint renderPass;
	private static nint copyPass;
	private static TextureResource swapchain;
	private static Target? renderPassTarget;
	private static bool supportsD24S8;
	private static readonly Dictionary<int, nint> graphicsPipelines = [];
	private static readonly Dictionary<TextureSampler, nint> samplers = [];
	private static nint emptyDefaultTexture;

	public static GraphicsDriver Driver { get; private set; } = GraphicsDriver.None;

	public static void CreateDevice()
	{
		if (device != nint.Zero)
			throw new Exception("GPU Device is already created");

		device = SDL_CreateGPUDevice(
			formatFlags: SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
			debugMode: 1,
			null);

		SDL_GetGPUDriver(device);

		if (device == IntPtr.Zero)
			throw new Exception($"Failed to create GPU Device: {Platform.GetSDLError()}");
	}
	
	public static void DestroyDevice()
	{
		SDL_DestroyGPUDevice(device);
		device = nint.Zero;
	}

	public static void Startup()
	{
		if (SDL_ClaimWindowForGPUDevice(device, Platform.Window) != 1)
			throw new Exception("SDL_GpuClaimWindow failed");

		cmd = nint.Zero;
		renderPass = nint.Zero;
		copyPass = nint.Zero;
		swapchain = default;
		renderPassTarget = null;
		graphicsPipelines.Clear();
		samplers.Clear();

		// provider user what driver is being used
		Driver = SDL_GetGPUDriver(device) switch
		{
			SDL_GPUDriver.SDL_GPU_DRIVER_INVALID => GraphicsDriver.D3D11,
			SDL_GPUDriver.SDL_GPU_DRIVER_PRIVATE => GraphicsDriver.Private,
			SDL_GPUDriver.SDL_GPU_DRIVER_VULKAN => GraphicsDriver.Vulkan,
			SDL_GPUDriver.SDL_GPU_DRIVER_D3D11 => GraphicsDriver.D3D11,
			SDL_GPUDriver.SDL_GPU_DRIVER_D3D12 => GraphicsDriver.D3D12,
			SDL_GPUDriver.SDL_GPU_DRIVER_METAL => GraphicsDriver.Metal,
			_ => GraphicsDriver.None
		};

		// some platforms don't support D24S8 depth/stencil format
		supportsD24S8 = SDL_GPUTextureSupportsFormat(
			device,
			SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
			SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
			SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET) != 0;

		// we always have a command buffer ready
		AcquireCommandBuffers();

		// default texture we fall back to rendering if passed a material with a missing texture
		emptyDefaultTexture = CreateTexture(1, 1, TextureFormat.R8G8B8A8, false);
	
		Log.Info($"Graphics Driver: SDL_GPU [{Driver}]");
	}

	public static void Shutdown()
	{
		SDL_ReleaseWindowFromGPUDevice(device, Platform.Window);

		DestroyTexture(emptyDefaultTexture);
		emptyDefaultTexture = nint.Zero;

		foreach (var sampler in samplers.Values)
			SDL_ReleaseGPUSampler(device, sampler);
		samplers.Clear();

		Flush(true);
	}

	public static void Present()
	{
		Flush(false);
		AcquireCommandBuffers();
	}

	public static nint CreateTexture(int width, int height, TextureFormat format, bool isTarget)
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

		nint texture = SDL_CreateGPUTexture(device, &info);
		if (texture == nint.Zero)
			throw new Exception($"Failed to create Texture: {Platform.GetSDLError()}");

		TextureResource* res = (TextureResource*)Marshal.AllocHGlobal(sizeof(TextureResource));
		*res = new TextureResource()
		{
			Device = device,
			Texture = texture,
			TransferBuffer = nint.Zero,
			Width = width,
			Height = height,
			Format = info.format
		};
		return new nint(res);
	}

	public static void SetTextureData(nint texture, void* data, int length)
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
			props->TransferBuffer = SDL_CreateGPUTransferBuffer(device, &info);
		}

		// copy data
		{
			var dst = SDL_MapGPUTransferBuffer(device, props->TransferBuffer, 1);
			Buffer.MemoryCopy(data, dst, length, length);
			SDL_UnmapGPUTransferBuffer(device, props->TransferBuffer);
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

	public static void GetTextureData(nint texture, void* data, int length)
	{
		throw new NotImplementedException();
	}

	public static void DestroyTexture(nint texture)
	{
		TextureResource* res = (TextureResource*)texture;
		if (res->Device != device)
			return;

		if (res->TransferBuffer != nint.Zero)
			SDL_ReleaseGPUTransferBuffer(device, res->TransferBuffer);

		SDL_ReleaseGPUTexture(device, res->Texture);
		Marshal.FreeHGlobal(texture);
	}

	public static nint CreateMesh()
	{
		MeshResource* res = (MeshResource*)Marshal.AllocHGlobal(sizeof(MeshResource));
		*res = new MeshResource()
		{
			Device = device
		};
		return new nint(res);
	}

	public static void SetMeshVertexData(nint mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format)
	{
		MeshResource* res = (MeshResource*)mesh;
		res->VertexFormat = format;
		UploadMeshBuffer(&res->Vertex, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX);
	}

	public static void SetMeshIndexData(nint mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format)
	{
		MeshResource* res = (MeshResource*)mesh;
		res->IndexFormat = format;
		UploadMeshBuffer(&res->Index, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX);
	}

	public static void DestroyMesh(nint mesh)
	{
		MeshResource* res = (MeshResource*)mesh;
		if (res->Device != device)
			return;

		DestroyMeshBuffer(&res->Vertex);
		DestroyMeshBuffer(&res->Index);
		DestroyMeshBuffer(&res->Instance);

		Marshal.FreeHGlobal(mesh);
	}

	private static void UploadMeshBuffer(BufferResource* res, nint data, int dataSize, int dataDestOffset, SDL_GPUBufferUsageFlags usage)
	{
		// recreate buffer
		var required = dataSize + dataDestOffset;
		if (required > res->Capacity ||
			res->Buffer == nint.Zero)
		{
			if (res->Buffer != nint.Zero)
			{
				SDL_ReleaseGPUBuffer(device, res->Buffer);
				SDL_ReleaseGPUTransferBuffer(device, res->TransferBuffer);
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
			
			res->Buffer = SDL_CreateGPUBuffer(device, &info);
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
			res->TransferBuffer = SDL_CreateGPUTransferBuffer(device, &info);
		}

		// copy data
		{
			byte* dst = (byte*)SDL_MapGPUTransferBuffer(device, res->TransferBuffer, 1);
			Buffer.MemoryCopy((void*)data, dst + dataDestOffset, dataSize, dataSize);
			SDL_UnmapGPUTransferBuffer(device, res->TransferBuffer);
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
				size = (uint)dataSize
			};

			SDL_UploadToGPUBuffer(copyPass, &location, &region, cycle: 1);
		}
	}

	private static void DestroyMeshBuffer(BufferResource* res)
	{
		if (res->Buffer != nint.Zero)
			SDL_ReleaseGPUBuffer(device, res->Buffer);

		if (res->TransferBuffer != nint.Zero)
			SDL_ReleaseGPUTransferBuffer(device, res->TransferBuffer);

		*res = new();
	}

	public static nint CreateShader(in ShaderCreateInfo shaderInfo)
	{
		var entryPoint = "main"u8;
		nint vertexProgram;
		nint fragmentProgram;

		// create vertex shader
		fixed (byte* entryPointPtr = entryPoint)
		fixed (byte* vertexCode = shaderInfo.VertexProgram.Code)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				codeSize = (uint)shaderInfo.VertexProgram.Code.Length,
				code = vertexCode,
				entryPointName = entryPointPtr,
				format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
				samplerCount = (uint)shaderInfo.VertexProgram.SamplerCount,
				storageTextureCount = 0,
				storageBufferCount = 0,
				uniformBufferCount = (uint)(shaderInfo.VertexProgram.Uniforms.Length > 0 ? 1 : 0)
			};

			vertexProgram = SDL_CreateGPUShader(device, &info);
			if (vertexProgram == nint.Zero)
				throw new Exception($"Failed to create Shader [Vertex]: {Platform.GetSDLError()}");
		}

		// create fragment program
		fixed (byte* entryPointPtr = entryPoint)
		fixed (byte* fragmentCode = shaderInfo.FragmentProgram.Code)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				codeSize = (uint)shaderInfo.FragmentProgram.Code.Length,
				code = fragmentCode,
				entryPointName = entryPointPtr,
				format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
				samplerCount = (uint)shaderInfo.FragmentProgram.SamplerCount,
				storageTextureCount = 0,
				storageBufferCount = 0,
				uniformBufferCount = (uint)(shaderInfo.FragmentProgram.Uniforms.Length > 0 ? 1 : 0)
			};

			fragmentProgram = SDL_CreateGPUShader(device, &info);
			if (fragmentProgram == nint.Zero)
				throw new Exception($"Failed to create Shader [Fragment]: {Platform.GetSDLError()}");
		}

		ShaderResource* res = (ShaderResource*)Marshal.AllocHGlobal(sizeof(ShaderResource));
		*res = new ShaderResource()
		{
			Device = device,
			VertexShader = vertexProgram,
			FragmentShader = fragmentProgram,
		};

		return new nint(res);
	}

	public static void DestroyShader(nint shader)
	{
		ShaderResource* res = (ShaderResource*)shader;
		if (res->Device != device)
			return;
		
		SDL_ReleaseGPUShader(device, res->VertexShader);
		SDL_ReleaseGPUShader(device, res->FragmentShader);

		Marshal.FreeHGlobal(shader);
	}

	public static void Draw(DrawCommand command)
	{
		var mat = command.Material ?? throw new Exception("Material is Invalid");
		var shader = mat.Shader;
		var target = command.Target;
		var mesh = command.Mesh;

		if (shader == null || shader.IsDisposed)
			throw new Exception("Material Shader is Invalid");

		if (target != null && target.IsDisposed)
			throw new Exception("Target is Invalid");

		if (mesh == null || mesh.IsDisposed)
			throw new Exception("Mesh is Invalid");

		RenderPassBegin(target, default);

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
			var meshRes = (MeshResource*)mesh.resource;

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

		// bind fragment samplers
		if (shader.Fragment.SamplerCount > 0)
		{
			var samplers = stackalloc SDL_GPUTextureSamplerBinding[shader.Fragment.SamplerCount];

			for (int i = 0; i < shader.Fragment.SamplerCount; i ++)
			{
				if (mat.FragmentSamplers[i].Texture is {} tex && !tex.IsDisposed)
					samplers[i].texture = ((TextureResource*)tex.resource)->Texture;
				else
					samplers[i].texture = ((TextureResource*)emptyDefaultTexture)->Texture;

				samplers[i].sampler = GetSampler(mat.FragmentSamplers[i].Sampler);
			}

			SDL_BindGPUFragmentSamplers(renderPass, 0, samplers, (uint)shader.Fragment.SamplerCount);
		}

		// TODO:
		// bind Vertex Samplers

		// Upload Vertex Uniforms
		if (shader.Vertex.Uniforms.Length > 0)
		{
			fixed (byte* ptr = mat.vertexUniformBuffer)
				SDL_PushGPUVertexUniformData(cmd, 0, ptr, (uint)shader.Vertex.UniformSizeInBytes);
		}

		// Upload Fragment Uniforms
		if (shader.Fragment.Uniforms.Length > 0)
		{
			fixed (byte* ptr = mat.fragmentUniformBuffer)
				SDL_PushGPUFragmentUniformData(cmd, 0, ptr, (uint)shader.Fragment.UniformSizeInBytes);
		}

		// perform draw
		SDL_DrawGPUIndexedPrimitives(renderPass, 
			indexCount: (uint)command.MeshIndexCount,
			instanceCount: 1,
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
		cmd = SDL_AcquireGPUCommandBuffer(device);

		uint w, h;
		nint swapchainTexture = SDL_AcquireGPUSwapchainTexture(cmd, Platform.Window, &w, &h);

		swapchain = new()
		{
			Texture = swapchainTexture,
			Format = SDL_GetGPUSwapchainTextureFormat(device, Platform.Window),
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
			SDL_WaitForGPUFences(device, 1, fences, 1);
			SDL_ReleaseGPUFence(device, fence);
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
				GetVertexFormat(it.Type, it.Normalized, out var format, out var size);
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
					sampleMask = 0xFFFF
				},
				depthStencilState = new()
				{
					depthTestEnable = (byte)(command.DepthTestEnabled ? 1 : 0),
					depthWriteEnable = (byte)(command.DepthWriteEnabled ? 1 : 0),
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
					hasDepthStencilAttachment = (byte)(depthStencilAttachment == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID ? 0 : 1),
					depthStencilFormat = depthStencilAttachment
				}
			};

			pipeline = SDL_CreateGPUGraphicsPipeline(device, &info);
			if (pipeline == nint.Zero)
				throw new Exception($"Failed to create Graphics Pipeline for drawing: {Platform.GetSDLError()}");

			graphicsPipelines[hash] = pipeline;
		}

		return pipeline;
	}

	private static SDL_GPUColorAttachmentBlendState GetBlendState(BlendMode blend)
	{
		static SDL_GPUBlendFactor GetFactor(BlendFactor factor) => factor switch
		{
			BlendFactor.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
			BlendFactor.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
			BlendFactor.SrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
			BlendFactor.OneMinusSrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
			BlendFactor.DstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
			BlendFactor.OneMinusDstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
			BlendFactor.SrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
			BlendFactor.OneMinusSrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
			BlendFactor.DstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
			BlendFactor.OneMinusDstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
			BlendFactor.ConstantColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_CONSTANT_COLOR,
			BlendFactor.OneMinusConstantColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_CONSTANT_COLOR,
			BlendFactor.SrcAlphaSaturate => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
			BlendFactor.ConstantAlpha => throw new NotImplementedException(),
			BlendFactor.OneMinusConstantAlpha => throw new NotImplementedException(),
			BlendFactor.Src1Color => throw new NotImplementedException(),
			BlendFactor.OneMinusSrc1Color => throw new NotImplementedException(),
			BlendFactor.Src1Alpha => throw new NotImplementedException(),
			BlendFactor.OneMinusSrc1Alpha => throw new NotImplementedException(),
			_ => throw new NotImplementedException()
		};

		static SDL_GPUBlendOp GetOp(BlendOp op) => op switch
		{
			BlendOp.Add => SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
			BlendOp.Subtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_SUBTRACT,
			BlendOp.ReverseSubtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_REVERSE_SUBTRACT,
			BlendOp.Min => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MIN,
			BlendOp.Max => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MAX,
			_ => throw new NotImplementedException()
		};

		static SDL_GPUColorComponentFlags GetFlags(BlendMask mask)
		{
			SDL_GPUColorComponentFlags flags = default;
			if (mask.Has(BlendMask.Red)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_R;
			if (mask.Has(BlendMask.Green)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_G;
			if (mask.Has(BlendMask.Blue)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_B;
			if (mask.Has(BlendMask.Alpha)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_A;
			return flags;
		}

		SDL_GPUColorAttachmentBlendState state = new()
		{
			blendEnable = 1,
			srcColorBlendFactor = GetFactor(blend.ColorSource),
			dstColorBlendFactor = GetFactor(blend.ColorDestination),
			colorBlendOp = GetOp(blend.ColorOperation),
			srcAlphaBlendFactor = GetFactor(blend.AlphaSource),
			dstAlphaBlendFactor = GetFactor(blend.AlphaDestination),
			alphaBlendOp = GetOp(blend.AlphaOperation),
			colorWriteMask = GetFlags(blend.Mask)
		};
		return state;
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
			result = SDL_CreateGPUSampler(device, &info);
			if (result == nint.Zero)
				throw new Exception($"Failed to create Texture Sampler: {Platform.GetSDLError()}");
			samplers[sampler] = result;
		}

		return result;
	}

	private static void GetVertexFormat(VertexType type, bool normalized, out SDL_GPUVertexElementFormat format, out int size)
	{
		format = (type, normalized) switch
		{
			(VertexType.Float, _)       => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT,
			(VertexType.Float2, _)      => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
			(VertexType.Float3, _)      => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
			(VertexType.Float4, _)      => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
			(VertexType.Byte4, false)   => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4,
			(VertexType.Byte4, true)    => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_BYTE4_NORM,
			(VertexType.UByte4, false)  => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4,
			(VertexType.UByte4, true)   => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
			(VertexType.Short2, false)  => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2,
			(VertexType.Short2, true)   => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT2_NORM,
			(VertexType.UShort2, false) => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2,
			(VertexType.UShort2, true)  => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT2_NORM,
			(VertexType.Short4, false)  => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4,
			(VertexType.Short4, true)   => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_SHORT4_NORM,
			(VertexType.UShort4, false) => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4,
			(VertexType.UShort4, true)  => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_USHORT4_NORM,

			_ => throw new NotImplementedException(),
		};

		size = type switch
		{
			VertexType.Float   => 4,
			VertexType.Float2  => 8,
			VertexType.Float3  => 12,
			VertexType.Float4  => 16,
			VertexType.Byte4   => 4,
			VertexType.UByte4  => 4,
			VertexType.Short2  => 4,
			VertexType.UShort2 => 4,
			VertexType.Short4  => 8,
			VertexType.UShort4 => 8,
			_ => 0,
		};
	}
}