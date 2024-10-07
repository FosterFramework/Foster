
namespace Foster.Framework;

internal abstract class Renderer
{
	public abstract nint Device { get; }
	public abstract GraphicsDriver Driver { get; }

	public abstract bool CreateDevice();
	public abstract void DestroyDevice();

	public abstract void Startup(nint window);
	public abstract void Shutdown();

	public abstract bool GetVSync();
	public abstract void SetVSync(bool enabled);

	public abstract void Present();

	public abstract nint CreateTexture(int width, int height, TextureFormat format, bool isTarget);
	public abstract void SetTextureData(nint texture, nint data, int length);
	public abstract void GetTextureData(nint texture, nint data, int length);
	public abstract void DestroyTexture(nint texture);

	public abstract nint CreateMesh();
	public abstract void SetMeshVertexData(nint mesh, nint data, int dataSize, int dataDestOffset, in VertexFormat format);
	public abstract void SetMeshIndexData(nint mesh, nint data, int dataSize, int dataDestOffset, IndexFormat format);
	public abstract void DestroyMesh(nint mesh);

	public abstract nint CreateShader(in ShaderCreateInfo shaderInfo);
	public abstract void DestroyShader(nint shader);

	public abstract void Draw(DrawCommand command);
	public abstract void Clear(Target? target, Color color, float depth, int stencil, ClearMask mask);
}
