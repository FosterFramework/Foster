using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Quad
/// </summary>
public struct Quad : IConvexShape, IEquatable<Quad>
{
	public Vector2 A;
	public Vector2 B;
	public Vector2 C;
	public Vector2 D;

	public Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		A = a;
		B = b;
		C = c;
		D = d;
	}

	public Quad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
	{
		A = new(x1, y1);
		B = new(x2, y2);
		C = new(x3, y3);
		D = new(x4, y4);
	}

	public Quad(in Rect rect)
		: this(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft)
	{

	}

	/// <summary>
	/// Get the normal of the edge from <see cref="A"/> to <see cref="B"/>. Normals will be away from edges if winding is clockwise.
	/// </summary>
	public readonly Vector2 NormalAB => (B - A).Normalized().TurnLeft();

	/// <summary>
	/// Get the normal of the edge from <see cref="B"/> to <see cref="C"/>. Normals will be away from edges if winding is clockwise.
	/// </summary>
	public readonly Vector2 NormalBC => (C - B).Normalized().TurnLeft();

	/// <summary>
	/// Get the normal of the edge from <see cref="C"/> to <see cref="D"/>. Normals will be away from edges if winding is clockwise.
	/// </summary>
	public readonly Vector2 NormalCD => (D - C).Normalized().TurnLeft();

	/// <summary>
	/// Get the normal of the edge from <see cref="D"/> to <see cref="A"/>. Normals will be away from edges if winding is clockwise.
	/// </summary>
	public readonly Vector2 NormalDA => (A - D).Normalized().TurnLeft();

	/// <summary>
	/// Get the edge from <see cref="A"/> to <see cref="B"/>
	/// </summary>
	public readonly Line AB => new(A, B);

	/// <summary>
	/// Get the edge from <see cref="B"/> to <see cref="C"/>
	/// </summary>
	public readonly Line BC => new(B, C);

	/// <summary>
	/// Get the edge from <see cref="C"/> to <see cref="D"/>
	/// </summary>
	public readonly Line CD => new(C, D);

	/// <summary>
	/// Get the edge from <see cref="D"/> to <see cref="A"/>
	/// </summary>
	public readonly Line DA => new(D, A);

	/// <summary>
	/// Get the axis-aligned bounds of the <see cref="Quad"/>
	/// </summary>
	public readonly Rect Bounds
	{
		get
		{
			var bounds = new Rect
			{
				X = Math.Min(A.X, Math.Min(B.X, Math.Min(C.X, D.X))),
				Y = Math.Min(A.Y, Math.Min(B.Y, Math.Min(C.Y, D.Y)))
			};
			bounds.Width = Math.Max(A.X, Math.Max(B.X, Math.Max(C.X, D.X))) - bounds.X;
			bounds.Height = Math.Max(A.Y, Math.Max(B.Y, Math.Max(C.Y, D.Y))) - bounds.Y;
			return bounds;
		}
	}

	/// <summary>
	/// Get the centerpoint of the <see cref="Quad"/>'s bounds
	/// </summary>
	public readonly Vector2 Center => Bounds.Center;

	/// <summary>
	/// Get the average of the 4 points of the <see cref="Quad"/>
	/// </summary>
	public readonly Vector2 Average => (A + B + C + D) / 4f;

	/// <summary>
	/// Get a new <see cref="Quad"/> translated by an <paramref name="amount"/>
	/// </summary>
	public readonly Quad Translated(in Vector2 amount)
		=> new(A + amount, B + amount, C + amount, D + amount);

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

	public readonly Vector2 GetAxis(int index)
		=> index switch
		{
			0 => (B - A).Normalized(),
			1 => (C - B).Normalized(),
			2 => (D - C).Normalized(),
			3 => (A - D).Normalized(),
			_ => throw new IndexOutOfRangeException(),
		};

	public readonly override bool Equals(object? obj) => obj is Quad other && this == other;
	public readonly override int GetHashCode() => HashCode.Combine(A, B, C, D);

	public static Quad Transform(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Matrix3x2 matrix, bool maintainWinding = false)
		=> Transform(new Quad(a, b, c, d), matrix, maintainWinding);

	public static Quad Transform(Quad quad, Matrix3x2 matrix, bool maintainWinding = false)
	{
		// If we're flipping the Quad we may need to reverse the points.
		// This way the Quad winding (clockwise or counter-clockwise) stays the same.
		bool reverse = maintainWinding && MathF.Sign(matrix.M11) * MathF.Sign(matrix.M22) < 0;

		if (reverse)
			return new(
				Vector2.Transform(quad.D, matrix),
				Vector2.Transform(quad.C, matrix),
				Vector2.Transform(quad.B, matrix),
				Vector2.Transform(quad.A, matrix)
				);
		else
			return new(
				Vector2.Transform(quad.A, matrix),
				Vector2.Transform(quad.B, matrix),
				Vector2.Transform(quad.C, matrix),
				Vector2.Transform(quad.D, matrix)
				);
	}

	public static Quad operator *(Quad lhs, float rhs) => new(lhs.A * rhs, lhs.B * rhs, lhs.C * rhs, lhs.D * rhs);
	public static Quad operator /(Quad lhs, float rhs) => new(lhs.A / rhs, lhs.B / rhs, lhs.C / rhs, lhs.D / rhs);
	public static Quad operator +(Quad lhs, Vector2 rhs) => new(lhs.A + rhs, lhs.B + rhs, lhs.C + rhs, lhs.D + rhs);
	public static Quad operator -(Quad lhs, Vector2 rhs) => new(lhs.A - rhs, lhs.B - rhs, lhs.C - rhs, lhs.D - rhs);
	public static Quad operator +(Quad lhs, Quad rhs) => new(lhs.A + rhs.A, lhs.B + rhs.B, lhs.C + rhs.C, lhs.D + rhs.D);
	public static Quad operator -(Quad lhs, Quad rhs) => new(lhs.A - rhs.A, lhs.B - rhs.B, lhs.C - rhs.C, lhs.D - rhs.D);
	public static bool operator ==(Quad lhs, Quad rhs) => lhs.A == rhs.A && lhs.B == rhs.B && lhs.C == rhs.C && lhs.D == rhs.D;
	public static bool operator !=(Quad lhs, Quad rhs) => lhs.A != rhs.A || lhs.B != rhs.B || lhs.C != rhs.C || lhs.D != rhs.D;

	public static implicit operator Quad(in Rect rect) => new(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft);

	public readonly bool Equals(Quad other) => A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C) && D.Equals(other.D);

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
