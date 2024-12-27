namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader(Renderer renderer) : Shader(renderer, CreateInfo(renderer))
{
	private static ShaderCreateInfo CreateInfo(Renderer renderer) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.vertex.{renderer.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			],
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.fragment.{renderer.Driver.GetShaderExtension()}"),
			SamplerCount: 1,
			Uniforms: [],
			EntryPoint: "fragment_main"
		)
	);
}