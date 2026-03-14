namespace Foster.Framework;

/// <summary>
/// Stores information required to submit a compute draw command.
/// Call <see cref="Submit"/> or <see cref="GraphicsDevice.Dispatch"/> to submit.
/// </summary>
public struct ComputeCommand
{
	/// <summary>
	/// Shader to perform
	/// </summary>
	public Shader? Shader;

	/// <summary>
	/// Texture Samplers
	/// </summary>
	public StackList16<BoundSampler> Samplers;

	/// <summary>
	/// Uniform Buffer Data
	/// </summary>
	public StackList8<UniformBuffer?> UniformBuffers;

	/// <summary>
	/// Read-only Storage Textures bound to the compute shader.<br/>
	/// The Textures used must be created with <see cref="TextureFlags.ComputeRead"/>
	/// </summary>
	public StackList4<Texture?> ReadOnlyStorageTextures;

	/// <summary>
	/// Read-write Storage Textures bound to the compute shader
	/// The Textures used must be created with <see cref="TextureFlags.ComputeRead"/> and <see cref="TextureFlags.ComputeWrite"/>
	/// </summary>
	public StackList4<Texture?> ReadWriteStorageTextures;

	/// <summary>
	/// Read-only Storage Buffers bound to the compute shader
	/// </summary>
	public StackList4<StorageBuffer?> ReadOnlyStorageBuffers;

	/// <summary>
	/// Read-write Storage Buffers bound to the compute shader
	/// </summary>
	public StackList4<ComputeStorageBuffer?> ReadWriteStorageBuffers;

	/// <summary>
	/// The number of workgroups to dispatch in X
	/// </summary>
	public int GroupCountX = 1;

	/// <summary>
	/// The number of workgroups to dispatch in Y
	/// </summary>
	public int GroupCountY = 1;

	/// <summary>
	/// The number of workgroups to dispatch in Z
	/// </summary>
	public int GroupCountZ = 1;

	/// <summary>
	/// Creates a Compute Command with the given shader
	/// </summary>
	public ComputeCommand(Shader shader) : this()
	{
		Shader = shader;
	}

	public readonly void Submit(GraphicsDevice graphicsDevice)
		=> graphicsDevice.Dispatch(this);
}
