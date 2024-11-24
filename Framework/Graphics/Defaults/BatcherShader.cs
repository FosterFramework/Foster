namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader() : Shader(CreateInfo())
{
	private static ShaderCreateInfo CreateInfo() => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.vertex.{Platform.GetGraphicsShaderExtension()}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			],
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.fragment.{Platform.GetGraphicsShaderExtension()}"),
			SamplerCount: 1,
			Uniforms: [],
			EntryPoint: "fragment_main"
		)
	);
}