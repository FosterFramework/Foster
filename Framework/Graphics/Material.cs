namespace Foster.Framework;

/// <summary>
/// A Material holds state for a Shader to be used during rendering, including bound Texture Samplers and Uniform Buffer data.
/// </summary>
public class Material
{
	/// <summary>
	/// Stores state data for Shader Stages
	/// </summary>
	public class Stage
	{
		public const int MaxUniformBuffers = 8;

		/// <summary>
        /// The Shader used for this stage
        /// </summary>
		public Shader? Shader
		{
			get => shader;
			set
            {
                if (shader != null && shader.Stage != stage)
					throw new Exception("Invalid Shader Stage");
				shader = value;
            }
        }

		/// <summary>
		/// Texture Samplers bound to this Shader Stage
		/// </summary>
		public readonly BoundSampler[] Samplers = new BoundSampler[16];

		/// <summary>
		/// Uniform Buffer Data bound to this Shader Stage
		/// </summary>
		public readonly UniformBuffer[] UniformBuffers = [..Enumerable.Range(0, MaxUniformBuffers).Select(it => new UniformBuffer())];

		private Shader? shader = null;
		private readonly ShaderStage stage;

		internal Stage(ShaderStage stage)
			=> this.stage = stage;

		/// <summary>
		/// Sets the Data stored in a Uniform Buffer
		/// </summary>
		public void SetUniformBuffer<T>(in T data, int slot = 0) where T : unmanaged
			=> UniformBuffers[slot].Set(data);

		/// <summary>
		/// Sets the Data stored in a Uniform Buffer
		/// </summary>
		public void SetUniformBuffer(in ReadOnlySpan<byte> data, int slot = 0)
			=> UniformBuffers[slot].Set(data);

		/// <summary>
		/// Sets the Data stored in a Uniform Buffer
		/// </summary>
		public void SetUniformBuffer(in ReadOnlySpan<float> data, int slot = 0)
			=> UniformBuffers[slot].Set(data);

		/// <summary>
		/// Gets the data stored in a Uniform Buffer, converted to a given Type
		/// </summary>
		public T GetUniformBuffer<T>(int slot = 0) where T : unmanaged
			=> UniformBuffers[slot].Get<T>();

		/// <summary>
		/// Gets the data stored in a Uniform Buffer
		/// </summary>
		public ReadOnlySpan<byte> GetUniformBuffer(int slot = 0)
			=> UniformBuffers[slot].Get();

		/// <summary>
		/// Copies the Data from this Stage to another
		/// </summary>
		public void CopyTo(Stage to)
		{
			to.Shader = Shader;
			Array.Copy(Samplers, to.Samplers, Samplers.Length);
			for (int i = 0; i < MaxUniformBuffers; i ++)
				UniformBuffers[i].CopyTo(to.UniformBuffers[i]);
		}
	}

	/// <summary>
	/// Data for the Vertex Shader stage
	/// </summary>
	public readonly Stage Vertex = new(ShaderStage.Vertex);

	/// <summary>
	/// Data for the Fragment Shader stage
	/// </summary>
	public readonly Stage Fragment = new(ShaderStage.Fragment);

	public Material() {}
	
	public Material(Shader? vertexShader, Shader? fragmentShader)
	{
		Vertex.Shader = vertexShader;
		Fragment.Shader = fragmentShader;
	}

	public Material(Material from)
	{
		from.CopyTo(this);
	}

	/// <summary>
	/// Copies the State of this Material to another
	/// </summary>
	public void CopyTo(Material to)
	{
		Vertex.CopyTo(to.Vertex);
		Fragment.CopyTo(to.Fragment);
	}

	/// <summary>
	/// Copies the State of another Material onto this Material
	/// </summary>
	public void CopyFrom(Material from)
		=> from.CopyTo(this);

	/// <summary>
    /// Creates a Copy of this Material
    /// </summary>
	public Material Clone()
	{
		var clone = new Material();
		CopyTo(clone);
		return clone;	
	}
}
