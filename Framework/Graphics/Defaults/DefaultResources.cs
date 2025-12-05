namespace Foster.Framework;

/// <summary>
/// Built In Default Foster Resources.<br/>
/// These are automatically created by referencing <see cref="GraphicsDevice.Defaults"/>
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

	/// <summary>
	/// An MSDF Font Material
	/// Expects <seealso cref="BatcherVertex"/> Vertices.
	/// </summary>
	public Material MsdfMaterial => msdf ??= Create(device, "Msdf", 0, 1, 1, 1);

	/// <summary>
	/// Default MSDF Font
	/// </summary>
	public MsdfFont DefaultMsdfFont => font ??= new(
		new Image(Platform.ReadEmbeddedBytes($"Fonts/Roboto.png")),
		Platform.ReadEmbeddedBytes($"Fonts/Roboto.json")
	);

	/// <summary>
	/// A Default Sprite Font used for rendering text
	/// </summary>
	public SpriteFont SpriteFont => spritefont ??= new(device, DefaultMsdfFont);

	private readonly GraphicsDevice device;
	private Material? batcher;
	private Material? textured;
	private Material? msdf;
	private MsdfFont? font;
	private SpriteFont? spritefont;

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
				Code: Platform.ReadEmbeddedBytes($"Shaders/{name}.vertex.{ext}"),
				SamplerCount: vertSamplers,
				UniformBufferCount: vertUniformBuffers,
				EntryPoint: "vertex_main"
			), $"{name}Vertex"),
			fragmentShader: new(device, new(
				Stage: ShaderStage.Fragment,
				Code: Platform.ReadEmbeddedBytes($"Shaders/{name}.fragment.{ext}"),
				SamplerCount: fragSamplers,
				UniformBufferCount: fragUniformBuffers,
				EntryPoint: "fragment_main"
			), $"{name}Fragment")
		);
	}
}