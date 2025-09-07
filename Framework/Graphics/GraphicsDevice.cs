namespace Foster.Framework;

/// <summary>
/// The GPU Rendering Module which can subbmit <see cref="DrawCommand"/>'s through the <see cref="Draw"/> method.
/// </summary>
public abstract class GraphicsDevice
{
	/// <summary>
	/// A graphical resource handle
	/// </summary>
	internal interface IHandle
	{
		public bool Disposed { get; }
	}

	/// <summary>
	/// The backing Graphics Driver in use
	/// </summary>
	public abstract GraphicsDriver Driver { get; }

	/// <summary>
	/// The Application this GraphicsDevice belongs to
	/// </summary>
	public readonly App App;

	/// <summary>
	/// If the underlying Graphics Driver considers 0,0 to be the bottom left.
	/// </summary>
	public abstract bool OriginBottomLeft { get; }

	/// <summary>
	/// If the GraphicsDevice has been disposed
	/// </summary>
	public abstract bool Disposed { get; }

	/// <summary>
	/// If V-Sync is enabled
	/// </summary>
	public abstract bool VSync { get; set; }

	internal GraphicsDevice(App app) => App = app;
	internal abstract void CreateDevice(in AppFlags flags);
	internal abstract void DestroyDevice();
	internal abstract void Startup(nint window);
	internal abstract void Shutdown();
	internal abstract void Present();
	internal abstract IHandle CreateTexture(string? name, int width, int height, TextureFormat format, SampleCount sampleCount, IHandle? targetBinding);
	internal abstract void SetTextureData(IHandle texture, nint data, int length);
	internal abstract void GetTextureData(IHandle texture, nint data, int length);
	internal abstract IHandle CreateTarget(int width, int height);
	internal abstract IHandle CreateShader(string? name, in ShaderCreateInfo shaderInfo);
	internal abstract IHandle CreateIndexBuffer(string? name, IndexFormat format);
	internal abstract IHandle CreateVertexBuffer(string? name);
	internal abstract void UploadBufferData(IHandle buffer, nint data, int dataSize, int dataDestOffset);
	internal abstract void DestroyResource(IHandle resource);
	internal abstract void PerformDraw(DrawCommand command);
	internal abstract void Clear(IDrawableTarget target, ReadOnlySpan<Color> color, float depth, int stencil, ClearMask mask);

	/// <summary>
	/// Checks if a given Texture Format is supported
	/// </summary>
	public abstract bool IsTextureFormatSupported(TextureFormat format);

	/// <summary>
	/// Checks if a given Texture Format and Sample Count combination is supproted
	/// </summary>
	public abstract bool IsTextureMultiSampleSupported(TextureFormat format, SampleCount sampleCount);

	/// <summary>
	/// Performs a draw command
	/// </summary>
	public void Draw(DrawCommand command)
	{
		// some of the following checks are exceptions, and others are warnings, depending on the
		// context. Opting to throw exceptions for more obvious user-errors, where as invalid state
		// that is more dynamic is just a warning.

		var mat = command.Material ?? throw new Exception("Attempting to render with a null Material");
		var shader = mat.Shader;
		var target = command.Target;

		// invalid shader state
		if (shader == null || shader.IsDisposed)
			throw new Exception("Attempting to render a null or disposed Shader");

		// invalid target state
		if (target == null || (target is Target t && t.IsDisposed))
			throw new Exception("Attempting to render a null or disposed Target");

		// no vertex buffer
		if (command.VertexBuffers.Count <= 0)
			throw new Exception("Attempting to render without a Vertex Buffer");

		// invalid index buffer state
		if (command.IndexBuffer != null && command.IndexBuffer.IsDisposed)
			throw new Exception("Attempting to render with a disposed Index Buffer");

		// using vertex count with an index buffer
		if (command.IndexBuffer != null && command.VertexCount != 0)
			throw new Exception("Attempting to render using a Vertex Count with an Index Buffer. Use IndexCount instead.");

		// using index offset without a vertex buffer
		if (command.IndexBuffer == null && command.IndexOffset != 0)
			throw new Exception("Attempting to render using an Index Offset without an Index Buffer.");

		// using index count without a vertex buffer
		if (command.IndexBuffer == null && command.IndexCount != 0)
			throw new Exception("Attempting to render using an Index Count without an Index Buffer.");

		// validate vertex buffers
		for (int i = 0; i < command.VertexBuffers.Count; i ++)
		{
			var it = command.VertexBuffers[i].Buffer;

			if (it == null || it.IsDisposed)
				throw new Exception("Attempting to render a null or disposed Vertex Buffer");

			if (it.Resource == null || it.Count <= 0)
			{
				Log.Warning("Attempting to render an empty Vertex Buffer; Nothing will be drawn");
				return;
			}
		}

		// using an index buffer that is empty
		if (command.IndexBuffer != null && command.IndexBuffer.Count <= 0)
		{
			Log.Warning("Attempting to render an empty Index Buffer; Nothing will be drawn");
			return;
		}

		// using an index buffer without an index count
		if (command.IndexBuffer != null && command.IndexCount <= 0)
		{
			Log.Warning("Attempting to render from an Index Buffer while using an IndexCount of 0; Nothing will be drawn");
			return;
		}

		// not using an index buffer and no vertex count
		if (command.IndexBuffer == null && command.VertexCount <= 0)
		{
			Log.Warning("Attempting to render with a VertexCount of 0; Nothing will be drawn");
			return;
		}

		// invalid viewport
		if (command.Viewport is {} viewport && (viewport.Width <= 0 || viewport.Height <= 0))
		{
			Log.Warning("Attempting to render with an empty Viewport; Nothing will be drawn");
			return;
		}

		// invalid scissor
		if (command.Scissor is {} scissor && (scissor.Width <= 0 || scissor.Height <= 0))
		{
			Log.Warning("Attempting to render with an empty Scissor; Nothing will be drawn");
			return;
		}

		PerformDraw(command);
	}
}
