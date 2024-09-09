namespace Foster.Framework;

internal static class ShaderDefaults
{
	public static readonly ShaderCreateInfo Default;

	static ShaderDefaults()
	{
		static byte[] ReadBytes(string name)
		{
			var assembly = typeof(ShaderDefaults).Assembly;
			using var stream = assembly.GetManifestResourceStream(name);
			if (stream != null)
			{
				var result = new byte[stream.Length];
				stream.Read(result, 0, result.Length);
				return result;
			}

			return [];
		}

		Default = new(
			VertexProgram: new(
				code: ReadBytes("Default.vert.spv"),
				samplerCount: 0,
				uniforms: [
					new("Matrix", UniformType.Mat4x4)
				]
			),
			FragmentProgram: new(
				code: ReadBytes("Default.frag.spv"),
				samplerCount: 1,
				uniforms: []
			)
		);
	}
}
