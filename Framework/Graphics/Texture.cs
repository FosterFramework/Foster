using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Texture used for Rendering
/// </summary>
public class Texture : IGraphicResource
{
	/// <summary>
	/// The GraphicsDevice this Texture was created in
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// Optional Texture Name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// If the Texture has been disposed
	/// </summary>
	public bool IsDisposed => disposed || GraphicsDevice.Disposed;

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
	/// The Texture Sample Count. This is always <see cref="SampleCount.One"/> unless created as a <see cref="Target"/> attachment.
	/// </summary>
	public readonly SampleCount SampleCount;

	/// <summary>
	/// If this Texture is an Attachment for a Render Target.
	/// </summary>
	public readonly bool IsTargetAttachment;

	/// <summary>
	/// The Memory Size of the Texture, in bytes
	/// </summary>
	public int MemorySize => Width * Height * Format.Size();

	internal readonly GraphicsDevice.ResourceHandle Resource;

	private bool disposed;

	public Texture(GraphicsDevice graphicsDevice, int width, int height, TextureFormat format = TextureFormat.Color, string? name = null)
		: this(graphicsDevice, width, height, format, SampleCount.One, targetBinding: null, name) {}

	public Texture(GraphicsDevice graphicsDevice, int width, int height, ReadOnlySpan<Color> pixels, string? name = null)
		: this(graphicsDevice, width, height, TextureFormat.Color, name) => SetData<Color>(pixels);

	public Texture(GraphicsDevice graphicsDevice, int width, int height, ReadOnlySpan<byte> pixels, string? name = null)
		: this(graphicsDevice, width, height, TextureFormat.Color, name) => SetData<byte>(pixels);

	public Texture(GraphicsDevice graphicsDevice, Image image, string? name = null)
		: this(graphicsDevice, image.Width, image.Height, TextureFormat.Color, name) => SetData<Color>(image.Data);

	internal Texture(GraphicsDevice graphicsDevice, int width, int height, TextureFormat format, SampleCount sampleCount, Target? targetBinding, string? name)
	{
		GraphicsDevice = graphicsDevice;

		if (width <= 0 || height <= 0)
			throw new Exception("Texture must have a size larger than 0");

		Resource = graphicsDevice.CreateTexture(name, width, height, format, sampleCount, targetBinding?.Resource);
		Name = name ?? string.Empty;
		Width = width;
		Height = height;
		Format = format;
		SampleCount = sampleCount;
		IsTargetAttachment = targetBinding != null;
	}

	~Texture() => Dispose(false);

	/// <summary>
	/// Sets the Texture data from the given buffer
	/// </summary>
	public void SetData<T>(ReadOnlySpan<T> data) where T : struct
		=> SetData(data, new RectInt(0, 0, Width, Height));

	/// <summary>
	/// Sets the Texture data in a region from the given buffer
	/// </summary>
	public unsafe void SetData<T>(ReadOnlySpan<T> data, RectInt destRegion) where T : struct
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (destRegion.Left < 0 || destRegion.Top < 0 || destRegion.Bottom > Height || destRegion.Right > Width)
			throw new Exception("Destination region is out of range");

		int dataLength = Unsafe.SizeOf<T>() * data.Length;

		if (dataLength < destRegion.Width * destRegion.Height * Format.Size())
			throw new Exception("Data Buffer is smaller than the Size of the Texture Destination");

		fixed (byte* ptr = MemoryMarshal.AsBytes(data))
		{
			GraphicsDevice.SetTextureData(Resource, new nint(ptr), dataLength, destRegion);
		}
	}

	/// <summary>
	/// Writes the Texture data to the given buffer
	/// </summary>
	public void GetData<T>(Span<T> data) where T : struct
		=> GetData(data, new RectInt(0, 0, Width, Height));

	/// <summary>
	/// Writes the Texture data from a region to the given buffer
	/// </summary>
	public unsafe void GetData<T>(Span<T> data, RectInt sourceRegion) where T : struct
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (sourceRegion.Left < 0 || sourceRegion.Top < 0 || sourceRegion.Bottom > Height || sourceRegion.Right > Width)
			throw new Exception("Source region is out of range");

		int dataLength = Unsafe.SizeOf<T>() * data.Length;

		if (dataLength < sourceRegion.Width * sourceRegion.Height * Format.Size())
			throw new Exception("Data Buffer is smaller than the Size of the Texture Source");

		fixed (byte* ptr = MemoryMarshal.AsBytes(data))
		{
			GraphicsDevice.GetTextureData(Resource, new nint(ptr), dataLength, sourceRegion);
		}
	}

	/// <summary>
	/// Blits the contents of this Texture to another Texture
	/// </summary>
	public void Blit(RectInt sourceRect, Texture destination, RectInt destinationRect, TextureFilter filter)
	{
		GraphicsDevice.BlitTexture(Resource, sourceRect, destination.Resource, destinationRect, filter);
	}

	/// <summary>
	/// Blits the contents of this Texture to another Texture
	/// </summary>
	public void Blit(Texture destination, TextureFilter filter)
	{
		Blit(new(Size), destination, new(destination.Size), filter);
	}

	/// <summary>
	/// Clones this Texture and creates a new one with the same pixel data
	/// </summary>
	public Texture Clone()
	{
		var clone = new Texture(GraphicsDevice, Width, Height, Format);
		Blit(clone, TextureFilter.Nearest);
		return clone;
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
			// Targets should dispose their Texture Attachments
			if (!IsTargetAttachment)
				GraphicsDevice.DestroyResource(Resource);

			disposed = true;
		}
	}
}
