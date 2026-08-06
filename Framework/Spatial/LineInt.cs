using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Foster.Framework.JsonConverters;

namespace Foster.Framework;

/// <summary>
/// A 2D Integer Line Segment
/// </summary>
[StructLayout(LayoutKind.Sequential), JsonConverter(typeof(JsonConverter))]
public struct LineInt(Point2 from, Point2 to) : IConvexShape, IEquatable<LineInt>
{
	/// <summary>
	/// The First point of the Line
	/// </summary>
	public Point2 From = from;

	/// <summary>
	/// The Second point of the Line
	/// </summary>
	public Point2 To = to;

	/// <summary>
	/// The bounding rectangle of the Line
	/// </summary>
	public readonly RectInt Bounds => RectInt.Between(From, To);

	/// <summary>
	/// The center point of the Line
	/// </summary>
	public readonly Vector2 Center => (From + To) / 2f;

	/// <summary>
	/// The length of the Line
	/// </summary>
	public readonly float Length => (To - From).Length();

	/// <summary>
	/// The length of the line, squared
	/// </summary>
	public readonly float LengthSquared => (To - From).LengthSquared();

	/// <summary>
	/// The normalized vector of the Line direction
	/// </summary>
	public readonly Vector2 Direction => (To - From).Normalized();

	readonly int IConvexShape.Points => 2;
	readonly int IConvexShape.Axes => 1;

	readonly Vector2 IConvexShape.GetAxis(int index)
	{
		var axis = (To - From).Normalized();
		return new Vector2(axis.Y, -axis.X);
	}

	readonly Vector2 IConvexShape.GetPoint(int index)
		=> index switch
		{
			0 => From,
			1 => To,
			_ => throw new IndexOutOfRangeException()
		};

	public readonly void Project(in Vector2 axis, out float min, out float max)
	{
		min = float.MaxValue;
		max = float.MinValue;

		var dot = From.X * axis.X + From.Y * axis.Y;
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = To.X * axis.X + To.Y * axis.Y;
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
	}

	public readonly bool Intersects(in LineInt other)
	{
		Vector2 b = To - From;
		Vector2 d = other.To - other.From;
		float bDotDPerp = b.X * d.Y - b.Y * d.X;

		// if b dot d == 0, it means the lines are parallel so have infinite intersection points
		if (bDotDPerp == 0)
			return false;

		Vector2 c = other.From - From;
		float t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
		if (t < 0 || t > 1)
			return false;

		float u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
		if (u < 0 || u > 1)
			return false;

		return true;
	}

	public override readonly int GetHashCode()
		=> HashCode.Combine(From, To);

	public override readonly bool Equals([NotNullWhen(true)] object? obj)
		=> obj is LineInt other && Equals(other);

	public readonly bool Equals(LineInt other)
		=> From == other.From && To == other.To;

	public static bool operator ==(LineInt left, LineInt right) => left.Equals(right);
	public static bool operator !=(LineInt left, LineInt right) => !(left == right);

	public static LineInt operator +(LineInt a, Point2 b) => new(a.From + b, a.To + b);
	public static LineInt operator -(LineInt a, Point2 b) => new(a.From - b, a.To - b);

	public class JsonConverter()
		: IntVectorJsonConverter<LineInt>([["X1"], ["Y1"], ["X2"], ["Y2"]]);
}
