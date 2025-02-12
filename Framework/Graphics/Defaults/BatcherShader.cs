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
			Code: Platform.ReadEmbeddedBytes($"Batcher.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			UniformBufferCount: 1,
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: graphicsDevice.SamplerCount,
			UniformBufferCount: 0,
			EntryPoint: "fragment_main"
		)
	);
}
