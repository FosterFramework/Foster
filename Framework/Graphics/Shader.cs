using System.Collections.Frozen;

namespace Foster.Framework;

/// <summary>
/// A combination of a Vertex and Fragment Shader programs used for Rendering.<br/>
/// <br/>
/// The Provided <see cref="ShaderProgramInfo.Code"/> must match the <see cref="GraphicsDriver"/>
/// in use, which can be checked with <see cref="Renderer.Driver"/>.<br/>
/// <br/>
/// Shaders must match SDL_GPU Shader resource binding rules:
/// https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks
/// </summary>
public class Shader : IGraphicResource
{
	/// <summary>
	/// Holds information about a Shader Program
	/// </summary>
	public class Program
	{
		public readonly record struct Uniform(
			UniformType Type,
			int ArrayElements,
			int OffsetInBytes,
			int SizeInBytes
		);

		public readonly int SamplerCount;
		public readonly int UniformSizeInBytes;
		public readonly FrozenDictionary<string, Uniform> Uniforms;

		internal Program(int samplerCount, ShaderUniform[] uniforms)
		{
			SamplerCount = samplerCount;

			var offset = 0;
			var dict = new Dictionary<string, Uniform>();

			// TODO: account for packing/offset/alignment

			foreach (var it in uniforms)
			{
				var uniform = new Uniform(it.Type, it.ArrayElements, offset, it.Type.SizeInBytes() * it.ArrayElements);
				dict.Add(it.Name, uniform);
				offset += uniform.SizeInBytes;
			}
			
			Uniforms = dict.ToFrozenDictionary();
			UniformSizeInBytes = offset;
		}
	}

	/// <summary>
	/// The Renderer this Shader was created in
	/// </summary>
	public readonly Renderer Renderer;

	/// <summary>
	/// Optional Shader Name
	/// </summary>
	public string Name { get; set; } = string.Empty;
	
	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => Resource.Disposed;

	/// <summary>
	/// Vertex Shader Program Reflection
	/// </summary>
	public readonly Program Vertex;

	/// <summary>
	/// Fragment Shader Program Reflection
	/// </summary>
	public readonly Program Fragment;

	internal readonly Renderer.IHandle Resource;

	public Shader(Renderer renderer, ShaderCreateInfo createInfo)
	{
		Renderer = renderer;

		// validate that uniforms are unique, or matching.
		// we treat vertex/fragment shaders as a combined singular shader, and thus
		// the uniforms between them must be unique (or at least matching in type)
		foreach (var uni0 in createInfo.Vertex.Uniforms)
			foreach (var uni1 in createInfo.Fragment.Uniforms)
			{
				if (uni0.Name == uni1.Name && (uni0.Type != uni1.Type || uni0.ArrayElements != uni1.ArrayElements))
					throw new Exception($"Uniform names must be unique between Vertex and Fragment shaders, or they must be matching types. (Uniform '{uni0.Name}' types aren't equal)");
			}

		Resource = Renderer.CreateShader(createInfo);
		Vertex = new(createInfo.Vertex.SamplerCount, createInfo.Vertex.Uniforms);
		Fragment = new(createInfo.Fragment.SamplerCount, createInfo.Fragment.Uniforms);
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
		Renderer.DestroyResource(Resource);
	}
}
