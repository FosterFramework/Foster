namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader() : Shader(CreateInfo())
{
	private static ShaderCreateInfo CreateInfo() => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.vertex.{Platform.GetGraphicsShaderExtension()}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			],
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.fragment.{Platform.GetGraphicsShaderExtension()}"),
			SamplerCount: 1,
			Uniforms: [],
			EntryPoint: "fragment_main"
		)
	);
}