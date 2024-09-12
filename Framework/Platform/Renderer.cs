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
	private static nint window;
	private static nint cmd;
	private static nint renderPass;
	private static nint copyPass;
	private static TextureResource? swapchain;
	private static Target? renderPassTarget;
	private static nint renderPassPipeline;
	private static nint renderPassMesh;
	private static RectInt? renderPassScissor;
	private static RectInt? renderPassViewport;
	private static bool supportsD24S8;
	private static bool supportsMailbox;
	private static readonly Dictionary<int, nint> graphicsPipelinesByHash = [];
	private static readonly Dictionary<nint, int> graphicsPipelinesToHash = [];
	private static readonly Dictionary<nint, List<nint>> graphicsPipelinesByResource = [];
	private static readonly Dictionary<TextureSampler, nint> samplers = [];
	private static nint emptyDefaultTexture;
	private static readonly Exception deviceNotCreated = new("GPU Device has not been created");

	public static GraphicsDriver Driver { get; private set; } = GraphicsDriver.None;

	public static void CreateDevice()
	{
		if (device != nint.Zero)
			throw new Exception("GPU Device is already created");

		device = SDL_CreateGPUDevice(
			format_flags: SDL_GPUShaderFormat.SHADERFORMAT_SPIRV,
			debug_mode: 1, // TODO: flag?
			name: nint.Zero);

		SDL_GetGPUDriver(device);

		if (device == IntPtr.Zero)
			throw new Exception($"Failed to create GPU Device: {Platform.GetSDLError()}");
	}
	
	public static void DestroyDevice()
	{
		SDL_DestroyGPUDevice(device);
		device = nint.Zero;
	}

	public static void Startup(nint window)
	{
		Log.Info($"Graphics Driver: SDL_GPU [{Driver}]");
		Renderer.window = window;

		if (SDL_ClaimWindowForGPUDevice(device, window) != 1)
			throw new Exception("SDL_GpuClaimWindow failed");

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
			SDL_GPUTextureUsageFlags.TEXTUREUSAGE_DEPTH_STENCIL_TARGET) != 0;

		supportsMailbox = SDL_WindowSupportsGPUPresentMode(device, window,
			SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX) == 1;

		// we always have a command buffer ready
		AcquireCommandBuffers();

		// default texture we fall back to rendering if passed a material with a missing texture
		emptyDefaultTexture = CreateTexture(1, 1, TextureFormat.R8G8B8A8, false);
		var data = stackalloc Color[1] { 0xe82979 };
		SetTextureData(emptyDefaultTexture, data, 4);

		// default to vsync on
		SetVSync(true);
	}

	public static void Shutdown()
	{
		Flush(true);

		// destroy default texture
		DestroyTexture(emptyDefaultTexture);
		emptyDefaultTexture = nint.Zero;

		// release pipelines
		lock (graphicsPipelinesByHash)
		{
			foreach (var pipeline in graphicsPipelinesByHash.Values)
				SDL_ReleaseGPUGraphicsPipeline(device, pipeline);
			graphicsPipelinesByHash.Clear();
			graphicsPipelinesToHash.Clear();
			graphicsPipelinesByResource.Clear();
		}

		// release samplers
		lock (samplers)
		{
			foreach (var sampler in samplers.Values)
				SDL_ReleaseGPUSampler(device, sampler);
			samplers.Clear();
		}
		
		SDL_ReleaseWindowFromGPUDevice(device, window);
		
		// clear state
		window = nint.Zero;
		cmd = nint.Zero;
		renderPass = nint.Zero;
		copyPass = nint.Zero;
		swapchain = default;
		renderPassTarget = null;
	}

	public static void SetVSync(bool enabled)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		SDL_SetGPUSwapchainParameters(device, window, 
			swapchain_composition: SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
			present_mode: (enabled, supportsMailbox) switch
			{
				(true, true) => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX,
				(true, false) => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC,
				(false, _) => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE
			}
		);
	}

	public static void Present()
	{
		Flush(false);
		AcquireCommandBuffers();
	}

	public static nint CreateTexture(int width, int height, TextureFormat format, bool isTarget)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

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
			usage = SDL_GPUTextureUsageFlags.TEXTUREUSAGE_SAMPLER,
			width = (uint)width,
			height = (uint)height,
			layer_count_or_depth = 1,
			num_levels = 1,
			sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
		};

		if (isTarget)
		{
			if (format == TextureFormat.Depth24Stencil8)
				info.usage |= SDL_GPUTextureUsageFlags.TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
			else
				info.usage |= SDL_GPUTextureUsageFlags.TEXTUREUSAGE_COLOR_TARGET;
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
		if (device == nint.Zero)
			throw deviceNotCreated;

		// get texture
		TextureResource* props = (TextureResource*)texture;

		// make sure transfer buffer exists
		if (props->TransferBuffer == nint.Zero)
		{
			SDL_GPUTransferBufferCreateInfo info = new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				size = (uint)length,
			};
			props->TransferBuffer = SDL_CreateGPUTransferBuffer(device, &info);
		}

		// copy data
		{
			var dst = SDL_MapGPUTransferBuffer(device, props->TransferBuffer, cycle: 1);
			Buffer.MemoryCopy(data, dst, length, length);
			SDL_UnmapGPUTransferBuffer(device, props->TransferBuffer);
		}

		// upload to the GPU
		{
			BeginCopyPass();

			SDL_GPUTextureTransferInfo info = new()
			{
				transfer_buffer = props->TransferBuffer,
				offset = 0,
				pixels_per_row = (uint)props->Width,
				rows_per_layer = (uint)props->Height,
			};

			SDL_GPUTextureRegion region = new()
			{
				texture = props->Texture,
				w = (uint)props->Width,
				h = (uint)props->Height
			};

			SDL_UploadToGPUTexture(copyPass, &info, &region, cycle: 1);
		}
	}

	public static void GetTextureData(nint texture, void* data, int length)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;
		throw new NotImplementedException();
	}

	public static void DestroyTexture(nint texture)
	{
		TextureResource* res = (TextureResource*)texture;
		if (res->Device != device)
			return;

		ReleaseGraphicsPipelinesAssociatedWith(texture);

		if (res->TransferBuffer != nint.Zero)
			SDL_ReleaseGPUTransferBuffer(device, res->TransferBuffer);

		SDL_ReleaseGPUTexture(device, res->Texture);
		Marshal.FreeHGlobal(texture);
	}

	public static nint CreateMesh()
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		MeshResource* res = (MeshResource*)Marshal.AllocHGlobal(sizeof(MeshResource));
		*res = new MeshResource()
		{
			Device = device
		};
		return new nint(res);
	}

	public static void SetMeshVertexData(nint mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		MeshResource* res = (MeshResource*)mesh;
		res->VertexFormat = format;
		UploadMeshBuffer(&res->Vertex, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.BUFFERUSAGE_VERTEX);
	}

	public static void SetMeshIndexData(nint mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		MeshResource* res = (MeshResource*)mesh;
		res->IndexFormat = format;
		UploadMeshBuffer(&res->Index, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.BUFFERUSAGE_INDEX);
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
				usage = usage,
				size = (uint)size
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
				size = (uint)res->Capacity,
				props = 0
			};
			res->TransferBuffer = SDL_CreateGPUTransferBuffer(device, &info);
		}

		// copy data
		{
			byte* dst = (byte*)SDL_MapGPUTransferBuffer(device, res->TransferBuffer, cycle: 1);
			Buffer.MemoryCopy((void*)data, dst, dataSize, dataSize);
			SDL_UnmapGPUTransferBuffer(device, res->TransferBuffer);
		}

		// submit to the GPU
		{
			BeginCopyPass();

			SDL_GPUTransferBufferLocation location = new()
			{
				offset = 0,
				transfer_buffer = res->TransferBuffer
			};

			SDL_GPUBufferRegion region = new()
			{
				buffer = res->Buffer,
				offset = (uint)dataDestOffset,
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
		if (device == nint.Zero)
			throw deviceNotCreated;

		var entryPoint = "main"u8;
		nint vertexProgram;
		nint fragmentProgram;

		// create vertex shader
		fixed (byte* entryPointPtr = entryPoint)
		fixed (byte* vertexCode = shaderInfo.VertexProgram.Code)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				code_size = (uint)shaderInfo.VertexProgram.Code.Length,
				code = new nint(vertexCode),
				entrypoint = new nint(entryPointPtr),
				format = SDL_GPUShaderFormat.SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
				num_samplers = (uint)shaderInfo.VertexProgram.SamplerCount,
				num_storage_textures = 0,
				num_storage_buffers = 0,
				num_uniform_buffers = (uint)(shaderInfo.VertexProgram.Uniforms.Length > 0 ? 1 : 0)
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
				code_size = (uint)shaderInfo.FragmentProgram.Code.Length,
				code = new nint(fragmentCode),
				entrypoint = new nint(entryPointPtr),
				format = SDL_GPUShaderFormat.SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
				num_samplers = (uint)shaderInfo.FragmentProgram.SamplerCount,
				num_storage_textures = 0,
				num_storage_buffers = 0,
				num_uniform_buffers = (uint)(shaderInfo.FragmentProgram.Uniforms.Length > 0 ? 1 : 0)
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
		
		ReleaseGraphicsPipelinesAssociatedWith(shader);
		SDL_ReleaseGPUShader(device, res->VertexShader);
		SDL_ReleaseGPUShader(device, res->FragmentShader);

		Marshal.FreeHGlobal(shader);
	}

	public static void Draw(DrawCommand command)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

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

		// try to start a render pass
		if (!BeginRenderPass(target, default))
			return;

		// set scissor
		if (command.Scissor != renderPassScissor)
		{
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
			renderPassScissor = command.Scissor;
		}

		// set viewport
		if (command.Viewport != renderPassViewport)
		{
			if (command.Viewport.HasValue)
			{
				SDL_GPUViewport viewport = new()
				{
					x = command.Viewport.Value.X, y = command.Viewport.Value.Y,
					w = command.Viewport.Value.Width, h = command.Viewport.Value.Height,
					min_depth = 0.1f, max_depth = 1.0f
				};
				SDL_SetGPUViewport(renderPass, &viewport);
			}
			else
				SDL_SetGPUViewport(renderPass, null);
			renderPassViewport = command.Viewport;
		}

		// figure out graphics pipeline, potentially create a new one
		var pipeline = GetGraphicsPipeline(command);
		if (renderPassPipeline != pipeline)
		{
			SDL_BindGPUGraphicsPipeline(renderPass, pipeline);
			renderPassPipeline = pipeline;
		}

		// bind mesh buffers
		if (renderPassMesh != mesh.resource)
		{
			renderPassMesh = mesh.resource;
			var it = (MeshResource*)mesh.resource;

			// bind index buffer
			SDL_GPUBufferBinding indexBinding = new()
			{
				buffer = it->Index.Buffer,
				offset = 0
			};
			SDL_BindGPUIndexBuffer(renderPass, &indexBinding, it->IndexFormat switch
			{
				IndexFormat.Sixteen => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT,
				IndexFormat.ThirtyTwo => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT,
				_ => throw new NotImplementedException()
			});

			// bind vertex buffer
			SDL_GPUBufferBinding vertexBinding = new()
			{
				buffer = it->Vertex.Buffer,
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

		// bind vertex samplers
		if (shader.Vertex.SamplerCount > 0)
		{
			var samplers = stackalloc SDL_GPUTextureSamplerBinding[shader.Vertex.SamplerCount];

			for (int i = 0; i < shader.Vertex.SamplerCount; i ++)
			{
				if (mat.VertexSamplers[i].Texture is {} tex && !tex.IsDisposed)
					samplers[i].texture = ((TextureResource*)tex.resource)->Texture;
				else
					samplers[i].texture = ((TextureResource*)emptyDefaultTexture)->Texture;

				samplers[i].sampler = GetSampler(mat.VertexSamplers[i].Sampler);
			}

			SDL_BindGPUVertexSamplers(renderPass, 0, samplers, (uint)shader.Vertex.SamplerCount);
		}

		// Upload Vertex Uniforms
		if (shader.Vertex.Uniforms.Length > 0)
		{
			fixed (byte* ptr = mat.vertexUniformBuffer)
				SDL_PushGPUVertexUniformData(cmd, 0, new nint(ptr), (uint)shader.Vertex.UniformSizeInBytes);
		}

		// Upload Fragment Uniforms
		if (shader.Fragment.Uniforms.Length > 0)
		{
			fixed (byte* ptr = mat.fragmentUniformBuffer)
				SDL_PushGPUFragmentUniformData(cmd, 0, new nint(ptr), (uint)shader.Fragment.UniformSizeInBytes);
		}

		// perform draw
		SDL_DrawGPUIndexedPrimitives(
			render_pass: renderPass,
			num_indices: (uint)command.MeshIndexCount,
			num_instances: 1,
			first_index: (uint)command.MeshIndexStart,
			vertex_offset: command.MeshVertexOffset,
			first_instance: 0
		);
	}

	public static void Clear(Target? target, Color color, float depth, int stencil, ClearMask mask)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		if (mask != ClearMask.None)
		{
			BeginRenderPass(target, new()
			{
				Color = mask.Has(ClearMask.Color) ? color : null,
				Depth = mask.Has(ClearMask.Depth) ? depth : null,
				Stencil = mask.Has(ClearMask.Stencil) ? stencil : null
			});
		}
	}

	private static void AcquireCommandBuffers()
	{
		if (cmd != nint.Zero)
			throw new Exception("Must Flush Command Buffers first!");
		cmd = SDL_AcquireGPUCommandBuffer(device);
	}

	private static void Flush(bool wait)
	{
		EndCopyPass();
		EndRenderPass();

		if (wait)
		{
			var fences = stackalloc nint[1];
			fences[0] = SDL_SubmitGPUCommandBufferAndAcquireFence(cmd);
			SDL_WaitForGPUFences(device, 1, fences, 1);
			SDL_ReleaseGPUFence(device, fences[0]);
		}
		else
		{
			SDL_SubmitGPUCommandBuffer(cmd);
		}

		cmd = nint.Zero;
		swapchain = default;
	}

	private static void BeginCopyPass()
	{
		if (copyPass != nint.Zero)
			return;
		EndRenderPass();
		copyPass = SDL_BeginGPUCopyPass(cmd);
	}

	private static void EndCopyPass()
	{
		if (copyPass != nint.Zero)
			SDL_EndGPUCopyPass(copyPass);
		copyPass = nint.Zero;
	}

	private static bool BeginRenderPass(Target? target, ClearInfo clear)
	{
		// only begin if we're not already in a render pass that is matching
		if (renderPass != nint.Zero &&
			renderPassTarget == target &&
			!clear.Color.HasValue && 
			!clear.Depth.HasValue &&
			!clear.Stencil.HasValue)
			return true;

		EndCopyPass();
		EndRenderPass();

		// set next target
		renderPassTarget = target;

		// configure lists of textures used
		StackList4<nint> colorTargets = new();
		nint depthStencilTarget = default;

		// drawing to a specific target
		if (target != null)
		{
			foreach (var it in target.Attachments)
			{
				var res = ((TextureResource*)it.resource)->Texture;

				// drawing to an invalid target
				if (it.IsDisposed || !it.IsTargetAttachment || res == nint.Zero)
					throw new Exception("Drawing to a Disposed or Invalid Texture");

				if (it.Format == TextureFormat.Depth24Stencil8)
					depthStencilTarget = res;
				else
					colorTargets.Add(res);
			}
		}
		// drawing to the backbuffer/swapchain
		else
		{
			// try to get swapchain if we haven't already
			if (!swapchain.HasValue)
			{
				uint w, h;
				nint swapchainTexture = SDL_AcquireGPUSwapchainTexture(cmd, window, &w, &h);
				swapchain = new()
				{
					Texture = swapchainTexture,
					Format = SDL_GetGPUSwapchainTextureFormat(device, window),
					Width = (int)w,
					Height = (int)h
				};
			}
			
			// there's a chance the swapchain is invalid, in which case we can't
			// render anything to it and should not start a renderpass
			if (swapchain.Value.Texture == nint.Zero)
				return false;

			colorTargets.Add(swapchain.Value.Texture);
		}

		var colorInfo = stackalloc SDL_GPUColorTargetInfo[colorTargets.Count];
		var depthStencilInfo = new SDL_GPUDepthStencilTargetInfo();

		// get color infos
		for (int i = 0; i < colorTargets.Count; i ++)
		{
			colorInfo[i] = new()
			{
				texture = colorTargets[i],
				mip_level = 0,
				layer_or_depth_plane = 0,
				clear_color = GetColor(clear.Color ?? Color.Transparent),
				load_op = clear.Color.HasValue ? 
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR : 
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
				cycle = (byte)(clear.Color.HasValue ? 1 : 0)
			};
		}

		// get depth info
		if (depthStencilTarget != nint.Zero)
		{
			depthStencilInfo = new()
			{
				texture = depthStencilTarget,
				clear_depth = clear.Depth ?? 0,
				load_op = clear.Depth.HasValue ?
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR :
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
				stencil_load_op = clear.Stencil.HasValue ?
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR :
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
				cycle = (byte)(clear.Depth.HasValue && clear.Stencil.HasValue ? 1 : 0),
				clear_stencil = (byte)(clear.Stencil ?? 0),
			};
		}

		// begin pass
		renderPass = SDL_BeginGPURenderPass(
			cmd,
			colorInfo,
			(uint)colorTargets.Count,
			depthStencilTarget != nint.Zero ? &depthStencilInfo : null
		);

		return renderPass != nint.Zero;
	}

	private static void EndRenderPass()
	{
		if (renderPass != nint.Zero)
			SDL_EndGPURenderPass(renderPass);
		renderPass = nint.Zero;
		renderPassTarget = null;
		renderPassPipeline = nint.Zero;
		renderPassMesh = nint.Zero;
		renderPassViewport = null;
		renderPassScissor = null;
	}

	private static nint GetGraphicsPipeline(in DrawCommand command)
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
		if (!graphicsPipelinesByHash.TryGetValue(hash, out var pipeline))
		{
			var colorBlendState = GetBlendState(command.BlendMode);
			var colorAttachments = stackalloc SDL_GPUColorTargetDescription[4];
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
							blend_state = colorBlendState
						};
						colorAttachmentCount++;
					}
				}
			}
			else if (swapchain.HasValue)
			{
				colorAttachments[0] = new()
				{
					format = swapchain.Value.Format,
					blend_state = colorBlendState
				};
				colorAttachmentCount = 1;
			}
			else
			{
				throw new Exception("Trying to create Pipeline on invalid Target");
			}

			vertexBindings[0] = new()
			{
				index = 0,
				pitch = (uint)vertexFormat.Stride,
				input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
				instance_step_rate = 0
			};

			for (int i = 0; i < vertexFormat.Elements.Count; i ++)
			{
				var it = vertexFormat.Elements[i];
				vertexAttributes[i] = new()
				{
					location = (uint)it.Index,
					binding_index = 0,
					format = GetVertexFormat(it.Type, it.Normalized),
					offset = (uint)vertexOffset
				};
				vertexOffset += it.Type.SizeInBytes(); 
			}
			
			SDL_GPUGraphicsPipelineCreateInfo info = new()
			{
				vertex_shader = shaderRes->VertexShader,
				fragment_shader = shaderRes->FragmentShader,
				vertex_input_state = new()
				{
					vertex_bindings = vertexBindings,
					num_vertex_bindings = 1,
					vertex_attributes = vertexAttributes,
					num_vertex_attributes = (uint)vertexFormat.Elements.Count
				},
				primitive_type = SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
				rasterizer_state = new()
				{
					fill_mode = SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
					cull_mode = command.CullMode switch
					{
						CullMode.None => SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
						CullMode.Front => SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT,
						CullMode.Back => SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
						_ => throw new NotImplementedException()
					},
					front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
					enable_depth_bias = 0
				},
				multisample_state = new()
				{
					sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
					sample_mask = 0xFFFFFFFF
				},
				depth_stencil_state = new()
				{
					enable_depth_test = (byte)(command.DepthTestEnabled ? 1 : 0),
					enable_depth_write = (byte)(command.DepthWriteEnabled ? 1 : 0),
					enable_stencil_test = 0, // TODO: allow this
					compare_op = command.DepthCompare switch
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
					back_stencil_state = default,
					front_stencil_state = default,
					compare_mask = 0xFF,
					write_mask = 0xFF,
				},
				target_info = new()
				{
					color_target_descriptions = colorAttachments,
					num_color_targets = (uint)colorAttachmentCount,
					has_depth_stencil_target = (byte)(depthStencilAttachment == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID ? 0 : 1),
					depth_stencil_format = depthStencilAttachment
				}
			};

			pipeline = SDL_CreateGPUGraphicsPipeline(device, &info);
			if (pipeline == nint.Zero)
				throw new Exception($"Failed to create Graphics Pipeline for drawing: {Platform.GetSDLError()}");

			lock (graphicsPipelinesByHash)
			{
				// track which shader uses this pipeline
				{
					if (!graphicsPipelinesByResource.TryGetValue(shader.resource, out var list))
						graphicsPipelinesByResource[shader.resource] = list = [];
					list.Add(pipeline);
				}

				// track which target uses this pipeline
				if (target != null)
				{
					foreach (var it in target.Attachments)
					{
						if (!graphicsPipelinesByResource.TryGetValue(it.resource, out var list))
							graphicsPipelinesByResource[it.resource] = list = [];
						list.Add(pipeline);
					}
				}

				graphicsPipelinesByHash[hash] = pipeline;
				graphicsPipelinesToHash[pipeline] = hash;
			}
		}

		return pipeline;
	}

	private static void ReleaseGraphicsPipelinesAssociatedWith(nint resource)
	{
		lock (graphicsPipelinesByHash)
		{
			if (!graphicsPipelinesByResource.Remove(resource, out var pipelines))
				return;

			foreach (var pipeline in pipelines)
			{
				if (!graphicsPipelinesToHash.Remove(pipeline, out var hash))
					continue;

				graphicsPipelinesByHash.Remove(hash);
				SDL_ReleaseGPUGraphicsPipeline(device, pipeline);
			}
		}
	}

	private static SDL_GPUColorTargetBlendState GetBlendState(BlendMode blend)
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

		SDL_GPUColorTargetBlendState state = new()
		{
			enable_blend = 1,
			src_color_blendfactor = GetFactor(blend.ColorSource),
			dst_color_blendfactor = GetFactor(blend.ColorDestination),
			color_blend_op = GetOp(blend.ColorOperation),
			src_alpha_blendfactor = GetFactor(blend.AlphaSource),
			dst_alpha_blendfactor = GetFactor(blend.AlphaDestination),
			alpha_blend_op = GetOp(blend.AlphaOperation),
			color_write_mask = GetFlags(blend.Mask)
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
				min_filter = filter,
				mag_filter = filter,
				address_mode_u = GetWrapMode(sampler.WrapX),
				address_mode_v = GetWrapMode(sampler.WrapY),
				address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
			};
			result = SDL_CreateGPUSampler(device, &info);
			if (result == nint.Zero)
				throw new Exception($"Failed to create Texture Sampler: {Platform.GetSDLError()}");
			samplers[sampler] = result;
		}

		return result;
	}

	private static SDL_GPUVertexElementFormat GetVertexFormat(VertexType type, bool normalized)
	{
		return (type, normalized) switch
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
	}

	private static SDL_FColor GetColor(Color color)
	{
		var vec4 = color.ToVector4();
		return new() { r = vec4.X, g = vec4.Y, b = vec4.Z, a = vec4.W, };
	}
}