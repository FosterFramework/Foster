using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Floating-Point Line
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Line(Vector2 from, Vector2 to) : IConvexShape, IEquatable<Line>
{
	public Vector2 From = from;
	public Vector2 To = to;

	public readonly int Points => 2;
	public readonly int Axes => 1;

	public readonly Rect Bounds => Rect.Between(From, To);
	public readonly float Length => (To - From).Length();
	public readonly float LengthSquared => (To - From).LengthSquared();
	public readonly Vector2 Normal => (To - From).Normalized();

	public Line(float x1, float y1, float x2, float y2)
		: this(new(x1, y1), new Vector2(x2, y2))
	{

	}

	public readonly Vector2 GetAxis(int index)
	{
		var axis = (To - From).Normalized();
		return new Vector2(axis.Y, -axis.X);
	}

	public readonly Vector2 GetPoint(int index)
		=> index switch
		{
			0 => From,
			1 => To,
			_ => throw new IndexOutOfRangeException()
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2 On(float percent)
		=> Vector2.Lerp(From, To, percent);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2 OnClamped(float percent)
		=> Vector2.Lerp(From, To, Calc.Clamp(percent));

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

	public readonly float ClosestTUnclamped(in Vector2 to)
	{
		var diff = To - From;
		if (diff == Vector2.Zero)
			return 0;
		else
			return Vector2.Dot(to - From, diff) / (diff.X * diff.X + diff.Y * diff.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float ClosestT(in Vector2 to)
		=> Calc.Clamp(ClosestTUnclamped(to));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly Vector2 ClosestPoint(in Vector2 to)
		=> (To - From) * ClosestT(to) + From;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float DistanceSquared(in Vector2 to)
		=> Vector2.DistanceSquared(ClosestPoint(to), to);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float Distance(in Vector2 to)
		=> Vector2.Distance(ClosestPoint(to), to);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Intersects(in Rect rect)
		=> rect.Overlaps(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Intersects(in Circle circle)
		=> circle.Overlaps(this);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Intersects(in Line other)
		=> Intersects(other, out _);

    public readonly bool Intersects(in Line other, out Vector2 point)
    {
		point = default;

		var b = To - From;
		var d = other.To - other.From;
		var bDotDPerp = b.X * d.Y - b.Y * d.X;

		// if b dot d == 0, it means the lines are parallel so have infinite intersection points
		if (bDotDPerp == 0)
			return false;

		var c = other.From - From;
		var t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
		if (t is < 0 or > 1)
			return false;

		var u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
		if (u is < 0 or > 1)
			return false;

		point = From + b * t;
		return true;
    }

	public static Line operator +(Line a, Vector2 b) => new(a.From + b, a.To + b);
	public static Line operator -(Line a, Vector2 b) => new(a.From - b, a.To - b);
	public static bool operator ==(Line left, Line right) => left.Equals(right);
	public static bool operator !=(Line left, Line right) => !(left == right);

	public static implicit operator Line(LineInt l) => new(l.From, l.To);
	public static explicit operator LineInt(Line l) => new((Point2)l.From, (Point2)l.To);

	public bool Equals(Line other) => From.Equals(other.From) && To.Equals(other.To);
	public override bool Equals(object? obj) => obj is Line other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(From, To);
}

