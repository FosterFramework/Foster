namespace Foster.Framework;

/// <summary>
/// A 2D Render Target used to draw content off-screen.
/// </summary>
public class Target : IGraphicResource, IDrawableTarget
{
	private static readonly TextureFormat[] defaultFormats = [ TextureFormat.Color ];

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

	public Target(GraphicsDevice graphicsDevice, int width, int height, string? name = null)
		: this(graphicsDevice, width, height, defaultFormats, name) { }

	public Target(GraphicsDevice graphicsDevice, int width, int height, in ReadOnlySpan<TextureFormat> attachments, string? name = null)
	{
		if (width <= 0 || height <= 0)
			throw new ArgumentException("Target width and height must be larger than 0");

		if (attachments.Length <= 0)
			throw new ArgumentException("Target needs at least 1 color attachment");

		Name = name ?? string.Empty;
		GraphicsDevice = graphicsDevice;
		Resource = GraphicsDevice.CreateTarget(width, height);
		Width = width;
		Height = height;
		Bounds = new RectInt(0, 0, Width, Height);
		Attachments = new Texture[attachments.Length];
		for (int i = 0; i < attachments.Length; i ++)
		{
			var attachmentName = !string.IsNullOrEmpty(name) ? $"{Name}-Attachment{i}" : null;
			Attachments[i] = new Texture(graphicsDevice, width, height, attachments[i], this, attachmentName);
		}
	}

	~Target()
	{
		Dispose();
	}

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
