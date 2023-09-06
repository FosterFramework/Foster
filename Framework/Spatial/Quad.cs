using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Quad
/// </summary>
public struct Quad : IConvexShape
{
	private Vector2 a;
	private Vector2 b;
	private Vector2 c;
	private Vector2 d;
	private Vector2 normalAB;
	private Vector2 normalBC;
	private Vector2 normalCD;
	private Vector2 normalDA;
	private bool dirty;

	public Vector2 A
	{
		get => a;
		set
		{
			if (a != value)
			{
				a = value;
				dirty = true;
			}
		}
	}

	public Vector2 B
	{
		get => b;
		set
		{
			if (b != value)
			{
				b = value;
				dirty = true;
			}
		}
	}

	public Vector2 C
	{
		get => c;
		set
		{
			if (c != value)
			{
				c = value;
				dirty = true;
			}
		}
	}

	public Vector2 D
	{
		get => d;
		set
		{
			if (d != value)
			{
				d = value;
				dirty = true;
			}
		}
	}

	public Vector2 NormalAB
	{
		get
		{
			if (dirty)
				UpdateQuad();
			return normalAB;
		}
	}

	public Vector2 NormalBC
	{
		get
		{
			if (dirty)
				UpdateQuad();
			return normalBC;
		}
	}

	public Vector2 NormalCD
	{
		get
		{
			if (dirty)
				UpdateQuad();
			return normalCD;
		}
	}

	public Vector2 NormalDA
	{
		get
		{
			if (dirty)
				UpdateQuad();
			return normalDA;
		}
	}

	public Vector2 Center => (a + b + c + d) / 4f;


	public Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
		normalAB = normalBC = normalCD = normalDA = Vector2.Zero;
		dirty = true;
	}

	private void UpdateQuad()
	{
		normalAB = (b - a).Normalized();
		normalAB = new Vector2(-normalAB.Y, normalAB.X);
		normalBC = (c - b).Normalized();
		normalBC = new Vector2(-normalBC.Y, normalBC.X);
		normalCD = (d - c).Normalized();
		normalCD = new Vector2(-normalCD.Y, normalCD.X);
		normalDA = (a - d).Normalized();
		normalDA = new Vector2(-normalDA.Y, normalDA.X);

		dirty = false;
	}

	public Quad Translate(in Vector2 amount)
	{
		A += amount;
		B += amount;
		C += amount;
		D += amount;
		return this;
	}

	public void Project(in Vector2 axis, out float min, out float max)
	{
		min = float.MaxValue;
		max = float.MinValue;

		var dot = Vector2.Dot(A, axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = Vector2.Dot(B, axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = Vector2.Dot(C, axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
		dot = Vector2.Dot(D, axis);
		min = Math.Min(dot, min);
		max = Math.Max(dot, max);
	}

	public int Points => 4;

	public Vector2 GetPoint(int index)
	{
		return index switch
		{
			0 => A,
			1 => B,
			2 => C,
			3 => D,
			_ => throw new IndexOutOfRangeException(),
		};
	}

	public int Axis => 4;

	public Vector2 GetAxis(int index)
	{
		return index switch
		{
			0 => new Vector2(-normalAB.Y, normalAB.X),
			1 => new Vector2(-normalBC.Y, normalBC.X),
			2 => new Vector2(-normalCD.Y, normalCD.X),
			3 => new Vector2(-normalDA.Y, normalDA.X),
			_ => throw new IndexOutOfRangeException(),
		};
	}

	public Rect BoundingRect()
	{
		var bounds = new Rect();
		bounds.X = Math.Min(a.X, Math.Min(b.X, Math.Min(c.X, d.X)));
		bounds.Y = Math.Min(a.Y, Math.Min(b.Y, Math.Min(c.Y, d.Y)));
		bounds.Width = Math.Max(a.X, Math.Max(b.X, Math.Max(c.X, d.X))) - bounds.X;
		bounds.Height = Math.Max(a.Y, Math.Max(b.Y, Math.Max(c.Y, d.Y))) - bounds.Y;
		return bounds;
	}

	public override bool Equals(object? obj)
		=> (obj is Quad other) && (this == other);

	public override int GetHashCode()
		=> HashCode.Combine(a, b, c, d);

	public static Quad Transform(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Matrix3x2 matrix)
	{
		return new Quad(
			Vector2.Transform(a, matrix),
			Vector2.Transform(b, matrix),
			Vector2.Transform(c, matrix),
			Vector2.Transform(d, matrix));
	}

	public static Quad Transform(Quad quad, Matrix3x2 matrix)
	{
		return new Quad(
			Vector2.Transform(quad.a, matrix),
			Vector2.Transform(quad.b, matrix),
			Vector2.Transform(quad.c, matrix),
			Vector2.Transform(quad.d, matrix));
	}

	public static bool operator ==(Quad a, Quad b)
	{
		return a.A == b.A && a.B == b.B && a.C == b.C && a.D == b.D;
	}

	public static bool operator !=(Quad a, Quad b)
	{
		return a.A != b.A || a.B != b.B || a.C != b.C || a.D != b.D;
	}
}
