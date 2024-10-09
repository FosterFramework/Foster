namespace Foster.Framework;

/// <summary>
/// The Default Shader used for the <seealso cref="Batcher"/>.
/// Expects <seealso cref="BatcherVertex"/> Vertices.
/// </summary>
public class BatcherShader() : Shader(CreateInfo())
{
	private static ShaderCreateInfo CreateInfo() => CreateInfo(App.Graphics.Driver switch
	{
		GraphicsDriver.OpenGL => "gl",
		_ => "spv",
	});

	private static ShaderCreateInfo CreateInfo(string extension) => new(
		Vertex: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.vert.{extension}"),
			SamplerCount: 0,
			Uniforms: [
				new("Matrix", UniformType.Mat4x4)
			]
		),
		Fragment: new(
			Code: Platform.ReadEmbeddedBytes($"Batcher.frag.{extension}"),
			SamplerCount: 1,
			Uniforms: []
		)
	);
}