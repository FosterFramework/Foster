using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Material references a Shader and holds the applied values for each Uniform.
/// This way, you can have a single Shader in memory but many different applied values.
/// </summary>
public class Material
{
	private readonly record struct Uniform(
		string Name,
		int Index,
		int BufferStart,
		int BufferLength,
		UniformType Type,
		int ArrayElemets
	);

	private TextureSampler[] samplerBuffer = Array.Empty<TextureSampler>();
	private Texture?[] textureBuffer = Array.Empty<Texture?>();
	private float[] floatBuffer = Array.Empty<float>();
	private readonly List<Uniform> uniforms = new();

	/// <summary>
	/// The current Shader the Material is using.
	/// If null, the Material will not have any Uniforms.
	/// </summary>
	public Shader? Shader { get; private set; }

	/// <summary>
	/// Constructs an Empty Material
	/// </summary>
	public Material() { }

	/// <summary>
	/// Constructs a Material using the given Shader
	/// </summary>
	public Material(Shader? shader) => SetShader(shader);

	/// <summary>
	/// Clears the Uniform state
	/// </summary>
	public void Clear()
	{
		Shader = null;
		uniforms.Clear();
		Array.Fill(samplerBuffer, new());
		Array.Fill(textureBuffer, null);
		Array.Fill(floatBuffer, 0.0f);
	}

	/// <summary>
	/// Copies the Shader & Uniform values from this Material to the given one
	/// </summary>
	public void CopyTo(Material material)
	{
		material.SetShader(Shader);
		samplerBuffer.AsSpan().CopyTo(material.samplerBuffer);
		textureBuffer.AsSpan().CopyTo(material.textureBuffer);
		floatBuffer.AsSpan().CopyTo(material.floatBuffer);
	}

	/// <summary>
	/// Sets the Shader this Material is currently using
	/// </summary>
	public void SetShader(Shader? shader)
	{
		if (shader == Shader)
			return;

		Clear();
		Shader = shader;
		if (Shader == null)
			return;

		int samplerLength = 0;
		int textureLength = 0;
		int floatLength = 0;

		foreach (var u in Shader.Uniforms.Values)
		{
			Uniform it = default;

			switch (u.Type)
			{
				case UniformType.None:
					break;
				case UniformType.Float:
					it = new(u.Name, u.Index, floatLength, u.ArrayElements, u.Type, u.ArrayElements);
					floatLength += it.BufferLength;
					break;
				case UniformType.Float2:
					it = new(u.Name, u.Index, floatLength, u.ArrayElements * 2, u.Type, u.ArrayElements);
					floatLength += it.BufferLength;
					break;
				case UniformType.Float3:
					it = new(u.Name, u.Index, floatLength, u.ArrayElements * 3, u.Type, u.ArrayElements);
					floatLength += it.BufferLength;
					break;
				case UniformType.Float4:
					it = new(u.Name, u.Index, floatLength, u.ArrayElements * 4, u.Type, u.ArrayElements);
					floatLength += it.BufferLength;
					break;
				case UniformType.Mat3x2:
					it = new(u.Name, u.Index, floatLength, u.ArrayElements * 6, u.Type, u.ArrayElements);
					floatLength += it.BufferLength;
					break;
				case UniformType.Mat4x4:
					it = new(u.Name, u.Index, floatLength, u.ArrayElements * 16, u.Type, u.ArrayElements);
					floatLength += it.BufferLength;
					break;
				case UniformType.Texture2D:
					it = new(u.Name, u.Index, textureLength, u.ArrayElements, u.Type, u.ArrayElements);
					textureLength += it.BufferLength;
					break;
				case UniformType.Sampler2D:
					it = new(u.Name, u.Index, samplerLength, u.ArrayElements, u.Type, u.ArrayElements);
					samplerLength += it.BufferLength;
					break;
			}

			uniforms.Add(it);
		}

		if (samplerLength > samplerBuffer.Length)
			Array.Resize(ref samplerBuffer, samplerLength);
		if (textureLength > textureBuffer.Length)
			Array.Resize(ref textureBuffer, textureLength);
		if (floatLength > floatBuffer.Length)
			Array.Resize(ref floatBuffer, floatLength);
	}

	public void Set(string uniform, float value)
		=> Set(uniform, stackalloc float[1] { value });

	public void Set(string uniform, Vector2 value)
		=> Set(uniform, stackalloc float[2] { value.X, value.Y });

	public void Set(string uniform, Vector3 value)
		=> Set(uniform, stackalloc float[3] { value.X, value.Y, value.Z });

	public void Set(string uniform, Vector4 value)
		=> Set(uniform, stackalloc float[4] { value.X, value.Y, value.Z, value.W });

	public void Set(string uniform, Matrix3x2 value)
		=> Set(uniform, stackalloc float[6] { value.M11, value.M12, value.M21, value.M22, value.M31, value.M32 });

	public void Set(string uniform, Matrix4x4 value)
		=> Set(uniform, stackalloc float[16] { 
			value.M11, value.M12, value.M13, value.M14, 
			value.M21, value.M22, value.M23, value.M24,
			value.M31, value.M32, value.M33, value.M34,
			value.M41, value.M42, value.M43, value.M44,
		});

	public void Set(string uniform, Color value)
		=> Set(uniform, value.ToVector4());

	public unsafe void Set(string uniform, ReadOnlySpan<Vector2> value)
	{
		fixed (Vector2* ptr = value)
			Set(uniform, new ReadOnlySpan<float>((float*)ptr, value.Length * 2));
	}

