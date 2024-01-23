using System.Diagnostics;
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
	public bool IsDisposed => disposed;

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

	internal readonly IntPtr resource;
	internal bool disposed = false;

	public Texture(int width, int height, TextureFormat format = TextureFormat.Color)
	{
		if (width <= 0 || height <= 0)
			throw new Exception("Texture must have a size larger than 0");

		resource = Platform.FosterTextureCreate(width, height, format);
		if (resource == IntPtr.Zero)
			throw new Exception("Failed to create Texture");

		Width = width;
		Height = height;
		Format = format;
		IsTargetAttachment = false;

		Graphics.Resources.RegisterAllocated(this, resource, Platform.FosterTextureDestroy);
	}

	public Texture(int width, int height, ReadOnlySpan<Color> pixels)
		: this(width, height, TextureFormat.Color)
	{
		SetData<Color>(pixels);
	}

	public Texture(int width, int height, ReadOnlySpan<byte> pixels)
		: this(width, height, TextureFormat.Color)
	{
		SetData<byte>(pixels);
	}

	public Texture(Image image) 
		: this(image.Width, image.Height, TextureFormat.Color)
	{
		SetData<Color>(image.Data);
	}

	internal Texture(IntPtr resource, int width, int height, TextureFormat format)
	{
		this.resource = resource;
		Width = width;
		Height = height;
		Format = format;
		IsTargetAttachment = true;
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
		
		if (Marshal.SizeOf<T>() * data.Length < MemorySize)
			throw new Exception("Data Buffer is smaller than the Size of the Texture");

		fixed (byte* ptr = MemoryMarshal.AsBytes(data))
		{
			int length = Marshal.SizeOf<T>()  * data.Length;
			Platform.FosterTextureSetData(resource, new nint(ptr), length);
		}
	}

	/// <summary>
	/// Writes the Texture data to the given buffer
	/// </summary>
	public unsafe void GetData<T>(Span<T> data) where T : struct
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (Marshal.SizeOf<T>() * data.Length < MemorySize)
			throw new Exception("Data Buffer is smaller than the Size of the Texture");

		fixed (byte* ptr = MemoryMarshal.AsBytes(data))
		{
			int length = Marshal.SizeOf<T>() * data.Length;
			Platform.FosterTextureGetData(resource, new nint(ptr), length);
		}
	}

	public void Dispose()
	{
		if (IsTargetAttachment)
			throw new InvalidOperationException("Cannot Dispose a Texture that is part of a Target. Instead, Dispose the Target.");

		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed && !IsTargetAttachment)
		{
			disposed = true;
			Graphics.Resources.RequestDelete(resource);
		}
	}
}
