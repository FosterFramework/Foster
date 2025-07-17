using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// 8-bit RGBA Color struct
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4), JsonConverter(typeof(JsonConverter))]
public struct Color : IEquatable<Color>
{
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
	/// <param name="destination">The destination to write the string to</param>
	public readonly void ToHexString(ReadOnlySpan<char> components, Span<char> destination)
	{
		if (destination.Length < components.Length * 2)
			throw new ArgumentOutOfRangeException(nameof(destination));

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
	/// Converts the Color to HSV values, where each resulting component is a value from 0 to 1
	/// </summary>
	public readonly (float H, float S, float V) ToHSV()
	{
		var value = ToVector4();
		var min = MathF.Min(value.X, MathF.Min(value.Y, value.Z));
		var max = MathF.Max(value.X, MathF.Max(value.Y, value.Z));
		var delta = max - min;

		(float H, float S, float V) result = (0f, 0f, max);

		if (delta <= 0 || max <= 0)
			return result;

		result.S = delta / max;

		if (value.X >= max)
			result.H = (value.Y - value.Z) / delta;
		else if (value.Y >= max)
			result.H = 2.0f + (value.Z - value.X) / delta;
		else
			result.H = 4.0f + (value.X - value.Y) / delta;

		result.H /= 6.0f;
		if (result.H < 0)
			result.H += 1.0f;

		return result;
	}

	/// <summary>
	/// Creates a Color value from HSV, where each component is a value from 0 to 1
	/// </summary>
	public static Color FromHSV(float h, float s, float v)
	{
		var hueSection = Math.Clamp(h, 0, 1) * 6;
		var hueIndex = (int)hueSection;
		var hueRemainder = hueSection - hueIndex;

		var a = v * (1f - s);
		var b = v * (1f - (s * hueRemainder));
		var c = v * (1f - (s * (1f - hueRemainder)));

		return hueIndex switch
		{
			0 => new(v, c, a, 1.0f),
			1 => new(b, v, c, 1.0f),
			2 => new(a, v, c, 1.0f),
			3 => new(a, b, v, 1.0f),
			4 => new(c, a, v, 1.0f),
			_ => new(v, a, b, 1.0f)
		};
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

	public class JsonConverter : JsonConverter<Color>
	{
		public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
				return FromHexStringRGBA(reader.GetString() ?? string.Empty);
			return new();
		}

		public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToHexStringRGBA());
	}

	#region Built In Colors (mostly from CSS3 standard colors)

	public static readonly Color Transparent = new(0, 0, 0, 0);
	
	public static readonly Color White = new(0xFFFFFF);
	public static readonly Color Black = new(0x000000);
	public static readonly Color LightGray = new(0xC0C0C0);
	public static readonly Color Gray = new(0x808080);
	public static readonly Color DarkGray = new(0x404040);
	public static readonly Color Red = new(0xFF0000);
	public static readonly Color Green = new(0x00FF00);
	public static readonly Color Blue = new(0x0000FF);
	public static readonly Color Yellow = new(0xFFFF00);
	public static readonly Color Magenta = new(0xFF00FF);
	public static readonly Color Cyan = new(0x00FFFF);

