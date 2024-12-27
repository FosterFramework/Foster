namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader(Renderer renderer) : Shader(renderer, CreateInfo(renderer))
{
	private static ShaderCreateInfo CreateInfo(Renderer renderer) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.vertex.{renderer.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			],
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.fragment.{renderer.Driver.GetShaderExtension()}"),
			SamplerCount: 1,
			Uniforms: [],
			EntryPoint: "fragment_main"
		)
	);
}