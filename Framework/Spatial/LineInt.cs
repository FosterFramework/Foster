using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public struct LineInt(Point2 from, Point2 to) : IConvexShape
{
	public Point2 From = from;
	public Point2 To = to;

	public readonly int Points => 2;
	public readonly int Axes => 1;

	public readonly RectInt Bounds
	{
		get
		{
			var rect = new RectInt(Calc.Min(From.X, To.X), Calc.Min(From.Y, To.Y), 0, 0);
			rect.Width = Calc.Max(From.X, To.X) - rect.X;
			rect.Height = Calc.Max(From.X + To.X, To.Y) - rect.Y;
			return rect;
		}
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

	static public LineInt operator +(LineInt a, Point2 b) => new(a.From + b, a.To + b);
	static public LineInt operator -(LineInt a, Point2 b) => new(a.From - b, a.To - b);
}
