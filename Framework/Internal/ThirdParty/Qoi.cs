using System.Runtime.CompilerServices;

namespace Foster.Framework;

/// <summary>
/// A C# port of the QOI format: https://github.com/phoboslab/qoi
/// </summary>
internal static class Qoi
{
	public struct Desc
	{
		public uint Width;
		public uint Height;
		public byte Channels;
		public byte Colorspace;
	}

	private struct Rgba
	{
		public byte R, G, B, A;
	}

	private const int QOI_SRGB =   0;
	private const int QOI_LINEAR = 1;
	
	private const int QOI_OP_INDEX  = 0x00; /* 00xxxxxx */
	private const int QOI_OP_DIFF   = 0x40; /* 01xxxxxx */
	private const int QOI_OP_LUMA   = 0x80; /* 10xxxxxx */
	private const int QOI_OP_RUN    = 0xc0; /* 11xxxxxx */
	private const int QOI_OP_RGB    = 0xfe; /* 11111110 */
	private const int QOI_OP_RGBA   = 0xff; /* 11111111 */
	private const int QOI_MASK_2    = 0xc0; /* 11000000 */
	private const int QOI_HEADER_SIZE =  14;

	private const uint QOI_MAGIC = (((uint)'q') << 24 | ((uint)'o') << 16 | ((uint)'i') <<  8 | ((uint)'f'));
	private const uint QOI_PIXELS_MAX = 400000000;

	private static readonly byte[] padding = [0,0,0,0,0,0,0,1];

	public static unsafe bool IsFormat(ReadOnlySpan<byte> data)
	{
		fixed (byte* ptr = data)
			return IsFormat((nint)ptr, data.Length);
	}

	public static unsafe bool IsFormat(nint data, int length)
	{
		if (length <= 4)
			return false;
		return *(uint*)data == QOI_MAGIC;
	}

	public static unsafe byte[] Encode(nint data, in Desc desc)
	{
		int i, max_size, p, run;
		int px_len, px_end, px_pos, channels;
		byte* pixels;
		Span<Rgba> index = stackalloc Rgba[64];
		Rgba px, px_prev;

		if (data == nint.Zero ||
			desc.Width == 0 || desc.Height == 0 ||
			desc.Channels < 3 || desc.Channels > 4 ||
			desc.Colorspace > 1 ||
			desc.Height >= QOI_PIXELS_MAX / desc.Width)
			return [];

		max_size = (int)(
			desc.Width * desc.Height * (desc.Channels + 1) +
			QOI_HEADER_SIZE + padding.Length);

		p = 0;
		var bytes = new byte[max_size];

		Write32(bytes, &p, QOI_MAGIC);
		Write32(bytes, &p, desc.Width);
		Write32(bytes, &p, desc.Height);
		bytes[p++] = desc.Channels;
		bytes[p++] = desc.Colorspace;

		pixels = (byte*)data;
		index.Fill(default);

		run = 0;
		px_prev.R = 0;
		px_prev.G = 0;
		px_prev.B = 0;
		px_prev.A = 255;
		px = px_prev;

		px_len = (int)(desc.Width * desc.Height * desc.Channels);
		px_end = px_len - desc.Channels;
		channels = desc.Channels;

		for (px_pos = 0; px_pos < px_len; px_pos += channels) {
			px.R = pixels[px_pos + 0];
			px.G = pixels[px_pos + 1];
			px.B = pixels[px_pos + 2];

			if (channels == 4) {
				px.A = pixels[px_pos + 3];
			}

			if (Equals(px, px_prev)) {
				run++;
				if (run == 62 || px_pos == px_end) {
					bytes[p++] = (byte)(QOI_OP_RUN | (run - 1));
					run = 0;
				}
			}
			else {
				int index_pos;

				if (run > 0) {
					bytes[p++] = (byte)(QOI_OP_RUN | (run - 1));
					run = 0;
				}

				index_pos = ColorHash(px) % 64;

				if (Equals(index[index_pos], px)) {
					bytes[p++] = (byte)(QOI_OP_INDEX | index_pos);
				}
				else {
					index[index_pos] = px;

					if (px.A == px_prev.A) {
						sbyte vr = (sbyte)(px.R - px_prev.R);
						sbyte vg = (sbyte)(px.G - px_prev.G);
						sbyte vb = (sbyte)(px.B - px_prev.B);

						sbyte vg_r = (sbyte)(vr - vg);
						sbyte vg_b = (sbyte)(vb - vg);

						if (
							vr > -3 && vr < 2 &&
							vg > -3 && vg < 2 &&
							vb > -3 && vb < 2
						) {
							bytes[p++] = (byte)(QOI_OP_DIFF | (vr + 2) << 4 | (vg + 2) << 2 | (vb + 2));
						}
						else if (
							vg_r >  -9 && vg_r <  8 &&
							vg   > -33 && vg   < 32 &&
							vg_b >  -9 && vg_b <  8
						) {
							bytes[p++] = (byte)(QOI_OP_LUMA     | (vg   + 32));
							bytes[p++] = (byte)((vg_r + 8) << 4 | (vg_b +  8));
						}
						else {
							bytes[p++] = QOI_OP_RGB;
							bytes[p++] = px.R;
							bytes[p++] = px.G;
							bytes[p++] = px.B;
						}
					}
					else {
						bytes[p++] = QOI_OP_RGBA;
						bytes[p++] = px.R;
						bytes[p++] = px.G;
						bytes[p++] = px.B;
						bytes[p++] = px.A;
					}
				}
			}
			px_prev = px;
		}

		for (i = 0; i < padding.Length; i++) {
			bytes[p++] = padding[i];
		}

		Array.Resize(ref bytes, p);
		return bytes;
	}

