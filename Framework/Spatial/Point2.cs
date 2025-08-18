using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Foster.Framework;

/// <summary>
/// A 2D Integer Point
/// </summary>
[StructLayout(LayoutKind.Sequential), JsonConverter(typeof(JsonConverter))]
public struct Point2(int x, int y) : IEquatable<Point2>
{
	public static readonly Point2 Zero = new(0, 0);
	public static readonly Point2 UnitX = new(1, 0);
	public static readonly Point2 UnitY = new(0, 1);
	public static readonly Point2 One = new(1, 1);
	public static readonly Point2 Right = new(1, 0);
	public static readonly Point2 Left = new(-1, 0);
	public static readonly Point2 Up = new(0, -1);
	public static readonly Point2 Down = new(0, 1);

	/// <summary>
	/// The X component of the <see cref="Point2"/>
	/// </summary>
	[JsonInclude] public int X = x;

	/// <summary>
	/// The Y component of the <see cref="Point2"/>
	/// </summary>
	[JsonInclude] public int Y = y;

	/// <summary>
	/// Constructs a 0,0 <see cref="Point2"/>
	/// </summary>
	public Point2()
		: this(0, 0) {}

	/// <summary>
	/// Gets the length of the <see cref="Point2"/>
	/// </summary>
	public readonly float Length() => new Vector2(X, Y).Length();

	/// <summary>
	/// Gets the length squared of the <see cref="Point2"/>
	/// </summary>
	public readonly float LengthSquared() => new Vector2(X, Y).LengthSquared();

	/// <summary>
	/// Gets the normalized <see cref="Vector2"/> of the Point
	/// </summary>
	public readonly Vector2 Normalized() => new Vector2(X, Y).Normalized();

	/// <summary>
	/// Floors both axes of the <see cref="Point2"/> to the given interval
	/// </summary>
	public readonly Point2 FloorTo(int interval) => (this / interval) * interval;

	/// <summary>
	/// Floors both axes of the <see cref="Point2"/> to the given intervals
	/// </summary>
	public readonly Point2 FloorTo(in Point2 intervals)
		=> new((X / intervals.X) * intervals.X, (Y / intervals.Y) * intervals.Y);

	/// <summary>
	/// Rounds both axes of the <see cref="Point2"/> to the given interval
	/// </summary>
	public readonly Point2 RoundTo(int interval) => (this / (float)interval).RoundToPoint2() * interval;

	/// <summary>
	/// Rounds both axes of the <see cref="Point2"/> to the given intervals
	/// </summary>
	public readonly Point2 RoundTo(in Point2 intervals)
		=> new(Calc.Round(X / (float)intervals.X) * intervals.X, Calc.Round(Y / (float)intervals.Y) * intervals.Y);

	/// <summary>
	/// Returns a <see cref="Point2"/> with the X-value of this <see cref="Point2"/>, but zero Y
	/// </summary>
	public readonly Point2 OnlyX() => new(X, 0);

	/// <summary>
	/// Returns a <see cref="Point2"/> with the Y-value of this <see cref="Point2"/>, but zero X
	/// </summary>
	public readonly Point2 OnlyY() => new(0, Y);

	/// <summary>
	/// Turns a <see cref="Point2"/>  to its right perpendicular
	/// </summary>
	public readonly Point2 TurnRight() => new(-Y, X);

	/// <summary>
	/// Turns a <see cref="Point2"/>  to its left perpendicular
	/// </summary>
	public readonly Point2 TurnLeft() => new(Y, -X);

	/// <summary>
	/// Returns a <see cref="Point2"/>  with the sign of both components
	/// </summary>
	public readonly Point2 Sign() => new(Math.Sign(X), Math.Sign(Y));

	/// <summary>
	/// Returns a <see cref="Point2"/> with the absolute value of both components
	/// </summary>
	public readonly Point2 Abs() => new(Math.Abs(X), Math.Abs(Y));

	/// <summary>
	/// Clamps the <see cref="Point2"/> inside the provided range.
	/// </summary>
	public readonly Point2 Clamp(in Point2 min, in Point2 max) =>
		new(Calc.Clamp(X, min.X, max.X), Calc.Clamp(Y, min.Y, max.Y));

	/// <summary>
	/// Clamps the <see cref="Point2"/> inside the bounding rectangle.
	/// </summary>
	public readonly Point2 Clamp(in RectInt bounds) =>
		Clamp(bounds.TopLeft, bounds.BottomRight);

	/// <summary>
	/// Convert the <see cref="Point2"/> into a tuple of integers
	/// </summary>
	public readonly (int X, int Y) Deconstruct() => (X, Y);

	/// <summary>
	/// Convert the <see cref="Point2"/> into a tuple of integers
	/// </summary>
	public readonly void Deconstruct(out int x, out int y) => (x, y) = (X, Y);

	public readonly override bool Equals(object? obj) => (obj is Point2 other) && (other == this);
	public readonly bool Equals(Point2 other) => (X == other.X && Y == other.Y);
	public readonly override int GetHashCode() => HashCode.Combine(X, Y);
	public readonly override string ToString() => $"[{X}, {Y}]";

