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
	/// Get the point with its X multiplied by the sign, and its Y untouched
	/// </summary>
	public Point2 SignX(Point2 point) => point with { X = point.X * AsInt };

	/// <summary>
	/// Get the point with its Y multiplied by the sign, and its X untouched
	/// </summary>
	public Point2 SignY(Point2 point) => point with { X = point.Y * AsInt };

	/// <summary>
	/// Get the vector with its X multiplied by the sign, and its Y untouched
	/// </summary>
	public Vector2 SignX(Vector2 vec) => vec with { X = vec.X * AsInt };

	/// <summary>
	/// Get the vector with its Y multiplied by the sign, and its X untouched
	/// </summary>
	public Vector2 SignY(Vector2 vec) => vec with { Y = vec.Y * AsInt };

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
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(Signs a, int b) => b == 0 || a.AsInt == Math.Sign(b);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(Signs a, int b) => b != 0 && a.AsInt != Math.Sign(b);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(int a, Signs b) => a == 0 || b.AsInt == Math.Sign(a);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(int a, Signs b) => a != 0 && b.AsInt != Math.Sign(a);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(Signs a, float b) => b == 0 || a.AsInt == Math.Sign(b);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(Signs a, float b) => b != 0 && a.AsInt != Math.Sign(b);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(float a, Signs b) => a == 0 || b.AsInt == Math.Sign(a);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(float a, Signs b) => a != 0 && b.AsInt != Math.Sign(a);

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
