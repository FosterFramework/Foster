namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader() : Shader(info)
{
	private static readonly ShaderCreateInfo info = new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes("Batcher.vert.spv"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			]
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes("Batcher.frag.spv"),
			SamplerCount: 1,
			Uniforms: []
		)
	);
}