	public static readonly Color AliceBlue = new(0xF0F8FF);
	public static readonly Color AntiqueWhite = new(0xFAEBD7);
	public static readonly Color AquaMarine = new(0x7FFFD4);
	public static readonly Color Azure = new(0xF0FFFF);
	public static readonly Color Aqua = new(0x05c3dd);
	public static readonly Color Beige = new(0xF5F5DC);
	public static readonly Color Bisque = new(0xFFE4C4);
	public static readonly Color BlanchedAlmond = new(0xFFEBCD);
	public static readonly Color BlueViolet = new(0x8A2BE2);
	public static readonly Color Brown = new(0xA52A2A);
	public static readonly Color BurlyWood = new(0xDEB887);
	public static readonly Color CadetBlue = new(0x5F9EA0);
	public static readonly Color Chartreuse = new(0x7FFF00);
	public static readonly Color Chocolate = new(0xD2691E);
	public static readonly Color Coral = new(0xFF7F50);
	public static readonly Color CornflowerBlue = new(0x6495ed);
	public static readonly Color Cornsilk = new(0xFFF8DC);
	public static readonly Color Crimson = new(0xDC143C);
	public static readonly Color DarkBlue = new(0x00008B);
	public static readonly Color DarkCyan = new(0x008B8B);
	public static readonly Color DarkGoldenrod = new(0xB8860B);
	public static readonly Color DarkGreen = new(0x006400);
	public static readonly Color DarkGrey = new(0xA9A9A9);
	public static readonly Color DarkKhaki = new(0xBDB76B);
	public static readonly Color DarkMagenta = new(0x8B008B);
	public static readonly Color DarkOliveGreen = new(0x556B2F);
	public static readonly Color DarkOrange = new(0xFF8C00);
	public static readonly Color DarkOrchid = new(0x9932CC);
	public static readonly Color DarkRed = new(0x8B0000);
	public static readonly Color DarkSalmon = new(0xE9967A);
	public static readonly Color DarkSeaGreen = new(0x8FBC8F);
	public static readonly Color DarkSlateBlue = new(0x483D8B);
	public static readonly Color DarkSlateGray = new(0x2F4F4F);
	public static readonly Color DarkTurquoise = new(0x00CED1);
	public static readonly Color DarkViolet = new(0x9400D3);
	public static readonly Color DeepPink = new(0xFF1493);
	public static readonly Color DeepSkyBlue = new(0x00BFFF);
	public static readonly Color DimGray = new(0x696969);
	public static readonly Color DodgerBlue = new(0x1E90FF);
	public static readonly Color Firebrick = new(0xB22222);
	public static readonly Color FloralWhite = new(0xFFFAF0);
	public static readonly Color ForestGreen = new(0x228B22);
	public static readonly Color Fuchsia = new(0xFF00FF);
	public static readonly Color Gainsboro = new(0xDCDCDC);
	public static readonly Color GhostWhite = new(0xF8F8FF);
	public static readonly Color Gold = new(0xFFD700);
	public static readonly Color Goldenrod = new(0xDAA520);
	public static readonly Color Greenyellow = new(0xADFF2F);
	public static readonly Color Honeydew = new(0xF0FFF0);
	public static readonly Color HotPink = new(0xFF69B4);
	public static readonly Color IndianRed = new(0xCD5C5C);
	public static readonly Color Indigo = new(0x4B0082);
	public static readonly Color Ivory = new(0xFFFFF0);
	public static readonly Color Khaki = new(0xF0E68C);
	public static readonly Color Lavender = new(0xE6E6FA);
	public static readonly Color LavenderBlush = new(0xFFF0F5);
	public static readonly Color LawnGreen = new(0x7CFC00);
	public static readonly Color LemonChiffon = new(0xFFFACD);
	public static readonly Color LightBlue = new(0xADD8E6);
	public static readonly Color LightCoral = new(0xF08080);
	public static readonly Color LightCyan = new(0xE0FFFF);
	public static readonly Color LightGoldenrodYellow = new(0xFAFAD2);
	public static readonly Color LightGreen = new(0x90EE90);
	public static readonly Color LightPink = new(0xFFB6C1);
	public static readonly Color LightSalmon = new(0xFFA07A);
	public static readonly Color LightSeaGreen = new(0x20B2AA);
	public static readonly Color LightSkyBlue = new(0x87CEFA);
	public static readonly Color LightSlateGray = new(0x778899);
	public static readonly Color LightSteelBlue = new(0xB0C4DE);
	public static readonly Color LightYellow = new(0xFFFFE0);
	public static readonly Color LimeGreen = new(0x32CD32);
	public static readonly Color Linen = new(0xFAF0E6);
	public static readonly Color Maroon = new(0x800000);
	public static readonly Color MediumAquamarine = new(0x66CDAA);
	public static readonly Color MediumBlue = new(0x0000CD);
	public static readonly Color MediumOrchid = new(0xBA55D3);
	public static readonly Color MediumPurple = new(0x9370DB);
	public static readonly Color MediumSeaGreen = new(0x3CB371);
	public static readonly Color MediumSlateBlue = new(0x7B68EE);
	public static readonly Color MediumSpringGreen = new(0x00FA9A);
	public static readonly Color MediumTurquoise = new(0x48D1CC);
	public static readonly Color MediumVioletRed = new(0xC71585);
	public static readonly Color MidnightBlue = new(0x191970);
	public static readonly Color MintCream = new(0xF5FFFA);
	public static readonly Color MistyRose = new(0xFFE4E1);
	public static readonly Color Moccasin = new(0xFFE4B5);
	public static readonly Color NavajoWhite = new(0xFFDEAD);
	public static readonly Color Navy = new(0x000080);
	public static readonly Color OldLace = new(0xFDF5E6);
	public static readonly Color Olive = new(0x808000);
	public static readonly Color OliveDrab = new(0x6B8E23);
	public static readonly Color Orange = new(0xFFA500);
	public static readonly Color OrangeRed = new(0xFF4500);
	public static readonly Color Orchid = new(0xDA70D6);
	public static readonly Color PaleGoldenrod = new(0xEEE8AA);
	public static readonly Color PaleGreen = new(0x98FD98);
	public static readonly Color PaleTurquoise = new(0xAFEEEE);
	public static readonly Color PaleVioletRed = new(0xDB7093);
	public static readonly Color PapayaWhip = new(0xFFEFD5);
	public static readonly Color Peachpuff = new(0xFFDAB9);
	public static readonly Color Peru = new(0xCD853F);
	public static readonly Color Pink = new(0xFFC0CD);
	public static readonly Color Plum = new(0xDDA0DD);
	public static readonly Color PowderBlue = new(0xB0E0E6);
	public static readonly Color Purple = new(0x800080);
	public static readonly Color RosyBrown = new(0xBC8F8F);
	public static readonly Color RoyalBlue = new(0x4169E1);
	public static readonly Color Salmon = new(0xFA8072);
	public static readonly Color SandyBrown = new(0xF4A460);
	public static readonly Color SeaGreen = new(0x2E8B57);
	public static readonly Color Seashell = new(0xFFF5EE);
	public static readonly Color Sienna = new(0xA0522D);
	public static readonly Color Silver = new(0xC0C0C0);
	public static readonly Color SkyBlue = new(0x87CEEB);
	public static readonly Color SlateBlue = new(0x6A5ACD);
	public static readonly Color SlateGray = new(0x708090);
	public static readonly Color Snow = new(0xFFFAFA);
	public static readonly Color SpringGreen = new(0x00FF7F);
	public static readonly Color SteelBlue = new(0x4682B4);
	public static readonly Color Tan = new(0xD2B48C);
	public static readonly Color Teal = new(0x008080);
	public static readonly Color Thistle = new(0xD8BFD8);
	public static readonly Color Tomato = new(0xFF6347);
	public static readonly Color Turquoise = new(0x40E0D0);
	public static readonly Color SaddleBrown = new(0x8B4513);
	public static readonly Color Violet = new(0xEE82EE);
	public static readonly Color Wheat = new(0xF5DEB3);
	public static readonly Color WhiteSmoke = new(0xF5F5F5);
	public static readonly Color YellowGreen = new(0x9ACD3);

	#endregion
}
