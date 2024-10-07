using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Texture used for Rendering
/// </summary>
public class Texture : IResource
{
	/// <summary>
	/// Optional Texture Name
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// If the Texture has been disposed
	/// </summary>
	public bool IsDisposed => disposed || Renderer.Device != device;

	/// <summary>
	/// Gets the Width of the Texture
	/// </summary>
	public readonly int Width;

	/// <summary>
	/// Gets the Height of the Texture
	/// </summary>
	public readonly int Height;

	/// <summary>
	/// Gets the Size (Width, Height) of the Texture
	/// </summary>
	public Point2 Size => new(Width, Height);

	/// <summary>
	/// The Texture Data Format
	/// </summary>
	public readonly TextureFormat Format;

	/// <summary>
	/// If this Texture is an Attachment for a Render Target.
	/// </summary>
	public readonly bool IsTargetAttachment;

	/// <summary>
	/// The Memory Size of the Texture, in bytes
	/// </summary>
	public int MemorySize => Width * Height * Format.Size();

	internal readonly nint resource;
	internal bool disposed = false;
	private readonly nint device;

	public Texture(int width, int height, TextureFormat format = TextureFormat.Color)
		: this(width, height, format, isTargetAttachment: false) {}

	public Texture(int width, int height, ReadOnlySpan<Color> pixels)
		: this(width, height, TextureFormat.Color) => SetData<Color>(pixels);

	public Texture(int width, int height, ReadOnlySpan<byte> pixels)
		: this(width, height, TextureFormat.Color) => SetData<byte>(pixels);

	public Texture(Image image) 
		: this(image.Width, image.Height, TextureFormat.Color) => SetData<Color>(image.Data);

	internal Texture(int width, int height, TextureFormat format, bool isTargetAttachment)
	{
		if (width <= 0 || height <= 0)
			throw new Exception("Texture must have a size larger than 0");

		device = Renderer.Device;
		resource = Renderer.CreateTexture(width, height, format, isTargetAttachment);
		Width = width;
		Height = height;
		Format = format;
		IsTargetAttachment = isTargetAttachment;
	}

	internal Texture(nint resource, int width, int height, TextureFormat format, bool isTargetAttachment)
	{
		if (width <= 0 || height <= 0)
			throw new Exception("Texture must have a size larger than 0");

		this.resource = resource;
		Width = width;
		Height = height;
		Format = format;
		IsTargetAttachment = isTargetAttachment;
	}

	~Texture()
	{
		if (!IsTargetAttachment)
			Dispose(false);
	}

	/// <summary>
	/// Sets the Texture data from the given buffer
	/// </summary>
	public unsafe void SetData<T>(ReadOnlySpan<T> data) where T : struct
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");
		
		if (Unsafe.SizeOf<T>() * data.Length < MemorySize)
			throw new Exception("Data Buffer is smaller than the Size of the Texture");

		fixed (byte* ptr = MemoryMarshal.AsBytes(data))
		{
			int length = Unsafe.SizeOf<T>()  * data.Length;
			Renderer.SetTextureData(resource, ptr, length);
		}
	}

	/// <summary>
	/// Writes the Texture data to the given buffer
	/// </summary>
	public unsafe void GetData<T>(Span<T> data) where T : struct
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (Unsafe.SizeOf<T>() * data.Length < MemorySize)
			throw new Exception("Data Buffer is smaller than the Size of the Texture");

		fixed (byte* ptr = MemoryMarshal.AsBytes(data))
		{
			int length = Unsafe.SizeOf<T>() * data.Length;
			Renderer.GetTextureData(resource, ptr, length);
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			disposed = true;
			Renderer.DestroyTexture(resource);
		}
	}
}
