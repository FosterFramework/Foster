using System.Collections.ObjectModel;

namespace Foster.Framework;

public struct ShaderCreateInfo
{
	public byte[] VertexShader;
	public byte[] FragmentShader;
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

	private struct FosterUniformInfo
	{
		public int index;
		public nint name;
		public UniformType type;
		public int arrayElements;
	}

	public Shader(in ShaderCreateInfo createInfo)
	{
		resource = Renderer.ShaderCreate(createInfo);

		FosterUniformInfo[] infos = [
			new() {
				index = 0,
				name = Platform.ToUTF8("u_matrix"),
				type = UniformType.Mat4x4,
				arrayElements = 1,
			},
			new() {
				index = 1,
				name = Platform.ToUTF8("u_palette"),
				type = UniformType.Float4,
				arrayElements = 4,
			},
			new() {
				index = 0,
				name = Platform.ToUTF8("u_texture"),
				type = UniformType.Texture2D,
				arrayElements = 1,
			},
			new() {
				index = 0,
				name = Platform.ToUTF8("u_texture_sampler"),
				type = UniformType.Sampler2D,
				arrayElements = 1,
			}
		];

		// add each uniform
		var uniforms = new Dictionary<string, Uniform>();
		for (int i = 0; i < infos.Length; i ++)
		{
			var info = infos[i];
			var name = Platform.ParseUTF8(info.name);
			uniforms.Add(name, new (info.index, name, info.type, info.arrayElements));
		}

		Uniforms = uniforms.AsReadOnly();
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
			Renderer.ShaderDestroy(resource);
		}
	}
}
