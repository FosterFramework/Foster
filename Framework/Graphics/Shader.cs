namespace Foster.Framework;

/// <summary>
/// A Shader program used for Rendering.<br/>
/// <br/>
/// The Provided code must match the <see cref="GraphicsDriver"/>
/// in use, which can be checked with <see cref="GraphicsDevice.Driver"/>.<br/>
/// <br/>
/// Shaders must match SDL_GPU resource binding rules:<br/>
///  - For Vertex/Fragment shaders, https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks <br/>
///  - For Compute shaders, https://wiki.libsdl.org/SDL3/SDL_CreateGPUComputePipeline#remarks
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
	/// The number of Samplers
	/// </summary>
	public readonly int SamplerCount;

	/// <summary>
	/// The number of Uniform Buffers
	/// </summary>
	public readonly int UniformBufferCount;

	/// <summary>
	/// The number of Storage Buffers (Vertex/Fragment only)
	/// </summary>
	public readonly int StorageBufferCount;

	/// <summary>
	/// The number of read-only storage textures (Compute only)
	/// </summary>
	public readonly int ReadOnlyStorageTextureCount;

	/// <summary>
	/// The number of read-only storage buffers (Compute only)
	/// </summary>
	public readonly int ReadOnlyStorageBufferCount;

	/// <summary>
	/// The number of read-write storage textures (Compute only)
	/// </summary>
	public readonly int ReadWriteStorageTextureCount;

	/// <summary>
	/// The number of read-write storage buffers (Compute only)
	/// </summary>
	public readonly int ReadWriteStorageBufferCount;

	/// <summary>
	/// The number of threads in the X dimension of the compute workgroup (Compute only)
	/// </summary>
	public readonly int ThreadCountX;

	/// <summary>
	/// The number of threads in the Y dimension of the compute workgroup (Compute only)
	/// </summary>
	public readonly int ThreadCountY;

	/// <summary>
	/// The number of threads in the Z dimension of the compute workgroup (Compute only)
	/// </summary>
	public readonly int ThreadCountZ;

	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => disposed || GraphicsDevice.Disposed;

	/// <summary>
	/// shader resource
	/// </summary>
	internal GraphicsDevice.ResourceHandle Resource;

	private bool disposed;

	/// <summary>
	/// Creates a new Shader program.
	/// </summary>
	/// <param name="graphicsDevice">The GraphicsDevice to create the Shader in</param>
	/// <param name="stage">The Shader Stage</param>
	/// <param name="code">The compiled shader bytecode, matching the current <see cref="GraphicsDevice.Driver"/></param>
	/// <param name="samplerCount">The number of texture samplers used by the shader</param>
	/// <param name="uniformBufferCount">The number of uniform buffers used by the shader</param>
	/// <param name="storageBufferCount">The number of storage buffers (Vertex/Fragment only)</param>
	/// <param name="readOnlyStorageTextureCount">The number of read-only storage textures (Compute only)</param>
	/// <param name="readOnlyStorageBufferCount">The number of read-only storage buffers (Compute only)</param>
	/// <param name="readWriteStorageTextureCount">The number of read-write storage textures (Compute only)</param>
	/// <param name="readWriteStorageBufferCount">The number of read-write storage buffers (Compute only)</param>
	/// <param name="threadCountX">Compute workgroup thread count in X (Compute only)</param>
	/// <param name="threadCountY">Compute workgroup thread count in Y (Compute only)</param>
	/// <param name="threadCountZ">Compute workgroup thread count in Z (Compute only)</param>
	/// <param name="entryPoint">The shader's entry point function name</param>
	/// <param name="name">Optional name for debugging</param>
	public Shader(
		GraphicsDevice graphicsDevice,
		ShaderStage stage,
		byte[] code,
		int samplerCount = 0,
		int uniformBufferCount = 0,
		int storageBufferCount = 0,
		int readOnlyStorageTextureCount = 0,
		int readOnlyStorageBufferCount = 0,
		int readWriteStorageTextureCount = 0,
		int readWriteStorageBufferCount = 0,
		int threadCountX = 1,
		int threadCountY = 1,
		int threadCountZ = 1,
		string entryPoint = "main",
		string? name = null)
	{
		GraphicsDevice = graphicsDevice;
		Stage = stage;
		SamplerCount = samplerCount;
		UniformBufferCount = uniformBufferCount;
		StorageBufferCount = storageBufferCount;
		ReadOnlyStorageTextureCount = readOnlyStorageTextureCount;
		ReadOnlyStorageBufferCount = readOnlyStorageBufferCount;
		ReadWriteStorageTextureCount = readWriteStorageTextureCount;
		ReadWriteStorageBufferCount = readWriteStorageBufferCount;
		ThreadCountX = threadCountX;
		ThreadCountY = threadCountY;
		ThreadCountZ = threadCountZ;
		Name = name ?? string.Empty;
		Resource = GraphicsDevice.CreateShader(this, code, entryPoint);
	}

	[Obsolete("Use Constructor with Parameters instead")]
	public Shader(GraphicsDevice graphicsDevice, in ShaderCreateInfo info)
		: this(graphicsDevice, stage: info.Stage, code: info.Code, samplerCount: info.SamplerCount, uniformBufferCount: info.UniformBufferCount, storageBufferCount: info.StorageBufferCount, entryPoint: info.EntryPoint) {}

	/// <summary>
	/// Recreates the shader with new code
	/// </summary>
	public void Recreate(byte[] code, string entryPoint = "main")
	{
		if (IsDisposed)
			throw new Exception("Cannot recreate a disposed Shader");

		GraphicsDevice.DestroyResource(Resource);
		Resource = GraphicsDevice.CreateShader(this, code, entryPoint);
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
