namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader(GraphicsDevice graphicsDevice) : Shader(graphicsDevice, CreateInfo(graphicsDevice))
{
	private static ShaderCreateInfo CreateInfo(GraphicsDevice graphicsDevice) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			],
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 1,
			Uniforms: [],
			EntryPoint: "fragment_main"
		)
	);
}