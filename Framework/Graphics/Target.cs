namespace Foster.Framework;

/// <summary>
/// A 2D Render Target used to draw content off-screen.
/// </summary>
public class Target : IGraphicResource, IDrawableTarget
{
	private static readonly (TextureFormat, SampleCount)[] defaultFormats = [ (TextureFormat.Color, SampleCount.One) ];

	/// <summary>
	/// The GraphicsDevice this Texture was created in
	/// </summary>
	public GraphicsDevice GraphicsDevice { get; private set; }

	/// <summary>
	/// Optional Target Name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Ii the Target has been disposed.
	/// </summary>
	public bool IsDisposed => Resource.Disposed;

	/// <summary>
	/// The Width of the Target.
	/// </summary>
	public readonly int Width;

	/// <summary>
	/// The Height of the Target.
	/// </summary>
	public readonly int Height;

	/// <summary>
	/// Target Bounds
	/// </summary>
	public readonly RectInt Bounds;

	/// <summary>
	/// The Texture attachments in the Target.
	/// </summary>
	public readonly Texture[] Attachments;

	object? IDrawableTarget.Surface => this;
	int IDrawableTarget.WidthInPixels => Width;
	int IDrawableTarget.HeightInPixels => Height;

	internal readonly GraphicsDevice.IHandle Resource;

	/// <summary>
	/// Constructs a Target with a single <see cref="TextureFormat.Color"/> attachment. 
	/// </summary>
	public Target(GraphicsDevice graphicsDevice, int width, int height, string? name = null)
		: this(graphicsDevice, width, height, defaultFormats, name) { }

	/// <summary>
	/// Constructs a Target with the given Attachments
	/// </summary>
	public Target(GraphicsDevice graphicsDevice, int width, int height, in ReadOnlySpan<TextureFormat> attachments, string? name = null)
		: this(graphicsDevice, width, height, GetAttachments(attachments), name) {}

	/// <summary>
	/// Constructs a Target with the given Attachments
	/// </summary>
	public Target(GraphicsDevice graphicsDevice, int width, int height, in ReadOnlySpan<(TextureFormat Format, SampleCount SampleCount)> attachments, string? name = null)
		: this(graphicsDevice, width, height, GetAttachments(attachments), name) {}

	private Target(GraphicsDevice graphicsDevice, int width, int height, in StackList16<(TextureFormat Format, SampleCount SampleCount)> attachments, string? name = null)
	{
		if (width <= 0 || height <= 0)
			throw new ArgumentException("Target width and height must be larger than 0");

		if (attachments.Count <= 0)
			throw new ArgumentException("Target needs at least 1 color attachment");

		Name = name ?? string.Empty;
		GraphicsDevice = graphicsDevice;
		Resource = GraphicsDevice.CreateTarget(width, height);
		Width = width;
		Height = height;
		Bounds = new RectInt(0, 0, Width, Height);
		Attachments = new Texture[attachments.Count];
		for (int i = 0; i < attachments.Count; i ++)
		{
			var attachmentName = !string.IsNullOrEmpty(name) ? $"{Name}-Attachment{i}" : null;
			Attachments[i] = new Texture(graphicsDevice, width, height, attachments[i].Format, attachments[i].SampleCount, this, attachmentName);
		}
	}

	~Target()
	{
		Dispose();
	}

	private static StackList16<(TextureFormat Format, SampleCount SampleCount)> GetAttachments(in ReadOnlySpan<TextureFormat> values)
	{
		StackList16<(TextureFormat, SampleCount)> result = [];
		for (int i = 0; i < values.Length; i ++)
			result.Add((values[i], SampleCount.One));
		return result;
	}

	private static StackList16<(TextureFormat Format, SampleCount SampleCount)> GetAttachments(in ReadOnlySpan<(TextureFormat, SampleCount)> values)
		=> [..values];

	/// <summary>
	/// Disposes of the Target and all its Attachments
	/// </summary>
	public void Dispose()
	{
		GraphicsDevice.DestroyResource(Resource);
		GC.SuppressFinalize(this);
	}

	public static implicit operator Texture(Target target) => target.Attachments[0];
}
