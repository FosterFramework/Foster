using System.Collections.Frozen;

namespace Foster.Framework;

/// <summary>
/// A combination of a Vertex and Fragment Shader programs used for Rendering.<br/>
/// <br/>
/// The Provided <see cref="ShaderStageInfo.Code"/> must match the <see cref="GraphicsDriver"/>
/// in use, which can be checked with <see cref="GraphicsDevice.Driver"/>.<br/>
/// <br/>
/// Shaders must match SDL_GPU Shader resource binding rules:
/// https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks
/// </summary>
public class Shader : IGraphicResource
{
	/// <summary>
	/// The GraphicsDevice this Shader was created in
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// Optional Shader Name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => Resource.Disposed;

	/// <summary>
	/// The Data the Shader was created with
	/// </summary>
	public readonly ShaderCreateInfo CreateInfo;

	internal readonly GraphicsDevice.IHandle Resource;

	public Shader(GraphicsDevice graphicsDevice, ShaderCreateInfo createInfo, string? name = null)
	{
		GraphicsDevice = graphicsDevice;
		CreateInfo = createInfo;
		Name = name ?? string.Empty;
		Resource = GraphicsDevice.CreateShader(name, createInfo);
	}

	~Shader()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		GraphicsDevice.DestroyResource(Resource);
	}
}
