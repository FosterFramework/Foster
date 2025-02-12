namespace Foster.Framework;

/// <summary>
/// A simple 2D Shader with a single Fragment Sampler.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedShader(GraphicsDevice graphicsDevice)
	: Shader(graphicsDevice, GetCreateInfo(graphicsDevice), name: "Textured")
{
	private static ShaderCreateInfo GetCreateInfo(GraphicsDevice graphicsDevice) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			UniformBufferCount: 1,
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Textured.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: graphicsDevice.SamplerCount,
			UniformBufferCount: 0,
			EntryPoint: "fragment_main"
		)
	);
}
