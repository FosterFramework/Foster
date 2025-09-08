using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// A binary struct, where Left is any negative value and Right is zero or any positive number
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public readonly struct Signs(bool positive) : IEquatable<Signs>
{
	public static readonly Signs Positive = default;
	public static readonly Signs Negative = new(false);

	/// <summary>
	/// Internal value. We store whether its negative (rather than whether its positive) so that the default value of false makes us positive
	/// </summary>
	private readonly bool negative = !positive;

	/// <summary>
	/// Returns -1 for <see cref="Negative"/>, 1 for <see cref="Positive"/>
	/// </summary>
	public int AsInt => negative ? -1 : 1;

	/// <summary>
	/// The opposite of our value
	/// </summary>
	public Signs Negate => new(negative);

	/// <summary>
	/// Integers convert to <see cref="Negative"/> if negative, otherwise (including 0) give <see cref="Positive"/>
	/// </summary>
	public static implicit operator Signs(int v) => v < 0 ? Negative : Positive;

	/// <summary>
	/// Floats convert to <see cref="Negative"/> if negative, otherwise (including 0) give <see cref="Positive"/>
	/// </summary>
	public static explicit operator Signs(float f) => f < 0 ? Negative : Positive;

	/// <summary>
	/// Returns -1 for <see cref="Negative"/>, 1 for <see cref="Positive"/>
	/// </summary>
	public static implicit operator int(Signs f) => f.AsInt;

	/// <summary>
	/// Cast to a unit <see cref="Point2"/> in our direction
	/// </summary>
	public static explicit operator Point2(Signs f) => Point2.UnitX * f.AsInt;

	/// <summary>
	/// Cast to a unit <see cref="Vector2"/> in our direction
	/// </summary>
	public static explicit operator Vector2(Signs f) => Vector2.UnitX * f.AsInt;

	/// <summary>
	/// Converts a <see cref="String"/> to a <see cref="Signs"/> value
	/// </summary>
	public static Signs Parse(string? value)
	{
		if (value != null && value.Equals("-", StringComparison.OrdinalIgnoreCase))
			return Negative;
		return Positive;
	}

	/// <summary>
	/// Convert the integer to a <see cref="Signs"/> value; if <paramref name="value"/> is zero, <paramref name="ifZero"/> is returned
	/// </summary>
	public static Signs FromInt(int value, Signs ifZero = default)
		=> value == 0 ? ifZero : value;

	/// <summary>
	/// Convert the float to a <see cref="Signs"/> value; if <paramref name="value"/> is zero, <paramref name="ifZero"/> is returned
	/// </summary>
	public static Signs FromFloat(float value, Signs ifZero = default)
		=> value == 0 ? ifZero : (Signs)value;

	/// <summary>
	/// Get the <see cref="Signs"/> as a normalized <see cref="Point2"/> where the sign is represented as the x-axis
	/// </summary>
	public Point2 NormalX => new(AsInt, 0);

	/// <summary>
	/// Get the <see cref="Signs"/> as a normalized <see cref="Point2"/> where the sign is represented as the y-axis
	/// </summary>
	public Point2 NormalY => new(0, AsInt);

	/// <summary>
	/// Get the <see cref="Signs"/> as a <see cref="Point2"/> where X is the sign and Y is 1.
	/// This is useful for multiplying by a <see cref="Point2"/> or <see cref="Vector2"/> to apply the sign to only its x-axis
	/// </summary>
	public Point2 SignX => new(AsInt, 1);

	/// <summary>
	/// Get the <see cref="Signs"/> as a <see cref="Point2"/> where Y is the sign and X is 1.
	/// This is useful for multiplying by a <see cref="Point2"/> or <see cref="Vector2"/> to apply the sign to only its y-axis
	/// </summary>
	public Point2 SignY => new(1, AsInt);

	/// <summary>
	/// Get the <see cref="Signs"/> as a <see cref="Cardinal"/> on the x-axis (positive = right, negative = left)
	/// </summary>
	public Cardinal CardinalX => Cardinal.Right * this;

	/// <summary>
	/// Get the <see cref="Signs"/> as a <see cref="Cardinal"/> on the y-axis (positive = down, negative = up)
	/// </summary>
	public Cardinal CardinalY => Cardinal.Down * this;

	// TODO: remove once Facing is deleted
#pragma warning disable 0618
	public static implicit operator Facing(Signs sign) => new(sign.AsInt);
	public static implicit operator Signs(Facing facing) => new(facing.Sign >= 0);
#pragma warning restore 0618

	public static Signs operator-(Signs a) => a.Negate;
	public static Signs operator+(Signs a) => a;
	public static bool operator ==(Signs a, Signs b) => a.AsInt == b.AsInt;
	public static bool operator !=(Signs a, Signs b) => a.AsInt != b.AsInt;
	public static int operator *(Signs a, int b) => (int)a * b;
	public static int operator *(int a, Signs b) => a * (int)b;
	public static float operator *(Signs a, float b) => (int)a * b;
	public static float operator *(float a, Signs b) => a * (int)b;
	public static Point2 operator*(Point2 point, Signs facing) => point * facing.AsInt;
	public static Point2 operator*(Signs facing, Point2 point) => point * facing.AsInt;
	public static Vector2 operator*(Vector2 vec, Signs facing) => vec * facing.AsInt;
	public static Vector2 operator*(Signs facing, Vector2 vec) => vec * facing.AsInt;

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator ==(Signs a, int b) => a.AsInt == Math.Sign(b);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator !=(Signs a, int b) => a.AsInt != Math.Sign(b);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator ==(int a, Signs b) => b.AsInt == Math.Sign(a);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator !=(int a, Signs b) => b.AsInt != Math.Sign(a);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator ==(Signs a, float b) => a.AsInt == Math.Sign(b);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator !=(Signs a, float b) => a.AsInt != Math.Sign(b);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator ==(float a, Signs b) => b.AsInt == Math.Sign(a);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator !=(float a, Signs b) => b.AsInt != Math.Sign(a);

	public override int GetHashCode() => AsInt;
	public override bool Equals(object? obj) => obj is Signs f && f == this;
	public bool Equals(Signs other) => this == other;
	public override string ToString() => negative ? "-" : "+";

	public class JsonConverter : JsonConverter<Signs>
	{
		public override Signs Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var asInt))
				return new(asInt >= 0);
			else if (reader.TokenType == JsonTokenType.String)
				return Parse(reader.GetString()!);
			return default;
		}

		public override void Write(Utf8JsonWriter writer, Signs value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}
