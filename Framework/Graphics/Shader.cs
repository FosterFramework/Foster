namespace Foster.Framework;

/// <summary>
/// A Shader program used for Rendering.<br/>
/// <br/>
/// The Provided <see cref="ShaderCreateInfo.Code"/> must match the <see cref="GraphicsDriver"/>
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
    /// The Stage this shader was built for
    /// </summary>
	public readonly ShaderStage Stage;

	/// <summary>
	/// Optional Shader Name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => disposed || GraphicsDevice.Disposed;

	/// <summary>
	/// The Data the Shader was created with
	/// </summary>
	public ShaderCreateInfo CreateInfo { get; private set; }

	/// <summary>
    /// shader resource
    /// </summary>
	internal GraphicsDevice.ResourceHandle Resource;

	private bool disposed;

	public Shader(GraphicsDevice graphicsDevice, in ShaderCreateInfo createInfo, string? name = null)
	{
		GraphicsDevice = graphicsDevice;
		CreateInfo = createInfo;
		Name = name ?? string.Empty;
		Stage = createInfo.Stage;
		Resource = GraphicsDevice.CreateShader(name, createInfo);
	}

	/// <summary>
    /// Recreates the shader with the new provided shader info
    /// </summary>
	public void Recreate(in ShaderCreateInfo createInfo)
	{
		if (createInfo.Stage != Stage)
			throw new Exception("Cannot recreate the Shader with a different stage");
		if (IsDisposed)
			throw new Exception("Cannout recreate a disposed Shader");

		GraphicsDevice.DestroyResource(Resource);
		CreateInfo = createInfo;
		Resource = GraphicsDevice.CreateShader(Name, createInfo);
	}

	~Shader()
		=> Dispose();

	public void Dispose()
	{
		if (!disposed)
		{
			GraphicsDevice.DestroyResource(Resource);
			disposed = true;
		}

		GC.SuppressFinalize(this);
	}
}
