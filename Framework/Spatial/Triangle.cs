using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public struct Triangle
{
	public Vector2 A;
	public Vector2 B;
	public Vector2 C;

	public Triangle(Vector2 a, Vector2 b, Vector2 c)
	{
		A = a;
		B = b;
		C = c;
	}

	public readonly bool Contains(in Vector2 pt)
		=> Calc.Cross(B - A, pt - A) > 0 && Calc.Cross(C - B, pt - B) > 0 && Calc.Cross(A - C, pt - C) > 0;

	public readonly Line AB => new(A, B);
	public readonly Line BC => new(B, C);
	public readonly Line CA => new(C, A);


	public static implicit operator Triangle((Vector2 a, Vector2 b, Vector2 c) tuple)
		=> new(tuple.a, tuple.b, tuple.c);
}