	public static Point2 Min(Point2 a, Point2 b) => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
	public static Point2 Min(Point2 a, Point2 b, Point2 c) => new(Calc.Min(a.X, b.X, c.X), Calc.Min(a.Y, b.Y, c.Y));
	public static Point2 Min(Point2 a, Point2 b, Point2 c, Point2 d) => new(Calc.Min(a.X, b.X, c.X, d.X), Calc.Min(a.Y, b.Y, c.Y, d.Y));

	public static Point2 Max(Point2 a, Point2 b) => new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
	public static Point2 Max(Point2 a, Point2 b, Point2 c) => new(Calc.Max(a.X, b.X, c.X), Calc.Max(a.Y, b.Y, c.Y));
	public static Point2 Max(Point2 a, Point2 b, Point2 c, Point2 d) => new(Calc.Max(a.X, b.X, c.X, d.X), Calc.Max(a.Y, b.Y, c.Y, d.Y));

	public static int ManhattanDist(in Point2 a, in Point2 b) =>
		Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

	public static Point2 FromBools(bool left, bool right, bool up, bool down)
		=> (left, right, up, down) switch
		{
			(_, true, _, _) => Right,
			(_, _, true, _) => Up,
			(true, _, _, _) => Left,
			(_, _, _, true) => Down,
			_ => Zero,
		};

	public static implicit operator Point2((int X, int Y) tuple) => new(tuple.X, tuple.Y);

	public static Point2 operator -(Point2 point) => new(-point.X, -point.Y);
	public static Point2 operator +(Point2 point) => point;
	public static Point2 operator /(Point2 point, int scaler) => new(point.X / scaler, point.Y / scaler);
	public static Point2 operator *(Point2 point, int scaler) => new(point.X * scaler, point.Y * scaler);
	public static Point2 operator %(Point2 point, int scaler) => new(point.X % scaler, point.Y % scaler);

	public static Vector2 operator /(Point2 point, float scaler) => new(point.X / scaler, point.Y / scaler);
	public static Vector2 operator *(Point2 point, float scaler) => new(point.X * scaler, point.Y * scaler);
	public static Vector2 operator %(Point2 point, float scaler) => new(point.X % scaler, point.Y % scaler);

	public static Point2 operator /(Point2 a, Point2 b) => new(a.X / b.X, a.Y / b.Y);
	public static Point2 operator *(Point2 a, Point2 b) => new(a.X * b.X, a.Y * b.Y);
	public static Point2 operator %(Point2 a, Point2 b) => new(a.X % b.X, a.Y % b.Y);

	public static Vector2 operator /(Point2 point, Vector2 vector) => new(point.X / vector.X, point.Y / vector.Y);
	public static Vector2 operator *(Point2 point, Vector2 vector) => new(point.X * vector.X, point.Y * vector.Y);
	public static Vector2 operator %(Point2 point, Vector2 vector) => new(point.X % vector.X, point.Y % vector.Y);

	public static Point2 operator +(Point2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);
	public static Point2 operator -(Point2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);

	public static Vector2 operator +(Point2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
	public static Vector2 operator -(Point2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
	public static Vector2 operator +(Vector2 a, Point2 b) => new(a.X + b.X, a.Y + b.Y);
	public static Vector2 operator -(Vector2 a, Point2 b) => new(a.X - b.X, a.Y - b.Y);

	public static Rect operator +(Rect a, Point2 b) => new(b.X + a.X, b.Y + a.Y, a.Width, a.Height);
	public static Rect operator +(Point2 a, Rect b) => new(b.X + a.X, b.Y + a.Y, b.Width, b.Height);
	public static Rect operator -(Rect a, Point2 b) => new(a.X - b.X, a.Y - b.Y, a.Width, a.Height);

	public static bool operator ==(Point2 a, Point2 b) => a.X == b.X && a.Y == b.Y;
	public static bool operator !=(Point2 a, Point2 b) => a.X != b.X || a.Y != b.Y;

	public static explicit operator Point2(Vector2 vector) => new((int)vector.X, (int)vector.Y);
	public static implicit operator Vector2(Point2 point) => new(point.X, point.Y);

	public class JsonConverter : JsonConverter<Point2>
	{
		public override Point2 ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var span = (reader.GetString() ?? string.Empty).AsSpan();
			var index = span.IndexOf(',');
			if (index >= 0)
			{
				var x = span[..index];
				var y = span[(index + 1)..];

				if (int.TryParse(x, out var px) && int.TryParse(y, out var py))
					return new(px, py);
			}

			return Zero;
		}

		public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Point2 value, JsonSerializerOptions options)
			=> writer.WritePropertyName($"{value.X},{value.Y}");

		public override Point2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Point2 value = new();
			if (reader.TokenType != JsonTokenType.StartObject)
				return value;

			while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
			{
				if (reader.TokenType != JsonTokenType.PropertyName)
					continue;

				var component = reader.ValueSpan;
				if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				{
					reader.Skip();
					continue;
				}

				if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "x"u8))
					value.X = reader.GetInt32();
				else if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "y"u8))
					value.Y = reader.GetInt32();
				else
					reader.Skip();
			}

			return value;
		}

		public override void Write(Utf8JsonWriter writer, Point2 value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteNumber("X", value.X);
			writer.WriteNumber("Y", value.Y);
			writer.WriteEndObject();
		}
	}
}
