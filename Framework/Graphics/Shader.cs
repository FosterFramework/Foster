using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Foster.Framework;

public struct ShaderCreateInfo
{
	public readonly struct Attribute
	{
		public readonly string SemanticName;
		public readonly int SemanticIndex;

		public Attribute(string semanticName, int semanticIndex)
		{
			SemanticName = semanticName;
			SemanticIndex = semanticIndex;
		}
	}

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
	public class Uniform
	{
		public readonly string Name;
		public readonly UniformType Type;
		public readonly int ArrayElements;

		private readonly Shader shader;
		private readonly int index;
		private readonly float[]? floatBuffer;
		private readonly Texture?[]? textureBuffer;
		private readonly TextureSampler[]? samplerBuffer;

		internal Uniform(Shader shader, int index, string name, UniformType type, int arrayElements)
		{
			this.shader = shader;
			this.index = index;
			Name = name;
			Type = type;
			ArrayElements = arrayElements;

			switch (type)
			{
				case UniformType.None:
					break;
				case UniformType.Float:
					floatBuffer = new float[ArrayElements];
					break;
				case UniformType.Float2:
					floatBuffer = new float[ArrayElements * 2];
					break;
				case UniformType.Float3:
					floatBuffer = new float[ArrayElements * 3];
					break;
				case UniformType.Float4:
					floatBuffer = new float[ArrayElements * 4];
					break;
				case UniformType.Mat3x2:
					floatBuffer = new float[ArrayElements * 6];
					break;
				case UniformType.Mat4x4:
					floatBuffer = new float[ArrayElements * 16];
					break;
				case UniformType.Texture2D:
					textureBuffer = new Texture[ArrayElements];
					break;
				case UniformType.Sampler2D:
					samplerBuffer = new TextureSampler[ArrayElements];
					break;
			}
		}

		public void Set(float value)
			=> Set(stackalloc float[1] { value });

		public void Set(Vector2 value)
			=> Set(stackalloc float[2] { value.X, value.Y });

		public void Set(Vector3 value)
			=> Set(stackalloc float[3] { value.X, value.Y, value.Z });

		public void Set(Vector4 value)
			=> Set(stackalloc float[4] { value.X, value.Y, value.Z, value.W });

		public void Set(Matrix3x2 value)
			=> Set(stackalloc float[6] { value.M11, value.M12, value.M21, value.M22, value.M31, value.M32 });

		public void Set(Matrix4x4 value)
			=> Set(stackalloc float[16] { 
				value.M11, value.M12, value.M13, value.M14, 
				value.M21, value.M22, value.M23, value.M24,
				value.M31, value.M32, value.M33, value.M34,
				value.M41, value.M42, value.M43, value.M44,
			});

		public void Set(Color value)
			=> Set(value.ToVector4());
			
		public unsafe void Set(Span<float> values)
		{
			Debug.Assert(!shader.IsDisposed, "Shader is Disposed");
			Debug.Assert(floatBuffer != null, "Uniform is not Float value type");

			// get a sub span of the data equal to our maximum length
			var subspan = values[0..Math.Min(values.Length, floatBuffer.Length)];

			// copy to our internal buffer
			subspan.CopyTo(floatBuffer);

			// upload it
			fixed (float* ptr = values)
				Platform.FosterShaderSetUniform(shader.resource, index, ptr);
		}

		public unsafe void Set(Texture? texture, int index = 0)
		{
			Debug.Assert(!shader.IsDisposed, "Shader is Disposed");
			Debug.Assert(textureBuffer != null, "Uniform is not Texture2D value type");

			if (textureBuffer[index] != texture)
			{
				// assign the texture at that index
				textureBuffer[index] = texture;

				// create a list of IntPtr's using the Texture resources
				IntPtr* ptr = stackalloc IntPtr[textureBuffer.Length];
				for (int i = 0; i < textureBuffer.Length; i ++)
					*(ptr + i) = (textureBuffer[i] is Texture tex && !tex.IsDisposed) ? tex.resource : IntPtr.Zero;
					
				// upload the list of textures
				Platform.FosterShaderSetTexture(shader.resource, this.index, ptr);
			}
		}

		public unsafe void Set(TextureSampler sampler, int index = 0)
		{
			Debug.Assert(!shader.IsDisposed, "Shader is Disposed");
			Debug.Assert(samplerBuffer != null, "Uniform is not Sampler2D value type");

			// assign the sampler at that index
			samplerBuffer[index] = sampler;

			// upload the list of samplers
			fixed (TextureSampler* ptr = samplerBuffer)
				Platform.FosterShaderSetSampler(shader.resource, this.index, ptr);
		}
	}

	/// <summary>
	/// Optional Shader Name
	/// </summary>
	public string Name { get; set; } = string.Empty;
	
	/// <summary>
	/// If the Shader is disposed
	/// </summary>
	public bool IsDisposed => isDisposed;

	/// <summary>
	/// Dictionary of Uniforms in the Shader
	/// </summary>
	public readonly ReadOnlyDictionary<string, Uniform> Uniforms;

	internal readonly IntPtr resource;
	internal bool isDisposed = false;

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
			uniforms.Add(name, new (this, info.index, name, info.type, info.arrayElements));
		}

		Uniforms = uniforms.AsReadOnly();
	}

	~Shader()
	{
		Dispose();
	}

	/// <summary>
	/// Checks if the Sahder contains a given Uniform
	/// </summary>
	public bool Has(string name)
		=> Uniforms.ContainsKey(name);

	/// <summary>
	/// Tries to get a Uniform from the Shader
	/// </summary>
	public bool TryGet(string name, [NotNullWhen(returnValue:true)] out Uniform? uniform)
		=> Uniforms.TryGetValue(name, out uniform);

	/// <summary>
	/// Gets a Uniform from the Shader, or returns null if not found.
	/// </summary>
	public Uniform? Get(string name)
	{
		if (Uniforms.TryGetValue(name, out var value))
			return value;
		return null;
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
		if (!isDisposed)
		{
			isDisposed = true;
			Platform.FosterShaderDestroy(resource);
		}
	}
}
