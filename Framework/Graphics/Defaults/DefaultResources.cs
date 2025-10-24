namespace Foster.Framework;

/// <summary>
/// Built In Default Foster Resources
/// </summary>
public class DefaultResources
{
	/// <summary>
	/// The Default Material used for the <seealso cref="BatchMaterial"/>.
	/// Expects <seealso cref="BatcherVertex"/> Vertices.
	/// </summary>
	public Material BatchMaterial => batcher ??= Create(device, "Batcher", 0, 1, 1, 0);

	/// <summary>
	/// A simple 2D Textured Shader.
	/// Expects <seealso cref="PosTexColVertex"/> Vertices.
	/// </summary>
	public Material TexturedMaterial => textured ??= Create(device, "Textured", 0, 1, 1, 0);

	private readonly GraphicsDevice device;
	private Material? batcher;
	private Material? textured;

	internal DefaultResources(GraphicsDevice device)
	{
		this.device = device;
	}

	static Material Create(GraphicsDevice device, string name, int vertSamplers, int vertUniformBuffers, int fragSamplers, int fragUniformBuffers)
	{
		var ext = device.Driver.GetShaderExtension();
		return new Material(
			vertexShader: new(device, new(
				Stage: ShaderStage.Vertex,
				Code: Platform.ReadEmbeddedBytes($"{name}.vertex.{ext}"),
				SamplerCount: vertSamplers,
				UniformBufferCount: vertUniformBuffers,
				EntryPoint: "vertex_main"
			), $"{name}Vertex"),
			fragmentShader: new(device, new(
				Stage: ShaderStage.Fragment,
				Code: Platform.ReadEmbeddedBytes($"{name}.fragment.{ext}"),
				SamplerCount: fragSamplers,
				UniformBufferCount: fragUniformBuffers,
				EntryPoint: "fragment_main"
			), $"{name}Fragment")
		);
	}
}