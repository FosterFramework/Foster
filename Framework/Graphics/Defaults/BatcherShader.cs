namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader(GraphicsDevice graphicsDevice)
	: Shader(graphicsDevice, GetCreateInfo(graphicsDevice), name: "Batcher")
{
	private static ShaderCreateInfo GetCreateInfo(GraphicsDevice graphicsDevice) => new(
		Vertex: new(
			Code: Calc.ReadEmbeddedBytes($"Batcher.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			UniformBufferCount: 1,
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Calc.ReadEmbeddedBytes($"Batcher.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 1,
			UniformBufferCount: 0,
			EntryPoint: "fragment_main"
		)
	);
}