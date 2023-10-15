using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public struct Line : IConvexShape
{
	public Vector2 From;
	public Vector2 To;

	public readonly int Points => 2;
	public readonly int Axis => 1;

	public Line(Vector2 from, Vector2 to)
	{
		From = from;
		To = to;
	}

	public readonly Rect Bounds => new(From, To);
	public readonly float Length() => (To - From).Length();

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

	static public Line operator +(Line a, Vector2 b) => new(a.From + b, a.To + b);
	static public Line operator -(Line a, Vector2 b) => new(a.From - b, a.To - b);
}

