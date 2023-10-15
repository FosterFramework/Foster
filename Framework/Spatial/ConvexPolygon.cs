using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ConvexPolygon : IConvexShape
{
	public const int MaxPoints = 32;

	private fixed float points[MaxPoints * 2];
	private int pointCount;

	public int Points
	{
		readonly get => pointCount;
		set
		{
			Debug.Assert(value >= 0 && value < MaxPoints);
			pointCount = value;
		}
	}

	public int Axis
	{
		readonly get => pointCount;
		set
		{
			Debug.Assert(value >= 0 && value < MaxPoints);
			pointCount = value;
		}
	}
	
	public void AddPoint(in Vector2 value)
	{
		Debug.Assert(pointCount < MaxPoints);
		SetPoint(pointCount, value);
		pointCount++;
	}
	
	public void RemovePointAt(int index)
	{
		Debug.Assert(index >= 0 && index < pointCount);
		for (int i = index; i < pointCount - 1; i ++)
			SetPoint(i, GetPoint(i + 1));
		pointCount--;
	}

	public void SetPoint(int index, Vector2 position)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		points[index * 2 + 0] = position.X;
		points[index * 2 + 1] = position.Y;
	}

	public readonly Vector2 GetPoint(int index)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		return new Vector2(
			points[index * 2 + 0], 
			points[index * 2 + 1]);
	}

	public Vector2 this[int index]
	{
		readonly get => GetPoint(index);
		set => SetPoint(index, value);
	}

	public readonly Vector2 GetAxis(int index)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		var a = GetPoint(index);
		var b = GetPoint(index >= pointCount - 1 ? 0 : index + 1);
		var normal = (b - a).Normalized();

		return new(-normal.Y, normal.X);
	}

	public readonly void Project(in Vector2 axis, out float min, out float max)
	{
		if (pointCount <= 0)
		{
			min = max = 0;
		}
		else
		{
			min = float.MaxValue;
			max = float.MinValue;

			for (int i = 0; i < pointCount; i++)
			{
				var dot = Vector2.Dot(new Vector2(points[i * 2 + 0], points[i * 2 + 1]), axis);
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
				result.AddPoint(Vector2.Transform(polygon.GetPoint(polygon.Points - i - 1), matrix));
		}
		else
		{
			for (int i = 0; i < polygon.Points; i ++)
				result.AddPoint(Vector2.Transform(polygon.GetPoint(i), matrix));
		}

		return result;
	}

	public static bool operator ==(in ConvexPolygon a, in ConvexPolygon b)
	{
		if (a.pointCount != b.pointCount)
			return false;

		for (int i = 0, n = a.pointCount * 2; i < n; i ++)
			if (a.points[i] != b.points[i])
				return false;

		return true;
	}

	public static bool operator !=(in ConvexPolygon a, in ConvexPolygon b) => !(a == b);

	public readonly override bool Equals(object? obj) => obj is ConvexPolygon value && value == this;
	public readonly override int GetHashCode()
	{
		var hash = pointCount.GetHashCode();
		for (int i = 0, n = pointCount * 2; i < n; i ++)
			hash = HashCode.Combine(hash, points[i]);
		return hash;
	}
}
