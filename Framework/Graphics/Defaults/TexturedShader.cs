namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader() : Shader(info)
{
	private static readonly ShaderCreateInfo info = new(
		VertexProgram: new(
			code: Platform.ReadEmbeddedBytes("Textured.vert.spv"),
			samplerCount: 0,
			uniforms: [
				new("Matrix", UniformType.Mat4x4)
			]
		),
		FragmentProgram: new(
			code: Platform.ReadEmbeddedBytes("Textured.frag.spv"),
			samplerCount: 1,
			uniforms: []
		)
	);
}