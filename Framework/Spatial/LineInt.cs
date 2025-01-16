using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 2D Integer Line
/// </summary>
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

	static public LineInt operator +(LineInt a, Point2 b) => new(a.From + b, a.To + b);
	static public LineInt operator -(LineInt a, Point2 b) => new(a.From - b, a.To - b);
}
