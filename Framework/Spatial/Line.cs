using System;
using System.Numerics;

namespace Foster.Framework;

public struct Line : IConvexShape
{
	public Vector2 From;
	public Vector2 To;

	public int Points => 2;
	public int Axis => 1;

	public Line(Vector2 from, Vector2 to)
	{
		From = from;
		To = to;
	}

	public Rect Bounds => new Rect(From, To);
	public float Length() => (To - From).Length();

	public Vector2 GetAxis(int index)
	{
		var axis = (To - From).Normalized();
		return new Vector2(axis.Y, -axis.X);
	}

	public Vector2 GetPoint(int index)
	{
		return index switch
		{
			0 => From,
			1 => To,
			_ => throw new IndexOutOfRangeException()
		};
	}

	public void Project(in Vector2 axis, out float min, out float max)
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

	static public Line operator +(Line a, Vector2 b)
	{
		return new Line(a.From + b, a.To + b);
	}

	static public Line operator -(Line a, Vector2 b)
	{
		return new Line(a.From - b, a.To - b);
	}
}

