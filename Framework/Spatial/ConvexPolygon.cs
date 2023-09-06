using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

public unsafe struct ConvexPolygon : IConvexShape
{
	public const int MaxPoints = 32;

	private fixed float points[MaxPoints * 2];
	private int pointCount;

	public int Points
	{
		get => pointCount;
		set
		{
			Debug.Assert(value >= 0 && value < MaxPoints);
			pointCount = value;
		}
	}

	public int Axis
	{
		get => pointCount;
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

	public void SetPoint(int index, Vector2 position)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		points[index * 2 + 0] = position.X;
		points[index * 2 + 1] = position.Y;
	}

	public Vector2 GetPoint(int index)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		return new Vector2(
			points[index * 2 + 0], 
			points[index * 2 + 1]);
	}

	public Vector2 this[int index]
	{
		get => GetPoint(index);
		set => SetPoint(index, value);
	}

	public Vector2 GetAxis(int index)
	{
		Debug.Assert(index >= 0 && index < MaxPoints);

		var a = GetPoint(index);
		var b = GetPoint(index >= pointCount - 1 ? 0 : index + 1);
		var normal = (b - a).Normalized();

		return new Vector2(-normal.Y, normal.X);
	}

	public void Project(in Vector2 axis, out float min, out float max)
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
}
