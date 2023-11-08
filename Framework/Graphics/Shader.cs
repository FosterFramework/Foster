using System.Collections.ObjectModel;

namespace Foster.Framework;

public struct ShaderCreateInfo
{
	public readonly record struct Attribute(string SemanticName, int SemanticIndex);

	/// <summary>
	/// Vertex Shader Code
	/// </summary>
	public string VertexShader;

	/// <summary>
	/// Fragment Shader Code
	/// </summary>
	public string FragmentShader;

	/// <summary>
	/// Attributes, required if using HLSL / D3D11
	/// </summary>
	public Attribute[]? Attributes;

	public ShaderCreateInfo(string vertexShader, string fragmentShader, Attribute[]? attributes = null)
	{
		VertexShader = vertexShader;
		FragmentShader = fragmentShader;
		Attributes = attributes;
	}
}

public class Shader : IResource
{
	/// <summary>
	/// Shader Uniform Entry
	/// </summary>
	public readonly record struct Uniform(
		int Index,
		string Name,
		UniformType Type, 
		int ArrayElements
	);

	/// <summary>
	/// Optional Shader Name
	/// </summary>
	public string Name { get; set; } = string.Empty;
	
	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => disposed;

	/// <summary>
	/// Dictionary of Uniforms in the Shader
	/// </summary>
	public readonly ReadOnlyDictionary<string, Uniform> Uniforms;

	internal readonly IntPtr resource;
	internal bool disposed = false;

	public Shader(in ShaderCreateInfo createInfo)
	{
		Platform.FosterShaderData data = new()
		{
			fragment = createInfo.FragmentShader,
			vertex = createInfo.VertexShader
		};

		resource = Platform.FosterShaderCreate(ref data);
		if (resource == IntPtr.Zero)
			throw new Exception("Failed to create Shader");

		var infos = new Platform.FosterUniformInfo[64];
		var count = 0;

		unsafe
		{
			// try to get all the uniforms
			fixed (Platform.FosterUniformInfo* it = infos)
				Platform.FosterShaderGetUniforms(resource, it, out count, infos.Length);
				
			// expand our buffer if there wasn't enough space
			if (infos.Length < count)
			{
				Array.Resize(ref infos, count); 
				fixed (Platform.FosterUniformInfo* it = infos)
					Platform.FosterShaderGetUniforms(resource, it, out count, count);
			}
		}

		// add each uniform
		var uniforms = new Dictionary<string, Uniform>();
		for (int i = 0; i < count; i ++)
		{
			var info = infos[i];
			var name = Platform.ParseUTF8(info.name);
			uniforms.Add(name, new (info.index, name, info.type, info.arrayElements));
		}

		Uniforms = uniforms.AsReadOnly();
		Graphics.Resources.RegisterAllocated(this, resource, Platform.FosterShaderDestroy);
	}

	~Shader()
	{
		Dispose(false);
	}

	/// <summary>
	/// Checks if the Sahder contains a given Uniform
	/// </summary>
	public bool Has(string name)
		=> Uniforms.ContainsKey(name);

	/// <summary>
	/// Tries to get a Uniform from the Shader
	/// </summary>
	public bool TryGet(string name, out Uniform uniform)
		=> Uniforms.TryGetValue(name, out uniform);

	/// <summary>
	/// Gets a Uniform from the Shader
	/// </summary>
	public Uniform Get(string name)
	{
		if (Uniforms.TryGetValue(name, out var value))
			return value;
		return default;
	}

	/// <summary>
	/// Gets a Unifrom from the Shader
	/// </summary>
	public Uniform this[string name]
		=> Uniforms[name];
		
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
			Graphics.Resources.RequestDelete(resource);
		}
	}
}
