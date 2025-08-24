using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D RGBA image representation stored offline.<br/>
/// To draw images to the screen using the GPU, use a <see cref="Texture"/>.
/// </summary>
public class Image : IDisposable
{
	/// <summary>
	/// Width of the Image
	/// </summary>
	public int Width { get; private set; }

	/// <summary>
	/// Height of the Image
	/// </summary>
	public int Height { get; private set; }

	/// <summary>
	/// Total number of pixels in the Image
	/// </summary>
	public int PixelCount => Width * Height;

	/// <summary>
	/// Width and Height of the Image
	/// </summary>
	public Point2 Size => new(Width, Height);

	/// <summary>
	/// Bounds of the Image
	/// </summary>
	public RectInt Bounds => new(0, 0, Width, Height);

	/// <summary>
	/// Gets a Span of the pixel data held by the Image.<br/>
	/// Note that this span is only valid as long as the Image has not been disposed.
	/// </summary>
	public unsafe Span<Color> Data
	{
		get
		{
			if (Width <= 0 || Height <= 0)
				return [];
			return new Span<Color>(ptr.ToPointer(), Width * Height);
		}
	}

	/// <summary>
	/// If the Image was disposed
	/// </summary>
	public bool IsDisposed { get; private set; }

	/// <summary>
	/// Gets a Pointer of the pixel data held by the Image
	/// </summary>
	public IntPtr Pointer => ptr;

	private IntPtr ptr;
	private GCHandle handle;
	private bool unmanaged = false;

	/// <summary>
	/// Creates an empty Image with no width or height
	/// </summary>
	public Image() {}

	/// <summary>
	/// Creates an image of a given size filled with transparent pixels
	/// </summary>
	public Image(int width, int height)
		: this(width, height, new Color[width * height]) {}

	/// <summary>
	/// Creates an image of a given size filled with the given color
	/// </summary>
	public Image(int width, int height, Color fill)
		: this(width, height, new Color[width * height])
	{
		unsafe
		{
			Color* pixels = (Color*)ptr.ToPointer();
			for (int i = 0, n = width * height; i < n; i++)
				pixels[i] = fill;
		}
	}

	/// <summary>
	/// Creates an image of a given size with the given pixel data
	/// </summary>
	public Image(int width, int height, Color[] pixels)
	{
		Width = width;
		Height = height;
		handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
		ptr = handle.AddrOfPinnedObject();
		unmanaged = false;
	}

	/// <summary>
	/// Creates an Image from a file.
	/// </summary>
	public Image(string file)
	{
		using var stream = File.OpenRead(file);
		Load(stream);
	}

	/// <summary>
	/// Creates an Image from a Stream. Expects a valid image format, not a stream of color values.
	/// </summary>
	public Image(Stream stream)
	{
		Load(stream);
	}

	/// <summary>
	/// Creates an image from a byte array. Expects a valid image format, not an array of color values.
	/// </summary>
	public Image(byte[] data)
	{
		using var stream = new MemoryStream(data);
		Load(stream);
	}

	~Image()
	{
		Dispose();
	}

	private unsafe void Load(Stream stream)
	{
		// get all the bytes
		var data = Calc.ReadAllBytes(stream);

		// load image from byte data
		nint mem;
		int w, h;
		fixed (byte* it = data)
		{
			mem = Platform.ImageLoad(it, data.Length, out w, out h);
		}

		// returns invalid ptr if unable to load
		if (mem == 0)
			throw new Exception("Failed to load Image");

		// update properties
		Clear();
		Width = w;
		Height = h;
		ptr = mem;
		unmanaged = true;
	}

	private void Clear()
	{
		if (unmanaged)
			Platform.ImageFree(ptr);
		else if (handle.IsAllocated)
			handle.Free();

		handle = new();
		ptr = new();
		unmanaged = false;
		Width = Height = 0;
	}

	/// <summary>
	/// Completely dispose of the image.
	/// </summary>
	public void Dispose()
	{
		Clear();
		GC.SuppressFinalize(this);
		IsDisposed = true;
	}

	/// <summary>
	/// Writes the image to a PNG file
	/// </summary>
	public void WritePng(string path)
	{
		using var stream = File.Create(path);
		WritePng(stream);
	}

	/// <summary>
	/// Write the image to PNG
	/// </summary>
	public void WritePng(Stream stream)
	{
		Write(stream, Platform.ImageWriteFormat.Png);
	}

	/// <summary>
	/// Writes the image to a QOI file
	/// </summary>
	public void WriteQoi(string path)
	{
		using var stream = File.Create(path);
		WriteQoi(stream);
	}

	/// <summary>
	/// Write the image to QOI
	/// </summary>
	public void WriteQoi(Stream stream)
	{
		Write(stream, Platform.ImageWriteFormat.Qoi);
	}

	private unsafe void Write(Stream stream, Platform.ImageWriteFormat format)
	{
		[UnmanagedCallersOnly]
		static unsafe void Write(IntPtr context, IntPtr data, int size)
		{
			var stream = GCHandle.FromIntPtr(context).Target as Stream;
			var ptr = (byte*)data.ToPointer();
			stream?.Write(new ReadOnlySpan<byte>(ptr, size));
		}

		GCHandle handle = GCHandle.Alloc(stream);
		Platform.ImageWrite(&Write, GCHandle.ToIntPtr(handle), format, Width, Height, ptr);
		handle.Free();
	}

