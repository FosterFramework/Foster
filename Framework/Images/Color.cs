using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// Color Data
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
public struct Color : IEquatable<Color>
{
	public static readonly Color Transparent = new(0, 0, 0, 0);
	public static readonly Color White = new(0xffffff);
	public static readonly Color Black = new(0x000000);
	public static readonly Color LightGray = new(0xc0c0c0);
	public static readonly Color Gray = new(0x808080);
	public static readonly Color DarkGray = new(0x404040);
	public static readonly Color Red = new(0xff0000);
	public static readonly Color Green = new(0x00ff00);
	public static readonly Color Blue = new(0x0000ff);
	public static readonly Color Yellow = new(0xffff00);
	public static readonly Color CornflowerBlue = new(0x6495ed);

	public byte R;
	public byte G;
	public byte B;
	public byte A;

	/// <summary>
	/// Gets the Color Value in a RGBA 32-bit unsigned integer
	/// </summary>
	public readonly uint RGBA => ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | (uint)A;

	/// <summary>
	/// The Color Value in a ABGR 32-bit unsigned integer
	/// </summary>
	public readonly uint ABGR => ((uint)A << 24) | ((uint)B << 16) | ((uint)G << 8) | (uint)R;

	/// <summary>
	/// Creates a color given the int32 RGB data
	/// </summary>
	public Color(int rgb, byte alpha = 255)
	{
		R = (byte)(rgb >> 16);
		G = (byte)(rgb >> 8);
		B = (byte)(rgb >> 0);
		A = alpha;
	}

	public Color(int rgb, float alpha)
	{
		R = (byte)((rgb >> 16) * alpha);
		G = (byte)((rgb >> 8) * alpha);
		B = (byte)((rgb >> 0) * alpha);
		A = (byte)(255 * alpha);
	}

	/// <summary>
	/// Creates a color given the uint32 RGBA data
	/// </summary>
	public Color(uint rgba)
	{
		R = (byte)(rgba >> 24);
		G = (byte)(rgba >> 16);
		B = (byte)(rgba >> 08);
		A = (byte)(rgba);
	}

