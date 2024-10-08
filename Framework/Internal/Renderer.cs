
namespace Foster.Framework;

internal abstract class Renderer
{
	public interface IHandle
	{
		public bool Disposed { get; }
	}

	public abstract nint Device { get; }
	public abstract GraphicsDriver Driver { get; }
	public abstract Version DriverVersion { get; }

	public abstract void CreateDevice();
	public abstract void DestroyDevice();

	public abstract void Startup(nint window);
	public abstract void Shutdown();

	public abstract bool GetVSync();
	public abstract void SetVSync(bool enabled);

	public abstract void Present();

	public abstract IHandle CreateTexture(int width, int height, TextureFormat format, IHandle? targetBinding);
	public abstract void SetTextureData(IHandle texture, nint data, int length);
	public abstract void GetTextureData(IHandle texture, nint data, int length);

	public abstract IHandle CreateTarget(int width, int height);

	public abstract IHandle CreateMesh();
	public abstract void SetMeshVertexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format);
	public abstract void SetMeshIndexData(IHandle mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format);

	public abstract IHandle CreateShader(in ShaderCreateInfo shaderInfo);

	public abstract void DestroyResource(IHandle resource);

	public abstract void Draw(DrawCommand command);
	public abstract void Clear(Target? target, Color color, float depth, int stencil, ClearMask mask);
}
