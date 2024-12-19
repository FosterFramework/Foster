using System.Diagnostics;
using System.Text;
using static SDL3.SDL;

namespace Foster.Framework;

internal unsafe class RendererSDL : Renderer
{
	private class Resource(Renderer renderer) : IHandle
	{
		public readonly Renderer Renderer = renderer;
		public bool Destroyed;
		public bool Disposed => Destroyed || Renderer != App.Renderer;
	}

	private class TextureResource(Renderer renderer) : Resource(renderer)
	{
		public nint Texture;
		public int Width;
		public int Height;
		public SDL_GPUTextureFormat Format;
	}

	private class TargetResource(Renderer renderer) : Resource(renderer)
	{
		public readonly List<TextureResource> Attachments = [];
	}

	private class MeshResource(Renderer renderer) : Resource(renderer)
	{
		public record struct Buffer(nint Handle, int Capacity, bool Dirty);

		public Buffer Index = new();
		public Buffer Vertex = new();
		public Buffer Instance = new();
		public IndexFormat IndexFormat;
		public VertexFormat VertexFormat;
	}

	private class ShaderResource(Renderer renderer) : Resource(renderer)
	{
		public nint VertexShader;
		public nint FragmentShader;
	}

	private record struct ClearInfo(Color? Color, float? Depth, int? Stencil);

	// TODO: this is set to 1 since SDL currently improperly awaits fences
	// change back to 3 once fixed
	private const int MaxFramesInFlight = 1;
	private const uint TransferBufferSize = 16 * 1024 * 1024; // 16MB
	private const uint MaxUploadCycleCount = 4;

	// object pointers
	private nint device;
	private nint window;
	private nint cmdUpload;
	private nint cmdRender;
	private nint renderPass;
	private nint copyPass;

	// render pass
	private Target? renderPassTarget;
	private Point2 renderPassTargetSize;
	private nint renderPassPipeline;
	private IHandle? renderPassMesh;
	private RectInt? renderPassScissor;
	private RectInt? renderPassViewport;

	// supported feature set
	private bool supportsD24S8;
	private bool supportsMailbox;

	// state
	private GraphicsDriver driver;
	private bool vsyncEnabled;

	// tracked / allocated resources
	private readonly Dictionary<int, nint> graphicsPipelinesByHash = [];
	private readonly Dictionary<nint, int> graphicsPipelinesToHash = [];
	private readonly Dictionary<IHandle, List<nint>> graphicsPipelinesByResource = [];
	private readonly HashSet<IHandle> resources = [];
	private readonly Dictionary<TextureSampler, nint> samplers = [];
	private IHandle? emptyDefaultTexture;

	// texture/mesh transfer buffers
	private nint textureUploadBuffer;
	private uint textureUploadBufferOffset;
	private uint textureUploadCycleCount;
	private nint bufferUploadBuffer;
	private uint bufferUploadBufferOffset;
	private uint bufferUploadCycleCount;

	// exceptions
	private readonly Exception deviceNotCreated = new("GPU Device has not been created");
	private readonly Exception deviceWasDestroyed = new("This Resource was created with a previous GPU Device which has been destroyed");
	
	// fence/frame counter
	private int frameCounter;
	private readonly StackList4<nint>[] fenceGroups = new StackList4<nint>[MaxFramesInFlight];

	// framebuffer
	private Target? frameBuffer;

	private readonly GraphicsDriver preferred;
	private readonly Version version;

	public override App.GraphicDriverProperties Properties => new(
		Driver: driver,
		DriverVersion: version,
		OriginBottomLeft: false
	);

	public RendererSDL(GraphicsDriver preferred)
	{
		this.preferred = preferred;
		var sdlv = SDL_GetVersion();
		version = new(sdlv / 1000000, (sdlv / 1000) % 1000, sdlv % 1000);
	}

