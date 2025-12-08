using System.Diagnostics;
using System.Runtime.InteropServices;

using static SDL3.SDL;

namespace Foster.Framework;

/// <summary>
/// Internal Image wrapper that handles decoding and encoding QOI or PNG images
/// </summary>
internal unsafe struct ImageData
{
    public enum Formats
    {
        PNG,
        QOI
    }

    private const int Components = 4;

    private readonly nint pixels;
    private readonly SDL_Surface* surface;
    private readonly GCHandle arrayHandle;

    public readonly int Width;
    public readonly int Height;
    public readonly int SizeInBytes => Width * Height * Components;

    public readonly Span<byte> Bytes => new((void*)pixels, SizeInBytes);
    public readonly Span<Color> Pixels => new((void*)pixels, Width * Height);
    public readonly nint Data => pixels;

    /// <summary>
    /// Decode PNG or QOI image Data from a Stream
    /// </summary>
    public static ImageData Decode(Stream stream)
    {
        var mem = new MemoryStream();
        stream.CopyTo(mem);
        return Decode(mem.GetBuffer());
    }

    /// <summary>
    /// Decode PNG or QOI image Data from a byte array
    /// </summary>
    public static ImageData Decode(ReadOnlySpan<byte> data)
    {
        // try qoi first
        if (Qoi.IsFormat(data))
        {
            var rgba = Qoi.Decode(data, 4, out var desc);
			if (rgba.Length <= 0 || desc.Width <= 0 || desc.Height <= 0)
                throw new Exception("Failed to decode QOI file");

            // TODO: handle non RGBA arrays
            if (desc.Channels != Components)
                throw new NotImplementedException("Only 4-channel QOI images are supported");

            return Rgba(rgba, (int)desc.Width, (int)desc.Height);
        }
        // try png next
        else
        {
            SDL_Surface* surface;
            fixed (byte* buffer = data)
            {
                var iostream = SDL_IOFromConstMem(new nint(buffer), (nuint)data.Length);
                surface = SDL_LoadPNG_IO(iostream, true);
            }

            if (surface == null || surface->pixels == nint.Zero || surface->w <= 0 || surface->h <= 0)
                throw new Exception("Failed to decode PNG file");

            // need to remove pitch, so allocate an array instead
            var stride = surface->w * Components;
            if (surface->pitch != stride)
            {
                // copy data
                var rgba = new byte[surface->w * surface->h * Components];
                fixed (byte* ptr = rgba)
                {
                    for (int i = 0; i < surface->h; i ++)
                        Buffer.MemoryCopy((byte*)surface->pixels + (i * surface->pitch), ptr + i * stride, stride, stride);
                }

                // destroy the surface since we aren't using it anymore
                int w = surface->w, h = surface->h;
                SDL_DestroySurface((nint)surface);

                // create image data result
                return Rgba(rgba, w, h);
            }
            // we can use the surface as-is
            else
            {
                return new ImageData(surface);
            }
        }
    }

    /// <summary>
    /// Create Image data from an RGBA array
    /// </summary>
    public static ImageData Rgba<T>(T[] rgba, int width, int height) where T : unmanaged
    {
        Debug.Assert(rgba.Length * sizeof(T) == width * height * Components);
        var handle = GCHandle.Alloc(rgba, GCHandleType.Pinned);
        return new ImageData(handle, width, height);
    }

    /// <summary>
    /// Encode RGBA image data to a specific format
    /// </summary>
    public void Encode(Stream stream, Formats format)
    {
        if (pixels == nint.Zero || Width <= 0 || Height <= 0)
            throw new Exception("Trying to Encode an invalid Image");

        if (format == Formats.QOI)
        {
            Qoi.Encode(pixels, new() { Width = (uint)Width, Height = (uint)Height, Channels = 4, Colorspace = 0 });
        }
        else if (format == Formats.PNG)
        {
            // get surface
            SDL_Surface* src;
            if (surface != null)
                src = surface;
            else
                src = SDL_CreateSurfaceFrom(Width, Height, SDL_PixelFormat.SDL_PIXELFORMAT_RGBA32, pixels, Width * Components);

            // write png
            var mem = SDL_IOFromDynamicMem();
            SDL_SavePNG_IO(src, mem, false);

            // destroy surface
            if (src != surface)
                SDL_DestroySurface((nint)src);

            // write data to stream
            var length = SDL_GetIOSize(mem);
            var props = SDL_GetIOProperties(mem);
            var result = SDL_GetPointerProperty(props, SDL_PROP_IOSTREAM_DYNAMIC_MEMORY_POINTER, nint.Zero);
            stream.Write(new ReadOnlySpan<byte>((byte*)result, (int)length));
            SDL_CloseIO(mem);
        }
        else
            throw new NotImplementedException();
    }

    /// <summary>
    /// Creates the Image from an Array GC Handle
    /// </summary>
    private ImageData(GCHandle handle, int width, int height)
    {
        arrayHandle = handle;
        surface = null;
        pixels = handle.AddrOfPinnedObject();
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates the Image from an SDL Surface
    /// </summary>
    private ImageData(SDL_Surface* surface)
    {
        this.surface = surface;
        pixels = surface->pixels;
        arrayHandle = default;
        Width = surface->w;
        Height = surface->h;
    }

    /// <summary>
    /// Frees the image data resources
    /// </summary>
    public void Free()
    {
        if (surface != null)
            SDL_DestroySurface((nint)surface);
        if (arrayHandle.IsAllocated)
            arrayHandle.Free();
    }
}