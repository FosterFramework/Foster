namespace Foster.Framework;

/// <summary>
/// The Default Vertex Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherVertexShader(GraphicsDevice graphicsDevice)
	: Shader(graphicsDevice, GetCreateInfo(graphicsDevice), name: "Batcher")
{
	private static ShaderCreateInfo GetCreateInfo(GraphicsDevice graphicsDevice) => new(
		Stage: ShaderStage.Vertex,
		Code: Platform.ReadEmbeddedBytes($"Batcher.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
		SamplerCount: 0,
		UniformBufferCount: 1,
		EntryPoint: "vertex_main"
	);
}

/// <summary>
/// The Default Fragment Shader used for the <seealso cref="Batcher"/>.
/// </summary>
public class BatcherFragmentShader(GraphicsDevice graphicsDevice)
	: Shader(graphicsDevice, GetCreateInfo(graphicsDevice), name: "Batcher")
{
	private static ShaderCreateInfo GetCreateInfo(GraphicsDevice graphicsDevice) => new(
		Stage: ShaderStage.Fragment,
		Code: Platform.ReadEmbeddedBytes($"Batcher.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
		SamplerCount: 1,
		UniformBufferCount: 0,
		EntryPoint: "fragment_main"
	);
}