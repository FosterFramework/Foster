namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader(GraphicsDevice graphicsDevice) : Shader(graphicsDevice, CreateInfo(graphicsDevice))
{
	private static ShaderCreateInfo CreateInfo(GraphicsDevice graphicsDevice) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.vertex.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			],
			EntryPoint: "vertex_main"
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.fragment.{graphicsDevice.Driver.GetShaderExtension()}"),
			SamplerCount: 1,
			Uniforms: [],
			EntryPoint: "fragment_main"
		)
	);
}