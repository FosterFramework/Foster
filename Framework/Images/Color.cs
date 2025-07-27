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
	/// <summary>
	/// Red Component
	/// </summary>
	public byte R;

	/// <summary>
	/// Green Component
	/// </summary>
	public byte G;

	/// <summary>
	/// Blue Component
	/// </summary>
	public byte B;

	/// <summary>
	/// Alpha Component
	/// </summary>
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

	/// <summary>
	/// Transparent Color with all components set to 0
	/// </summary>
	public static readonly Color Transparent = new(0, 0, 0, 0);
	/// <summary>
	/// White Color with a Hex value of #FFFFFF
	/// </summary>
	public static readonly Color White = new(0xFFFFFF);
	/// <summary>
	/// Black Color with a Hex value of #000000
	/// </summary>
	public static readonly Color Black = new(0x000000);
	/// <summary>
	/// LightGray Color with a Hex value of #C0C0C0
	/// </summary>
	public static readonly Color LightGray = new(0xC0C0C0);
	/// <summary>
	/// Gray Color with a Hex value of #808080
	/// </summary>
	public static readonly Color Gray = new(0x808080);
	/// <summary>
	/// DarkGray Color with a Hex value of #404040
	/// </summary>
	public static readonly Color DarkGray = new(0x404040);
	/// <summary>
	/// Red Color with a Hex value of #FF0000
	/// </summary>
	public static readonly Color Red = new(0xFF0000);
	/// <summary>
	/// Green Color with a Hex value of #00FF00
	/// </summary>
	public static readonly Color Green = new(0x00FF00);
	/// <summary>
	/// Blue Color with a Hex value of #0000FF
	/// </summary>
	public static readonly Color Blue = new(0x0000FF);
	/// <summary>
	/// Yellow Color with a Hex value of #FFFF00
	/// </summary>
	public static readonly Color Yellow = new(0xFFFF00);
	/// <summary>
	/// Magenta Color with a Hex value of #FF00FF
	/// </summary>
	public static readonly Color Magenta = new(0xFF00FF);
	/// <summary>
	/// Cyan Color with a Hex value of #00FFFF
	/// </summary>
	public static readonly Color Cyan = new(0x00FFFF);
	/// <summary>
	/// AliceBlue Color with a Hex value of #F0F8FF
	/// </summary>
	public static readonly Color AliceBlue = new(0xF0F8FF);
	/// <summary>
	/// AntiqueWhite Color with a Hex value of #FAEBD7
	/// </summary>
	public static readonly Color AntiqueWhite = new(0xFAEBD7);
	/// <summary>
	/// AquaMarine Color with a Hex value of #7FFFD4
	/// </summary>
	public static readonly Color AquaMarine = new(0x7FFFD4);
	/// <summary>
	/// Azure Color with a Hex value of #F0FFFF
	/// </summary>
	public static readonly Color Azure = new(0xF0FFFF);
	/// <summary>
	/// Aqua Color with a Hex value of #05c3dd
	/// </summary>
	public static readonly Color Aqua = new(0x05c3dd);
	/// <summary>
	/// Beige Color with a Hex value of #F5F5DC
	/// </summary>
	public static readonly Color Beige = new(0xF5F5DC);
	/// <summary>
	/// Bisque Color with a Hex value of #FFE4C4
	/// </summary>
	public static readonly Color Bisque = new(0xFFE4C4);
	/// <summary>
	/// BlanchedAlmond Color with a Hex value of #FFEBCD
	/// </summary>
	public static readonly Color BlanchedAlmond = new(0xFFEBCD);
	/// <summary>
	/// BlueViolet Color with a Hex value of #8A2BE2
	/// </summary>
	public static readonly Color BlueViolet = new(0x8A2BE2);
	/// <summary>
	/// Brown Color with a Hex value of #A52A2A
	/// </summary>
	public static readonly Color Brown = new(0xA52A2A);
	/// <summary>
	/// BurlyWood Color with a Hex value of #DEB887
	/// </summary>
	public static readonly Color BurlyWood = new(0xDEB887);
	/// <summary>
	/// CadetBlue Color with a Hex value of #5F9EA0
	/// </summary>
	public static readonly Color CadetBlue = new(0x5F9EA0);
	/// <summary>
	/// Chartreuse Color with a Hex value of #7FFF00
	/// </summary>
	public static readonly Color Chartreuse = new(0x7FFF00);
	/// <summary>
	/// Chocolate Color with a Hex value of #D2691E
	/// </summary>
	public static readonly Color Chocolate = new(0xD2691E);
	/// <summary>
	/// Coral Color with a Hex value of #FF7F50
	/// </summary>
	public static readonly Color Coral = new(0xFF7F50);
	/// <summary>
	/// CornflowerBlue Color with a Hex value of #6495ed
	/// </summary>
	public static readonly Color CornflowerBlue = new(0x6495ed);
	/// <summary>
	/// Cornsilk Color with a Hex value of #FFF8DC
	/// </summary>
	public static readonly Color Cornsilk = new(0xFFF8DC);
	/// <summary>
	/// Crimson Color with a Hex value of #DC143C
	/// </summary>
	public static readonly Color Crimson = new(0xDC143C);
	/// <summary>
	/// DarkBlue Color with a Hex value of #00008B
	/// </summary>
	public static readonly Color DarkBlue = new(0x00008B);
	/// <summary>
	/// DarkCyan Color with a Hex value of #008B8B
	/// </summary>
	public static readonly Color DarkCyan = new(0x008B8B);
	/// <summary>
	/// DarkGoldenrod Color with a Hex value of #B8860B
	/// </summary>
	public static readonly Color DarkGoldenrod = new(0xB8860B);
	/// <summary>
	/// DarkGreen Color with a Hex value of #006400
	/// </summary>
	public static readonly Color DarkGreen = new(0x006400);
	/// <summary>
	/// DarkKhaki Color with a Hex value of #BDB76B
	/// </summary>
	public static readonly Color DarkKhaki = new(0xBDB76B);
	/// <summary>
	/// DarkMagenta Color with a Hex value of #8B008B
	/// </summary>
	public static readonly Color DarkMagenta = new(0x8B008B);
	/// <summary>
	/// DarkOliveGreen Color with a Hex value of #556B2F
	/// </summary>
	public static readonly Color DarkOliveGreen = new(0x556B2F);
	/// <summary>
	/// DarkOrange Color with a Hex value of #FF8C00
	/// </summary>
	public static readonly Color DarkOrange = new(0xFF8C00);
	/// <summary>
	/// DarkOrchid Color with a Hex value of #9932CC
	/// </summary>
	public static readonly Color DarkOrchid = new(0x9932CC);
	/// <summary>
	/// DarkRed Color with a Hex value of #8B0000
	/// </summary>
	public static readonly Color DarkRed = new(0x8B0000);
	/// <summary>
	/// DarkSalmon Color with a Hex value of #E9967A
	/// </summary>
	public static readonly Color DarkSalmon = new(0xE9967A);
	/// <summary>
	/// DarkSeaGreen Color with a Hex value of #8FBC8F
	/// </summary>
	public static readonly Color DarkSeaGreen = new(0x8FBC8F);
	/// <summary>
	/// DarkSlateBlue Color with a Hex value of #483D8B
	/// </summary>
	public static readonly Color DarkSlateBlue = new(0x483D8B);
	/// <summary>
	/// DarkSlateGray Color with a Hex value of #2F4F4F
	/// </summary>
	public static readonly Color DarkSlateGray = new(0x2F4F4F);
	/// <summary>
	/// DarkTurquoise Color with a Hex value of #00CED1
	/// </summary>
	public static readonly Color DarkTurquoise = new(0x00CED1);
	/// <summary>
	/// DarkViolet Color with a Hex value of #9400D3
	/// </summary>
	public static readonly Color DarkViolet = new(0x9400D3);
	/// <summary>
	/// DeepPink Color with a Hex value of #FF1493
	/// </summary>
	public static readonly Color DeepPink = new(0xFF1493);
	/// <summary>
	/// DeepSkyBlue Color with a Hex value of #00BFFF
	/// </summary>
	public static readonly Color DeepSkyBlue = new(0x00BFFF);
	/// <summary>
	/// DimGray Color with a Hex value of #696969
	/// </summary>
	public static readonly Color DimGray = new(0x696969);
	/// <summary>
	/// DodgerBlue Color with a Hex value of #1E90FF
	/// </summary>
	public static readonly Color DodgerBlue = new(0x1E90FF);
	/// <summary>
	/// Firebrick Color with a Hex value of #B22222
	/// </summary>
	public static readonly Color Firebrick = new(0xB22222);
	/// <summary>
	/// FloralWhite Color with a Hex value of #FFFAF0
	/// </summary>
	public static readonly Color FloralWhite = new(0xFFFAF0);
	/// <summary>
	/// ForestGreen Color with a Hex value of #228B22
	/// </summary>
	public static readonly Color ForestGreen = new(0x228B22);
	/// <summary>
	/// Fuchsia Color with a Hex value of #FF00FF
	/// </summary>
	public static readonly Color Fuchsia = new(0xFF00FF);
	/// <summary>
	/// Gainsboro Color with a Hex value of #DCDCDC
	/// </summary>
	public static readonly Color Gainsboro = new(0xDCDCDC);
	/// <summary>
	/// GhostWhite Color with a Hex value of #F8F8FF
	/// </summary>
	public static readonly Color GhostWhite = new(0xF8F8FF);
	/// <summary>
	/// Gold Color with a Hex value of #FFD700
	/// </summary>
	public static readonly Color Gold = new(0xFFD700);
	/// <summary>
	/// Goldenrod Color with a Hex value of #DAA520
	/// </summary>
	public static readonly Color Goldenrod = new(0xDAA520);
	/// <summary>
	/// Greenyellow Color with a Hex value of #ADFF2F
	/// </summary>
	public static readonly Color GreenYellow = new(0xADFF2F);
	/// <summary>
	/// Honeydew Color with a Hex value of #F0FFF0
	/// </summary>
	public static readonly Color Honeydew = new(0xF0FFF0);
	/// <summary>
	/// HotPink Color with a Hex value of #FF69B4
	/// </summary>
	public static readonly Color HotPink = new(0xFF69B4);
	/// <summary>
	/// IndianRed Color with a Hex value of #CD5C5C
	/// </summary>
	public static readonly Color IndianRed = new(0xCD5C5C);
	/// <summary>
	/// Indigo Color with a Hex value of #4B0082
	/// </summary>
	public static readonly Color Indigo = new(0x4B0082);
	/// <summary>
	/// Ivory Color with a Hex value of #FFFFF0
	/// </summary>
	public static readonly Color Ivory = new(0xFFFFF0);
	/// <summary>
	/// Khaki Color with a Hex value of #F0E68C
	/// </summary>
	public static readonly Color Khaki = new(0xF0E68C);
	/// <summary>
	/// Lavender Color with a Hex value of #E6E6FA
	/// </summary>
	public static readonly Color Lavender = new(0xE6E6FA);
	/// <summary>
	/// LavenderBlush Color with a Hex value of #FFF0F5
	/// </summary>
	public static readonly Color LavenderBlush = new(0xFFF0F5);
	/// <summary>
	/// LawnGreen Color with a Hex value of #7CFC00
	/// </summary>
	public static readonly Color LawnGreen = new(0x7CFC00);
	/// <summary>
	/// LemonChiffon Color with a Hex value of #FFFACD
	/// </summary>
	public static readonly Color LemonChiffon = new(0xFFFACD);
	/// <summary>
	/// LightBlue Color with a Hex value of #ADD8E6
	/// </summary>
	public static readonly Color LightBlue = new(0xADD8E6);
	/// <summary>
	/// LightCoral Color with a Hex value of #F08080
	/// </summary>
	public static readonly Color LightCoral = new(0xF08080);
	/// <summary>
	/// LightCyan Color with a Hex value of #E0FFFF
	/// </summary>
	public static readonly Color LightCyan = new(0xE0FFFF);
	/// <summary>
	/// LightGoldenrodYellow Color with a Hex value of #FAFAD2
	/// </summary>
	public static readonly Color LightGoldenrodYellow = new(0xFAFAD2);
	/// <summary>
	/// LightGreen Color with a Hex value of #90EE90
	/// </summary>
	public static readonly Color LightGreen = new(0x90EE90);
	/// <summary>
	/// LightPink Color with a Hex value of #FFB6C1
	/// </summary>
	public static readonly Color LightPink = new(0xFFB6C1);
	/// <summary>
	/// LightSalmon Color with a Hex value of #FFA07A
	/// </summary>
	public static readonly Color LightSalmon = new(0xFFA07A);
	/// <summary>
	/// LightSeaGreen Color with a Hex value of #20B2AA
	/// </summary>
	public static readonly Color LightSeaGreen = new(0x20B2AA);
	/// <summary>
	/// LightSkyBlue Color with a Hex value of #87CEFA
	/// </summary>
	public static readonly Color LightSkyBlue = new(0x87CEFA);
	/// <summary>
	/// LightSlateGray Color with a Hex value of #778899
	/// </summary>
	public static readonly Color LightSlateGray = new(0x778899);
	/// <summary>
	/// LightSteelBlue Color with a Hex value of #B0C4DE
	/// </summary>
	public static readonly Color LightSteelBlue = new(0xB0C4DE);
	/// <summary>
	/// LightYellow Color with a Hex value of #FFFFE0
	/// </summary>
	public static readonly Color LightYellow = new(0xFFFFE0);
	/// <summary>
	/// LimeGreen Color with a Hex value of #32CD32
	/// </summary>
	public static readonly Color LimeGreen = new(0x32CD32);
	/// <summary>
	/// Linen Color with a Hex value of #FAF0E6
	/// </summary>
	public static readonly Color Linen = new(0xFAF0E6);
	/// <summary>
	/// Maroon Color with a Hex value of #800000
	/// </summary>
	public static readonly Color Maroon = new(0x800000);
	/// <summary>
	/// MediumAquamarine Color with a Hex value of #66CDAA
	/// </summary>
	public static readonly Color MediumAquamarine = new(0x66CDAA);
	/// <summary>
	/// MediumBlue Color with a Hex value of #0000CD
	/// </summary>
	public static readonly Color MediumBlue = new(0x0000CD);
	/// <summary>
	/// MediumOrchid Color with a Hex value of #BA55D3
	/// </summary>
	public static readonly Color MediumOrchid = new(0xBA55D3);
	/// <summary>
	/// MediumPurple Color with a Hex value of #9370DB
	/// </summary>
	public static readonly Color MediumPurple = new(0x9370DB);
	/// <summary>
	/// MediumSeaGreen Color with a Hex value of #3CB371
	/// </summary>
	public static readonly Color MediumSeaGreen = new(0x3CB371);
	/// <summary>
	/// MediumSlateBlue Color with a Hex value of #7B68EE
	/// </summary>
	public static readonly Color MediumSlateBlue = new(0x7B68EE);
	/// <summary>
	/// MediumSpringGreen Color with a Hex value of #00FA9A
	/// </summary>
	public static readonly Color MediumSpringGreen = new(0x00FA9A);
	/// <summary>
	/// MediumTurquoise Color with a Hex value of #48D1CC
	/// </summary>
	public static readonly Color MediumTurquoise = new(0x48D1CC);
	/// <summary>
	/// MediumVioletRed Color with a Hex value of #C71585
	/// </summary>
	public static readonly Color MediumVioletRed = new(0xC71585);
	/// <summary>
	/// MidnightBlue Color with a Hex value of #191970
	/// </summary>
	public static readonly Color MidnightBlue = new(0x191970);
	/// <summary>
	/// MintCream Color with a Hex value of #F5FFFA
	/// </summary>
	public static readonly Color MintCream = new(0xF5FFFA);
	/// <summary>
	/// MistyRose Color with a Hex value of #FFE4E1
	/// </summary>
	public static readonly Color MistyRose = new(0xFFE4E1);
	/// <summary>
	/// Moccasin Color with a Hex value of #FFE4B5
	/// </summary>
	public static readonly Color Moccasin = new(0xFFE4B5);
	/// <summary>
	/// NavajoWhite Color with a Hex value of #FFDEAD
	/// </summary>
	public static readonly Color NavajoWhite = new(0xFFDEAD);
	/// <summary>
	/// Navy Color with a Hex value of #000080
	/// </summary>
	public static readonly Color Navy = new(0x000080);
	/// <summary>
	/// OldLace Color with a Hex value of #FDF5E6
	/// </summary>
	public static readonly Color OldLace = new(0xFDF5E6);
	/// <summary>
	/// Olive Color with a Hex value of #808000
	/// </summary>
	public static readonly Color Olive = new(0x808000);
	/// <summary>
	/// OliveDrab Color with a Hex value of #6B8E23
	/// </summary>
	public static readonly Color OliveDrab = new(0x6B8E23);
	/// <summary>
	/// Orange Color with a Hex value of #FFA500
	/// </summary>
	public static readonly Color Orange = new(0xFFA500);
	/// <summary>
	/// OrangeRed Color with a Hex value of #FF4500
	/// </summary>
	public static readonly Color OrangeRed = new(0xFF4500);
	/// <summary>
	/// Orchid Color with a Hex value of #DA70D6
	/// </summary>
	public static readonly Color Orchid = new(0xDA70D6);
	/// <summary>
	/// PaleGoldenrod Color with a Hex value of #EEE8AA
	/// </summary>
	public static readonly Color PaleGoldenrod = new(0xEEE8AA);
	/// <summary>
	/// PaleGreen Color with a Hex value of #98FD98
	/// </summary>
	public static readonly Color PaleGreen = new(0x98FD98);
	/// <summary>
	/// PaleTurquoise Color with a Hex value of #AFEEEE
	/// </summary>
	public static readonly Color PaleTurquoise = new(0xAFEEEE);
	/// <summary>
	/// PaleVioletRed Color with a Hex value of #DB7093
	/// </summary>
	public static readonly Color PaleVioletRed = new(0xDB7093);
	/// <summary>
	/// PapayaWhip Color with a Hex value of #FFEFD5
	/// </summary>
	public static readonly Color PapayaWhip = new(0xFFEFD5);
	/// <summary>
	/// Peachpuff Color with a Hex value of #FFDAB9
	/// </summary>
	public static readonly Color Peachpuff = new(0xFFDAB9);
	/// <summary>
	/// Peru Color with a Hex value of #CD853F
	/// </summary>
	public static readonly Color Peru = new(0xCD853F);
	/// <summary>
	/// Pink Color with a Hex value of #FFC0CD
	/// </summary>
	public static readonly Color Pink = new(0xFFC0CD);
	/// <summary>
	/// Plum Color with a Hex value of #DDA0DD
	/// </summary>
	public static readonly Color Plum = new(0xDDA0DD);
	/// <summary>
	/// PowderBlue Color with a Hex value of #B0E0E6
	/// </summary>
	public static readonly Color PowderBlue = new(0xB0E0E6);
	/// <summary>
	/// Purple Color with a Hex value of #800080
	/// </summary>
	public static readonly Color Purple = new(0x800080);
	/// <summary>
	/// RosyBrown Color with a Hex value of #BC8F8F
	/// </summary>
	public static readonly Color RosyBrown = new(0xBC8F8F);
	/// <summary>
	/// RoyalBlue Color with a Hex value of #4169E1
	/// </summary>
	public static readonly Color RoyalBlue = new(0x4169E1);
	/// <summary>
	/// Salmon Color with a Hex value of #FA8072
	/// </summary>
	public static readonly Color Salmon = new(0xFA8072);
	/// <summary>
	/// SandyBrown Color with a Hex value of #F4A460
	/// </summary>
	public static readonly Color SandyBrown = new(0xF4A460);
	/// <summary>
	/// SeaGreen Color with a Hex value of #2E8B57
	/// </summary>
	public static readonly Color SeaGreen = new(0x2E8B57);
	/// <summary>
	/// Seashell Color with a Hex value of #FFF5EE
	/// </summary>
	public static readonly Color Seashell = new(0xFFF5EE);
	/// <summary>
	/// Sienna Color with a Hex value of #A0522D
	/// </summary>
	public static readonly Color Sienna = new(0xA0522D);
	/// <summary>
	/// Silver Color with a Hex value of #C0C0C0
	/// </summary>
	public static readonly Color Silver = new(0xC0C0C0);
	/// <summary>
	/// SkyBlue Color with a Hex value of #87CEEB
	/// </summary>
	public static readonly Color SkyBlue = new(0x87CEEB);
	/// <summary>
	/// SlateBlue Color with a Hex value of #6A5ACD
	/// </summary>
	public static readonly Color SlateBlue = new(0x6A5ACD);
	/// <summary>
	/// SlateGray Color with a Hex value of #708090
	/// </summary>
	public static readonly Color SlateGray = new(0x708090);
	/// <summary>
	/// Snow Color with a Hex value of #FFFAFA
	/// </summary>
	public static readonly Color Snow = new(0xFFFAFA);
	/// <summary>
	/// SpringGreen Color with a Hex value of #00FF7F
	/// </summary>
	public static readonly Color SpringGreen = new(0x00FF7F);
	/// <summary>
	/// SteelBlue Color with a Hex value of #4682B4
	/// </summary>
	public static readonly Color SteelBlue = new(0x4682B4);
	/// <summary>
	/// Tan Color with a Hex value of #D2B48C
	/// </summary>
	public static readonly Color Tan = new(0xD2B48C);
	/// <summary>
	/// Teal Color with a Hex value of #008080
	/// </summary>
	public static readonly Color Teal = new(0x008080);
	/// <summary>
	/// Thistle Color with a Hex value of #D8BFD8
	/// </summary>
	public static readonly Color Thistle = new(0xD8BFD8);
	/// <summary>
	/// Tomato Color with a Hex value of #FF6347
	/// </summary>
	public static readonly Color Tomato = new(0xFF6347);
	/// <summary>
	/// Turquoise Color with a Hex value of #40E0D0
	/// </summary>
	public static readonly Color Turquoise = new(0x40E0D0);
	/// <summary>
	/// SaddleBrown Color with a Hex value of #8B4513
	/// </summary>
	public static readonly Color SaddleBrown = new(0x8B4513);
	/// <summary>
	/// Violet Color with a Hex value of #EE82EE
	/// </summary>
	public static readonly Color Violet = new(0xEE82EE);
	/// <summary>
	/// Wheat Color with a Hex value of #F5DEB3
	/// </summary>
	public static readonly Color Wheat = new(0xF5DEB3);
	/// <summary>
	/// WhiteSmoke Color with a Hex value of #F5F5F5
	/// </summary>
	public static readonly Color WhiteSmoke = new(0xF5F5F5);
	/// <summary>
	/// YellowGreen Color with a Hex value of #9ACD3
	/// </summary>
	public static readonly Color YellowGreen = new(0x9ACD3);

	#endregion
}
