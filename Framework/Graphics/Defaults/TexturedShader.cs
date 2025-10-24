namespace Foster.Framework;

/// <summary>
/// A simple 2D Vertex Shader.
/// Expects <seealso cref="PosTexColVertex"/> Vertices.
/// </summary>
public class TexturedVertexShader(GraphicsDevice graphicsDevice)
	: Shader(graphicsDevice, GetCreateInfo(graphicsDevice), name: "Textured")
{
	private static ShaderCreateInfo GetCreateInfo(GraphicsDevice graphicsDevice) => new(
		Stage: ShaderStage.Vertex,
		Code: Platform.ReadEmbeddedBytes($"Textured.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
		SamplerCount: 0,
		UniformBufferCount: 1,
		EntryPoint: "vertex_main"
	);
}
/// <summary>
/// A simple 2D Fragment Shader with a single Fragment Sampler.
/// </summary>
public class TexturedFragmentShader(GraphicsDevice graphicsDevice)
	: Shader(graphicsDevice, GetCreateInfo(graphicsDevice), name: "Textured")
{
	private static ShaderCreateInfo GetCreateInfo(GraphicsDevice graphicsDevice) => new(
		Stage: ShaderStage.Fragment,
		Code: Platform.ReadEmbeddedBytes($"Textured.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
		SamplerCount: 1,
		UniformBufferCount: 0,
		EntryPoint: "fragment_main"
	);
}