
using System.Runtime.InteropServices;
using static SDL3.SDL;

namespace Foster.Framework;

internal sealed unsafe class RendererOpenGL : Renderer
{
	private class TextureResource(Renderer renderer) : IHandle
	{
		public readonly Renderer Renderer = renderer;
		public uint id;
		public int width;
		public int height;
		public TextureFormat format;
		public GL glInternalFormat;
		public GL glFormat;
		public GL glType;
		public GL glAttachment;
		public TextureSampler sampler;
		public bool Destroyed;
		public bool Disposed => Destroyed || Renderer != App.Renderer;
	}

	private class TargetResource(Renderer renderer) : IHandle
	{
		public readonly Renderer Renderer = renderer;
		public uint id;
		public int width;
		public int height;
		public readonly List<TextureResource> colorAttachments = [];
		public TextureResource? depthAttachment;
		
		public bool Destroyed;
		public bool Disposed => Destroyed || Renderer != App.Renderer;
	}

	private struct Uniform
	{
		string name;
		string samplerName;
		int glLocation;
		int glSize;
		GL glType;
		int samplerIndex;
	}

	private class ShaderResource(Renderer renderer) : IHandle
	{
		public readonly Renderer Renderer = renderer;
		public uint id;
		public int uniformCount;
		public int samplerCount;
		public readonly List<Uniform> uniforms = [];
		public readonly TextureResource[] textures = new TextureResource[32];
		public readonly TextureSampler[] samplers = new TextureSampler[32];
		public bool Destroyed;
		public bool Disposed => Destroyed || Renderer != App.Renderer;
	}

	private class MeshResource(Renderer renderer) : IHandle
	{
		public readonly Renderer Renderer = renderer;
		public uint id;
		public uint indexBuffer;
		public uint vertexBuffer;
		public uint instanceBuffer;
		public int vertexAttributesEnabled;
		public int instanceAttributesEnabled;
		public uint[] vertexAttributes = new uint[32];
		public uint[] instanceAttributes = new uint[32];
		public GL indexFormat;
		public int indexSize;
		public int vertexBufferSize;
		public int indexBufferSize;
		public bool Destroyed;
		public bool Disposed => Destroyed || Renderer != App.Renderer;
	}

	private struct State
	{
		public bool Initializing;
		public int ActiveTextureSlot;
		public fixed uint TextureSlots[32];
		public uint Program;
		public uint FrameBuffer;
		public uint VertexArray;
		public uint ArrayBuffer;
		public uint ElementBuffer;
		public int FrameBufferWidth;
		public int FrameBufferHeight;
		public RectInt Viewport;
		public bool ScissorEnabled;
		public RectInt Scissor;
		public CullMode Cull;
		public BlendMode Blend;
		public bool DepthCompareEnabled;
		public DepthCompare DepthCompare;
		public bool DepthMaskEnabled;
	}

	private GLFuncs gl = null!;
	private Version version = new();
	private nint window;
	private nint context;
	private bool vsync = false;
	private State state;

	public override nint Device { get; }
	public override GraphicsDriver Driver => GraphicsDriver.OpenGL;
	public override Version DriverVersion => version;

	public override void CreateDevice()
	{
		if (!SDL_GL_LoadLibrary(null!))
			throw Platform.CreateExceptionFromSDL(nameof(SDL_GL_LoadLibrary));
	}

	public override void DestroyDevice()
	{
		SDL_GL_UnloadLibrary();
	}

