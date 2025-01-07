namespace Foster.Framework;

/// <summary>
/// The Rendering Module
/// </summary>
public abstract class Renderer
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
	/// The Application this Renderer belongs to
	/// </summary>
	public readonly App App;

	/// <summary>
	/// If the underlying Graphics Driver considers 0,0 to be the bottom left.
	/// This is only ever true for <see cref="GraphicsDriver.OpenGL"/>.
	/// </summary>
	public abstract bool OriginBottomLeft { get; }

	/// <summary>
	/// If the Renderer has been disposed
	/// </summary>
	public abstract bool Disposed { get; }

	/// <summary>
	/// If V-Sync is enabled
	/// </summary>
	public abstract bool VSync { get; set; }

	internal Renderer(App app) => App = app;
	internal abstract void CreateDevice();
	internal abstract void DestroyDevice();
	internal abstract void Startup(nint window);
	internal abstract void Shutdown();
	internal abstract void Present();
	internal abstract IHandle CreateTexture(int width, int height, TextureFormat format, IHandle? targetBinding);
	internal abstract void SetTextureData(IHandle texture, nint data, int length);
	internal abstract void GetTextureData(IHandle texture, nint data, int length);
	internal abstract IHandle CreateTarget(int width, int height);
	internal abstract IHandle CreateMesh();
	internal abstract void SetMeshVertexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format);
	internal abstract void SetMeshIndexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format);
	internal abstract IHandle CreateShader(in ShaderCreateInfo shaderInfo);
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
		var mesh = command.Mesh;

		if (shader == null || shader.IsDisposed)
			throw new Exception("Attempting to render a null or disposed Shader");

		if (target == null || (target is Target t && t.IsDisposed))
			throw new Exception("Attempting to render a null or disposed Target");

		if (mesh == null || mesh.IsDisposed)
			throw new Exception("Attempting to render a null or disposed Mesh");

		if (mesh.Resource == null || mesh.VertexCount <= 0 || mesh.IndexCount <= 0)
		{
			Log.Warning("Attempting to render an empty Mesh");
			return;
		}

		if (command.MeshIndexCount <= 0)
		{
			Log.Warning("Attempting to render 0 indices");
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
