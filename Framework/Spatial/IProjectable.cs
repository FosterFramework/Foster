using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D shape that can be projected onto an Axis
/// </summary>
public interface IProjectable
{
	/// <summary>
	/// Projects the shape onto the Axis
	/// </summary>
	void Project(in Vector2 axis, out float min, out float max);
}

public static class IProjectableExt
{
	/// <summary>
	/// Checks if two Projectable Shapes overlap on the given Axis, and returns the amount
	/// </summary>
	public static bool AxisOverlaps<TProjA, TProjB>(this TProjA a, in TProjB b, in Vector2 axis, out float amount)
		where TProjA : IProjectable
		where TProjB : IProjectable
	{
		a.Project(axis, out float min0, out float max0);
		b.Project(axis, out float min1, out float max1);

		if (Math.Abs(min1 - max0) < Math.Abs(max1 - min0))
			amount = min1 - max0;
		else
			amount = max1 - min0;

		return (min0 < max1 && max0 > min1);
	}
}