	public override void Startup(nint window)
	{
		this.window = window;

		// get desidered opengl version
		// TODO: Emscripten needs to be 3.0
		Version desiredVersion;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			desiredVersion = new(3, 3);
		else
			desiredVersion = new(4, 5);

		// setup GL context
		SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, desiredVersion.Major);
		SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, desiredVersion.Minor);
		SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
		SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL_GLcontextFlag.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
		SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);

		// create context
		context = SDL_GL_CreateContext(window);
		if (context == nint.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL_GL_CreateContext));
		if (!SDL_GL_MakeCurrent(window, context))
			throw Platform.CreateExceptionFromSDL(nameof(SDL_GL_MakeCurrent));

		// load bindings
		gl = new();
		gl.Enable(GL.DEBUG_OUTPUT);
		gl.Enable(GL.DEBUG_OUTPUT_SYNCHRONOUS);
		gl.DebugMessageCallback(&OnDebugMessageCallback, nint.Zero);

		// get version / renderer device
		gl.GetIntegerv((GL)0x821B, out int major);
		gl.GetIntegerv((GL)0x821C, out int minor);
		version = new(major, minor);
		Log.Info($"Graphics Driver: OpenGL {major}.{minor} [{Platform.ParseUTF8(gl.GetString(GL.RENDERER))}]");

		// don't include row padding
		gl.PixelStorei(GL.PACK_ALIGNMENT, 1);
		gl.PixelStorei(GL.UNPACK_ALIGNMENT, 1);

		// blend is always enabled
		gl.Enable(GL.BLEND);

		// default starting state
		state.Initializing = true;
		FosterBindProgram(0);
		FosterBindFrameBuffer(null);
		FosterBindArray(0);
		FosterSetViewport(false, default);
		FosterSetScissor(false, default);
		FosterSetBlend(new());
		FosterSetCull(CullMode.None);
		FosterSetDepthCompare(false, DepthCompare.Always);
		FosterSetDepthMask(false);
		state.Initializing = false;

		// vsync is on by default
		SetVSync(true);
	}

	public override void Shutdown()
	{
		SDL_GL_DestroyContext(context);
		context = nint.Zero;
	}
	
	public override bool GetVSync() => vsync;

	public override void SetVSync(bool enabled)
	{
		if (vsync != enabled)
		{
			vsync = enabled;
			if (!SDL_GL_SetSwapInterval(enabled ? 1 : 0))
				Log.Warning($"Setting V-Sync faled: {SDL_GetError()}");
		}
	}

	public override void Present()
	{
		gl.Flush();

		// bind 0 to the frame buffer as per SDL's suggestion for macOS:
		// https://wiki.libsdl.org/SDL3/SDL_GL_SwapWindow#remarks
		FosterBindFrameBuffer(null);

		SDL_GL_SwapWindow(window);
	}

	public override IHandle CreateTexture(int width, int height, TextureFormat format, IHandle? targetBinding)
	{
		TextureResource texture = new(this)
		{
			id = 0,
			width = width,
			height = height,
			format = format,
			glInternalFormat = GL.RED,
			glFormat = GL.RED,
			glType = GL.UNSIGNED_BYTE,
			sampler = new()
		};

		switch (format)
		{
			case TextureFormat.R8:
				texture.glInternalFormat = GL.RED;
				texture.glFormat = GL.RED;
				texture.glType = GL.UNSIGNED_BYTE;
				break;
			case TextureFormat.R8G8B8A8:
				texture.glInternalFormat = GL.RGBA;
				texture.glFormat = GL.RGBA;
				texture.glType = GL.UNSIGNED_BYTE;
				break;
			case TextureFormat.Depth24Stencil8:
				texture.glInternalFormat = GL.DEPTH24_STENCIL8;
				texture.glFormat = GL.DEPTH_STENCIL;
				texture.glType = GL.UNSIGNED_INT_24_8;
				break;
		}

		// generate ID
		{
			var ids = stackalloc uint[1];
			gl.GenTextures(1, new nint(ids));
			if (ids[0] == 0)
				throw new Exception("Failed to create Texture");
			texture.id = ids[0];
		}

		// setup texture properties
		{
			FosterBindTexture(0, texture.id);
			gl.TexImage2D(GL.TEXTURE_2D, 0, texture.glInternalFormat, width, height, 0, texture.glFormat, texture.glType, nint.Zero);
		}

		// potentially bind to a target
		if (targetBinding is TargetResource target)
		{
			FosterBindFrameBuffer(target);

			if (texture.glInternalFormat == GL.DEPTH24_STENCIL8)
			{
				texture.glAttachment = GL.DEPTH_STENCIL_ATTACHMENT;
				target.depthAttachment = texture;
			}
			else
			{
				texture.glAttachment = GL.COLOR_ATTACHMENT0 + target.colorAttachments.Count;
				target.colorAttachments.Add(texture);
			}

			gl.FramebufferTexture2D(GL.FRAMEBUFFER, texture.glAttachment, GL.TEXTURE_2D, texture.id, 0);
		}

		return texture;
	}

	public override void SetTextureData(IHandle texture, nint data, int length)
	{
		if (texture is TextureResource res && !res.Disposed)
		{
			FosterBindTexture(0, res.id);
			gl.TexImage2D(GL.TEXTURE_2D, 0, res.glInternalFormat, res.width, res.height, 0, res.glFormat, res.glType, data);
		}
	}

	public override void GetTextureData(IHandle texture, nint data, int length)
	{

	}

	public void DestroyTexture(IHandle texture)
	{

	}

	public override IHandle CreateTarget(int width, int height)
	{
		// create ID
		var ids = stackalloc uint[1];
		gl.GenFramebuffers(1, new nint(ids));
		if (ids[0] == 0)
			throw new Exception("Failed to create Target's FrameBuffer");

		// create target
		var target = new TargetResource(this)
		{
			id = ids[0],
			width = width,
			height = height
		};
		FosterBindFrameBuffer(target);
		return target;
	}

	public override IHandle CreateMesh()
	{
		return new MeshResource(this);
	}

	public override void SetMeshVertexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format)
	{

	}

	public override void SetMeshIndexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format)
	{

	}

	public void DestroyMesh(IHandle mesh)
	{

	}

	public override IHandle CreateShader(in ShaderCreateInfo shaderInfo)
	{
		return new ShaderResource(this);
	}

	public void DestroyShader(IHandle shader)
	{

	}

	public override void DestroyResource(IHandle texture)
	{

	}

	public override void Draw(DrawCommand command)
	{

	}

	public override void Clear(Target? target, Color color, float depth, int stencil, ClearMask mask)
	{
		FosterBindFrameBuffer(target?.Resource as TargetResource);
		FosterSetViewport(false, default);
		FosterSetScissor(false, default);

		GL clear = 0;

		if (mask.Has(ClearMask.Color))
		{
			clear |= GL.COLOR_BUFFER_BIT;
			gl.ColorMask(true, true, true, true);
			gl.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
		}

		if (mask.Has(ClearMask.Depth))
		{
			FosterSetDepthMask(true);

			clear |= GL.DEPTH_BUFFER_BIT;
			gl.ClearDepth?.Invoke(depth);
		}

		if (mask.Has(ClearMask.Stencil))
		{
			clear |= GL.STENCIL_BUFFER_BIT;
			gl.ClearStencil?.Invoke(stencil);
		}

		gl.Clear(clear);
	}

	private void FosterBindFrameBuffer(TargetResource? target)
	{
		uint framebuffer = 0;

		if (target == null)
		{
			framebuffer = 0;
			state.FrameBufferWidth = App.Running ? App.WidthInPixels : 1;
			state.FrameBufferHeight = App.Running ? App.HeightInPixels : 1;
		}
		else
		{
			framebuffer = target.id;
			state.FrameBufferWidth = target.width;
			state.FrameBufferHeight = target.height;
		}

		if (state.Initializing || state.FrameBuffer != framebuffer)
		{
			var attachments = stackalloc GL[4];
			gl.BindFramebuffer(GL.FRAMEBUFFER, framebuffer);

			// figure out draw buffers
			if (target == null)
			{
				attachments[0] = GL.BACK_LEFT;
				gl.DrawBuffers(1, attachments);
			}
			else
			{
				for (int i = 0; i < target.colorAttachments.Count; i ++)
					attachments[i] = GL.COLOR_ATTACHMENT0 + i;
				gl.DrawBuffers(target.colorAttachments.Count, attachments);
			}

		}
		state.FrameBuffer = framebuffer;
	}

	private void FosterBindProgram(uint id)
	{
		if (state.Initializing || state.Program != id)
			gl.UseProgram(id);
		state.Program = id;
	}

	private void FosterBindArray(uint id)
	{
		if (state.Initializing || state.VertexArray != id)
			gl.BindVertexArray(id);
		state.VertexArray = id;
	}

	private void FosterBindTexture(int slot, uint id)
	{
		if (state.ActiveTextureSlot != slot)
		{
			gl.ActiveTexture(GL.TEXTURE0);
			state.ActiveTextureSlot = slot;
		}

		if (state.TextureSlots[slot] != id)
		{
			gl.BindTexture(GL.TEXTURE_2D, id);
			state.TextureSlots[slot] = id;
		}
	}


	// Same as FosterBindTexture, except it the resulting global state doesn't
	// necessarily have the slot active or texture bound, if no changes were required.
	private void FosterEnsureTextureSlotIs(int slot, uint id)
	{
		if (state.TextureSlots[slot] != id)
		{
			if (state.ActiveTextureSlot != slot)
			{
				gl.ActiveTexture(GL.TEXTURE0 + slot);
				state.ActiveTextureSlot = slot;
			}

			gl.BindTexture(GL.TEXTURE_2D, id);
			state.TextureSlots[slot] = id;
		}
	}

	private void FosterSetTextureSampler(TextureResource tex, TextureSampler sampler)
	{
		if (!tex.Disposed && tex.sampler != sampler)
		{
			FosterBindTexture(0, tex.id);

			if (tex.sampler.Filter != sampler.Filter)
			{
				gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MIN_FILTER, (int)FosterFilterToGL(sampler.Filter));
				gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_MAG_FILTER, (int)FosterFilterToGL(sampler.Filter));
			}

			if (tex.sampler.WrapX != sampler.WrapX)
				gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_S, (int)FosterWrapToGL(sampler.WrapX));

			if (tex.sampler.WrapY != sampler.WrapY)
				gl.TexParameteri(GL.TEXTURE_2D, GL.TEXTURE_WRAP_T, (int)FosterWrapToGL(sampler.WrapY));

			tex.sampler = sampler;
		}
	}

	private void FosterSetViewport(bool enabled, RectInt rect)
	{
		RectInt viewport;

		if (enabled)
		{
			viewport = rect;
			viewport.Y = state.FrameBufferHeight - viewport.Y - viewport.Height;
		}
		else
		{
			viewport = new()
			{
				X = 0, Y = 0,
				Width = state.FrameBufferWidth, Height = state.FrameBufferHeight
			};
		}

		if (state.Initializing || viewport != state.Viewport)
		{
			gl.Viewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
			state.Viewport = viewport;
		}
	}

	private void FosterSetScissor(bool enabled, in RectInt rect)
	{
		// get input scissor first
		var scissor = rect;
		scissor.Y = state.FrameBufferHeight - scissor.Y - scissor.Height;
		if (scissor.Width < 0) scissor.Width = 0;
		if (scissor.Height < 0) scissor.Height = 0;

		// toggle scissor
		if (state.Initializing ||
			enabled != state.ScissorEnabled ||
			(enabled && scissor != state.Scissor))
		{
			if (enabled)
			{
				if (!state.ScissorEnabled)
					gl.Enable(GL.SCISSOR_TEST);
				gl.Scissor(scissor.X, scissor.Y, scissor.Width, scissor.Height);
				state.Scissor = scissor;
			}
			else
			{
				gl.Disable(GL.SCISSOR_TEST);
			}

			state.ScissorEnabled = enabled;
		}
	}

	private void FosterSetBlend(BlendMode blend)
	{
		if (state.Initializing || state.Blend != blend)
		{
			GL colorOp = FosterBlendOpToGL(blend.ColorOperation);
			GL alphaOp = FosterBlendOpToGL(blend.AlphaOperation);
			gl.BlendEquationSeparate(colorOp, alphaOp);

			GL colorSrc = FosterBlendFactorToGL(blend.ColorSource);
			GL colorDst = FosterBlendFactorToGL(blend.ColorDestination);
			GL alphaSrc = FosterBlendFactorToGL(blend.AlphaSource);
			GL alphaDst = FosterBlendFactorToGL(blend.AlphaDestination);
			gl.BlendFuncSeparate(colorSrc, colorDst, alphaSrc, alphaDst);
		}

		if (state.Initializing || state.Blend.Mask != blend.Mask)
		{
			gl.ColorMask(
				blend.Mask.Has(BlendMask.Red),
				blend.Mask.Has(BlendMask.Green),
				blend.Mask.Has(BlendMask.Blue),
				blend.Mask.Has(BlendMask.Alpha));
		}

		if (state.Initializing || state.Blend.Color != blend.Color)
		{
			gl.BlendColor(
				blend.Color.R / 255.0f,
				blend.Color.G / 255.0f,
				blend.Color.B / 255.0f,
				blend.Color.A / 255.0f);
		}

		state.Blend = blend;
	}

	private void FosterSetDepthCompare(bool enabled, DepthCompare compare)
	{
		if (state.Initializing || enabled != state.DepthCompareEnabled || compare != state.DepthCompare)
		{
			if (!enabled)
			{
				gl.Disable(GL.DEPTH_TEST);
			}
			else
			{
				gl.Enable(GL.DEPTH_TEST);

				switch (compare)
				{
					case DepthCompare.Always: gl.DepthFunc(GL.ALWAYS); break;
					case DepthCompare.Equal: gl.DepthFunc(GL.EQUAL); break;
					case DepthCompare.Greater: gl.DepthFunc(GL.GREATER); break;
					case DepthCompare.GreatorOrEqual: gl.DepthFunc(GL.GEQUAL); break;
					case DepthCompare.Less: gl.DepthFunc(GL.LESS); break;
					case DepthCompare.LessOrEqual: gl.DepthFunc(GL.LEQUAL); break;
					case DepthCompare.Never: gl.DepthFunc(GL.NEVER); break;
					case DepthCompare.NotEqual: gl.DepthFunc(GL.NOTEQUAL); break;
				}
			}
		}

		state.DepthCompare = compare;
		state.DepthCompareEnabled = enabled;
	}

	private void FosterSetDepthMask(bool depthMask)
	{
		if (state.Initializing || depthMask != state.DepthMaskEnabled)
			gl.DepthMask(depthMask);
		state.DepthMaskEnabled = depthMask;
	}

	private void FosterSetCull(CullMode cull)
	{
		if (state.Initializing || cull != state.Cull)
		{
			if (cull == CullMode.None)
			{
				gl.Disable(GL.CULL_FACE);
			}
			else
			{
				if (state.Cull == CullMode.None)
					gl.Enable(GL.CULL_FACE);

				switch (cull)
				{
					case CullMode.Back: gl.CullFace(GL.BACK); break;
					case CullMode.Front: gl.CullFace(GL.FRONT); break;
				}
			}
		}

		state.Cull = cull;
	}

	private static GL FosterWrapToGL(TextureWrap wrap) => wrap switch
	{
		TextureWrap.Repeat => GL.REPEAT,
		TextureWrap.MirroredRepeat => GL.MIRRORED_REPEAT,
		TextureWrap.Clamp => GL.CLAMP_TO_EDGE,
		_ => GL.REPEAT,
	};

	private static GL FosterFilterToGL(TextureFilter filter) => filter switch
	{
		TextureFilter.Nearest => GL.NEAREST,
		TextureFilter.Linear => GL.LINEAR,
		_ => GL.NEAREST,
	};

	private static GL FosterBlendOpToGL(BlendOp operation) => operation switch
	{
		BlendOp.Add => GL.FUNC_ADD,
		BlendOp.Subtract => GL.FUNC_SUBTRACT,
		BlendOp.ReverseSubtract => GL.FUNC_REVERSE_SUBTRACT,
		BlendOp.Min => GL.MIN,
		BlendOp.Max => GL.MAX,
		_ => GL.FUNC_ADD,
	};

	private static GL FosterBlendFactorToGL(BlendFactor factor) => factor switch
	{
		BlendFactor.Zero => GL.ZERO,
		BlendFactor.One => GL.ONE,
		BlendFactor.SrcColor => GL.SRC_COLOR,
		BlendFactor.OneMinusSrcColor => GL.ONE_MINUS_SRC_COLOR,
		BlendFactor.DstColor => GL.DST_COLOR,
		BlendFactor.OneMinusDstColor => GL.ONE_MINUS_DST_COLOR,
		BlendFactor.SrcAlpha => GL.SRC_ALPHA,
		BlendFactor.OneMinusSrcAlpha => GL.ONE_MINUS_SRC_ALPHA,
		BlendFactor.DstAlpha => GL.DST_ALPHA,
		BlendFactor.OneMinusDstAlpha => GL.ONE_MINUS_DST_ALPHA,
		BlendFactor.ConstantColor => GL.CONSTANT_COLOR,
		BlendFactor.OneMinusConstantColor => GL.ONE_MINUS_CONSTANT_COLOR,
		BlendFactor.ConstantAlpha => GL.CONSTANT_ALPHA,
		BlendFactor.OneMinusConstantAlpha => GL.ONE_MINUS_CONSTANT_ALPHA,
		BlendFactor.SrcAlphaSaturate => GL.SRC_ALPHA_SATURATE,
		BlendFactor.Src1Color => GL.SRC1_COLOR,
		BlendFactor.OneMinusSrc1Color => GL.ONE_MINUS_SRC1_COLOR,
		BlendFactor.Src1Alpha => GL.SRC1_ALPHA,
		BlendFactor.OneMinusSrc1Alpha => GL.ONE_MINUS_SRC1_ALPHA,
		_ => throw new NotImplementedException(),
	};

	private static UniformType FosterUniformTypeFromGL(GL value) => value switch
	{
		GL.FLOAT => UniformType.Float,
		GL.FLOAT_VEC2 => UniformType.Float2,
		GL.FLOAT_VEC3 => UniformType.Float3,
		GL.FLOAT_VEC4 => UniformType.Float4,
		GL.FLOAT_MAT3x2 => UniformType.Mat3x2,
		GL.FLOAT_MAT4 => UniformType.Mat4x4,
		_ => UniformType.None,
	};

	[UnmanagedCallersOnly]
	private static void OnDebugMessageCallback(GL source, GL type, uint id, GL severity, uint length, sbyte* message, IntPtr userParam)
	{
		if (severity == GL.DEBUG_SEVERITY_NOTIFICATION)
			return;

		var output = new string(message, 0, (int)length);
		if (type == GL.DEBUG_TYPE_ERROR)
			throw new Exception(output);

		var typeName = type switch
		{
			GL.DEBUG_TYPE_ERROR => "ERROR",
			GL.DEBUG_TYPE_DEPRECATED_BEHAVIOR => "DEPRECATED BEHAVIOR",
			GL.DEBUG_TYPE_MARKER => "MARKER",
			GL.DEBUG_TYPE_OTHER => "OTHER",
			GL.DEBUG_TYPE_PERFORMANCE => "PEROFRMANCE",
			GL.DEBUG_TYPE_POP_GROUP => "POP GROUP",
			GL.DEBUG_TYPE_PORTABILITY => "PORTABILITY",
			GL.DEBUG_TYPE_PUSH_GROUP => "PUSH GROUP",
			_ => "UNDEFINED BEHAVIOR",
		};

		var severityName = severity switch
		{
			GL.DEBUG_SEVERITY_HIGH => "HIGH",
			GL.DEBUG_SEVERITY_MEDIUM => "MEDIUM",
			GL.DEBUG_SEVERITY_LOW => "LOW",
			_ => string.Empty,
		};

		Log.Warning($"OpenGL {typeName}, {severityName}: {output}");
	}

	#region OpenGL Enum

	internal enum GL
	{
		DONT_CARE = 0x1100,
		ZERO = 0x0000,
		ONE = 0x0001,
		BYTE = 0x1400,
		UNSIGNED_BYTE = 0x1401,
		SHORT = 0x1402,
		UNSIGNED_SHORT = 0x1403,
		INT = 0x1404,
		UNSIGNED_INT = 0x1405,
		FLOAT = 0x1406,
		HALF_FLOAT = 0x140B,
		UNSIGNED_SHORT_4_4_4_4_REV = 0x8365,
		UNSIGNED_SHORT_5_5_5_1_REV = 0x8366,
		UNSIGNED_INT_2_10_10_10_REV = 0x8368,
		UNSIGNED_SHORT_5_6_5 = 0x8363,
		UNSIGNED_INT_24_8 = 0x84FA,
		VENDOR = 0x1F00,
		RENDERER = 0x1F01,
		VERSION = 0x1F02,
		EXTENSIONS = 0x1F03,
		COLOR_BUFFER_BIT = 0x4000,
		DEPTH_BUFFER_BIT = 0x0100,
		STENCIL_BUFFER_BIT = 0x0400,
		SCISSOR_TEST = 0x0C11,
		DEPTH_TEST = 0x0B71,
		STENCIL_TEST = 0x0B90,
		LINE = 0x1B01,
		FILL = 0x1B02,
		CW = 0x0900,
		CCW = 0x0901,
		FRONT = 0x0404,
		BACK = 0x0405,
		BACK_LEFT = 0x0402,
		FRONT_AND_BACK = 0x0408,
		CULL_FACE = 0x0B44,
		POLYGON_OFFSET_FILL = 0x8037,
		TEXTURE_2D = 0x0DE1,
		TEXTURE_3D = 0x806F,
		TEXTURE_CUBE_MAP = 0x8513,
		TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515,
		BLEND = 0x0BE2,
		SRC_COLOR = 0x0300,
		ONE_MINUS_SRC_COLOR = 0x0301,
		SRC_ALPHA = 0x0302,
		ONE_MINUS_SRC_ALPHA = 0x0303,
		DST_ALPHA = 0x0304,
		ONE_MINUS_DST_ALPHA = 0x0305,
		DST_COLOR = 0x0306,
		ONE_MINUS_DST_COLOR = 0x0307,
		SRC_ALPHA_SATURATE = 0x0308,
		CONSTANT_COLOR = 0x8001,
		ONE_MINUS_CONSTANT_COLOR = 0x8002,
		CONSTANT_ALPHA = 0x8003,
		ONE_MINUS_CONSTANT_ALPHA = 0x8004,
		SRC1_ALPHA = 0x8589,
		SRC1_COLOR = 0x88F9,
		ONE_MINUS_SRC1_COLOR = 0x88FA,
		ONE_MINUS_SRC1_ALPHA = 0x88FB,
		MIN = 0x8007,
		MAX = 0x8008,
		FUNC_ADD = 0x8006,
		FUNC_SUBTRACT = 0x800A,
		FUNC_REVERSE_SUBTRACT = 0x800B,
		NEVER = 0x0200,
		LESS = 0x0201,
		EQUAL = 0x0202,
		LEQUAL = 0x0203,
		GREATER = 0x0204,
		NOTEQUAL = 0x0205,
		GEQUAL = 0x0206,
		ALWAYS = 0x0207,
		INVERT = 0x150A,
		KEEP = 0x1E00,
		REPLACE = 0x1E01,
		INCR = 0x1E02,
		DECR = 0x1E03,
		INCR_WRAP = 0x8507,
		DECR_WRAP = 0x8508,
		REPEAT = 0x2901,
		CLAMP_TO_EDGE = 0x812F,
		MIRRORED_REPEAT = 0x8370,
		NEAREST = 0x2600,
		LINEAR = 0x2601,
		NEAREST_MIPMAP_NEAREST = 0x2700,
		NEAREST_MIPMAP_LINEAR = 0x2702,
		LINEAR_MIPMAP_NEAREST = 0x2701,
		LINEAR_MIPMAP_LINEAR = 0x2703,
		COLOR_ATTACHMENT0 = 0x8CE0,
		DEPTH_ATTACHMENT = 0x8D00,
		STENCIL_ATTACHMENT = 0x8D20,
		DEPTH_STENCIL_ATTACHMENT = 0x821A,
		RED = 0x1903,
		RGB = 0x1907,
		RGBA = 0x1908,
		LUMINANCE = 0x1909,
		RGB8 = 0x8051,
		RGBA8 = 0x8058,
		RGBA4 = 0x8056,
		RGB5_A1 = 0x8057,
		RGB10_A2_EXT = 0x8059,
		RGBA16 = 0x805B,
		BGRA = 0x80E1,
		DEPTH_COMPONENT16 = 0x81A5,
		DEPTH_COMPONENT24 = 0x81A6,
		RG = 0x8227,
		RG8 = 0x822B,
		RG16 = 0x822C,
		R16F = 0x822D,
		R32F = 0x822E,
		RG16F = 0x822F,
		RG32F = 0x8230,
		RGBA32F = 0x8814,
		RGBA16F = 0x881A,
		DEPTH24_STENCIL8 = 0x88F0,
		COMPRESSED_TEXTURE_FORMATS = 0x86A3,
		COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1,
		COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2,
		COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3,
		DEPTH_COMPONENT = 0x1902,
		DEPTH_STENCIL = 0x84F9,
		TEXTURE_WRAP_S = 0x2802,
		TEXTURE_WRAP_T = 0x2803,
		TEXTURE_WRAP_R = 0x8072,
		TEXTURE_MAG_FILTER = 0x2800,
		TEXTURE_MIN_FILTER = 0x2801,
		TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE,
		TEXTURE_BASE_LEVEL = 0x813C,
		TEXTURE_MAX_LEVEL = 0x813D,
		TEXTURE_LOD_BIAS = 0x8501,
		PACK_ALIGNMENT = 0x0D05,
		UNPACK_ALIGNMENT = 0x0CF5,
		TEXTURE0 = 0x84C0,
		MAX_TEXTURE_IMAGE_UNITS = 0x8872,
		MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C,
		ARRAY_BUFFER = 0x8892,
		ELEMENT_ARRAY_BUFFER = 0x8893,
		STREAM_DRAW = 0x88E0,
		STATIC_DRAW = 0x88E4,
		DYNAMIC_DRAW = 0x88E8,
		MAX_VERTEX_ATTRIBS = 0x8869,
		FRAMEBUFFER = 0x8D40,
		READ_FRAMEBUFFER = 0x8CA8,
		DRAW_FRAMEBUFFER = 0x8CA9,
		RENDERBUFFER = 0x8D41,
		MAX_DRAW_BUFFERS = 0x8824,
		POINTS = 0x0000,
		LINES = 0x0001,
		LINE_STRIP = 0x0003,
		TRIANGLES = 0x0004,
		TRIANGLE_STRIP = 0x0005,
		QUERY_RESULT = 0x8866,
		QUERY_RESULT_AVAILABLE = 0x8867,
		SAMPLES_PASSED = 0x8914,
		MULTISAMPLE = 0x809D,
		MAX_SAMPLES = 0x8D57,
		SAMPLE_MASK = 0x8E51,
		FRAGMENT_SHADER = 0x8B30,
		VERTEX_SHADER = 0x8B31,
		ACTIVE_UNIFORMS = 0x8B86,
		ACTIVE_ATTRIBUTES = 0x8B89,
		FLOAT_VEC2 = 0x8B50,
		FLOAT_VEC3 = 0x8B51,
		FLOAT_VEC4 = 0x8B52,
		SAMPLER_2D = 0x8B5E,
		FLOAT_MAT3x2 = 0x8B67,
		FLOAT_MAT4 = 0x8B5C,
		NUM_EXTENSIONS = 0x821D,
		DEBUG_SOURCE_API = 0x8246,
		DEBUG_SOURCE_WINDOW_SYSTEM = 0x8247,
		DEBUG_SOURCE_SHADER_COMPILER = 0x8248,
		DEBUG_SOURCE_THIRD_PARTY = 0x8249,
		DEBUG_SOURCE_APPLICATION = 0x824A,
		DEBUG_SOURCE_OTHER = 0x824B,
		DEBUG_TYPE_ERROR = 0x824C,
		DEBUG_TYPE_PUSH_GROUP = 0x8269,
		DEBUG_TYPE_POP_GROUP = 0x826A,
		DEBUG_TYPE_MARKER = 0x8268,
		DEBUG_TYPE_DEPRECATED_BEHAVIOR = 0x824D,
		DEBUG_TYPE_UNDEFINED_BEHAVIOR = 0x824E,
		DEBUG_TYPE_PORTABILITY = 0x824F,
		DEBUG_TYPE_PERFORMANCE = 0x8250,
		DEBUG_TYPE_OTHER = 0x8251,
		DEBUG_SEVERITY_HIGH = 0x9146,
		DEBUG_SEVERITY_MEDIUM = 0x9147,
		DEBUG_SEVERITY_LOW = 0x9148,
		DEBUG_SEVERITY_NOTIFICATION = 0x826B,
		DEBUG_OUTPUT = 0x92E0,
		DEBUG_OUTPUT_SYNCHRONOUS = 0x8242,
		COMPILE_STATUS = 0x8B81,
		LINK_STATUS = 0x8B82,
	}

	#endregion

	#region OpenGL Proc Address Methods

	private class GLFuncs
	{
		private static T? GetProcAddress<T>(string name) where T : Delegate
		{
			var addr = SDL_GL_GetProcAddress(name);
			if (addr != nint.Zero && Marshal.GetDelegateForFunctionPointer<T>(addr) is T it)
				return it;
			return null;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DebugMessageCallbackFn(delegate* unmanaged<GL, GL, uint, GL, uint, sbyte*, IntPtr, void> callback, IntPtr userdata);
		public readonly DebugMessageCallbackFn DebugMessageCallback = GetProcAddress<DebugMessageCallbackFn>($"gl{nameof(DebugMessageCallback)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate nint GetStringFn(GL name);
		public readonly GetStringFn GetString = GetProcAddress<GetStringFn>($"gl{nameof(GetString)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void FlushFn();
		public readonly FlushFn Flush = GetProcAddress<FlushFn>($"gl{nameof(Flush)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void EnableFn(GL mode);
		public readonly EnableFn Enable = GetProcAddress<EnableFn>($"gl{nameof(Enable)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DisableFn(GL mode);
		public readonly DisableFn Disable = GetProcAddress<DisableFn>($"gl{nameof(Disable)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ClearFn(GL mode);
		public readonly ClearFn Clear = GetProcAddress<ClearFn>($"gl{nameof(Clear)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ClearColorFn(float r, float g, float b, float a);
		public readonly ClearColorFn ClearColor = GetProcAddress<ClearColorFn>($"gl{nameof(ClearColor)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ClearDepthFn(double depth);
		public readonly ClearDepthFn ClearDepth = GetProcAddress<ClearDepthFn>($"gl{nameof(ClearDepth)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ClearStencilFn(int stencil);
		public readonly ClearStencilFn ClearStencil = GetProcAddress<ClearStencilFn>($"gl{nameof(ClearStencil)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DepthMaskFn(bool enabled);
		public readonly DepthMaskFn DepthMask = GetProcAddress<DepthMaskFn>($"gl{nameof(DepthMask)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DepthFuncFn(GL func);
		public readonly DepthFuncFn DepthFunc = GetProcAddress<DepthFuncFn>($"gl{nameof(DepthFunc)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ViewportFn(int x, int y, int width, int height);
		public readonly ViewportFn Viewport = GetProcAddress<ViewportFn>($"gl{nameof(Viewport)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ScissorFn(int x, int y, int width, int height);
		public readonly ScissorFn Scissor = GetProcAddress<ScissorFn>($"gl{nameof(Scissor)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void CullFaceFn(GL mode);
		public readonly CullFaceFn CullFace = GetProcAddress<CullFaceFn>($"gl{nameof(CullFace)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BlendEquationFn(GL eq);
		public readonly BlendEquationFn BlendEquation = GetProcAddress<BlendEquationFn>($"gl{nameof(BlendEquation)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BlendEquationSeparateFn(GL modeRGB, GL modeAlpha);
		public readonly BlendEquationSeparateFn BlendEquationSeparate = GetProcAddress<BlendEquationSeparateFn>($"gl{nameof(BlendEquationSeparate)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BlendFuncFn(GL sfactor, GL dfactor);
		public readonly BlendFuncFn BlendFunc = GetProcAddress<BlendFuncFn>($"gl{nameof(BlendFunc)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BlendFuncSeparateFn(GL srcRGB, GL dstRGB, GL srcAlpha, GL dstAlpha);
		public readonly BlendFuncSeparateFn BlendFuncSeparate = GetProcAddress<BlendFuncSeparateFn>($"gl{nameof(BlendFuncSeparate)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BlendColorFn(float red, float green, float blue, float alpha);
		public readonly BlendColorFn BlendColor = GetProcAddress<BlendColorFn>($"gl{nameof(BlendColor)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ColorMaskFn(bool red, bool green, bool blue, bool alpha);
		public readonly ColorMaskFn ColorMask = GetProcAddress<ColorMaskFn>($"gl{nameof(ColorMask)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void PixelStoreiFn(GL name, int value);
		public readonly PixelStoreiFn PixelStorei = GetProcAddress<PixelStoreiFn>($"gl{nameof(PixelStorei)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetIntegervFn(GL name, out int data);
		public readonly GetIntegervFn GetIntegerv = GetProcAddress<GetIntegervFn>($"gl{nameof(GetIntegerv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GenTexturesFn(int n, IntPtr textures);
		public readonly GenTexturesFn GenTextures = GetProcAddress<GenTexturesFn>($"gl{nameof(GenTextures)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GenRenderbuffersFn(int n, IntPtr textures);
		public readonly GenRenderbuffersFn GenRenderbuffers = GetProcAddress<GenRenderbuffersFn>($"gl{nameof(GenRenderbuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GenFramebuffersFn(int n, IntPtr textures);
		public readonly GenFramebuffersFn GenFramebuffers = GetProcAddress<GenFramebuffersFn>($"gl{nameof(GenFramebuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ActiveTextureFn(GL id);
		public readonly ActiveTextureFn ActiveTexture = GetProcAddress<ActiveTextureFn>($"gl{nameof(ActiveTexture)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BindTextureFn(GL target, uint id);
		public readonly BindTextureFn BindTexture = GetProcAddress<BindTextureFn>($"gl{nameof(BindTexture)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BindRenderbufferFn(GL target, uint id);
		public readonly BindRenderbufferFn BindRenderbuffer = GetProcAddress<BindRenderbufferFn>($"gl{nameof(BindRenderbuffer)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BindFramebufferFn(GL target, uint id);
		public readonly BindFramebufferFn BindFramebuffer = GetProcAddress<BindFramebufferFn>($"gl{nameof(BindFramebuffer)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TexImage2DFn(GL target, int level, GL internalFormat, int width, int height, int border, GL format, GL type, IntPtr data);
		public readonly TexImage2DFn TexImage2D = GetProcAddress<TexImage2DFn>($"gl{nameof(TexImage2D)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void FramebufferRenderbufferFn(GL target​, GL attachment​, GL renderbuffertarget​, uint renderbuffer​);
		public readonly FramebufferRenderbufferFn FramebufferRenderbuffer = GetProcAddress<FramebufferRenderbufferFn>($"gl{nameof(FramebufferRenderbuffer)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void FramebufferTexture2DFn(GL target, GL attachment, GL textarget, uint texture, int level);
		public readonly FramebufferTexture2DFn FramebufferTexture2D = GetProcAddress<FramebufferTexture2DFn>($"gl{nameof(FramebufferTexture2D)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void TexParameteriFn(GL target, GL name, int param);
		public readonly TexParameteriFn TexParameteri = GetProcAddress<TexParameteriFn>($"gl{nameof(TexParameteri)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void RenderbufferStorageFn(GL target​, GL internalformat​, int width​, int height​);
		public readonly RenderbufferStorageFn RenderbufferStorage = GetProcAddress<RenderbufferStorageFn>($"gl{nameof(RenderbufferStorage)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetTexImageFn(GL target, int level, GL format, GL type, IntPtr data);
		public readonly GetTexImageFn GetTexImage = GetProcAddress<GetTexImageFn>($"gl{nameof(GetTexImage)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DrawElementsFn(GL mode, int count, GL type, IntPtr indices);
		public readonly DrawElementsFn DrawElements = GetProcAddress<DrawElementsFn>($"gl{nameof(DrawElements)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DrawElementsInstancedFn(GL mode, int count, GL type, IntPtr indices, int amount);
		public readonly DrawElementsInstancedFn DrawElementsInstanced = GetProcAddress<DrawElementsInstancedFn>($"gl{nameof(DrawElementsInstanced)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DrawBuffersFn(IntPtr n, GL* bufs);
		public readonly DrawBuffersFn DrawBuffers = GetProcAddress<DrawBuffersFn>($"gl{nameof(DrawBuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteTexturesFn(int n, nint textures);
		public readonly DeleteTexturesFn DeleteTextures = GetProcAddress<DeleteTexturesFn>($"gl{nameof(DeleteTextures)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteRenderbuffersFn(int n, nint renderbuffers);
		public readonly DeleteRenderbuffersFn DeleteRenderbuffers = GetProcAddress<DeleteRenderbuffersFn>($"gl{nameof(DeleteRenderbuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteFramebuffersFn(int n, nint textures);
		public readonly DeleteFramebuffersFn DeleteFramebuffers = GetProcAddress<DeleteFramebuffersFn>($"gl{nameof(DeleteFramebuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GenVertexArraysFn(int n, nint arrays);
		public readonly GenVertexArraysFn GenVertexArrays = GetProcAddress<GenVertexArraysFn>($"gl{nameof(GenVertexArrays)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BindVertexArrayFn(uint id);
		public readonly BindVertexArrayFn BindVertexArray = GetProcAddress<BindVertexArrayFn>($"gl{nameof(BindVertexArray)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GenBuffersFn(int n, nint arrays);
		public readonly GenBuffersFn GenBuffers = GetProcAddress<GenBuffersFn>($"gl{nameof(GenBuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BindBufferFn(GL target, uint buffer);
		public readonly BindBufferFn BindBuffer = GetProcAddress<BindBufferFn>($"gl{nameof(BindBuffer)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BufferDataFn(GL target, IntPtr size, IntPtr data, GL usage);
		public readonly BufferDataFn BufferData = GetProcAddress<BufferDataFn>($"gl{nameof(BufferData)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void BufferSubDataFn(GL target, IntPtr offset, IntPtr size, IntPtr data);
		public readonly BufferSubDataFn BufferSubData = GetProcAddress<BufferSubDataFn>($"gl{nameof(BufferSubData)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteBuffersFn(int n, nint buffers);
		public readonly DeleteBuffersFn DeleteBuffers = GetProcAddress<DeleteBuffersFn>($"gl{nameof(DeleteBuffers)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteVertexArraysFn(int n, nint arrays);
		public readonly DeleteVertexArraysFn DeleteVertexArrays = GetProcAddress<DeleteVertexArraysFn>($"gl{nameof(DeleteVertexArrays)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void EnableVertexAttribArrayFn(uint location);
		public readonly EnableVertexAttribArrayFn EnableVertexAttribArray = GetProcAddress<EnableVertexAttribArrayFn>($"gl{nameof(EnableVertexAttribArray)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DisableVertexAttribArrayFn(uint location);
		public readonly DisableVertexAttribArrayFn DisableVertexAttribArray = GetProcAddress<DisableVertexAttribArrayFn>($"gl{nameof(DisableVertexAttribArray)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void VertexAttribPointerFn(uint index, int size, GL type, bool normalized, int stride, IntPtr pointer);
		public readonly VertexAttribPointerFn VertexAttribPointer = GetProcAddress<VertexAttribPointerFn>($"gl{nameof(VertexAttribPointer)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void VertexAttribDivisorFn(uint index, uint divisor);
		public readonly VertexAttribDivisorFn VertexAttribDivisor = GetProcAddress<VertexAttribDivisorFn>($"gl{nameof(VertexAttribDivisor)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate uint CreateShaderFn(GL type);
		public readonly CreateShaderFn CreateShader = GetProcAddress<CreateShaderFn>($"gl{nameof(CreateShader)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void AttachShaderFn(uint program, uint shader);
		public readonly AttachShaderFn AttachShader = GetProcAddress<AttachShaderFn>($"gl{nameof(AttachShader)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DetachShaderFn(uint program, uint shader);
		public readonly DetachShaderFn DetachShader = GetProcAddress<DetachShaderFn>($"gl{nameof(DetachShader)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteShaderFn(uint shader);
		public readonly DeleteShaderFn DeleteShader = GetProcAddress<DeleteShaderFn>($"gl{nameof(DeleteShader)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ShaderSourceFn(uint shader, int count, string[] source, int[] length);
		public readonly ShaderSourceFn ShaderSource = GetProcAddress<ShaderSourceFn>($"gl{nameof(ShaderSource)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void CompileShaderFn(uint shader);
		public readonly CompileShaderFn CompileShader = GetProcAddress<CompileShaderFn>($"gl{nameof(CompileShader)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetShaderivFn(uint shader, GL pname, out int result);
		public readonly GetShaderivFn GetShaderiv = GetProcAddress<GetShaderivFn>($"gl{nameof(GetShaderiv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetShaderInfoLogFn(uint shader, int maxLength, out int length, IntPtr infoLog);
		public readonly GetShaderInfoLogFn GetShaderInfoLog = GetProcAddress<GetShaderInfoLogFn>($"gl{nameof(GetShaderInfoLog)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate uint CreateProgramFn();
		public readonly CreateProgramFn CreateProgram = GetProcAddress<CreateProgramFn>($"gl{nameof(CreateProgram)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void DeleteProgramFn(uint program);
		public readonly DeleteProgramFn DeleteProgram = GetProcAddress<DeleteProgramFn>($"gl{nameof(DeleteProgram)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void LinkProgramFn(uint program);
		public readonly LinkProgramFn LinkProgram = GetProcAddress<LinkProgramFn>($"gl{nameof(LinkProgram)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetProgramivFn(uint program, GL pname, out int result);
		public readonly GetProgramivFn GetProgramiv = GetProcAddress<GetProgramivFn>($"gl{nameof(GetProgramiv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetProgramInfoLogFn(uint program, int maxLength, out int length, IntPtr infoLog);
		public readonly GetProgramInfoLogFn GetProgramInfoLog = GetProcAddress<GetProgramInfoLogFn>($"gl{nameof(GetProgramInfoLog)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetActiveUniformFn(uint program, uint index, int bufSize, out int length, out int size, out GL type, IntPtr name);
		public readonly GetActiveUniformFn GetActiveUniform = GetProcAddress<GetActiveUniformFn>($"gl{nameof(GetActiveUniform)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void GetActiveAttribFn(uint program, uint index, int bufSize, out int length, out int size, out GL type, IntPtr name);
		public readonly GetActiveAttribFn GetActiveAttrib = GetProcAddress<GetActiveAttribFn>($"gl{nameof(GetActiveAttrib)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UseProgramFn(uint program);
		public readonly UseProgramFn UseProgram = GetProcAddress<UseProgramFn>($"gl{nameof(UseProgram)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int GetUniformLocationFn(uint program, string name);
		public readonly GetUniformLocationFn GetUniformLocation = GetProcAddress<GetUniformLocationFn>($"gl{nameof(GetUniformLocation)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int GetAttribLocationFn(uint program, string name);
		public readonly GetAttribLocationFn GetAttribLocation = GetProcAddress<GetAttribLocationFn>($"gl{nameof(GetAttribLocation)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform1fFn(int location, float v0);
		public readonly Uniform1fFn Uniform1f = GetProcAddress<Uniform1fFn>($"gl{nameof(Uniform1f)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform2fFn(int location, float v0, float v1);
		public readonly Uniform2fFn Uniform2f = GetProcAddress<Uniform2fFn>($"gl{nameof(Uniform2f)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform3fFn(int location, float v0, float v1, float v2);
		public readonly Uniform3fFn Uniform3f = GetProcAddress<Uniform3fFn>($"gl{nameof(Uniform3f)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform4fFn(int location, float v0, float v1, float v2, float v3);
		public readonly Uniform4fFn Uniform4f = GetProcAddress<Uniform4fFn>($"gl{nameof(Uniform4f)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform1fvFn(int location, int count, IntPtr value);
		public readonly Uniform1fvFn Uniform1fv = GetProcAddress<Uniform1fvFn>($"gl{nameof(Uniform1fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform2fvFn(int location, int count, IntPtr value);
		public readonly Uniform2fvFn Uniform2fv = GetProcAddress<Uniform2fvFn>($"gl{nameof(Uniform2fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform3fvFn(int location, int count, IntPtr value);
		public readonly Uniform3fvFn Uniform3fv = GetProcAddress<Uniform3fvFn>($"gl{nameof(Uniform3fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform4fvFn(int location, int count, IntPtr value);
		public readonly Uniform4fvFn Uniform4fv = GetProcAddress<Uniform4fvFn>($"gl{nameof(Uniform4fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform1iFn(int location, int v0);
		public readonly Uniform1iFn Uniform1i = GetProcAddress<Uniform1iFn>($"gl{nameof(Uniform1i)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform2iFn(int location, int v0, int v1);
		public readonly Uniform2iFn Uniform2i = GetProcAddress<Uniform2iFn>($"gl{nameof(Uniform2i)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform3iFn(int location, int v0, int v1, int v2);
		public readonly Uniform3iFn Uniform3i = GetProcAddress<Uniform3iFn>($"gl{nameof(Uniform3i)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform4iFn(int location, int v0, int v1, int v2, int v3);
		public readonly Uniform4iFn Uniform4i = GetProcAddress<Uniform4iFn>($"gl{nameof(Uniform4i)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform1ivFn(int location, int count, IntPtr value);
		public readonly Uniform1ivFn Uniform1iv = GetProcAddress<Uniform1ivFn>($"gl{nameof(Uniform1iv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform2ivFn(int location, int count, IntPtr value);
		public readonly Uniform2ivFn Uniform2iv = GetProcAddress<Uniform2ivFn>($"gl{nameof(Uniform2iv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform3ivFn(int location, int count, IntPtr value);
		public readonly Uniform3ivFn Uniform3iv = GetProcAddress<Uniform3ivFn>($"gl{nameof(Uniform3iv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform4ivFn(int location, int count, IntPtr value);
		public readonly Uniform4ivFn Uniform4iv = GetProcAddress<Uniform4ivFn>($"gl{nameof(Uniform4iv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform1uiFn(int location, uint v0);
		public readonly Uniform1uiFn Uniform1ui = GetProcAddress<Uniform1uiFn>($"gl{nameof(Uniform1ui)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform2uiFn(int location, uint v0, uint v1);
		public readonly Uniform2uiFn Uniform2ui = GetProcAddress<Uniform2uiFn>($"gl{nameof(Uniform2ui)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform3uiFn(int location, uint v0, uint v1, uint v2);
		public readonly Uniform3uiFn Uniform3ui = GetProcAddress<Uniform3uiFn>($"gl{nameof(Uniform3ui)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform4uiFn(int location, uint v0, uint v1, uint v2, uint v3);
		public readonly Uniform4uiFn Uniform4ui = GetProcAddress<Uniform4uiFn>($"gl{nameof(Uniform4ui)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform1uivFn(int location, int count, IntPtr value);
		public readonly Uniform1uivFn Uniform1uiv = GetProcAddress<Uniform1uivFn>($"gl{nameof(Uniform1uiv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform2uivFn(int location, int count, IntPtr value);
		public readonly Uniform2uivFn Uniform2uiv = GetProcAddress<Uniform2uivFn>($"gl{nameof(Uniform2uiv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform3uivFn(int location, int count, IntPtr value);
		public readonly Uniform3uivFn Uniform3uiv = GetProcAddress<Uniform3uivFn>($"gl{nameof(Uniform3uiv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void Uniform4uivFn(int location, int count, IntPtr value);
		public readonly Uniform4uivFn Uniform4uiv = GetProcAddress<Uniform4uivFn>($"gl{nameof(Uniform4uiv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix2fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix2fvFn UniformMatrix2fv = GetProcAddress<UniformMatrix2fvFn>($"gl{nameof(UniformMatrix2fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix3fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix3fvFn UniformMatrix3fv = GetProcAddress<UniformMatrix3fvFn>($"gl{nameof(UniformMatrix3fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix4fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix4fvFn UniformMatrix4fv = GetProcAddress<UniformMatrix4fvFn>($"gl{nameof(UniformMatrix4fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix2x3fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix2x3fvFn UniformMatrix2x3fv = GetProcAddress<UniformMatrix2x3fvFn>($"gl{nameof(UniformMatrix2x3fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix3x2fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix3x2fvFn UniformMatrix3x2fv = GetProcAddress<UniformMatrix3x2fvFn>($"gl{nameof(UniformMatrix3x2fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix2x4fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix2x4fvFn UniformMatrix2x4fv = GetProcAddress<UniformMatrix2x4fvFn>($"gl{nameof(UniformMatrix2x4fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix4x2fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix4x2fvFn UniformMatrix4x2fv = GetProcAddress<UniformMatrix4x2fvFn>($"gl{nameof(UniformMatrix4x2fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix3x4fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix3x4fvFn UniformMatrix3x4fv = GetProcAddress<UniformMatrix3x4fvFn>($"gl{nameof(UniformMatrix3x4fv)}")!;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void UniformMatrix4x3fvFn(int location, int count, bool transpose, IntPtr value);
		public readonly UniformMatrix4x3fvFn UniformMatrix4x3fv = GetProcAddress<UniformMatrix4x3fvFn>($"gl{nameof(UniformMatrix4x3fv)}")!;
	}

	#endregion
}
