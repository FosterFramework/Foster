namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader() : Shader(info)
{
	private static readonly ShaderCreateInfo info = new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes("Textured.vert.spv"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			]
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes("Textured.frag.spv"),
			SamplerCount: 1,
			Uniforms: []
		)
	);
}