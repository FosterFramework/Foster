using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// A 2D 4-directional struct representing the Cardinal Directions.
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public readonly struct Cardinal : IEquatable<Cardinal>
{
	public const int RightValue = 0;
	public const int DownValue = 1;
	public const int LeftValue = 2;
	public const int UpValue = 3;

	public static readonly Cardinal Right = new (RightValue);
	public static readonly Cardinal Down = new (DownValue);
	public static readonly Cardinal Left = new (LeftValue);
	public static readonly Cardinal Up = new (UpValue);

	public readonly int Value;

	public Cardinal(int val)
	{
		Debug.Assert(val >= 0 && val < 4);
		Value = val;
	}

	public Cardinal Reverse => new ((Value + 2) % 4);
	public Cardinal TurnRight => new ((Value + 1) % 4);
	public Cardinal TurnLeft => new ((Value + 3) % 4);

	public bool Horizontal => Value % 2 == 0;
	public bool Vertical => Value % 2 == 1;
	public Point2 Point => new(X, Y);

	public int X => Value switch
		{
			RightValue => 1,
			LeftValue => -1,
			_ => 0
		};

	public int Y => Value switch
		{
			UpValue => -1,
			DownValue => 1,
			_ => 0
		};

	/// <summary>
	/// The cardinal's direction represented as radians
	/// </summary>
	public float Angle => Value switch
		{
			RightValue => 0,
			UpValue => -Calc.HalfPI,
			LeftValue => Calc.PI,
			DownValue => Calc.HalfPI,
			_ => throw new Exception()
		};

	public static implicit operator Cardinal(Facing f) => f.Sign > 0 ? Right : Left;
	public static implicit operator Point2(Cardinal c) => c.Point;
	public static bool operator ==(Cardinal a, Cardinal b) => a.Value == b.Value;
	public static bool operator !=(Cardinal a, Cardinal b) => a.Value != b.Value;
	public static Point2 operator *(Cardinal a, int b) => a.Point * b;
	public static Vector2 operator *(Cardinal a, float b) => a.Point * b;
	public static Cardinal operator -(Cardinal a) => a.Reverse;

	public override int GetHashCode() => Value;

	public override bool Equals(object? obj)
		=> obj is Cardinal c && c == this;

	public override string ToString()
		=> Value switch
		{
			DownValue => "Down",
			LeftValue => "Left",
			UpValue => "Up",
			_ => "Right",
		};

	/// <summary>
	/// Returns a Cardinal from the raw integer value (one of <see cref="RightValue"/>, <see cref="LeftValue"/>, <see cref="UpValue"/>, <see cref="DownValue"/>)
	/// </summary>
	public static Cardinal FromRawValue(int v)
	{
		Debug.Assert(v >= 0 && v < 4, "Argument out of range");
		return new Cardinal(v);
	}

	/// <summary>
	/// Returns a Cardinal from a 2D Vector Normal
	/// </summary>
	public static Cardinal FromVector(Vector2 dir)
	{
		if (Math.Abs(dir.X) > Math.Abs(dir.Y))
			return dir.X < 0 ? Left : Right;
		return dir.Y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a Cardinal from a 2D Vector Normal
	/// </summary>
	public static Cardinal FromVector(float x, float y)
	{
		if (Math.Abs(x) > Math.Abs(y))
			return x < 0 ? Left : Right;
		return y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a Cardinal from a 2D Point Normal
	/// </summary>
	public static Cardinal FromPoint(Point2 dir)
	{
		if (Math.Abs(dir.X) > Math.Abs(dir.Y))
			return dir.X < 0 ? Left : Right;
		return dir.Y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a Cardinal from a 2D Point Normal
	/// </summary>
	public static Cardinal FromPoint(int x, int y)
	{
		if (Math.Abs(x) > Math.Abs(y))
			return x < 0 ? Left : Right;
		return y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a Cardinal from a string value (one of "Right", "Left", "Up", or "Down")
	/// </summary>
	public static Cardinal FromString(string value)
	{
		if (value.Equals("Right", StringComparison.OrdinalIgnoreCase)) return Right;
		if (value.Equals("Left", StringComparison.OrdinalIgnoreCase)) return Left;
		if (value.Equals("Up", StringComparison.OrdinalIgnoreCase)) return Up;
		if (value.Equals("Down", StringComparison.OrdinalIgnoreCase)) return Down;
		return default;
	}

	public bool Equals(Cardinal other) => this == other;

	public static IEnumerable<Cardinal> All
	{
		get
		{
			yield return Right;
			yield return Down;
			yield return Left;
			yield return Up;
		}
	}

	public static Cardinal operator++(Cardinal c) => new((c.Value + 1) % 4);
	public static Cardinal operator --(Cardinal c) => new ((c.Value + 3) % 4);

	public class JsonConverter : JsonConverter<Cardinal>
	{
		public override Cardinal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var asInt))
				return FromRawValue(asInt);
			else if (reader.TokenType == JsonTokenType.String)
				return FromString(reader.GetString()!);
			return default;
		}

		public override void Write(Utf8JsonWriter writer, Cardinal value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
}
