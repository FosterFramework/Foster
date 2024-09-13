namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader() : Shader(info)
{
	private static readonly ShaderCreateInfo info = new(
		VertexProgram: new(
			code: Platform.ReadEmbeddedBytes("Batcher.vert.spv"),
			samplerCount: 0,
			uniforms: [
				new("Matrix", UniformType.Mat4x4)
			]
		),
		FragmentProgram: new(
			code: Platform.ReadEmbeddedBytes("Batcher.frag.spv"),
			samplerCount: 1,
			uniforms: []
		)
	);
}