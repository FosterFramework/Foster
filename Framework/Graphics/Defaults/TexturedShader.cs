namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader() : Shader(CreateInfo())
{
	private static ShaderCreateInfo CreateInfo() => CreateInfo(App.Graphics.Driver switch
	{
		GraphicsDriver.OpenGL => "gl",
		_ => "spv",
	});

	private static ShaderCreateInfo CreateInfo(string extension) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.vert.{extension}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			]
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.frag.{extension}"),
			SamplerCount: 1,
			Uniforms: []
		)
	);
}