	/// <summary>
	/// Get &amp; Set the color of a pixel.
	/// </summary>
	public Color this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetPixel(index);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => SetPixel(index, value);
	}

	/// <summary>
	/// Get &amp; Set the color of a pixel.
	/// </summary>
	public Color this[int x, int y]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => GetPixel(x, y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => SetPixel(x, y, value);
	}

	/// <summary>
	/// Get the color of a pixel.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe Color GetPixel(int x, int y)
	{
		if (x < 0 || y < 0 || x >= Width || y >= Height)
			throw new IndexOutOfRangeException();

		Color* pixels = (Color*)ptr;
		return pixels[x + y * Width];
	}

	/// <summary>
	/// Get the color of a pixel.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Color GetPixel(int i)
	{
		if (i < 0 || i >= Width * Height)
			throw new IndexOutOfRangeException();

		unsafe
		{
			Color* pixels = (Color*)ptr;
			return pixels[i];
		}
	}

	/// <summary>
	/// Set the color of a pixel.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixel(int x, int y, Color color)
	{
		if (x < 0 || y < 0 || x >= Width || y >= Height)
			throw new IndexOutOfRangeException();

		unsafe
		{
			Color* pixels = (Color*)ptr;
			pixels[x + y * Width] = color;
		}
	}

	/// <summary>
	/// Set the color of a pixel.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixel(int i, Color color)
	{
		if (i < 0 || i >= Width * Height)
			throw new IndexOutOfRangeException();

		unsafe
		{
			Color* pixels = (Color*)ptr;
			pixels[i] = color;
		}
	}

	/// <summary>
	/// Copies the data from a source image to this image.
	/// </summary>
	/// <param name="sourcePixels">Pixels to copy</param>
	/// <param name="sourceWidth">Width of source pixels</param>
	/// <param name="sourceHeight">Height of source pixels</param>
	/// <param name="sourceRect">Rectangle within the source image to copy</param>
	/// <param name="destination">Destination to copy the source pixels to</param>
	/// <param name="blend">Optional blend method</param>
	public unsafe void CopyPixels(ReadOnlySpan<Color> sourcePixels, int sourceWidth, int sourceHeight, in RectInt sourceRect, in Point2 destination, Func<Color, Color, Color>? blend = null)
	{
		if (sourcePixels.Length < sourceWidth * sourceHeight)
			throw new Exception("Trying to write more pixels than the provided data length");

		var target = new RectInt(destination.X, destination.Y, sourceRect.Width, sourceRect.Height);

		var dst = Bounds.GetIntersection(in target);
		if (dst.Width <= 0 || dst.Height <= 0)
			return;

		var p = sourceRect.TopLeft + (dst.TopLeft - target.TopLeft);

		fixed (Color* sourcePtr = sourcePixels)
		{
			var sourceEnd = sourcePtr + sourceWidth * sourceHeight;
			var destinationPtr = (Color*)ptr.ToPointer();
			var destinationEnd = destinationPtr + Width * Height;
			var len = dst.Width;

			for (int y = 0; y < dst.Height; y++)
			{
				var srcPtr = sourcePtr + ((p.Y + y) * sourceWidth + p.X);
				var dstPtr = destinationPtr + ((dst.Y + y) * Width + dst.X);

				if (srcPtr + len > sourceEnd ||
					dstPtr + len > destinationEnd)
					throw new Exception("Out of range");

				if (blend == null)
				{
					Buffer.MemoryCopy(srcPtr, dstPtr, len * 4, len * 4);
				}
				else
				{
					for (int i = 0; i < len; i++)
						dstPtr[i] = blend(srcPtr[i], dstPtr[i]);
				}
			}
		}
	}

	public void CopyPixels(ReadOnlySpan<Color> sourcePixels, int sourceWidth, int sourceHeight, in Point2? destination = null, Func<Color, Color, Color>? blend = null)
	{
		CopyPixels(sourcePixels, sourceWidth, sourceHeight, new RectInt(0, 0, sourceWidth, sourceHeight), destination ?? Point2.Zero, blend);
	}

	public void CopyPixels(Image source, in RectInt sourceRect, Point2? destination, Func<Color, Color, Color>? blend = null)
	{
		CopyPixels(source.Data, source.Width, source.Height, sourceRect, destination ?? Point2.Zero, blend);
	}

	public void CopyPixels(Image source, Point2? destination, Func<Color, Color, Color>? blend = null)
	{
		CopyPixels(source.Data, source.Width, source.Height, new RectInt(0, 0, source.Width, source.Height), destination ?? Point2.Zero, blend);
	}

	public void Premultiply()
	{
		unsafe
		{
			if (ptr != nint.Zero)
			{
				Color* pixels = (Color*)ptr.ToPointer();
				for (int i = 0, n = PixelCount; i < n; ++i)
					pixels[i] = pixels[i].Premultiply();
			}
		}
	}
}
