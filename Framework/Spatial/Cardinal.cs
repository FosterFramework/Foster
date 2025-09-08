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
	private const string InvalidStateMessage = "Invalid Cardinal State";

	public const int RightValue = 0;
	public const int DownValue = 1;
	public const int LeftValue = 2;
	public const int UpValue = 3;

	public static readonly Cardinal Right = new(RightValue);
	public static readonly Cardinal Down  = new(DownValue);
	public static readonly Cardinal Left  = new(LeftValue);
	public static readonly Cardinal Up    = new(UpValue);
	public static readonly Cardinal East  = new(RightValue);
	public static readonly Cardinal South = new(DownValue);
	public static readonly Cardinal West  = new(LeftValue);
	public static readonly Cardinal North = new(UpValue);

	public readonly int Value;

	public Cardinal(int val)
	{
		Debug.Assert(val is >= 0 and < 4, InvalidStateMessage);
		Value = val;
	}

	/// <summary>
	/// Get the reverse of the <see cref="Cardinal"/>
	/// </summary>
	public Cardinal Reverse => new ((Value + 2) % 4);

	/// <summary>
	/// Get the <see cref="Cardinal"/> turned right by 90 degrees
	/// </summary>
	public Cardinal TurnRight => new ((Value + 1) % 4);

	/// <summary>
	/// Get the <see cref="Cardinal"/> turned left by 90 degrees
	/// </summary>
	public Cardinal TurnLeft => new ((Value + 3) % 4);

	/// <summary>
	/// Whether the <see cref="Cardinal"/> points along the X-axis (left or right)
	/// </summary>
	public bool Horizontal => Value % 2 == 0;

	/// <summary>
	/// Whether the <see cref="Cardinal"/> points along the Y-axis (up or down)
	/// </summary>
	public bool Vertical => Value % 2 == 1;

	/// <summary>
	/// Get the <see cref="Cardinal"/> as a unit <see cref="Point2"/>
	/// </summary>
	public Point2 Point => new(X, Y);

	/// <summary>
	/// Get the <see cref="Cardinal"/> as a unit <see cref="Vector2"/>
	/// </summary>
	public Vector2 Normal => new(X, Y);

	/// <summary>
	/// Get the X-component of the <see cref="Cardinal"/> as a unit vector
	/// </summary>
	public int X => Value switch
		{
			RightValue => 1,
			LeftValue => -1,
			_ => 0
		};

	/// <summary>
	/// Get the Y-component of the <see cref="Cardinal"/> as a unit vector
	/// </summary>
	public int Y => Value switch
		{
			UpValue => -1,
			DownValue => 1,
			_ => 0
		};

	/// <summary>
	/// The <see cref="Cardinal"/>'s direction represented as radians
	/// </summary>
	public float Angle => Value switch
		{
			RightValue => 0,
			UpValue    => -Calc.HalfPI,
			LeftValue  => Calc.PI,
			DownValue  => Calc.HalfPI,
			_          => throw new Exception(InvalidStateMessage)
		};

	/// <summary>
	/// Get the <see cref="Cardinal"/> in the positive direction on its axis
	/// </summary>
	public Cardinal Abs() => Value switch
	{
		RightValue or LeftValue => Right,
		UpValue or DownValue    => Down,
		_                       => throw new Exception(InvalidStateMessage)
	};

	// TODO: remove once Facing is deleted
#pragma warning disable 0618
	public static implicit operator Cardinal(Facing f) => f.Sign > 0 ? Right : Left;
	public static Cardinal operator *(Cardinal a, Facing b) => b == Facing.Left ? a.Reverse : a;
#pragma warning restore 0618

	public static implicit operator Cardinal(int val) => new(val);
	public static implicit operator Point2(Cardinal c) => c.Point;
	public static bool operator ==(Cardinal a, Cardinal b) => a.Value == b.Value;
	public static bool operator !=(Cardinal a, Cardinal b) => a.Value != b.Value;
	public static Point2 operator *(Cardinal a, int b) => a.Point * b;
	public static Vector2 operator *(Cardinal a, float b) => a.Point * b;
	public static Cardinal operator *(Cardinal a, Signs b) => b == Signs.Positive ? a : a.Reverse;
	public static Cardinal operator -(Cardinal a) => a.Reverse;
	public static Cardinal operator ++(Cardinal c) => new((c.Value + 1) % 4);
	public static Cardinal operator --(Cardinal c) => new ((c.Value + 3) % 4);

	public override int GetHashCode() => Value;
	public override bool Equals(object? obj) => obj is Cardinal c && c == this;
	public bool Equals(Cardinal other) => this == other;

	public override string ToString()
		=> Value switch
		{
			DownValue => "Down",
			LeftValue => "Left",
			UpValue => "Up",
			_ => "Right",
		};

	/// <summary>
	/// Returns a <see cref="Cardinal"/> from the raw integer value (one of <see cref="RightValue"/>, <see cref="LeftValue"/>, <see cref="UpValue"/>, <see cref="DownValue"/>)
	/// </summary>
	public static Cardinal FromRawValue(int v)
	{
		Debug.Assert(v is >= 0 and < 4, InvalidStateMessage);
		return new Cardinal(v);
	}

	/// <summary>
	/// Returns a <see cref="Cardinal"/> from a unit <see cref="Vector2"/>
	/// </summary>
	public static Cardinal FromVector(Vector2 dir)
	{
		if (Math.Abs(dir.X) > Math.Abs(dir.Y))
			return dir.X < 0 ? Left : Right;
		return dir.Y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a <see cref="Cardinal"/> from a unit <see cref="Vector2"/>
	/// </summary>
	public static Cardinal FromVector(float x, float y)
	{
		if (Math.Abs(x) > Math.Abs(y))
			return x < 0 ? Left : Right;
		return y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a <see cref="Cardinal"/> from a unit <see cref="Point2"/>
	/// </summary>
	public static Cardinal FromPoint(Point2 dir)
	{
		if (Math.Abs(dir.X) > Math.Abs(dir.Y))
			return dir.X < 0 ? Left : Right;
		return dir.Y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a <see cref="Cardinal"/> from a unit <see cref="Point2"/>
	/// </summary>
	public static Cardinal FromPoint(int x, int y)
	{
		if (Math.Abs(x) > Math.Abs(y))
			return x < 0 ? Left : Right;
		return y < 0 ? Up : Down;
	}

	/// <summary>
	/// Returns a <see cref="Cardinal"/> from a string value (one of "Right", "Left", "Up", or "Down")
	/// </summary>
	public static Cardinal FromString(string value)
	{
		if (value.Equals("Right", StringComparison.OrdinalIgnoreCase)) return Right;
		if (value.Equals("Left", StringComparison.OrdinalIgnoreCase)) return Left;
		if (value.Equals("Up", StringComparison.OrdinalIgnoreCase)) return Up;
		if (value.Equals("Down", StringComparison.OrdinalIgnoreCase)) return Down;
		return default;
	}

	/// <summary>
	/// Enumerate the possible values of <see cref="Cardinal"/>s, starting with <see cref="Right"/> and proceeding clockwise
	/// </summary>
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
