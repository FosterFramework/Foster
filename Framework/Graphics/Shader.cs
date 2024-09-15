
namespace Foster.Framework;

/// <summary>
/// Holds information on an individual Shader Uniform
/// </summary>
public readonly record struct ShaderUniform(
	string Name,
	UniformType Type,
	int ArrayElements = 1
);

/// <summary>
/// Reflection Data used to create a new Shader Program
/// </summary>
public readonly record struct ShaderProgramInfo(
	byte[] Code,
	int SamplerCount,
	ShaderUniform[] Uniforms
);

/// <summary>
/// Data Required to create a new Shader
/// </summary>
public readonly record struct ShaderCreateInfo(
	ShaderProgramInfo Vertex, 
	ShaderProgramInfo Fragment
);

/// <summary>
/// A combination of a Vertex and Fragment Shader programs used for Rendering
/// </summary>
public class Shader : IResource
{
	/// <summary>
	/// Holds information about a Shader Program
	/// </summary>
	public class Program(int samplerCount, ShaderUniform[] uniforms)
	{
		public readonly int SamplerCount = samplerCount;
		public readonly ShaderUniform[] Uniforms = uniforms;
		public readonly int UniformSizeInBytes = uniforms.Sum(it => it.Type.SizeInBytes() * it.ArrayElements);
	}

	/// <summary>
	/// Optional Shader Name
	/// </summary>
	public string Name { get; set; } = string.Empty;
	
	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => disposed;

	/// <summary>
	/// Vertex Shader Program Reflection
	/// </summary>
	public readonly Program Vertex;

	/// <summary>
	/// Fragment Shader Program Reflection
	/// </summary>
	public readonly Program Fragment;

	internal readonly IntPtr resource;
	internal bool disposed = false;

	public Shader(ShaderCreateInfo createInfo)
	{
		// validate that uniforms are unique, or matching.
		// we treat vertex/fragment shaders as a combined singular shader, and thus
		// the uniforms between them must be unique (or at least matching in type)
		foreach (var uni0 in createInfo.Vertex.Uniforms)
			foreach (var uni1 in createInfo.Fragment.Uniforms)
			{
				if (uni0.Name == uni1.Name && (uni0.Type != uni1.Type || uni0.ArrayElements != uni1.ArrayElements))
					throw new Exception($"Uniform names must be unique between Vertex and Fragment shaders, or they must be matching types. (Uniform '{uni0.Name}' types aren't equal)");
			}

		resource = Renderer.CreateShader(createInfo);
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
		if (!disposed)
		{
			disposed = true;
			Renderer.DestroyShader(resource);
		}
	}
}
