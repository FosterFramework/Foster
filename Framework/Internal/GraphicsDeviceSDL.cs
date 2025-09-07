using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using static SDL3.SDL;

namespace Foster.Framework;

internal unsafe class GraphicsDeviceSDL : GraphicsDevice
{
	private class Resource(GraphicsDevice graphicsDevice) : IHandle
	{
		public readonly GraphicsDevice GraphicsDevice = graphicsDevice;
		public bool Destroyed;
		public bool Disposed => Destroyed || GraphicsDevice.Disposed;
	}

	private class TextureResource(GraphicsDevice graphicsDevice) : Resource(graphicsDevice)
	{
		public nint Texture;
		public int Width;
		public int Height;
		public SDL_GPUTextureFormat Format;
		public SDL_GPUSampleCount SampleCount;

		/// <summary>
		/// If we are multisampled, we need a resolve texture
		/// </summary>
		public TextureResource? MultiSampleResolve;

		/// <summary>
		/// If we are multisampled, we should sample from the resolve texture,
		/// NOT the texture that is being multisampled.
		/// </summary>
		public nint SamplerTexture => MultiSampleResolve?.Texture ?? Texture;
	}

	private class TargetResource(GraphicsDevice graphicsDevice) : Resource(graphicsDevice)
	{
		public readonly List<TextureResource> Attachments = [];
	}

	private class BufferResource(GraphicsDevice graphicsDevice, string? name, SDL_GPUBufferUsageFlags usage, IndexFormat indexFormat) : Resource(graphicsDevice)
	{
		public readonly string? Name = name;
		public readonly IndexFormat IndexFormat = indexFormat;
		public readonly SDL_GPUBufferUsageFlags Usage = usage;
		public nint Handle;
		public int Capacity;
		public bool Dirty;
	}

	private class ShaderResource(GraphicsDevice graphicsDevice) : Resource(graphicsDevice)
	{
		public nint VertexShader;
		public nint FragmentShader;
		public readonly ConcurrentDictionary<int, nint> Pipelines = [];
	}

	private record struct ClearInfo(StackList8<Color>? Color, float? Depth, int? Stencil);

	private const int MaxFramesInFlight = 3;
	private const uint TransferBufferSize = 16 * 1024 * 1024; // 16MB
	private const uint MaxUploadCycleCount = 4;
	private const int MaxColorAttachments = 8;
	private (TextureFormat Format, SampleCount SampleCount)[] backbufferFormat;

	// object pointers
	private nint device;
	private nint window;
	private nint cmdUpload;
	private nint cmdRender;
	private nint renderPass;
	private nint copyPass;

	// render pass
	private IDrawableTarget? renderPassTarget;
	private Point2 renderPassTargetSize;
	private nint renderPassPipeline;
	private StackList4<IHandle> renderPassVertexBuffers;
	private IHandle? renderPassIndexBuffer;
	private RectInt? renderPassScissor;
	private RectInt? renderPassViewport;

	// supported feature set
	private bool supportsMailbox;

	// state
	private GraphicsDriver driver;
	private bool vsyncEnabled;

	// render buffer, drawn to before being applied to the swapchain
	private Target? backbuffer;
	private Point2 backbufferSize;

	// tracked / allocated resources
	private readonly HashSet<IHandle> resources = [];
	private readonly Dictionary<TextureSampler, nint> samplers = [];
	private IHandle? emptyDefaultTexture;

	// texture/mesh transfer buffers
	private nint bufferUploadBuffer;
	private uint bufferUploadBufferOffset;
	private uint bufferUploadCycleCount;
	private nint textureUploadBuffer;
	private uint textureUploadBufferOffset;
	private uint textureUploadCycleCount;
	private nint textureDownloadBuffer;
	private uint textureDownloadBufferSize;
	private readonly Lock textureDownloadMutex = new();

	// exceptions
	private readonly Exception deviceNotCreated = new("GPU Device has not been created");
	private readonly Exception deviceWasDestroyed = new("This Resource was created with a previous GPU Device which has been destroyed");

	private readonly GraphicsDriver preferred;
	private readonly Version version;

	public override GraphicsDriver Driver => driver;

	public override bool OriginBottomLeft => false;

	public override bool VSync
	{
		get => vsyncEnabled;
		set
		{
			if (device == nint.Zero)
				throw deviceNotCreated;

			SDL_SetGPUSwapchainParameters(device, window,
				swapchain_composition: SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR,
				present_mode: (value, supportsMailbox) switch
				{
					(true, true) => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX,
					(true, false) => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC,
					(false, _) => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE
				}
			);

			vsyncEnabled = value;
		}
	}

	public override bool Disposed => device == nint.Zero;

	public GraphicsDeviceSDL(App app, GraphicsDriver preferred) : base(app)
	{
		this.preferred = preferred;
		var sdlv = SDL_GetVersion();
		version = new(sdlv / 1000000, (sdlv / 1000) % 1000, sdlv % 1000);
		backbufferFormat = [( TextureFormat.Color, SampleCount.One )];
	}

