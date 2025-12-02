using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Quad
/// </summary>
public struct Quad : IConvexShape, IEquatable<Quad>
{
	private Vector2 a;
	private Vector2 b;
	private Vector2 c;
	private Vector2 d;
	private Vector2 normalAB;
	private Vector2 normalBC;
	private Vector2 normalCD;
	private Vector2 normalDA;
	private bool normalsDirty;

	public Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
		normalAB = normalBC = normalCD = normalDA = Vector2.Zero;
		normalsDirty = true;
	}

	public Quad(in Rect rect)
		: this(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft) {}

	public Vector2 A
	{
		readonly get => a;
		set
		{
			if (a != value)
			{
				a = value;
				normalsDirty = true;
			}
		}
	}

	public Vector2 B
	{
		readonly get => b;
		set
		{
			if (b != value)
			{
				b = value;
				normalsDirty = true;
			}
		}
	}

	public Vector2 C
	{
		readonly get => c;
		set
		{
			if (c != value)
			{
				c = value;
				normalsDirty = true;
			}
		}
	}

	public Vector2 D
	{
		readonly get => d;
		set
		{
			if (d != value)
			{
				d = value;
				normalsDirty = true;
			}
		}
	}

	public Vector2 NormalAB
	{
		get
		{
			UpdateNormals();
			return normalAB;
		}
	}

	public Vector2 NormalBC
	{
		get
		{
			UpdateNormals();
			return normalBC;
		}
	}

	public Vector2 NormalCD
	{
		get
		{
			UpdateNormals();
			return normalCD;
		}
	}

	public Vector2 NormalDA
	{
		get
		{
			UpdateNormals();
			return normalDA;
		}
	}

	public readonly Line AB => new(A, B);
	public readonly Line BC => new(B, C);
	public readonly Line CD => new(C, D);
	public readonly Line DA => new(D, A);

	public readonly Rect Bounds
	{
		get
		{
			var bounds = new Rect
			{
				X = Math.Min(a.X, Math.Min(b.X, Math.Min(c.X, d.X))),
				Y = Math.Min(a.Y, Math.Min(b.Y, Math.Min(c.Y, d.Y)))
			};
			bounds.Width = Math.Max(a.X, Math.Max(b.X, Math.Max(c.X, d.X))) - bounds.X;
			bounds.Height = Math.Max(a.Y, Math.Max(b.Y, Math.Max(c.Y, d.Y))) - bounds.Y;
			return bounds;
		}
	}

	public readonly Vector2 Center => Bounds.Center;
	public readonly Vector2 Average => (a + b + c + d) / 4f;

	private void UpdateNormals()
	{
		if (!normalsDirty)
			return;
		normalAB = (b - a).Normalized();
		normalAB = new Vector2(-normalAB.Y, normalAB.X);
		normalBC = (c - b).Normalized();
		normalBC = new Vector2(-normalBC.Y, normalBC.X);
		normalCD = (d - c).Normalized();
		normalCD = new Vector2(-normalCD.Y, normalCD.X);
		normalDA = (a - d).Normalized();
		normalDA = new Vector2(-normalDA.Y, normalDA.X);
		normalsDirty = false;
	}

	public Quad Translate(in Vector2 amount)
	{
		A += amount;
		B += amount;
		C += amount;
		D += amount;
		return this;
	}

	public readonly void Project(in Vector2 axis, out float min, out float max)
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

	public readonly int Points => 4;

	public readonly Vector2 GetPoint(int index)
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

	public readonly int Axes => 4;

	public Vector2 GetAxis(int index)
	{
		UpdateNormals();
		return index switch
		{
			0 => new Vector2(-normalAB.Y, normalAB.X),
			1 => new Vector2(-normalBC.Y, normalBC.X),
			2 => new Vector2(-normalCD.Y, normalCD.X),
			3 => new Vector2(-normalDA.Y, normalDA.X),
			_ => throw new IndexOutOfRangeException(),
		};
	}

	[Obsolete("Use Quad.Bounds")]
	public readonly Rect BoundingRect() => Bounds;

	public readonly override bool Equals(object? obj) => obj is Quad other && this == other;
	public readonly override int GetHashCode() => HashCode.Combine(a, b, c, d);

	public static Quad Transform(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Matrix3x2 matrix, bool maintainWinding = false)
		=> Transform(new Quad(a, b, c, d), matrix, maintainWinding);

	public static Quad Transform(Quad quad, Matrix3x2 matrix, bool maintainWinding = false)
	{
		// If we're flipping the Quad we may need to reverse the points.
		// This way the Quad winding (clockwise or counter-clockwise) stays the same.
		bool reverse = maintainWinding && MathF.Sign(matrix.M11) * MathF.Sign(matrix.M22) < 0;

		if (reverse)
		{
			return new Quad(
				Vector2.Transform(quad.d, matrix),
				Vector2.Transform(quad.c, matrix),
				Vector2.Transform(quad.b, matrix),
				Vector2.Transform(quad.a, matrix));
		}
		else
		{
			return new Quad(
				Vector2.Transform(quad.a, matrix),
				Vector2.Transform(quad.b, matrix),
				Vector2.Transform(quad.c, matrix),
				Vector2.Transform(quad.d, matrix));
		}
	}

	public static bool operator ==(Quad a, Quad b) => a.a == b.a && a.b == b.b && a.c == b.c && a.d == b.d;
	public static bool operator !=(Quad a, Quad b) => a.a != b.a || a.b != b.b || a.c != b.c || a.d != b.d;
	public static implicit operator Quad(in Rect rect) => new(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft);

	public bool Equals(Quad other) => a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c) && d.Equals(other.d);

	#region Enumerate Lines

	public readonly LineEnumerable Lines => new(this);

	public readonly struct LineEnumerable(Quad Quad)
	{
		public LineEnumerator GetEnumerator() => new(Quad);
	}

	public struct LineEnumerator(Quad Quad)
	{
		public Line Current { get; private set; }
		private int index = 0;

		public bool MoveNext()
		{
			switch (index++)
			{
			case 0:
				Current = Quad.AB;
				return true;
			case 1:
				Current = Quad.BC;
				return true;
			case 2:
				Current = Quad.CD;
				return true;
			case 3:
				Current = Quad.DA;
				return true;
			default:
				return false;
			}
		}
	}

	#endregion
}
