namespace Foster.Framework;

/// <summary>
/// A 2D Render Target used to draw content off-frame.
/// </summary>
public class Target : IResource, IDrawableTarget
{
	private static readonly TextureFormat[] defaultFormats = [ TextureFormat.Color ];

	/// <summary>
	/// The Renderer this Texture was created in
	/// </summary>
	public Renderer Renderer { get; private set; }

	/// <summary>
	/// Optional Target Name
	/// </summary>
	public string Name { get; set; } = string.Empty;

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

	public int WidthInPixels => Width;
	public int HeightInPixels => Height;

	internal readonly Renderer.IHandle Resource;

	public Target(Renderer renderer, int width, int height)
		: this(renderer, width, height, defaultFormats) { }

	public Target(Renderer renderer, int width, int height, in ReadOnlySpan<TextureFormat> attachments)
	{
		if (width <= 0 || height <= 0)
			throw new ArgumentException("Target width and height must be larger than 0");

		if (attachments.Length <= 0)
			throw new ArgumentException("Target needs at least 1 color attachment");

		Renderer = renderer;
		Resource = Renderer.CreateTarget(width, height);
		Width = width;
		Height = height;
		Bounds = new RectInt(0, 0, Width, Height);
		Attachments = new Texture[attachments.Length];
		for (int i = 0; i < attachments.Length; i ++)
			Attachments[i] = new Texture(renderer, width, height, attachments[i], this);
	}

	~Target()
	{
		Dispose(false);
	}

	/// <summary>
	/// Clears the Target
	/// </summary>
	public unsafe void Clear(Color color, float depth, int stencil, ClearMask mask)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");
		Renderer.Clear(this, color, depth, stencil, mask);
	}

	/// <summary>
	/// Disposes of the Target and all its Attachments
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		Renderer.DestroyResource(Resource);
	}

	public static implicit operator Texture(Target target) => target.Attachments[0];
}