	internal override void CreateDevice(in AppFlags flags)
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
			_ => null,
		};

		device = SDL_CreateGPUDevice(
			format_flags:
				SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV |
				SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL |
				SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
			debug_mode: Calc.Has(flags, AppFlags.GraphicsDebugging),
			name: driverName!);

		if (device == IntPtr.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUDevice));
		
		if (flags.Has(AppFlags.MultiSampledBackBuffer))
		{
			if (IsTextureMultiSampleSupported(TextureFormat.Color, SampleCount.Eight))
				backbufferFormat = [( TextureFormat.Color, SampleCount.Eight )];
			else if (IsTextureMultiSampleSupported(TextureFormat.Color, SampleCount.Four))
				backbufferFormat = [( TextureFormat.Color, SampleCount.Four )];
			else if (IsTextureMultiSampleSupported(TextureFormat.Color, SampleCount.Two))
				backbufferFormat = [( TextureFormat.Color, SampleCount.Two )];
		}
	}

	internal override void DestroyDevice()
	{
		SDL_DestroyGPUDevice(device);
		device = nint.Zero;
	}

	internal override void Startup(nint window)
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
			emptyDefaultTexture = CreateTexture("Fallback", 1, 1, TextureFormat.R8G8B8A8, SampleCount.One, null);
			var data = stackalloc Color[1] { 0xe82979 };
			SetTextureData(emptyDefaultTexture, new nint(data), 4);
		}

		// get backbuffer
		SDL_GetWindowSizeInPixels(window, out backbufferSize.X, out backbufferSize.Y);
		backbufferSize = Point2.Max(Point2.One, backbufferSize);
		backbuffer = new(this, backbufferSize.X, backbufferSize.Y, backbufferFormat);

		// default to 3 frames in flight
		SDL_SetGPUAllowedFramesInFlight(device, 3);

		// default to vsync on
		VSync = true;
	}

	internal override void Shutdown()
	{
		// submit remaining commands
		FlushCommands(stall: false);
		SDL_SubmitGPUCommandBuffer(cmdUpload);
		SDL_SubmitGPUCommandBuffer(cmdRender);
		SDL_WaitForGPUIdle(device);

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
			if (textureDownloadBuffer != nint.Zero)
				SDL_ReleaseGPUTransferBuffer(device, textureDownloadBuffer);
			textureDownloadBuffer = nint.Zero;
			SDL_ReleaseGPUTransferBuffer(device, textureUploadBuffer);
			textureUploadBuffer = nint.Zero;
			SDL_ReleaseGPUTransferBuffer(device, bufferUploadBuffer);
			bufferUploadBuffer = nint.Zero;
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

	internal override void Present()
	{
		EndCopyPass();
		EndRenderPass();

		// copy buffer to swap chain
		if (SDL_WaitAndAcquireGPUSwapchainTexture(cmdRender, window, out var scTex, out var scW, out var scH))
		{
			if (scTex != nint.Zero && backbuffer != null && scW > 0 && scH > 0 && backbufferSize.X > 0 && backbufferSize.Y > 0)
			{
				SDL_GPUBlitInfo blit = new()
				{
					source = new()
					{
						texture = ((TextureResource)backbuffer.Attachments[0].Resource).SamplerTexture,
						mip_level = 0,
						layer_or_depth_plane = 0,
						x = 0,
						y = 0,
						w = Math.Min(scW, (uint)backbuffer.Width),
						h = Math.Min(scH, (uint)backbuffer.Height)
					},
					destination = new()
					{
						texture = scTex,
						mip_level = 0,
						layer_or_depth_plane = 0,
						x = 0,
						y = 0,
						w = Math.Min(scW, (uint)backbuffer.Width),
						h = Math.Min(scH, (uint)backbuffer.Height)
					},
					load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
					flip_mode = SDL_FlipMode.SDL_FLIP_NONE,
					filter = SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
					cycle = false
				};

				SDL_BlitGPUTexture(cmdRender, blit);
			}

			// update buffer size (if non-zero)
			if (scW > 0 && scH > 0)
			{
				backbufferSize = new Point2((int)scW, (int)scH);

				// intentionally resizing the buffer a bit larger so we're not
				// constantly recreating buffers as the window is dragged/scaled
				if (backbuffer == null || backbuffer.Width < backbufferSize.X || backbuffer.Height < backbufferSize.Y)
				{
					backbuffer?.Dispose();
					backbuffer = new(this, backbufferSize.X + 64, backbufferSize.Y + 64, backbufferFormat);
				}
				// resize buffer if it's too large
				else if (backbuffer.Width > backbufferSize.X + 128 || backbuffer.Height > backbufferSize.Y + 128)
				{
					backbuffer?.Dispose();
					backbuffer = new(this, backbufferSize.X, backbufferSize.Y, backbufferFormat);
				}
			}
		}
		else
		{
			throw Platform.CreateExceptionFromSDL(nameof(SDL_WaitAndAcquireGPUSwapchainTexture));
		}

		FlushCommands(stall: false);
	}

	public override bool IsTextureFormatSupported(TextureFormat format)
	{
		var usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;
		if (format.IsDepthStencilFormat())
			usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
		else
			usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;

		var type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;

		return SDL_GPUTextureSupportsFormat(device, GetTextureFormat(format), type, usage);
	}

	public override bool IsTextureMultiSampleSupported(TextureFormat format, SampleCount sampleCount)
	{
		return SDL_GPUTextureSupportsSampleCount(device, GetTextureFormat(format), GetSampleCount(sampleCount));
	}

	internal override IHandle CreateTexture(string? name, int width, int height, TextureFormat format, SampleCount sampleCount, IHandle? targetBinding)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		uint props = 0;
		if (!string.IsNullOrEmpty(name))
		{
			props = SDL_CreateProperties();
			SDL_SetStringProperty(props, SDL_PROP_GPU_TEXTURE_CREATE_NAME_STRING, name);
		}

		SDL_GPUTextureCreateInfo info = new()
		{
			type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
			format = GetTextureFormat(format),
			usage = 0,
			width = (uint)width,
			height = (uint)height,
			layer_count_or_depth = 1,
			num_levels = 1,
			sample_count = GetSampleCount(sampleCount),
			props = props
		};

		// only a sampler if not multisampled
		// (otherwise there's a resolve texture used for that, created later)
		if (sampleCount == SampleCount.One)
			info.usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

		// we're a render target attachment
		if (targetBinding != null)
		{
			if (format.IsDepthStencilFormat())
				info.usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
			else
				info.usage |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
		}

		// try to create texture on GPU
		nint texture = SDL_CreateGPUTexture(device, info);
		if (props != 0)
			SDL_DestroyProperties(props);
		if (texture == nint.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUTexture));

		// create a resolve texture if we're multisampled
		TextureResource? resolve = null;
		if (sampleCount != SampleCount.One)
		{
			var resolveName = name != null ? $"Resolve-{name}" : null;
			resolve = (TextureResource)CreateTexture(resolveName, width, height, format, SampleCount.One, targetBinding);
		}

		// create resulting texture resource
		TextureResource res = new(this)
		{
			Texture = texture,
			Width = width,
			Height = height,
			Format = info.format,
			SampleCount = GetSampleCount(sampleCount),
			MultiSampleResolve = resolve
		};
		lock (resources)
			resources.Add(res);
		return res;
	}

	internal override void SetTextureData(IHandle texture, nint data, int length)
	{
		static uint RoundToAlignment(uint value, uint alignment)
			=> alignment * ((value + alignment - 1) / alignment);

		if (device == nint.Zero)
			throw deviceNotCreated;

		// get texture
		TextureResource res = (TextureResource)texture;
		if (res.GraphicsDevice != this)
			throw deviceWasDestroyed;
		
		// search up for resolve texture if we're multisampled
		if (res.MultiSampleResolve != null)
		{
			SetTextureData(res.MultiSampleResolve, data, length);
			return;
		}

		bool usingTemporaryTransferBuffer = false;
		nint transferBuffer = textureUploadBuffer;
		uint transferOffset;
		bool transferCycle;

		textureUploadBufferOffset = RoundToAlignment(textureUploadBufferOffset, SDL_GPUTextureFormatTexelBlockSize(res.Format));

		// uploading more data than our transfer buffer, so create a temporary one
		if (length >= TransferBufferSize)
		{
			transferBuffer = SDL_CreateGPUTransferBuffer(device, new()
			{
				usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
				size = (uint)length,
				props = 0
			});
			usingTemporaryTransferBuffer = true;
			transferCycle = false;
			transferOffset = 0;
		}
		// cycle transfer buffer because we're going to run out of space in the current one
		else if (textureUploadBufferOffset + length >= TransferBufferSize)
		{
			// only allow MaxUploadCycleCount cycles before flushing
			if (textureUploadCycleCount < MaxUploadCycleCount)
			{
				transferCycle = true;
				textureUploadCycleCount += 1;
				textureUploadBufferOffset = 0;
				transferOffset = 0;
			}
			// otherwise flush
			else
			{
				FlushCommands(stall: true);
				transferCycle = true;
				transferOffset = 0;
			}
		}
		// normal transfer using existing buffer, only cycle if we're at the start
		else
		{
			transferCycle = textureUploadBufferOffset == 0;
			transferOffset = textureUploadBufferOffset;
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

			SDL_UploadToGPUTexture(
				copyPass,
				source: new()
				{
					transfer_buffer = transferBuffer,
					offset = transferOffset,
					pixels_per_row = (uint)res.Width, // TODO: FNA3D uses 0?
					rows_per_layer = (uint)res.Height, // TODO: FNA3D uses 0?
				},
				destination: new()
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
				},
				cycle: false
			);
		}

		// transfer buffer management
		if (usingTemporaryTransferBuffer)
			SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
		else
			textureUploadBufferOffset += (uint)length;
	}

	internal override void GetTextureData(IHandle texture, nint data, int length)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		// get texture
		TextureResource res = (TextureResource)texture;
		if (res.GraphicsDevice != this)
			throw deviceWasDestroyed;

		// search up for the resolve texture
		if (res.MultiSampleResolve != null)
		{
			GetTextureData(res.MultiSampleResolve, data, length);
			return;
		}

		// we only allow one download at a time
		lock (textureDownloadMutex)
		{
			// TODO:
			// If you create a texture and immediately call GetData it doesn't seem to work the first frame,
			// unless this additional stall is here. But why?
			FlushCommands(stall: true);

			// verify download buffer is big enough
			if (textureDownloadBuffer == nint.Zero || textureDownloadBufferSize < length)
			{
				if (textureDownloadBuffer != nint.Zero)
					SDL_ReleaseGPUTransferBuffer(device, textureDownloadBuffer);

				textureDownloadBuffer = SDL_CreateGPUTransferBuffer(device, new()
				{
					usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD,
					size = (uint)length,
					props = 0
				});
				textureDownloadBufferSize = (uint)length;
			}

			// download from the gpu
			{
				BeginCopyPass();

				SDL_DownloadFromGPUTexture(
					copyPass,
					source: new()
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
					},
					destination: new()
					{
						transfer_buffer = textureDownloadBuffer,
						offset = 0,
						pixels_per_row = (uint)res.Width, // TODO: FNA3D uses 0?
						rows_per_layer = (uint)res.Height, // TODO: FNA3D uses 0?
					}
				);
			}

			// flush and stall so the data is up to date
			FlushCommands(stall: true);

			// copy data
			{
				byte* src = (byte*)SDL_MapGPUTransferBuffer(device, textureDownloadBuffer, false);
				Buffer.MemoryCopy(src, (void*)data, length, length);
				SDL_UnmapGPUTransferBuffer(device, textureDownloadBuffer);
			}
		}
	}

	public void DestroyTexture(IHandle texture)
	{
		if (!texture.Disposed)
		{
			var res = (TextureResource)texture;

			if (res.MultiSampleResolve != null)
				DestroyTexture(res.MultiSampleResolve);

			lock (resources)
			{
				resources.Remove(texture);
				res.Destroyed = true;
			}

			SDL_ReleaseGPUTexture(device, res.Texture);
		}
	}

	internal override IHandle CreateTarget(int width, int height)
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

	internal override IHandle CreateIndexBuffer(string? name, IndexFormat format)
	{
		var res = new BufferResource(this, name, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX, format);
		lock (resources)
			resources.Add(res);
		return res;
	}

	internal override IHandle CreateVertexBuffer(string? name)
	{
		var res = new BufferResource(this, name, SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX, default);
		lock (resources)
			resources.Add(res);
		return res;
	}

	internal override void UploadBufferData(IHandle buffer, nint data, int dataSize, int dataDestOffset)
	{
		var res = (BufferResource)buffer;
		res.Dirty = true;

		// (re)create buffer if needed
		var required = dataSize + dataDestOffset;
		if (required > res.Capacity || res.Handle == nint.Zero)
		{
			// TODO: A resize wipes all contents but the Buffer API doesn't expect this
			if (res.Handle != nint.Zero)
			{
				SDL_ReleaseGPUBuffer(device, res.Handle);
				res.Handle = nint.Zero;
			}

			// TODO: Upon first creation we should probably just create a perfectly sized buffer, and afterward next Po2
			int size;
			if (res.Capacity == 0)
			{
				// never create a buffer that has 0 length
				size = Math.Max(8, required);
			}
			else
			{
				size = 8;
				while (size < required)
					size *= 2;
			}

			uint props = 0;
			if (!string.IsNullOrEmpty(res.Name))
			{
				props = SDL_CreateProperties();
				SDL_SetStringProperty(props, SDL_PROP_GPU_BUFFER_CREATE_NAME_STRING, res.Name);
			}

			res.Handle = SDL_CreateGPUBuffer(device, new()
			{
				usage = res.Usage,
				size = (uint)size,
				props = props
			});

			if (props != 0)
				SDL_DestroyProperties(props);

			if (res.Handle == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUBuffer), "Mesh Creation Failed");
			res.Capacity = size;
		}

		// exit out of there's no data to upload
		if (data == nint.Zero)
			return;

		bool usingTemporaryTransferBuffer = false;
		nint transferBuffer = bufferUploadBuffer;
		uint transferOffset;
		bool transferCycle;

		// uploading more data than our transfer buffer, so create a temporary one
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
		// cycle transfer buffer because we're going to run out of space in the current one
		else if (bufferUploadBufferOffset + dataSize >= TransferBufferSize)
		{
			// only allow MaxUploadCycleCount cycles before flushing
			if (bufferUploadCycleCount < MaxUploadCycleCount)
			{
				transferCycle = true;
				transferOffset = 0;
				bufferUploadCycleCount += 1;
				bufferUploadBufferOffset = 0;
			}
			// otherwise flush
			else
			{
				FlushCommands(stall: true);
				transferCycle = true;
				transferOffset = 0;
			}
		}
		// normal transfer using existing buffer, only cycle if we're at the start
		else
		{
			transferCycle = bufferUploadBufferOffset == 0;
			transferOffset = bufferUploadBufferOffset;
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
			SDL_UploadToGPUBuffer(copyPass,
				source: new()
				{
					offset = transferOffset,
					transfer_buffer = transferBuffer
				},
				destination: new()
				{
					buffer = res.Handle,
					offset = (uint)dataDestOffset,
					size = (uint)dataSize
				},
				cycle: true
			);
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

	public void DestroyBuffer(IHandle buffer)
	{
		if (!buffer.Disposed)
		{
			var res = (BufferResource)buffer;

			lock (resources)
			{
				resources.Remove(buffer);
				res.Destroyed = true;
			}

			SDL_ReleaseGPUBuffer(device, res.Handle);
		}
	}

	internal override IHandle CreateShader(string? name, in ShaderCreateInfo shaderInfo)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var format = driver switch
		{
			GraphicsDriver.Vulkan => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
			GraphicsDriver.D3D12 => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
			GraphicsDriver.Metal => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
			_ => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
		};

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
				format = format,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
				num_samplers = (uint)shaderInfo.Vertex.SamplerCount,
				num_storage_textures = 0,
				num_storage_buffers = 0,
				num_uniform_buffers = (uint)shaderInfo.Vertex.UniformBufferCount,
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
				format = format,
				stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
				num_samplers = (uint)shaderInfo.Fragment.SamplerCount,
				num_storage_textures = 0,
				num_storage_buffers = 0,
				num_uniform_buffers = (uint)shaderInfo.Fragment.UniformBufferCount,
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

			ReleaseGraphicsPipelinesAssociatedWith(res);
			SDL_ReleaseGPUShader(device, res.VertexShader);
			SDL_ReleaseGPUShader(device, res.FragmentShader);
		}
	}

	internal override void DestroyResource(IHandle resource)
	{
		if (!resource.Disposed)
		{
			if (resource is TextureResource)
				DestroyTexture(resource);
			else if (resource is TargetResource)
				DestroyTarget(resource);
			else if (resource is BufferResource)
				DestroyBuffer(resource);
			else if (resource is ShaderResource)
				DestroyShader(resource);
		}
	}

	internal override void PerformDraw(DrawCommand command)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		var mat = command.Material;
		var shader = mat.Shader!;
		var target = command.Target;

		// try to start a render pass
		if (!BeginRenderPass(target, default))
			return;

		// set viewport
		var nextViewport = command.Viewport ?? new RectInt(0, 0, renderPassTargetSize.X, renderPassTargetSize.Y);
		if (renderPassViewport != nextViewport)
		{
			renderPassViewport = nextViewport;
			SDL_SetGPUViewport(renderPass, new()
			{
				x = nextViewport.X, y = nextViewport.Y,
				w = nextViewport.Width, h = nextViewport.Height,
				min_depth = 0, max_depth = 1
			});
		}

		// set scissor
		var nextScissor = command.Scissor ?? nextViewport;
		if (renderPassScissor != nextScissor)
		{
			renderPassScissor = nextScissor;
			SDL_SetGPUScissor(renderPass, new()
			{
				x = nextScissor.X, y = nextScissor.Y,
				w = nextScissor.Width, h = nextScissor.Height,
			});
		}

		// figure out graphics pipeline, potentially create a new one
		var pipeline = GetGraphicsPipeline(command);
		if (renderPassPipeline != pipeline)
		{
			renderPassPipeline = pipeline;
			SDL_BindGPUGraphicsPipeline(renderPass, pipeline);
		}

		// bind index buffer
		if (renderPassIndexBuffer != command.IndexBuffer?.Resource ||
			(command.IndexBuffer != null && ((BufferResource)command.IndexBuffer.Resource).Dirty))
		{
			renderPassIndexBuffer = command.IndexBuffer?.Resource;
			if (renderPassIndexBuffer != null)
			{
				var it = (BufferResource)renderPassIndexBuffer;
				it.Dirty = false;

				SDL_GPUBufferBinding indexBinding = new()
				{
					buffer = it.Handle,
					offset = 0
				};
				SDL_BindGPUIndexBuffer(renderPass, indexBinding, it.IndexFormat switch
				{
					IndexFormat.Sixteen => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT,
					IndexFormat.ThirtyTwo => SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT,
					_ => throw new NotImplementedException()
				});
			}
		}

		bool rebindVertexBuffers = renderPassVertexBuffers.Count != command.VertexBuffers.Count;
		if (!rebindVertexBuffers)
		{
			for (int i = 0; i < command.VertexBuffers.Count; i ++)
				if (renderPassVertexBuffers[i] != command.VertexBuffers[i].Buffer.Resource ||
					((BufferResource)command.VertexBuffers[i].Buffer.Resource).Dirty)
				{
					rebindVertexBuffers = true;
					break;
				}
		}

		// bind buffers
		if (rebindVertexBuffers)
		{
			Span<SDL_GPUBufferBinding> vertexBinding = stackalloc SDL_GPUBufferBinding[command.VertexBuffers.Count];

			for (int i = 0; i < command.VertexBuffers.Count; i ++)
			{
				var res = (BufferResource)command.VertexBuffers[i].Buffer.Resource;
				res.Dirty = false;

				vertexBinding[i] = new()
				{
					buffer = res.Handle,
					offset = 0
				};
			}

			SDL_BindGPUVertexBuffers(renderPass, 0, vertexBinding, (uint)command.VertexBuffers.Count);
		}

		var fragmentInfo = shader.CreateInfo.Fragment;
		var vertexInfo = shader.CreateInfo.Vertex;

		// bind fragment samplers
		// TODO: only do this if Samplers change
		if (fragmentInfo.SamplerCount > 0)
		{
			Span<SDL_GPUTextureSamplerBinding> samplers = stackalloc SDL_GPUTextureSamplerBinding[fragmentInfo.SamplerCount];

			for (int i = 0; i < fragmentInfo.SamplerCount; i++)
			{
				if (mat.Fragment.Samplers[i].Texture is { } tex && !tex.IsDisposed)
					samplers[i].texture = ((TextureResource)tex.Resource).SamplerTexture;
				else
					samplers[i].texture = ((TextureResource)emptyDefaultTexture!).SamplerTexture;

				samplers[i].sampler = GetSampler(mat.Fragment.Samplers[i].Sampler);
			}

			SDL_BindGPUFragmentSamplers(renderPass, 0, samplers, (uint)fragmentInfo.SamplerCount);
		}

		// bind vertex samplers
		// TODO: only do this if Samplers change
		if (vertexInfo.SamplerCount > 0)
		{
			Span<SDL_GPUTextureSamplerBinding> samplers = stackalloc SDL_GPUTextureSamplerBinding[vertexInfo.SamplerCount];

			for (int i = 0; i < vertexInfo.SamplerCount; i++)
			{
				if (mat.Vertex.Samplers[i].Texture is { } tex && !tex.IsDisposed)
					samplers[i].texture = ((TextureResource)tex.Resource).SamplerTexture;
				else
					samplers[i].texture = ((TextureResource)emptyDefaultTexture!).SamplerTexture;

				samplers[i].sampler = GetSampler(mat.Vertex.Samplers[i].Sampler);
			}

			SDL_BindGPUVertexSamplers(renderPass, 0, samplers, (uint)vertexInfo.SamplerCount);
		}

		// Upload Fragment Uniforms
		// TODO: only do this if Uniforms change
		for (int i = 0; i < fragmentInfo.UniformBufferCount; i ++)
		{
			fixed (byte* ptr = mat.Fragment.UniformBuffers[i])
				SDL_PushGPUFragmentUniformData(cmdRender, (uint)i, new nint(ptr), (uint)mat.Fragment.UniformBuffers[i].Length);
		}

		// Upload Vertex Uniforms
		// TODO: only do this if Uniforms change
		for (int i = 0; i < vertexInfo.UniformBufferCount; i ++)
		{
			fixed (byte* ptr = mat.Vertex.UniformBuffers[i])
				SDL_PushGPUVertexUniformData(cmdRender, (uint)i, new nint(ptr), (uint)mat.Vertex.UniformBuffers[i].Length);
		}

		// perform draw
		if (command.IndexBuffer != null)
		{
			SDL_DrawGPUIndexedPrimitives(
				render_pass: renderPass,
				num_indices: (uint)command.IndexCount,
				num_instances: (uint)Math.Max(1, command.InstanceCount),
				first_index: (uint)command.IndexOffset,
				vertex_offset: command.VertexOffset,
				first_instance: 0
			);
		}
		else
		{
			SDL_DrawGPUPrimitives(
				render_pass: renderPass,
				num_vertices: (uint)command.VertexCount,
				num_instances: (uint)Math.Max(1, command.InstanceCount),
				first_vertex: (uint)command.VertexOffset,
				first_instance: 0
			);
		}
	}

	internal override void Clear(IDrawableTarget target, ReadOnlySpan<Color> color, float depth, int stencil, ClearMask mask)
	{
		if (device == nint.Zero)
			throw deviceNotCreated;

		if (mask != ClearMask.None)
		{
			StackList8<Color>? colors = null;
			if (Calc.Has(mask, ClearMask.Color))
				colors = [..color[..Math.Min(MaxColorAttachments, color.Length)]];

			BeginRenderPass(target, new()
			{
				Color = colors,
				Depth = Calc.Has(mask, ClearMask.Depth) ? depth : null,
				Stencil = Calc.Has(mask, ClearMask.Stencil) ? stencil : null
			});
		}
	}

	private void FlushCommands(bool stall)
	{
		EndCopyPass();
		EndRenderPass();

		StackList4<nint> fences = new();

		// submit buffers
		if (stall)
		{
			var uploadFence = SDL_SubmitGPUCommandBufferAndAcquireFence(cmdUpload);
			if (uploadFence != nint.Zero)
				fences.Add(uploadFence);
			else
				Log.Warning($"Failed to acquire upload fence: {SDL_GetError()}");

			var renderFence = SDL_SubmitGPUCommandBufferAndAcquireFence(cmdRender);
			if (renderFence != nint.Zero)
				fences.Add(renderFence);
			else
				Log.Warning($"Failed to acquire render fence: {SDL_GetError()}");
		}
		else
		{
			SDL_SubmitGPUCommandBuffer(cmdUpload);
			SDL_SubmitGPUCommandBuffer(cmdRender);
		}

		// reset state
		cmdUpload = nint.Zero;
		cmdRender = nint.Zero;
		ResetCommandBufferState();

		// wait for gpu fences
		if (fences.Count > 0)
			SDL_WaitForGPUFences(device, true, fences.Span, (uint)fences.Count);

		// release gpu fences
		for (int i = 0; i < fences.Count; i ++)
			SDL_ReleaseGPUFence(device, fences[i]);
	}

	private void ResetCommandBufferState()
	{
		if (cmdRender != nint.Zero || cmdUpload != nint.Zero)
			throw new Exception("Must submit previous command buffers!");

		cmdRender = SDL_AcquireGPUCommandBuffer(device);
		cmdUpload = SDL_AcquireGPUCommandBuffer(device);

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

	private bool BeginRenderPass(IDrawableTarget drawableTarget, ClearInfo clear)
	{
		// only begin if we're not already in a render pass that is matching
		if (renderPass != nint.Zero &&
			renderPassTarget == drawableTarget &&
			!clear.Color.HasValue &&
			!clear.Depth.HasValue &&
			!clear.Stencil.HasValue)
			return true;

		EndRenderPass();

		// make sure we have something to draw to
		var target = GetDrawTarget(drawableTarget, out renderPassTargetSize);
		if (target == null)
			return false;

		// configure lists of textures used
		renderPassTarget = drawableTarget;

		var colorTargets = new StackList8<(nint Attachment, nint Resolve)>();
		var depthStencilTarget = nint.Zero;

		foreach (var it in target.Attachments)
		{
			var tex = (TextureResource)it.Resource;
			var res = tex.Texture;
			var resolve = tex.MultiSampleResolve?.Texture ?? nint.Zero;

			// drawing to an invalid target
			if (it.IsDisposed || !it.IsTargetAttachment || res == nint.Zero)
				throw new Exception("Drawing to a Disposed or Invalid Texture");

			if (it.Format.IsDepthStencilFormat())
				depthStencilTarget = res;
			else
				colorTargets.Add((res, resolve));
		}

		Span<SDL_GPUColorTargetInfo> colorInfo = stackalloc SDL_GPUColorTargetInfo[colorTargets.Count];

		// get color infos
		for (int i = 0; i < colorTargets.Count; i++)
		{
			var col = clear.Color.HasValue && clear.Color.Value.Count > i ? clear.Color.Value[i] : Color.Transparent;
			colorInfo[i] = new()
			{
				texture = colorTargets[i].Attachment,
				mip_level = 0,
				layer_or_depth_plane = 0,
				clear_color = GetColor(col),
				load_op = clear.Color.HasValue ?
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR :
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				store_op = colorTargets[i].Resolve == nint.Zero ?
					SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE :
					SDL_GPUStoreOp.SDL_GPU_STOREOP_RESOLVE,
				cycle = clear.Color.HasValue,
				resolve_texture = colorTargets[i].Resolve
			};
		}

		// get depth info
		// the assignment here is a bit weird as SDL_BeginGPURenderPass takes an "in"
		// parameter for the depth target, which we sometimes want to be NULL
		var depthValue = new SDL_GPUDepthStencilTargetInfo();
		scoped ref var depthTarget = ref Unsafe.NullRef<SDL_GPUDepthStencilTargetInfo>();
		if (depthStencilTarget != nint.Zero)
		{
			depthValue = new()
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
			depthTarget = ref depthValue;
		}

		// begin pass
		renderPass = SDL_BeginGPURenderPass(
			cmdRender,
			colorInfo,
			(uint)colorTargets.Count,
			depthTarget
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
		renderPassViewport = null;
		renderPassScissor = null;
		renderPassIndexBuffer = null;
		renderPassVertexBuffers.Clear();
	}

	private nint GetGraphicsPipeline(in DrawCommand command)
	{
		var shader = command.Material.Shader!;
		var shaderRes = (ShaderResource)shader.Resource;
		if (shaderRes.GraphicsDevice != this)
			throw deviceWasDestroyed;

		// build a big hashcode of everything in use
		var hash = HashCode.Combine(
			shader.Resource,
			command.CullMode,
			command.DepthCompare,
			command.DepthTestEnabled,
			command.DepthWriteEnabled,
			command.BlendMode
		);

		if (command.IndexBuffer != null)
			hash = HashCode.Combine(hash, command.IndexBuffer.Format);

		foreach (var vb in command.VertexBuffers)
			hash = HashCode.Combine(hash, vb.Buffer.Format, vb.InstanceInputRate);

		// combine with target attachment formats
		foreach (var format in GetDrawTargetFormatsAndSampleCount(command.Target))
			hash = HashCode.Combine(hash, format);

		// try to find an existing pipeline
		var pipeline = shaderRes.Pipelines.GetOrAdd<(GraphicsDeviceSDL, DrawCommand)>(hash, static (hash, args) =>
		{
			var self = args.Item1;
			var command = args.Item2;
			var shaderRes = (ShaderResource)command.Material.Shader!.Resource;
			var vertexAttributeCount = 0;
			foreach (var vb in command.VertexBuffers)
				vertexAttributeCount += vb.Buffer.Format.Elements.Count;

			var colorBlendState = GetBlendState(command.BlendMode);
			var colorAttachments = stackalloc SDL_GPUColorTargetDescription[MaxColorAttachments];
			var colorAttachmentCount = 0;
			var depthStencilAttachment = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;
			var vertexBindings = stackalloc SDL_GPUVertexBufferDescription[command.VertexBuffers.Count];
			var vertexAttributes = stackalloc SDL_GPUVertexAttribute[vertexAttributeCount];

			// get highest sampler count ...
			var sampleCount = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;

			foreach (var it in self.GetDrawTargetFormatsAndSampleCount(command.Target))
			{
				if (IsDepthTextureFormat(it.Format))
				{
					depthStencilAttachment = it.Format;
				}
				else
				{
					colorAttachments[colorAttachmentCount] = new()
					{
						format = it.Format,
						blend_state = colorBlendState
					};

					colorAttachmentCount++;
				}

				if ((int)it.SampleCount > (int)sampleCount)
					sampleCount = it.SampleCount;
			}

			var attrbIndex = 0;
			for (int slot = 0; slot < command.VertexBuffers.Count; slot ++)
			{
				var it = command.VertexBuffers[slot].Buffer;
				var instanceRate = command.VertexBuffers[slot].InstanceInputRate;
				var vertexOffset = 0;

				vertexBindings[slot] = new()
				{
					slot = (uint)slot,
					pitch = (uint)it.Format.Stride,
					input_rate = instanceRate
						? SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_INSTANCE
						: SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
					instance_step_rate = 0
				};

				foreach (var el in it.Format.Elements)
				{
					vertexAttributes[attrbIndex] = new()
					{
						location = (uint)el.Index,
						buffer_slot = (uint)slot,
						format = GetVertexFormat(el.Type, el.Normalized),
						offset = (uint)vertexOffset
					};
					vertexOffset += el.Type.SizeInBytes();
					attrbIndex++;
				}
			}

			SDL_GPUGraphicsPipelineCreateInfo info = new()
			{
				vertex_shader = shaderRes.VertexShader,
				fragment_shader = shaderRes.FragmentShader,
				vertex_input_state = new()
				{
					vertex_buffer_descriptions = vertexBindings,
					num_vertex_buffers = (uint)command.VertexBuffers.Count,
					vertex_attributes = vertexAttributes,
					num_vertex_attributes = (uint)vertexAttributeCount
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
					sample_count = sampleCount,
					sample_mask = 0,        // not actually used per SDL docs
					enable_mask = false,    // not actually used per SDL docs
					padding1 = 1,
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

			var pipeline = SDL_CreateGPUGraphicsPipeline(self.device, info);
			if (pipeline == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUGraphicsPipeline));
			return pipeline;

		}, (this, command));

		return pipeline;
	}

	private Target? GetDrawTarget(IDrawableTarget drawableTarget, out Point2 size)
	{
		// get specific target
		if (drawableTarget.Surface is Target target)
		{
			size = target.SizeInPixels();
			return target;
		}

		// get backbuffer target
		if (drawableTarget.Surface is Window && backbuffer != null)
		{
			size = backbufferSize;
			return backbuffer;
		}

		size = Point2.Zero;
		return null;
	}

	private StackList32<(SDL_GPUTextureFormat Format, SDL_GPUSampleCount SampleCount)> GetDrawTargetFormatsAndSampleCount(IDrawableTarget drawableTarget)
	{
		var formats = new StackList32<(SDL_GPUTextureFormat Format, SDL_GPUSampleCount SampleCount)>();
		var target = GetDrawTarget(drawableTarget, out _);
		if (target != null)
			foreach (var it in target.Attachments)
				formats.Add((GetTextureFormat(it.Format), GetSampleCount(it.SampleCount)));
		return formats;
	}

	private void ReleaseGraphicsPipelinesAssociatedWith(ShaderResource shader)
	{
		while (!shader.Pipelines.IsEmpty)
		{
			var removing = shader.Pipelines.Keys.ToArray();
			foreach (var it in removing)
				if (shader.Pipelines.TryRemove(it, out var pipeline))
				{
					SDL_ReleaseGPUGraphicsPipeline(device, pipeline);
				}
		}
	}

	private static SDL_GPUColorTargetBlendState GetBlendState(BlendMode blend)
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
			if (Calc.Has(mask, BlendMask.Red)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_R;
			if (Calc.Has(mask, BlendMask.Green)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_G;
			if (Calc.Has(mask, BlendMask.Blue)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_B;
			if (Calc.Has(mask, BlendMask.Alpha)) flags |= SDL_GPUColorComponentFlags.SDL_GPU_COLORCOMPONENT_A;
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
		static SDL_GPUSamplerAddressMode GetWrapMode(TextureWrap wrap) => wrap switch
		{
			TextureWrap.Repeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
			TextureWrap.MirroredRepeat => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
			TextureWrap.Clamp => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
			_ => throw new ArgumentException("Invalid Texture Wrap", nameof(wrap)),
		};

		if (!samplers.TryGetValue(sampler, out var result))
		{
			var filter = sampler.Filter switch
			{
				TextureFilter.Nearest => SDL_GPUFilter.SDL_GPU_FILTER_NEAREST,
				TextureFilter.Linear => SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
				_ => throw new ArgumentException("Invalid Texture Filter", nameof(sampler)),
			};

			SDL_GPUSamplerCreateInfo info = new()
			{
				min_filter = filter,
				mag_filter = filter,
				address_mode_u = GetWrapMode(sampler.WrapX),
				address_mode_v = GetWrapMode(sampler.WrapY),
				address_mode_w = SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
				compare_op = SDL_GPUCompareOp.SDL_GPU_COMPAREOP_ALWAYS,
				enable_compare = false,
			};
			result = SDL_CreateGPUSampler(device, info);
			if (result == nint.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateGPUSampler));
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

			_ => throw new ArgumentException("Invalid Vertex Format", nameof(type)),
		};
	}

	private static SDL_GPUTextureFormat GetTextureFormat(TextureFormat format) => format switch
	{
		TextureFormat.R8G8B8A8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
		TextureFormat.R8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM,
		TextureFormat.R8G8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM,
		TextureFormat.Depth24Stencil8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT,
		TextureFormat.Depth32Stencil8 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT,
		TextureFormat.Depth16 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM,
		TextureFormat.Depth24 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM,
		TextureFormat.Depth32 => SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT,
		_ => throw new ArgumentException("Invalid Texture Format", nameof(format)),
	};

	private static SDL_GPUSampleCount GetSampleCount(SampleCount sampleCount) => sampleCount switch
	{
		SampleCount.One => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
		SampleCount.Two => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_2,
		SampleCount.Four => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_4,
		SampleCount.Eight => SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_8,
		_ => throw new ArgumentException("Invalid Sample Count", nameof(sampleCount)),
	};

	private static bool IsDepthTextureFormat(SDL_GPUTextureFormat format) => format switch
	{
		SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM => true,
		SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM => true,
		SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT => true,
		SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT => true,
		SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT => true,
		_ => false
	};

	private static SDL_FColor GetColor(Color color)
	{
		var vec4 = color.ToVector4();
		return new() { r = vec4.X, g = vec4.Y, b = vec4.Z, a = vec4.W, };
	}
}
