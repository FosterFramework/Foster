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
	/// Performs a draw command
	/// </summary>
	public void Draw(DrawCommand command)
	{
		var mat = command.Material ?? throw new Exception("Material is Invalid");
		var shader = mat.Shader;
		var target = command.Target;
		var mesh = command.Mesh;

		if (shader == null || shader.IsDisposed)
			throw new Exception("Material Shader is Invalid");

		if (target == null)
			throw new Exception("Target is Invalid");

		if (mesh == null || mesh.Resource == null || mesh.IsDisposed)
			throw new Exception("Mesh is Invalid");

		PerformDraw(command);
	}
}
