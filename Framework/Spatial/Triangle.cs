using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public struct Triangle(Vector2 a, Vector2 b, Vector2 c) : IConvexShape
{
	public Vector2 A = a;
	public Vector2 B = b;
	public Vector2 C = c;

	public readonly bool Contains(in Vector2 pt)
		=> Calc.Cross(B - A, pt - A) > 0 && Calc.Cross(C - B, pt - B) > 0 && Calc.Cross(A - C, pt - C) > 0;

	public Vector2 this[int index]
	{
		readonly get => index switch
		{
			0 => A,
			1 => B,
			2 => C,
			_ => throw new IndexOutOfRangeException(),
		};

		set
		{
			switch (index)
			{
			case 0: A = value; break;
			case 1: B = value; break;
			case 2: C = value; break;
			default: throw new IndexOutOfRangeException();
			}
		}
	}

	public readonly Line AB => new(A, B);
	public readonly Line BC => new(B, C);
	public readonly Line CA => new(C, A);

	public static implicit operator Triangle((Vector2 a, Vector2 b, Vector2 c) tuple)
		=> new(tuple.a, tuple.b, tuple.c);

	#region IConvexShape

	public readonly int Points => 3;

	public readonly int Axes => 3;

	public readonly Vector2 GetPoint(int index) => this[index];

	public readonly Vector2 GetAxis(int index)
		=> index switch
		{
			0 => AB.Normal.TurnRight(),
			1 => BC.Normal.TurnRight(),
			2 => CA.Normal.TurnRight(),
			_ => throw new IndexOutOfRangeException(),
		};

	public readonly void Project(in Vector2 axis, out float min, out float max)
	{
		float dotA = Vector2.Dot(A, axis);
		float dotB = Vector2.Dot(B, axis);
		float dotC = Vector2.Dot(C, axis);

		min = Calc.Min(dotA, dotB, dotC);
		max = Calc.Max(dotA, dotB, dotC);
	}

	#endregion
}
