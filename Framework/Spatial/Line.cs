using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public struct Line(Vector2 from, Vector2 to) : IConvexShape
{
	public Vector2 From = from;
	public Vector2 To = to;

	public readonly int Points => 2;
	public readonly int Axes => 1;

	public readonly Rect Bounds => new(From, To);
	public readonly float Length => (To - From).Length();
	public readonly Vector2 Normal => (To - From).Normalized();

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

	public readonly Vector2 ClosestPoint(in Vector2 to)
	{
		var diff = To - From;
		if (diff.X == 0 && diff.Y == 0)
			return From;

		var w = to - From;

		var t = Vector2.Dot(w, diff) / (diff.X * diff.X + diff.Y * diff.Y);
		if (t < 0)
			t = 0;
		else if (t > 1)
			t = 1;

		return diff * t + From;
	}

	public readonly float DistanceSquared(in Vector2 to)
		=> Vector2.DistanceSquared(ClosestPoint(to), to);

	public readonly float Distance(in Vector2 to)
		=> Vector2.Distance(ClosestPoint(to), to);

	public readonly bool Intersects(in Rect rect)
		=> rect.Overlaps(this);

	public readonly bool Intersects(in Circle circle)
		=> circle.Overlaps(this);

	public readonly bool Intersects(in Line other)
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

	static public Line operator +(Line a, Vector2 b) => new(a.From + b, a.To + b);
	static public Line operator -(Line a, Vector2 b) => new(a.From - b, a.To - b);
}