	public Color(byte r, byte g, byte b, byte a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public Color(float r, float g, float b, float a)
	{
		R = (byte)(r * 255);
		G = (byte)(g * 255);
		B = (byte)(b * 255);
		A = (byte)(a * 255);
	}

	public Color(in Vector3 value)
	{
		R = (byte)(value.X * 255);
		G = (byte)(value.Y * 255);
		B = (byte)(value.Z * 255);
		A = 255;
	}

	public Color(in Vector4 value)
	{
		R = (byte)(value.X * 255);
		G = (byte)(value.Y * 255);
		B = (byte)(value.Z * 255);
		A = (byte)(value.W * 255);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color Grayscale(byte r, byte a)
	{
		return new Color(r, r, r, a);
	}

	/// <summary>
	/// Premultiplies the color value based on its Alpha component
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Color Premultiply()
	{
		byte a = A;
		return new Color((byte)(R * a / 255), (byte)(G * a / 255), (byte)(B * a / 255), a);
	}

	/// <summary>
	/// Converts the Color to a Vector4
	/// </summary>
	public readonly Vector4 ToVector4()
		=> new(R / 255f, G / 255f, B / 255f, A / 255f);

	/// <summary>
	/// Converts the Color to a Vector3
	/// </summary>
	public readonly Vector3 ToVector3()
		=> new(R / 255f, G / 255f, B / 255f);

	public override readonly bool Equals(object? obj)
		=> (obj is Color other) && (this == other);

	public readonly bool Equals(Color other)
		=> this == other;

	public override readonly int GetHashCode()
		=> (int)RGBA;

	public override readonly string ToString()
		=> ($"[{R}, {G}, {B}, {A}]");

	/// <summary>
	/// Returns a Hex String representation of the Color's given components
	/// </summary>
	/// <param name="components">The Components, in any order. ex. "RGBA" or "RGB" or "ARGB"</param>
	public readonly void ToHexString(ReadOnlySpan<char> components, Span<char> destination)
	{
		Debug.Assert(destination.Length >= components.Length * 2);

		const string HEX = "0123456789ABCDEF";

		for (int i = 0; i < components.Length; i++)
		{
			switch (components[i])
			{
				case 'R':
				case 'r':
					destination[i * 2 + 0] = HEX[(R & 0xf0) >> 4];
					destination[i * 2 + 1] = HEX[(R & 0x0f)];
					break;
				case 'G':
				case 'g':
					destination[i * 2 + 0] = HEX[(G & 0xf0) >> 4];
					destination[i * 2 + 1] = HEX[(G & 0x0f)];
					break;
				case 'B':
				case 'b':
					destination[i * 2 + 0] = HEX[(B & 0xf0) >> 4];
					destination[i * 2 + 1] = HEX[(B & 0x0f)];
					break;
				case 'A':
				case 'a':
					destination[i * 2 + 0] = HEX[(A & 0xf0) >> 4];
					destination[i * 2 + 1] = HEX[(A & 0x0f)];
					break;
			}
		}
	}

	/// <summary>
	/// Returns a Hex String representation of the Color's given components
	/// </summary>
	/// <param name="components">The Components, in any order. ex. "RGBA" or "RGB" or "ARGB"</param>
	public readonly string ToHexString(ReadOnlySpan<char> components)
	{
		Span<char> dest = stackalloc char[components.Length * 2];
		ToHexString(components, dest);
		return dest.ToString();
	}

	/// <summary>
	/// Returns an RGB Hex string representation of the Color
	/// </summary>
	public readonly string ToHexStringRGB()
	{
		return ToHexString("RGB");
	}

	/// <summary>
	/// Returns an RGBA Hex string representation of the Color
	/// </summary>
	public readonly string ToHexStringRGBA()
	{
		return ToHexString("RGBA");
	}

	/// <summary>
	/// Creates a new Color with the given components from the given string value
	/// </summary>
	/// <param name="components">The components to parse in order, ex. "RGBA"</param>
	/// <param name="value">The Hex value to parse</param>
	public static Color FromHexString(ReadOnlySpan<char> components, ReadOnlySpan<char> value)
	{
		// skip past useless string data (ex. if the string was 0xffffff or #ffffff)
		if (value.Length > 0 && value[0] == '#')
			value = value.Slice(1);
		if (value.Length > 1 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X'))
			value = value.Slice(2);

		var color = Black;

		for (int i = 0; i < components.Length && i * 2 + 2 <= value.Length; i++)
		{
			switch (components[i])
			{
				case 'R':
				case 'r':
					if (byte.TryParse(value.Slice(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r))
						color.R = r;
					break;
				case 'G':
				case 'g':
					if (byte.TryParse(value.Slice(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g))
						color.G = g;
					break;
				case 'B':
				case 'b':
					if (byte.TryParse(value.Slice(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
						color.B = b;
					break;
				case 'A':
				case 'a':
					if (byte.TryParse(value.Slice(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a))
						color.A = a;
					break;
			}
		}

		return color;
	}

	/// <summary>
	/// Creates a new Color from the given RGB Hex value
	/// </summary>
	public static Color FromHexStringRGB(ReadOnlySpan<char> value)
	{
		return FromHexString("RGB", value);
	}

	/// <summary>
	/// Creates a new Color from the given RGBA Hex value
	/// </summary>
	public static Color FromHexStringRGBA(ReadOnlySpan<char> value)
	{
		return FromHexString("RGBA", value);
	}

	/// <summary>
	/// Linearly interpolates between two colors
	/// </summary>
	public static Color Lerp(Color a, Color b, float amount)
	{
		amount = Math.Max(0, Math.Min(1, amount));

		return new Color(
			(byte)(a.R + (b.R - a.R) * amount),
			(byte)(a.G + (b.G - a.G) * amount),
			(byte)(a.B + (b.B - a.B) * amount),
			(byte)(a.A + (b.A - a.A) * amount)
		);
	}

	/// <summary>
	/// Implicitely converts an int32 to a Color, ex 0xffffff
	/// This does not include Alpha values
	/// </summary>
	public static implicit operator Color(int color) => new(color);

	/// <summary>
	/// Implicitely converts an uint32 to a Color, ex 0xffffffff
	/// </summary>
	public static implicit operator Color(uint color) => new(color);

	/// <summary>
	/// Multiplies a Color by a scaler
	/// </summary>
	public static Color operator *(Color value, float scaler)
	{
		return new Color(
			(byte)(value.R * scaler),
			(byte)(value.G * scaler),
			(byte)(value.B * scaler),
			(byte)(value.A * scaler)
		);
	}

	public static bool operator ==(Color a, Color b) => a.RGBA == b.RGBA;
	public static bool operator !=(Color a, Color b) => a.RGBA != b.RGBA;
	public static implicit operator Color(Vector4 vec) => new Color(vec.X, vec.Y, vec.Z, vec.W);
	public static implicit operator Color(Vector3 vec) => new Color(vec.X, vec.Y, vec.Z, 1.0f);
	public static implicit operator Vector4(Color col) => col.ToVector4();
	public static implicit operator Vector3(Color col) => col.ToVector3();
}
