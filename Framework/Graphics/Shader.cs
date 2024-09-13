
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
public class ShaderProgramInfo(byte[] code, int samplerCount, params ShaderUniform[] uniforms)
{
	public readonly byte[] Code = code;
	public readonly int SamplerCount = samplerCount;
	public readonly ShaderUniform[] Uniforms = uniforms;
}

/// <summary>
/// Data Required to create a new Shader
/// </summary>
public readonly record struct ShaderCreateInfo(
	ShaderProgramInfo VertexProgram, 
	ShaderProgramInfo FragmentProgram
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
		public int SamplerCount = samplerCount;
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
		resource = Renderer.CreateShader(createInfo);
		Vertex = new(createInfo.VertexProgram.SamplerCount, createInfo.VertexProgram.Uniforms);
		Fragment = new(createInfo.FragmentProgram.SamplerCount, createInfo.FragmentProgram.Uniforms);
	}

	~Shader()
	{
		Dispose(false);
	}
		
	/// <summary>
	/// Disposes of the Shader
	/// </summary>
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