	public unsafe void Set(string uniform, ReadOnlySpan<Vector3> value)
	{
		fixed (Vector3* ptr = value)
			Set(uniform, new ReadOnlySpan<float>((float*)ptr, value.Length * 3));
	}

	public unsafe void Set(string uniform, ReadOnlySpan<Vector4> value)
	{
		fixed (Vector4* ptr = value)
			Set(uniform, new ReadOnlySpan<float>((float*)ptr, value.Length * 4));
	}
	
	public void Set(string uniform, ReadOnlySpan<Color> value)
	{
		Span<float> data = stackalloc float[value.Length * 4];
		for (int i = 0, n = 0; i < value.Length; i ++, n += 4)
		{
			var vec4 = value[i].ToVector4();
			data[n + 0] = vec4.X;
			data[n + 1] = vec4.Y;
			data[n + 2] = vec4.Z;
			data[n + 3] = vec4.W;
		}
		Set(uniform, data);
	}
	
	public void Set(string uniform, ReadOnlySpan<Matrix4x4> value)
	{
		Span<float> data = stackalloc float[value.Length * 16];
		for (int i = 0, n = 0; i < value.Length; i ++, n += 16)
		{
			data[n + 0] = value[i].M11; data[n + 1] = value[i].M12; data[n + 2] = value[i].M13; data[n + 3] = value[i].M14; 
			data[n + 4] = value[i].M21; data[n + 5] = value[i].M22; data[n + 6] = value[i].M23; data[n + 7] = value[i].M24;
			data[n + 8] = value[i].M31; data[n + 9] = value[i].M32; data[n +10] = value[i].M33; data[n +11] = value[i].M34;
			data[n +12] = value[i].M41; data[n +13] = value[i].M42; data[n +14] = value[i].M43; data[n +15] = value[i].M44;
		}
		Set(uniform, data);
	}

	public unsafe void Set(string uniform, ReadOnlySpan<float> values)
	{
		var it = Get(uniform);

		if (!IsFloat(it.Type))
			throw new Exception($"Uniform '{uniform}' is not a Float value type");

		var subspan = values[0..Math.Min(values.Length, it.BufferLength)];
		subspan.CopyTo(floatBuffer.AsSpan()[it.BufferStart..]);
	}

	public unsafe void Set(string uniform, Texture? texture, int index = 0)
	{
		var it = Get(uniform);

		if (it.Type != UniformType.Texture2D)
			throw new Exception($"Uniform '{uniform}' is not a Texture2D value type");
		if (index >= it.BufferLength)
			throw new Exception($"Uniform '{uniform}' with index {index} is out of bounds");

		textureBuffer[it.BufferStart + index] = texture;
	}

	public unsafe void Set(string uniform, TextureSampler sampler, int index = 0)
	{
		var it = Get(uniform);

		if (it.Type != UniformType.Sampler2D)
			throw new Exception($"Uniform '{uniform}' is not a Sampler2D value type");
		if (index >= it.BufferLength)
			throw new Exception($"Uniform '{uniform}' with index {index} is out of bounds");

		samplerBuffer[it.BufferStart + index] = sampler;
	}

	/// <summary>
	/// Uploads the Uniform Values in this Material to the Shader
	/// </summary>
	internal unsafe void Apply()
	{
		if (Shader == null || Shader.IsDisposed)
			return;

		var id = Shader.resource;

		fixed (float* floatPtr = floatBuffer)
		fixed (TextureSampler* samplerPtr = samplerBuffer)
		{
			// copy texture values to int pointers buffer
			var texturePtr = stackalloc IntPtr[textureBuffer.Length];
			for (int i = 0; i < textureBuffer.Length; i ++)
			{
				if (textureBuffer[i] is Texture texture && !texture.IsDisposed)
					texturePtr[i] = texture.resource;
				else
					texturePtr[i] = IntPtr.Zero;
			}

			// apply each uniform value
			for (var i = 0; i < uniforms.Count; i++)
			{
				var uniform = uniforms[i];
				if (IsFloat(uniform.Type))
				{
					Platform.FosterShaderSetUniform(id, uniform.Index, floatPtr + uniform.BufferStart);
				}
				else if (uniform.Type == UniformType.Sampler2D)
				{
					Platform.FosterShaderSetSampler(id, uniform.Index, samplerPtr + uniform.BufferStart);
				}
				else if (uniform.Type == UniformType.Texture2D)
				{
					Platform.FosterShaderSetTexture(id, uniform.Index, texturePtr + uniform.BufferStart);
				}
			}
		}
	}

	/// <summary>
	/// Tries to find a Uniform of a given name
	/// </summary>
	private Uniform Get(string uniform)
	{
		for (var i = 0; i < uniforms.Count; i++)
		{
			var it = uniforms[i];
			if (it.Name == uniform)
				return it;
		}

		throw new Exception($"Uniform '{uniform}' does not exist");
	}

	/// <summary>
	/// Checks if the given Uniform Type is a float
	/// </summary>
	private static bool IsFloat(UniformType type) => type switch
	{
		UniformType.None => false,
		UniformType.Float => true,
		UniformType.Float2 => true,
		UniformType.Float3 => true,
		UniformType.Float4 => true,
		UniformType.Mat3x2 => true,
		UniformType.Mat4x4 => true,
		UniformType.Texture2D => false,
		UniformType.Sampler2D => false,
		_ => false
	};
}