	public static unsafe byte[] Decode(ReadOnlySpan<byte> data, int channels, out Desc desc)
	{
		fixed (byte* ptr = data)
			return Decode((nint)ptr, data.Length, channels, out desc);
	}

	public static unsafe byte[] Decode(nint data, int size, int channels, out Desc desc)
	{
		desc = default;

		byte* bytes;
		uint header_magic;
		Span<Rgba> index = stackalloc Rgba[64];
		Rgba px;
		int px_len, chunks_len, px_pos;
		int p = 0, run = 0;

		if (data == nint.Zero ||
			(channels != 0 && channels != 3 && channels != 4) ||
			size < QOI_HEADER_SIZE + padding.Length
		) return [];

		bytes = (byte*)data;

		header_magic = Read32(bytes, &p);
		desc.Width = Read32(bytes, &p);
		desc.Height = Read32(bytes, &p);
		desc.Channels = bytes[p++];
		desc.Colorspace = bytes[p++];

		if (desc.Width == 0 || desc.Height == 0 ||
			desc.Channels < 3 || desc.Channels > 4 ||
			desc.Colorspace > 1 ||
			header_magic != QOI_MAGIC ||
			desc.Height >= QOI_PIXELS_MAX / desc.Width
		) return [];

		if (channels == 0) {
			channels = desc.Channels;
		}

		px_len = (int)(desc.Width * desc.Height * channels);
		var pixels = new byte[px_len];

		index.Fill(default);
		px.R = 0;
		px.G = 0;
		px.B = 0;
		px.A = 255;

		chunks_len = size - padding.Length;
		for (px_pos = 0; px_pos < px_len; px_pos += channels) {
			if (run > 0) {
				run--;
			}
			else if (p < chunks_len) {
				int b1 = bytes[p++];

				if (b1 == QOI_OP_RGB) {
					px.R = bytes[p++];
					px.G = bytes[p++];
					px.B = bytes[p++];
				}
				else if (b1 == QOI_OP_RGBA) {
					px.R = bytes[p++];
					px.G = bytes[p++];
					px.B = bytes[p++];
					px.A = bytes[p++];
				}
				else if ((b1 & QOI_MASK_2) == QOI_OP_INDEX) {
					px = index[b1];
				}
				else if ((b1 & QOI_MASK_2) == QOI_OP_DIFF) {
					px.R = (byte)(px.R + ((b1 >> 4) & 0x03) - 2);
					px.G = (byte)(px.G + ((b1 >> 2) & 0x03) - 2);
					px.B = (byte)(px.B + ( b1       & 0x03) - 2);
				}
				else if ((b1 & QOI_MASK_2) == QOI_OP_LUMA) {
					int b2 = bytes[p++];
					int vg = (b1 & 0x3f) - 32;
					px.R = (byte)(px.R + vg - 8 + ((b2 >> 4) & 0x0f));
					px.G = (byte)(px.G + vg);
					px.B = (byte)(px.B + vg - 8 +  (b2       & 0x0f));
				}
				else if ((b1 & QOI_MASK_2) == QOI_OP_RUN) {
					run = (b1 & 0x3f);
				}

				index[ColorHash(px) % 64] = px;
			}

			pixels[px_pos + 0] = px.R;
			pixels[px_pos + 1] = px.G;
			pixels[px_pos + 2] = px.B;
			
			if (channels == 4) {
				pixels[px_pos + 3] = px.A;
			}
		}

		return pixels;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static unsafe void Write32(byte[] bytes, int* p, uint v)
	{
		bytes[(*p)++] = (byte)((0xff000000 & v) >> 24);
		bytes[(*p)++] = (byte)((0x00ff0000 & v) >> 16);
		bytes[(*p)++] = (byte)((0x0000ff00 & v) >> 8);
		bytes[(*p)++] = (byte)((0x000000ff & v));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static unsafe uint Read32(byte* bytes, int* p)
	{
		uint a = bytes[(*p)++];
		uint b = bytes[(*p)++];
		uint c = bytes[(*p)++];
		uint d = bytes[(*p)++];
		return a << 24 | b << 16 | c << 8 | d;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static unsafe bool Equals(Rgba a, Rgba b)
		=> *((uint*)&a) == *((uint*)&b);

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static int ColorHash(Rgba color)
		=> (color.R * 3 + color.G * 5 + color.B * 7 + color.A * 11);
}