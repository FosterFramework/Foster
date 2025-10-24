using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A Material holds state for a Shader to be used during rendering, including bound Texture Samplers and Uniform Buffer data.<br/>
/// <br/>
/// This way, you can have a single Shader in memory but many different states.
/// </summary>
public class Material
{
	/// <summary>
	/// Combination of Texture and Sampler bound to a Slot in the Material
	/// </summary>
	public readonly record struct BoundSampler(Texture? Texture, TextureSampler Sampler);

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
		internal readonly byte[][] UniformBuffers = [..Enumerable.Range(0, MaxUniformBuffers).Select(it => Array.Empty<byte>())];

		private Shader? shader = null;
		private readonly ShaderStage stage;

		internal Stage(ShaderStage stage)
			=> this.stage = stage;

		/// <summary>
		/// Sets the Data stored in a Uniform Buffer
		/// </summary>
		public unsafe void SetUniformBuffer<T>(in T data, int slot = 0) where T : unmanaged
		{
			fixed (T* ptr = &data)
				SetUniformBuffer(new ReadOnlySpan<byte>((byte*)ptr, Marshal.SizeOf<T>()), slot);
		}

		/// <summary>
		/// Sets the Data stored in a Uniform Buffer
		/// </summary>
		public void SetUniformBuffer(in ReadOnlySpan<byte> data, int slot = 0)
		{
			if (data.Length > UniformBuffers[slot].Length)
				Array.Resize(ref UniformBuffers[slot], data.Length);
			data.CopyTo(UniformBuffers[slot]);
		}

		/// <summary>
		/// Sets the Data stored in a Uniform Buffer
		/// </summary>
		public unsafe void SetUniformBuffer(in ReadOnlySpan<float> data, int slot = 0)
		{
			fixed (void* ptr = data)
				SetUniformBuffer(new ReadOnlySpan<byte>(ptr, data.Length * sizeof(float)), slot);
		}

		/// <summary>
		/// Gets the data stored in a Uniform Buffer, converted to a given Type
		/// </summary>
		public unsafe T GetUniformBuffer<T>(int slot = 0) where T : unmanaged
		{
			var data = GetUniformBuffer(slot);
			if (data.Length < Marshal.SizeOf<T>())
				return new();
			fixed (byte* ptr = data)
				return *(T*)ptr;
		}

		/// <summary>
		/// Gets the data stored in a Uniform Buffer
		/// </summary>
		public ReadOnlySpan<byte> GetUniformBuffer(int slot = 0)
			=> new(UniformBuffers[slot]);

		/// <summary>
		/// Copies the Data from this Stage to another
		/// </summary>
		public void CopyTo(Stage to)
		{
			to.Shader = Shader;
			Array.Copy(Samplers, to.Samplers, Samplers.Length);
			for (int i = 0; i < MaxUniformBuffers; i ++)
			{
				if (to.UniformBuffers[i].Length < UniformBuffers[i].Length)
					Array.Resize(ref to.UniformBuffers[i], UniformBuffers[i].Length);
				Array.Copy(UniformBuffers[i], to.UniformBuffers[i], UniformBuffers[i].Length);
			}
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

	/// <summary>
	/// Copies the State of this Material to another
	/// </summary>
	public void CopyTo(Material to)
	{
		Vertex.CopyTo(to.Vertex);
		Fragment.CopyTo(to.Fragment);
	}

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
