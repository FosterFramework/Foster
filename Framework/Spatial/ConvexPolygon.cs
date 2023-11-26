using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ConvexPolygon : IConvexShape, IEnumerable<Vector2>
{
	public const int MaxPoints = StackList32<Vector2>.TypeCapacity;
	public StackList32<Vector2> Vertices;

	public readonly int Points => Vertices.Count;
	public readonly int Axes => Vertices.Count;

	public readonly Rect Bounds
	{
		get
		{
			Rect bounds = new(Vertices[0].X, Vertices[0].Y, 0, 0);

			for (int i = 1; i < Points; i++)
			{
				if (Vertices[i].X < bounds.X)
				{
					bounds.Width += bounds.X - Vertices[i].X;
					bounds.X = Vertices[i].X;
				}

				if (Vertices[i].X > bounds.Right)
					bounds.Width = Vertices[i].X - bounds.X;

				if (Vertices[i].Y < bounds.Y)
				{
					bounds.Height += bounds.Y - Vertices[i].Y;
					bounds.Y = Vertices[i].Y;
				}

				if (Vertices[i].Y > bounds.Bottom)
					bounds.Height = Vertices[i].Y - bounds.Y;
			}	

			return bounds;
		}
	}

	public void Add(in Vector2 value)
		=> Vertices.Add(value);

	public void RemoveAt(int index)
		=> Vertices.RemoveAt(index);
	
	[Obsolete("Use ConvexPolygon.Add")]
	public void AddPoint(in Vector2 value)
		=> Vertices.Add(value);
	
	[Obsolete("Use ConvexPolygon.RemoveAt")]
	public void RemovePointAt(int index)
		=> Vertices.RemoveAt(index);

	[Obsolete("Use ConvexPolygon.this[int index]")]
	public void SetPoint(int index, Vector2 value)
		=> Vertices[index] = value;

	[Obsolete("Use ConvexPolygon.this[int index]")]
	public readonly Vector2 GetPoint(int index)
		=> Vertices[index];

	public Vector2 this[int index]
	{
		readonly get => Vertices[index];
		set => Vertices[index] = value;
	}

	public readonly Vector2 GetAxis(int index)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		var a = Vertices[index];
		var b = Vertices[index >= Vertices.Count - 1 ? 0 : index + 1];
		var normal = (b - a).Normalized();

		return new(-normal.Y, normal.X);
	}

	public readonly bool Contains(in Vector2 vec2)
	{
		int total = 0;
		for (int i = 0; i < Points; i++)
			total += Calc.Orient(Vertices[i], Vertices[(i + 1) % Vertices.Count], vec2);
		return Math.Abs(total) == Points;
	}

	public readonly void Project(in Vector2 axis, out float min, out float max)
	{
		if (Vertices.Count <= 0)
		{
			min = max = 0;
		}
		else
		{
			min = max = Vector2.Dot(Vertices[0], axis);
			for (int i = 1; i < Vertices.Count; i++)
			{
				var dot = Vector2.Dot(Vertices[i], axis);
				min = Math.Min(dot, min);
				max = Math.Max(dot, max);
			}
		}
	}

	public static ConvexPolygon Transform(in ConvexPolygon polygon, in Matrix3x2 matrix, bool maintainWinding = false)
	{
		ConvexPolygon result = new();

		// If we're flipping the Polygon we may need to reverse the points.
		// This way the Polygon winding (clockwise or counter-clockwise) stays the same.
		bool reverse = maintainWinding && MathF.Sign(matrix.M11) * MathF.Sign(matrix.M22) < 0;

		if (reverse)
		{
			for (int i = 0; i < polygon.Points; i ++)
				result.Vertices.Add(Vector2.Transform(polygon.Vertices[polygon.Points - i - 1], matrix));
		}
		else
		{
			for (int i = 0; i < polygon.Points; i ++)
				result.Vertices.Add(Vector2.Transform(polygon.Vertices[i], matrix));
		}

		return result;
	}

	public static bool operator ==(in ConvexPolygon a, in ConvexPolygon b)
	{
		if (a.Vertices.Count != b.Vertices.Count)
			return false;

		for (int i = 0; i < a.Vertices.Count; i ++)
			if (a.Vertices[i] != b.Vertices[i])
				return false;

		return true;
	}

	public static bool operator !=(in ConvexPolygon a, in ConvexPolygon b) => !(a == b);

	public static ConvexPolygon operator +(in ConvexPolygon a, in Vector2 b)
	{
		ConvexPolygon result = a;
		for (int i = 0; i < result.Vertices.Count; i ++)
			result.Vertices[i] += b;
		return result;
	}

	public static ConvexPolygon operator -(in ConvexPolygon a, in Vector2 b)
	{
		ConvexPolygon result = a;
		for (int i = 0; i < result.Vertices.Count; i ++)
			result.Vertices[i] -= b;
		return result;
	}

	public readonly override bool Equals(object? obj)
		=> obj is ConvexPolygon value && value == this;

	public readonly override int GetHashCode()
	{
		var hash = Vertices.Count.GetHashCode();
		for (int i = 0; i < Vertices.Count; i ++)
			hash = HashCode.Combine(hash, Vertices[i]);
		return hash;
	}

	public readonly StackList32<Vector2>.Enumerator GetEnumerator()
		=> new(Vertices);

	readonly IEnumerator<Vector2> IEnumerable<Vector2>.GetEnumerator()
		=> new StackList32<Vector2>.Enumerator(Vertices);

	readonly IEnumerator IEnumerable.GetEnumerator()
		=> new StackList32<Vector2>.Enumerator(Vertices);
}