	public override void CreateDevice()
	{
		if (device != nint.Zero)
			throw new Exception("GPU Device is already created");

		string? driverName = preferred switch
		{
			GraphicsDriver.None => null,
			GraphicsDriver.Private => "private",
			GraphicsDriver.Vulkan => "vulkan",
			GraphicsDriver.D3D12 => "direct3d12",
			GraphicsDriver.Metal => "metal",
			GraphicsDriver.OpenGL => throw new NotImplementedException(),
			_ => null,
		};

		device = SDL_CreateGPUDevice(
			format_flags: 
				SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV |
				SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL |
				SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
			debug_mode: true, // TODO: flag?
			name: driverName!);

		if (device == IntPtr.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUDevice));
	}

	public override void DestroyDevice()
	{
		SDL_DestroyGPUDevice(device);
		device = nint.Zero;
	}

	public override void Startup(nint window)
	{
		this.window = window;

		// provider user what driver is being used
		var driverName = SDL_GetGPUDeviceDriver(device);
		driver = driverName switch
		{
			"private" => GraphicsDriver.Private,
			"vulkan" => GraphicsDriver.Vulkan,
			"direct3d12" => GraphicsDriver.D3D12,
			"metal" => GraphicsDriver.Metal,
			_ => GraphicsDriver.None
		};

		Log.Info($"Graphics Driver: SDL_GPU [{driverName}]");

		if (!SDL_ClaimWindowForGPUDevice(device, window))
			throw Platform.CreateExceptionFromSDL(nameof(SDL_ClaimWindowForGPUDevice));

		// some platforms don't support D24S8 depth/stencil format
		supportsD24S8 = SDL_GPUTextureSupportsFormat(
			device,
			SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
			SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
			SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET);

		supportsMailbox = SDL_WindowSupportsGPUPresentMode(device, window,
			SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX);

		// we always have a command buffer ready
		ResetCommandBufferState();

		// create texture upload buffer
		{
			textureUploadBuffer = SDL_CreateGPUTransferBuffer(device, new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				size = TransferBufferSize,
				props = 0
			});
			textureUploadBufferOffset = 0;
		}

		// create buffer upload buffer
		{
			bufferUploadBuffer = SDL_CreateGPUTransferBuffer(device, new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				size = TransferBufferSize,
				props = 0
			});
			bufferUploadBufferOffset = 0;
		}

		// default texture we fall back to rendering if passed a material with a missing texture
		{
			emptyDefaultTexture = CreateTexture(1, 1, TextureFormat.R8G8B8A8, null);
			var data = stackalloc Color[1] { 0xe82979 };
			SetTextureData(emptyDefaultTexture, new nint(data), 4);
		}

		// create framebuffer
		SDL_GetWindowSize(window, out int w, out int h);
		frameBuffer = new Target(w, h, [TextureFormat.R8G8B8A8]);

		// default to vsync on
		SetVSync(true);
	}

	public override void Shutdown()
	{
		// submit remaining commands
		FlushCommands();
		SDL_SubmitGPUCommandBuffer(cmdUpload);
		SDL_SubmitGPUCommandBuffer(cmdRender);
		SDL_WaitForGPUIdle(device);

		// destroy framebuffer
		frameBuffer?.Dispose();
		frameBuffer = null;

		// destroy default texture
		{
			DestroyTexture(emptyDefaultTexture!);
			emptyDefaultTexture = null;
		}

		// destroy resources
		{
			IHandle[] destroying = [.. resources];
			foreach (var it in destroying)
				DestroyResource(it);
		}

		// destroy transfer buffers
		{
			SDL_ReleaseGPUTransferBuffer(device, textureUploadBuffer);
			textureUploadBuffer = nint.Zero;
			SDL_ReleaseGPUTransferBuffer(device, bufferUploadBuffer);
			bufferUploadBuffer = nint.Zero;
		}

		// release fences
		for (int i = 0; i < fenceGroups.Length; i ++)
		{
			for (int j = 0; j < fenceGroups[i].Count; j++)
				SDL_ReleaseGPUFence(device, fenceGroups[i][j]);
			fenceGroups[i].Clear();
		}

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
		cmdUpload = nint.Zero;
		cmdRender = nint.Zero;
		renderPass = nint.Zero;
		copyPass = nint.Zero;
		renderPassTarget = null;
		driver = GraphicsDriver.None;
	}

	public override bool GetVSync() => vsyncEnabled;

	public override void SetVSync(bool enabled)
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

		vsyncEnabled = enabled;
	}

	public override void Present()
	{
		EndCopyPass();
		EndRenderPass();

		// Wait for the least-recent fence
		if (fenceGroups[frameCounter].Count > 0)
		{
			SDL_WaitForGPUFences(device, true, fenceGroups[frameCounter].Span, (uint)fenceGroups[frameCounter].Count);
			for (int i = 0; i < fenceGroups[frameCounter].Count; i ++)
				SDL_ReleaseGPUFence(device, fenceGroups[frameCounter][i]);
			fenceGroups[frameCounter].Clear();
		}

		// if swapchain can be acquired, blit framebuffer to it
		if (SDL_AcquireGPUSwapchainTexture(cmdRender, window, out var scTex, out var scW, out var scH))
		{
			// SDL_AcquireGPUSwapchainTexture can return true, but no texture for a variety of reasons
			// - window is minimized
			// - awaiting previous frame to render
			if (scTex != nint.Zero)
			{
				var resource = (TextureResource)frameBuffer?.Attachments[0].Resource!;
				var blitInfo = new SDL_GPUBlitInfo();

				blitInfo.source.texture = resource.Texture;
				blitInfo.source.mip_level = 0;
				blitInfo.source.layer_or_depth_plane = 0;
				blitInfo.source.x = 0;
				blitInfo.source.y = 0;
				blitInfo.source.w = (uint)resource.Width;
				blitInfo.source.h = (uint)resource.Height;

				blitInfo.destination.texture = scTex;
				blitInfo.destination.mip_level = 0;
				blitInfo.destination.layer_or_depth_plane = 0;
				blitInfo.destination.x = 0;
				blitInfo.destination.y = 0;
				blitInfo.destination.w = scW;
				blitInfo.destination.h = scH;

				blitInfo.load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE;
				blitInfo.clear_color.r = 0;
				blitInfo.clear_color.g = 0;
				blitInfo.clear_color.b = 0;
				blitInfo.clear_color.a = 0;
				blitInfo.flip_mode = SDL_FlipMode.SDL_FLIP_NONE;
				blitInfo.filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR;
				blitInfo.cycle = false;

				SDL_BlitGPUTexture(
					cmdRender,
					blitInfo
				);

				// resize framebuffer if needed
				// TODO: is this correct?
				if (scW != (uint)resource.Width || scH != (uint)resource.Height)
				{
					frameBuffer?.Dispose();
					frameBuffer = new Target((int)scW, (int)scH);
				}
			}
		}
		else
		{
			Log.Warning($"{nameof(SDL_AcquireGPUSwapchainTexture)} failed: {SDL_GetError()}");
		}

		// flush commands from this frame
		FlushCommandsAndAcquireFence(out var nextFences);
		fenceGroups[frameCounter] = nextFences;
		frameCounter = (frameCounter + 1) % MaxFramesInFlight;
	}

	public override IHandle CreateTexture(int width, int height, TextureFormat format, IHandle? targetBinding)
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
				_ => throw new System.ComponentModel.InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextureFormat))
			},
			usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
			width = (uint)width,
			height = (uint)height,
			layer_count_or_depth = 1,
			num_levels = 1,
			sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
		};

		if (targetBinding != null)
		{
			if (format == TextureFormat.Depth24Stencil8)
				info.usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
			else
				info.usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
		}

		nint texture = SDL_CreateGPUTexture(device, info);
		if (texture == nint.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUTexture));

		TextureResource res = new(this)
		{
			Texture = texture,
			Width = width,
			Height = height,
			Format = info.format
		};

		lock (resources)
			resources.Add(res);

		return res;
	}

	public override void SetTextureData(IHandle texture, nint data, int length)
	{
		static uint RoundToAlignment(uint value, uint alignment)
			=> alignment * ((value + alignment - 1) / alignment);

		if (device == nint.Zero)
			throw deviceNotCreated;

		// get texture
		TextureResource res = (TextureResource)texture;
		if (res.Renderer != this)
			throw deviceWasDestroyed;

		bool transferCycle = textureUploadBufferOffset == 0;
		bool usingTemporaryTransferBuffer = false;
		nint transferBuffer = textureUploadBuffer;
		uint transferOffset;

		textureUploadBufferOffset = RoundToAlignment(textureUploadBufferOffset, SDL_GPUTextureFormatTexelBlockSize(res.Format));
		transferOffset = textureUploadBufferOffset;

		// acquire transfer buffer
		if (length >= TransferBufferSize)
		{
			SDL_GPUTransferBufferCreateInfo info = new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				size = (uint)length,
				props = 0
			};
			transferBuffer = SDL_CreateGPUTransferBuffer(device, info);
			usingTemporaryTransferBuffer = true;
			transferCycle = false;
			transferOffset = 0;
		}
		else if (textureUploadBufferOffset + length >= TransferBufferSize)
		{
			if (textureUploadCycleCount < MaxUploadCycleCount)
			{
				transferCycle = true;
				textureUploadCycleCount += 1;
				textureUploadBufferOffset = 0;
				transferOffset = 0;
			}
			else
			{
				FlushCommandsAndStall();
				BeginCopyPass();
				transferCycle = true;
				transferOffset = 0;
			}
		}

		// copy data
		{
			byte* dst = (byte*)SDL_MapGPUTransferBuffer(device, transferBuffer, transferCycle) + transferOffset;
			Buffer.MemoryCopy((void*)data, dst, length, length);
			SDL_UnmapGPUTransferBuffer(device, transferBuffer);
		}

		// upload to the GPU
		{
			BeginCopyPass();

			SDL_GPUTextureTransferInfo info = new()
			{
				transfer_buffer = transferBuffer,
				offset = transferOffset,
				pixels_per_row = (uint)res.Width, // TODO: FNA3D uses 0
				rows_per_layer = (uint)res.Height, // TODO: FNA3D uses 0
			};

			SDL_GPUTextureRegion region = new()
			{
				texture = res.Texture,
				layer = 0,
				mip_level = 0,
				x = 0,
				y = 0,
				z = 0,
				w = (uint)res.Width,
				h = (uint)res.Height,
				d = 1
			};

			SDL_UploadToGPUTexture(copyPass, info, region, cycle: false); // TODO: FNA uses false, we were using true
		}

		// transfer buffer management
		if (usingTemporaryTransferBuffer)
		{
			SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
		}
		else
		{
			textureUploadBufferOffset += (uint)length;
		}
	}

	public override void GetTextureData(IHandle texture, nint data, int length)
	{
		throw new NotImplementedException();
	}

	public void DestroyTexture(IHandle texture)
	{
		if (!texture.Disposed)
		{
			var res = (TextureResource)texture;

			lock (resources)
			{
				resources.Remove(texture);
				res.Destroyed = true;
			}

			ReleaseGraphicsPipelinesAssociatedWith(texture);
			SDL_ReleaseGPUTexture(device, res.Texture);
		}
	}

	public override IHandle CreateTarget(int width, int height)
	{
		var res = new TargetResource(this);
		lock (resources)
			resources.Add(res);
		return res;
	}

	public void DestroyTarget(IHandle target)
	{
		if (!target.Disposed)
		{
			var res = (TargetResource)target;

			foreach (var it in res.Attachments)
				DestroyTexture(it);

			lock (resources)
			{
				resources.Remove(target);
				res.Destroyed = true;
			}
		}
	}

	public override IHandle CreateMesh()
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var res = new MeshResource(this);

		lock (resources)
			resources.Add(res);

		return res;
	}

	public override void SetMeshVertexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var res = (MeshResource)mesh;
		if (res.Renderer != this)
			throw deviceWasDestroyed;

		res.VertexFormat = format;
		res.Vertex.Dirty = true;
		UploadMeshBuffer(ref res.Vertex, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX);
	}

	public override void SetMeshIndexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var res = (MeshResource)mesh;
		if (res.Renderer != this)
			throw deviceWasDestroyed;

		res.IndexFormat = format;
		res.Index.Dirty = true;
		UploadMeshBuffer(ref res.Index, data, dataSize, dataDestOffset, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX);
	}

	public void DestroyMesh(IHandle mesh)
	{
		if (!mesh.Disposed)
		{
			var res = (MeshResource)mesh;
			
			lock (resources)
			{
				resources.Remove(mesh);
				res.Destroyed = true;
			}

			DestroyMeshBuffer(ref res.Vertex);
			DestroyMeshBuffer(ref res.Index);
			DestroyMeshBuffer(ref res.Instance);
		}
	}

	private void UploadMeshBuffer(ref MeshResource.Buffer res, nint data, int dataSize, int dataDestOffset, SDL_GPUBufferUsageFlags usage)
	{
		// (re)create buffer if needed
		var required = dataSize + dataDestOffset;
		if (required > res.Capacity ||
			res.Handle == nint.Zero)
		{
			// TODO: A resize wipes all contents, not particularly ideal
			if (res.Handle != nint.Zero)
			{
				SDL_ReleaseGPUBuffer(device, res.Handle);
				res.Handle = nint.Zero;
			}

			// TODO: Upon first creation we should probably just create a perfectly sized buffer, and afterward next Po2
			int size;
			if(res.Capacity == 0)
			{
				size = required;
			}
			else
			{
				size = 8;
				while (size < required)
					size *= 2;
			}

			SDL_GPUBufferCreateInfo info = new()
			{
				usage = usage,
				size = (uint)size,
				props = 0
			};

			res.Handle = SDL_CreateGPUBuffer(device, info);
			if (res.Handle == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUBuffer), "Mesh Creation Failed");
			res.Capacity = size;
		}

		bool cycle = true; // TODO: this is controlled by hints/logic in FNA3D, where it can lead to a potential flush
		bool transferCycle = bufferUploadBufferOffset == 0;
		bool usingTemporaryTransferBuffer = false;
		nint transferBuffer = bufferUploadBuffer;
		uint transferOffset = bufferUploadBufferOffset;

		// acquire transfer buffer
		if (dataSize >= TransferBufferSize)
		{
			transferBuffer = SDL_CreateGPUTransferBuffer(device, new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				size = (uint)dataSize,
				props = 0
			});
			usingTemporaryTransferBuffer = true;
			transferCycle = false;
			transferOffset = 0;
		}
		else if (bufferUploadBufferOffset + dataSize >= TransferBufferSize)
		{
			if (bufferUploadCycleCount < MaxUploadCycleCount)
			{
				transferCycle = true;
				bufferUploadCycleCount += 1;
				bufferUploadBufferOffset = 0;
				transferOffset = 0;
			}
			else
			{
				FlushCommandsAndStall();
				//BeginCopyPass(); // TODO: FNA3D does not have this, but maybe it should? It had it for texture data.
				transferCycle = true;
				transferOffset = 0;
			}
		}

		// copy data
		{
			byte* dst = (byte*)SDL_MapGPUTransferBuffer(device, transferBuffer, transferCycle) + transferOffset;
			Buffer.MemoryCopy(data.ToPointer(), dst, dataSize, dataSize);
			SDL_UnmapGPUTransferBuffer(device, transferBuffer);
		}

		// submit to the GPU
		{
			BeginCopyPass();

			SDL_GPUTransferBufferLocation location = new()
			{
				offset = transferOffset,
				transfer_buffer = transferBuffer
			};

			SDL_GPUBufferRegion region = new()
			{
				buffer = res.Handle,
				offset = (uint)dataDestOffset,
				size = (uint)dataSize
			};

			SDL_UploadToGPUBuffer(copyPass, location, region, cycle);
		}

		// transfer buffer management
		if (usingTemporaryTransferBuffer)
		{
			SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
		}
		else
		{
			bufferUploadBufferOffset += (uint)dataSize;
		}
	}

	private void DestroyMeshBuffer(ref MeshResource.Buffer res)
	{
		if (res.Handle != nint.Zero)
			SDL_ReleaseGPUBuffer(device, res.Handle);
		res = default;
	}

	public override IHandle CreateShader(in ShaderCreateInfo shaderInfo)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var vertexEntryPoint = Encoding.UTF8.GetBytes(shaderInfo.Vertex.EntryPoint);
		var fragmentEntryPoint = Encoding.UTF8.GetBytes(shaderInfo.Fragment.EntryPoint);
		nint vertexProgram;
		nint fragmentProgram;

		// create vertex shader
		fixed (byte* entryPointPtr = vertexEntryPoint)
		fixed (byte* vertexCode = shaderInfo.Vertex.Code)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				code_size = (nuint)shaderInfo.Vertex.Code.Length,
				code = vertexCode,
				entrypoint = entryPointPtr,
				format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
				num_samplers = (uint)shaderInfo.Vertex.SamplerCount,
				num_storage_textures = 0,
				num_storage_buffers = 0,
				num_uniform_buffers = (uint)(shaderInfo.Vertex.Uniforms.Length > 0 ? 1 : 0)
			};

			vertexProgram = SDL_CreateGPUShader(device, info);
			if (vertexProgram == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUShader), "Failed to create Vertex Shader");
		}

		// create fragment program
		fixed (byte* entryPointPtr = fragmentEntryPoint)
		fixed (byte* fragmentCode = shaderInfo.Fragment.Code)
		{
			SDL_GPUShaderCreateInfo info = new()
			{
				code_size = (nuint)shaderInfo.Fragment.Code.Length,
				code = fragmentCode,
				entrypoint = entryPointPtr,
				format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
				num_samplers = (uint)shaderInfo.Fragment.SamplerCount,
				num_storage_textures = 0,
				num_storage_buffers = 0,
				num_uniform_buffers = (uint)(shaderInfo.Fragment.Uniforms.Length > 0 ? 1 : 0)
			};

			fragmentProgram = SDL_CreateGPUShader(device, info);
			if (fragmentProgram == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUShader), "Failed to create Fragment Shader");
		}

		var res = new ShaderResource(this)
		{
			VertexShader = vertexProgram,
			FragmentShader = fragmentProgram,
		};

		lock (resources)
			resources.Add(res);

		return res;
	}

	public void DestroyShader(IHandle shader)
	{
		var res = (ShaderResource)shader;
		if (!res.Disposed)
		{
			lock (resources)
			{
				resources.Remove(shader);
				res.Destroyed = true;
			}
			
			ReleaseGraphicsPipelinesAssociatedWith(shader);
			SDL_ReleaseGPUShader(device, res.VertexShader);
			SDL_ReleaseGPUShader(device, res.FragmentShader);
		}
	}

	public override void DestroyResource(IHandle resource)
	{
		if (!resource.Disposed)
		{
			if (resource is TextureResource)
				DestroyTexture(resource);
			else if (resource is TargetResource)
				DestroyTarget(resource);
			else if (resource is MeshResource)
				DestroyMesh(resource);
			else if (resource is ShaderResource)
				DestroyShader(resource);
		}
	}

	public override void PerformDraw(DrawCommand command)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var mat = command.Material;
		var shader = mat.Shader!;
		var target = command.Target;
		var mesh = command.Mesh;

		// try to start a render pass
		if (!BeginRenderPass(target, default))
			return;

		// set scissor
		if (command.Scissor != renderPassScissor)
		{
			renderPassScissor = command.Scissor;
			if (command.Scissor.HasValue)
			{
				SDL_SetGPUScissor(renderPass, new()
				{
					x = command.Scissor.Value.X, y = command.Scissor.Value.Y,
					w = command.Scissor.Value.Width, h = command.Scissor.Value.Height,
				});
			}
			else
			{
				SDL_SetGPUScissor(renderPass, new()
				{
					x = 0, y = 0,
					w = renderPassTargetSize.X, h = renderPassTargetSize.Y,
				});
			}
		}

		// set viewport
		if (command.Viewport != renderPassViewport)
		{
			renderPassViewport = command.Viewport;
			if (command.Viewport.HasValue)
			{
				SDL_SetGPUViewport(renderPass, new()
				{
					x = command.Viewport.Value.X, y = command.Viewport.Value.Y,
					w = command.Viewport.Value.Width, h = command.Viewport.Value.Height,
					min_depth = 0, max_depth = float.MaxValue
				});
			}
			else
			{
				SDL_SetGPUViewport(renderPass, new()
				{
					x = 0, y = 0,
					w = renderPassTargetSize.X, h = renderPassTargetSize.Y,
					min_depth = 0, max_depth = float.MaxValue
				});
			}
		}

		// figure out graphics pipeline, potentially create a new one
		var pipeline = GetGraphicsPipeline(command);
		if (renderPassPipeline != pipeline)
		{
			renderPassPipeline = pipeline;
			SDL_BindGPUGraphicsPipeline(renderPass, pipeline);
		}

		// bind mesh buffers
		var meshResource = (MeshResource)mesh.Resource!;
		if (meshResource.Renderer != this)
			throw deviceWasDestroyed;

		if (renderPassMesh != mesh.Resource
			|| meshResource.Vertex.Dirty
			|| meshResource.Index.Dirty
			|| meshResource.Instance.Dirty)
		{
			renderPassMesh = mesh.Resource;
			meshResource.Vertex.Dirty = false;
			meshResource.Index.Dirty = false;
			meshResource.Instance.Dirty = false;

			// bind index buffer
			SDL_GPUBufferBinding indexBinding = new()
			{
				buffer = meshResource.Index.Handle,
				offset = 0
			};
			SDL_BindGPUIndexBuffer(renderPass, indexBinding, meshResource.IndexFormat switch
			{
				IndexFormat.Sixteen => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT,
				IndexFormat.ThirtyTwo => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT,
				_ => throw new NotImplementedException()
			});

			// bind vertex buffer
			SDL_GPUBufferBinding vertexBinding = new()
			{
				buffer = meshResource.Vertex.Handle,
				offset = 0
			};
			SDL_BindGPUVertexBuffers(renderPass, 0, [vertexBinding], 1);
		}

		// bind fragment samplers
		// TODO: only do this if Samplers change
		if (shader.Fragment.SamplerCount > 0)
		{
			Span<SDL_GPUTextureSamplerBinding> samplers = stackalloc SDL_GPUTextureSamplerBinding[shader.Fragment.SamplerCount];

			for (int i = 0; i < shader.Fragment.SamplerCount; i++)
			{
				if (mat.FragmentSamplers[i].Texture is { } tex && !tex.IsDisposed)
					samplers[i].texture = ((TextureResource)tex.Resource).Texture;
				else
					samplers[i].texture = ((TextureResource)emptyDefaultTexture!).Texture;

				samplers[i].sampler = GetSampler(mat.FragmentSamplers[i].Sampler);
			}

			SDL_BindGPUFragmentSamplers(renderPass, 0, samplers, (uint)shader.Fragment.SamplerCount);
		}

		// bind vertex samplers
		// TODO: only do this if Samplers change
		if (shader.Vertex.SamplerCount > 0)
		{
			Span<SDL_GPUTextureSamplerBinding> samplers = stackalloc SDL_GPUTextureSamplerBinding[shader.Vertex.SamplerCount];

			for (int i = 0; i < shader.Vertex.SamplerCount; i++)
			{
				if (mat.VertexSamplers[i].Texture is { } tex && !tex.IsDisposed)
					samplers[i].texture = ((TextureResource)tex.Resource).Texture;
				else
					samplers[i].texture = ((TextureResource)emptyDefaultTexture!).Texture;

				samplers[i].sampler = GetSampler(mat.VertexSamplers[i].Sampler);
			}

			SDL_BindGPUVertexSamplers(renderPass, 0, samplers, (uint)shader.Vertex.SamplerCount);
		}

		// Upload Vertex Uniforms
		// TODO: only do this if Uniforms change
		if (shader.Vertex.Uniforms.Length > 0)
		{
			fixed (byte* ptr = mat.VertexUniformBuffer)
				SDL_PushGPUVertexUniformData(cmdRender, 0, new nint(ptr), (uint)shader.Vertex.UniformSizeInBytes);
		}

		// Upload Fragment Uniforms
		// TODO: only do this if Uniforms change
		if (shader.Fragment.Uniforms.Length > 0)
		{
			fixed (byte* ptr = mat.FragmentUniformBuffer)
				SDL_PushGPUFragmentUniformData(cmdRender, 0, new nint(ptr), (uint)shader.Fragment.UniformSizeInBytes);
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

	public override void Clear(Target? target, Color color, float depth, int stencil, ClearMask mask)
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

	private void FlushCommands()
	{
		EndCopyPass();
		EndRenderPass();
		SDL_SubmitGPUCommandBuffer(cmdUpload);
		SDL_SubmitGPUCommandBuffer(cmdRender);
		cmdUpload = nint.Zero;
		cmdRender = nint.Zero;
		ResetCommandBufferState();
	}

	private void FlushCommandsAndAcquireFence(out StackList4<nint> fences)
	{
		EndCopyPass();
		EndRenderPass();

		fences = new();

		var uploadFence = SDL_SubmitGPUCommandBufferAndAcquireFence(cmdUpload);
		if (uploadFence == nint.Zero)
			Log.Warning($"Failed to acquire upload fence: {SDL_GetError()}");
		else
			fences.Add(uploadFence);

		var renderFence = SDL_SubmitGPUCommandBufferAndAcquireFence(cmdRender);
		if (renderFence == nint.Zero)
			Log.Warning($"Failed to acquire render fence: {SDL_GetError()}");
		else
			fences.Add(renderFence);

		cmdUpload = nint.Zero;
		cmdRender = nint.Zero;
		ResetCommandBufferState();
	}

	private void FlushCommandsAndStall()
	{
		FlushCommandsAndAcquireFence(out var fences);

		if (fences.Count > 0)
			SDL_WaitForGPUFences(device, true, fences.Span, (uint)fences.Count);

		for (int i = 0; i < fences.Count; i ++)
			SDL_ReleaseGPUFence(device, fences[i]);
	}

	private void ResetCommandBufferState()
	{
		if (cmdRender != nint.Zero || cmdUpload != nint.Zero)
			throw new Exception("Must submit previous command buffers!");

		cmdRender = SDL_AcquireGPUCommandBuffer(device);
		cmdUpload = SDL_AcquireGPUCommandBuffer(device);

		// TODO: Ensure _all_ state is reset

		textureUploadBufferOffset = 0;
		textureUploadCycleCount = 0;
		bufferUploadBufferOffset = 0;
		bufferUploadCycleCount = 0;
	}

	private void BeginCopyPass()
	{
		if (copyPass != nint.Zero)
			return;
		copyPass = SDL_BeginGPUCopyPass(cmdUpload);
	}

	private void EndCopyPass()
	{
		if (copyPass != nint.Zero)
			SDL_EndGPUCopyPass(copyPass);
		copyPass = nint.Zero;
	}

	private bool BeginRenderPass(Target? target, ClearInfo clear)
	{
		target ??= frameBuffer;

		// only begin if we're not already in a render pass that is matching
		if (renderPass != nint.Zero &&
			renderPassTarget == target &&
			!clear.Color.HasValue &&
			!clear.Depth.HasValue &&
			!clear.Stencil.HasValue)
			return true;

		EndRenderPass();

		// set next target
		renderPassTarget = target;

		// configure lists of textures used
		StackList4<nint> colorTargets = new();
		nint depthStencilTarget = default;

		// drawing to a specific target
		if (target != null)
		{
			renderPassTargetSize = target.Bounds.Size;

			foreach (var it in target.Attachments)
			{
				var res = ((TextureResource)it.Resource).Texture;

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
			throw new Exception("No Target");
		}

		Span<SDL_GPUColorTargetInfo> colorInfo = stackalloc SDL_GPUColorTargetInfo[colorTargets.Count];
		var depthStencilInfo = new SDL_GPUDepthStencilTargetInfo();

		// get color infos
		for (int i = 0; i < colorTargets.Count; i++)
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
				cycle = clear.Color.HasValue
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
				cycle = clear.Depth.HasValue && clear.Stencil.HasValue,
				clear_stencil = (byte)(clear.Stencil ?? 0),
			};
		}

		// begin pass
		renderPass = SDL_BeginGPURenderPass(
			cmdRender,
			colorInfo,
			(uint)colorTargets.Count,
			depthStencilTarget != nint.Zero ? &depthStencilInfo : null
		);

		return renderPass != nint.Zero;
	}

	private void EndRenderPass()
	{
		if (renderPass != nint.Zero)
			SDL_EndGPURenderPass(renderPass);
		renderPass = nint.Zero;
		renderPassTarget = null;
		renderPassPipeline = nint.Zero;
		renderPassMesh = null;
		renderPassViewport = null;
		renderPassScissor = null;
	}

	private nint GetGraphicsPipeline(in DrawCommand command)
	{
		var target = command.Target ?? frameBuffer;
		var mesh = command.Mesh;
		var material = command.Material;
		var shader = material.Shader!;
		var shaderRes = (ShaderResource)shader.Resource;
		var vertexFormat = mesh.VertexFormat!.Value;

		if (shaderRes.Renderer != this)
			throw deviceWasDestroyed;

		// build a big hashcode of everything in use
		var hash = HashCode.Combine(
			target,
			shader.Resource,
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
			var vertexBindings = stackalloc SDL_GPUVertexBufferDescription[1];
			var vertexAttributes = stackalloc SDL_GPUVertexAttribute[vertexFormat.Elements.Count];
			var vertexOffset = 0;

			if (target != null)
			{
				foreach (var it in target.Attachments)
				{
					if (it.Format == TextureFormat.Depth24Stencil8)
					{
						depthStencilAttachment = ((TextureResource)it.Resource).Format;
					}
					else
					{
						colorAttachments[colorAttachmentCount] = new()
						{
							format = ((TextureResource)it.Resource).Format,
							blend_state = colorBlendState
						};
						colorAttachmentCount++;
					}
				}
			}
			else
			{
				throw new Exception("Trying to create Pipeline on invalid Target");
			}

			vertexBindings[0] = new()
			{
				slot = 0,
				pitch = (uint)vertexFormat.Stride,
				input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
				instance_step_rate = 0
			};

			for (int i = 0; i < vertexFormat.Elements.Count; i++)
			{
				var it = vertexFormat.Elements[i];
				vertexAttributes[i] = new()
				{
					location = (uint)it.Index,
					buffer_slot = 0,
					format = GetVertexFormat(it.Type, it.Normalized),
					offset = (uint)vertexOffset
				};
				vertexOffset += it.Type.SizeInBytes();
			}

			SDL_GPUGraphicsPipelineCreateInfo info = new()
			{
				vertex_shader = shaderRes.VertexShader,
				fragment_shader = shaderRes.FragmentShader,
				vertex_input_state = new()
				{
					vertex_buffer_descriptions = vertexBindings,
					num_vertex_buffers = 1,
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
					enable_depth_bias = false
				},
				multisample_state = new()
				{
					sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
					sample_mask = 0xFFFFFFFF
				},
				depth_stencil_state = new()
				{
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
					enable_depth_test = command.DepthTestEnabled,
					enable_depth_write = command.DepthWriteEnabled,
					enable_stencil_test = false, // TODO: allow this
				},
				target_info = new()
				{
					color_target_descriptions = colorAttachments,
					num_color_targets = (uint)colorAttachmentCount,
					has_depth_stencil_target = depthStencilAttachment != SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID,
					depth_stencil_format = depthStencilAttachment
				}
			};

			pipeline = SDL_CreateGPUGraphicsPipeline(device, info);
			if (pipeline == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUGraphicsPipeline));

			lock (graphicsPipelinesByHash)
			{
				// track which shader uses this pipeline
				{
					if (!graphicsPipelinesByResource.TryGetValue(shader.Resource, out var list))
						graphicsPipelinesByResource[shader.Resource] = list = [];
					list.Add(pipeline);
				}

				// track which textures uses this pipeline
				if (target != null)
				{
					foreach (var it in target.Attachments)
					{
						if (!graphicsPipelinesByResource.TryGetValue(it.Resource, out var list))
							graphicsPipelinesByResource[it.Resource] = list = [];
						list.Add(pipeline);
					}
				}

				graphicsPipelinesByHash[hash] = pipeline;
				graphicsPipelinesToHash[pipeline] = hash;
			}
		}

		return pipeline;
	}

	private void ReleaseGraphicsPipelinesAssociatedWith(IHandle resource)
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

	private SDL_GPUColorTargetBlendState GetBlendState(BlendMode blend)
	{
		SDL_GPUBlendFactor GetFactor(BlendFactor factor) => factor switch
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

		SDL_GPUBlendOp GetOp(BlendOp op) => op switch
		{
			BlendOp.Add => SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
			BlendOp.Subtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_SUBTRACT,
			BlendOp.ReverseSubtract => SDL_GPUBlendOp.SDL_GPU_BLENDOP_REVERSE_SUBTRACT,
			BlendOp.Min => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MIN,
			BlendOp.Max => SDL_GPUBlendOp.SDL_GPU_BLENDOP_MAX,
			_ => throw new NotImplementedException()
		};

		SDL_GPUColorComponentFlags GetFlags(BlendMask mask)
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
			enable_blend = true,
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

	private nint GetSampler(in TextureSampler sampler)
	{
		SDL_GPUSamplerAddressMode GetWrapMode(TextureWrap wrap) => wrap switch
		{
			TextureWrap.Repeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
			TextureWrap.MirroredRepeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
			TextureWrap.Clamp => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
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
			result = SDL_CreateGPUSampler(device, info);
			if (result == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUSampler));
			samplers[sampler] = result;
		}

		return result;
	}

	private SDL_GPUVertexElementFormat GetVertexFormat(VertexType type, bool normalized)
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

	private SDL_FColor GetColor(Color color)
	{
		var vec4 = color.ToVector4();
		return new() { r = vec4.X, g = vec4.Y, b = vec4.Z, a = vec4.W, };
	}
}
