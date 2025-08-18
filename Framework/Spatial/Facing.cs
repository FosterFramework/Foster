using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// A binary struct, where Left is any negative value and Right is zero or any positive number
/// </summary>
[Obsolete($"Use {nameof(Signs)} instead")]
[JsonConverter(typeof(JsonConverter))]
public readonly struct Facing(int val) : IEquatable<Facing>
{
	public static readonly Facing Right = new(1);
	public static readonly Facing Left = new(-1);

	private readonly int value = val < 0 ? -1 : 1;

	/// <summary>
	/// Returns -1 if Left, or +1 if Right
	/// </summary>
	public int Sign => value < 0 ? -1 : 1;

	/// <summary>
	/// The opposite of our value
	/// </summary>
	public Facing Reverse => new(-value);

	/// <summary>
	/// Integers convert to Left if negative, otherwise Right
	/// </summary>
	public static implicit operator Facing(int v) => v < 0 ? Left : Right;

	/// <summary>
	/// Floats convert to Left if negative, otherwise Right
	/// </summary>
	public static explicit operator Facing(float f) => f < 0 ? Left : Right;

	/// <summary>
	/// -1 for Left, 1 for Right
	/// </summary>
	public static implicit operator int(Facing f) => f.Sign;

	/// <summary>
	/// Cast to a unit Point2 in our direction
	/// </summary>
	public static explicit operator Point2(Facing f) => Point2.UnitX * f.Sign;

	/// <summary>
	/// Cast to a unit Vector2 in our direction
	/// </summary>
	public static explicit operator Vector2(Facing f) => Vector2.UnitX * f.Sign;

	/// <summary>
	/// Converts a String to a Facing value
	/// </summary>
	public static Facing FromString(string? value)
	{
		if (value != null && value.Equals("Left", StringComparison.OrdinalIgnoreCase))
			return Left;
		return Right;
	}

	/// <summary>
	/// Convert the integer to a Facing value; if <paramref name="value"/> is zero, <paramref name="ifZero"/> is returned
	/// </summary>
	public static Facing FromInt(int value, Facing ifZero)
		=> value == 0 ? ifZero : value;

	public static Facing FromFloat(float value, Facing ifZero)
		=> value == 0 ? ifZero : (Facing)value;

	public static Facing operator-(Facing a) => a.Reverse;
	public static Facing operator+(Facing a) => a;
	public static bool operator ==(Facing a, Facing b) => a.Sign == b.Sign;
	public static bool operator !=(Facing a, Facing b) => a.Sign != b.Sign;
	public static int operator *(Facing a, int b) => (int)a * b;
	public static int operator *(int a, Facing b) => a * (int)b;

	public static Point2 operator *(Point2 point, Facing facing) => new(point.X * facing.Sign, point.Y);
	public static Point2 operator *(Facing facing, Point2 point) => new(point.X * facing.Sign, point.Y);
	public static Vector2 operator *(Vector2 vec, Facing facing) => new(vec.X * facing.Sign, vec.Y);
	public static Vector2 operator *(Facing facing, Vector2 vec) => new(vec.X * facing.Sign, vec.Y);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(Facing a, int b) => b == 0 || a.Sign == Math.Sign(b);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(Facing a, int b) => b != 0 && a.Sign != Math.Sign(b);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(int a, Facing b) => a == 0 || b.Sign == Math.Sign(a);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(int a, Facing b) => a != 0 && b.Sign != Math.Sign(a);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(Facing a, float b) => b == 0 || a.Sign == Math.Sign(b);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(Facing a, float b) => b != 0 && a.Sign != Math.Sign(b);

	/// <summary>
	/// Check if the facing equals the sign of the number. If the number is zero, true is always returned
	/// </summary>
	public static bool operator ==(float a, Facing b) => a == 0 || b.Sign == Math.Sign(a);

	/// <summary>
	/// Check if the facing does not equal the sign of the number. If the number is zero, false is always returned
	/// </summary>
	public static bool operator !=(float a, Facing b) => a != 0 && b.Sign != Math.Sign(a);

	public override int GetHashCode() => Sign;
	public override bool Equals(object? obj) =>
		obj is Facing f && f == this;

	public bool Equals(Facing other) => this == other;

	public override string ToString() => value < 0 ? "Left" : "Right";

	public class JsonConverter : JsonConverter<Facing>
	{
		public override Facing Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var asInt))
				return new(asInt);
			else if (reader.TokenType == JsonTokenType.String)
				return FromString(reader.GetString()!);
			return default;
		}

		public override void Write(Utf8JsonWriter writer, Facing value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}
