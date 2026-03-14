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
	public MsdfFont MsdfFont => font ??= new(
		new Image(ReadEmbeddedBytes($"Fonts/Roboto.png")),
		ReadEmbeddedBytes($"Fonts/Roboto.json")
	);

	/// <summary>
	/// A Default Sprite Font used for rendering text
	/// </summary>
	public SpriteFont SpriteFont => spritefont ??= new(device, MsdfFont);

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
			vertexShader: new(device, ShaderStage.Vertex,
				code: ReadEmbeddedBytes($"Shaders/{name}.vertex.{ext}"),
				samplerCount: vertSamplers,
				uniformBufferCount: vertUniformBuffers,
				entryPoint: "vertex_main",
				name: $"{name}Vertex"),
			fragmentShader: new(device, ShaderStage.Fragment,
				code: ReadEmbeddedBytes($"Shaders/{name}.fragment.{ext}"),
				samplerCount: fragSamplers,
				uniformBufferCount: fragUniformBuffers,
				entryPoint: "fragment_main",
				name: $"{name}Fragment")
		);
	}

	public static byte[] ReadEmbeddedBytes(string name)
		=> Calc.ReadEmbeddedBytes(typeof(DefaultResources).Assembly, name);

}