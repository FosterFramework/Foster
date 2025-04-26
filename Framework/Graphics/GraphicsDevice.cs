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
	internal abstract IHandle CreateTexture(string? name, int width, int height, TextureFormat format, IHandle? targetBinding);
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
	/// Performs a draw command
	/// </summary>
	public void Draw(DrawCommand command)
	{
		var mat = command.Material ?? throw new Exception("Attempting to render with a null Material");
		var shader = mat.Shader;
		var target = command.Target;

		if (shader == null || shader.IsDisposed)
			throw new Exception("Attempting to render a null or disposed Shader");

		if (target == null || (target is Target t && t.IsDisposed))
			throw new Exception("Attempting to render a null or disposed Target");

		if (command.VertexBuffers.Count <= 0)
			throw new Exception("Attempting to render without a Vertex Buffer");

		if (command.IndexBuffer != null && command.VertexCount > 0)
			throw new Exception("Attempting to render using a Vertex Count with an Index Buffer. Use IndexCount instead.");

		for (int i = 0; i < command.VertexBuffers.Count; i ++)
		{
			var it = command.VertexBuffers[i].Buffer;

			if (it == null || it.IsDisposed)
				throw new Exception("Attempting to render a null or disposed Vertex Buffer");

			if (it.Resource == null || it.Count <= 0)
			{
				Log.Warning("Attempting to render an empty Vertex Buffer");
				return;
			}
		}

		if (command.IndexBuffer != null && command.IndexBuffer.Count <= 0)
		{
			Log.Warning("Attempting to render an empty Index Buffer");
			return;
		}

		if (command.IndexBuffer != null && command.IndexCount <= 0)
		{
			Log.Warning("Attempting to render 0 indices from an Index Buffer");
			return;
		}

		if (command.IndexBuffer == null && command.VertexCount <= 0)
		{
			Log.Warning("Attempting to render without any vertices");
			return;
		}

		if (command.Viewport is {} viewport && (viewport.Width <= 0 || viewport.Height <= 0))
		{
			Log.Warning("Attempting to render with an empty Viewport");
			return;
		}

		if (command.Scissor is {} scissor && (scissor.Width <= 0 || scissor.Height <= 0))
		{
			Log.Warning("Attempting to render with an empty Scissor");
			return;
		}

		PerformDraw(command);
	}
}
