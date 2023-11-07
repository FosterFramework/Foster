using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Convex Shape
/// </summary>
public interface IConvexShape : IProjectable
{
	/// <summary>
	/// The number of sides of the Convex shape
	/// </summary>
	public int Points { get; }

	/// <summary>
	/// Gets a point of the Convex Shape at the given index
	/// </summary>
	public Vector2 GetPoint(int index);

	/// <summary>
	/// The number of axes of the Convex Shape
	/// </summary>
	public int Axes { get; }

	/// <summary>
	/// Gets a axis of the Convex Shape at the given index
	/// </summary>
	public Vector2 GetAxis(int index);
}

/// <summary>
/// 2D Convex Shape Extension methods
/// </summary>
public static class IConvexShape2DExt
{
	/// <summary>
	/// Checks if the Convex Shape overlaps a Circle, and returns the pushout vector
	/// </summary>
	public static bool Overlaps<TConvexA>(this TConvexA a, in Circle b, out Vector2 pushout)
		where TConvexA : IConvexShape
	{
		pushout = Vector2.Zero;

		var distance = float.MaxValue;

		// check against axis
		for (int i = 0; i < a.Axes; i++)
		{
			var axis = a.GetAxis(i);
			if (!a.AxisOverlaps(b, axis, out float amount))
				return false;

			if (Math.Abs(amount) < distance)
			{
				pushout = axis * amount;
				distance = Math.Abs(amount);
			}
		}

		// check against points
		for (int i = 0; i < a.Points; i++)
		{
			var axis = (a.GetPoint(i) - b.Position).Normalized();

			// if the circle's center exactly overlaps with the point, the axis
			// will be zero which is an invalid state, so just set it to unit-x
			if (axis == Vector2.Zero)
				axis = Vector2.UnitX;

			if (!a.AxisOverlaps(b, axis, out float amount))
				return false;

			if (Math.Abs(amount) < distance)
			{
				pushout = axis * amount;
				distance = Math.Abs(amount);
			}
		}

		return true;
	}

	/// <summary>
	/// Checks if the Convex Shape overlaps another Convex Shape, and returns the pushout vector
	/// </summary>
	public static bool Overlaps<TConvexA, TConvexB>(this TConvexA a, in TConvexB b, out Vector2 pushout) 
		where TConvexA : IConvexShape
		where TConvexB : IConvexShape
	{
		pushout = Vector2.Zero;

		var distance = float.MaxValue;

		// a-axis
		{
			for (int i = 0; i < a.Axes; i++)
			{
				var axis = a.GetAxis(i);
				if (!a.AxisOverlaps(b, axis, out float amount))
					return false;

				if (Math.Abs(amount) < distance)
				{
					pushout = axis * amount;
					distance = Math.Abs(amount);
				}
			}
		}

		// b-axis
		{
			for (int i = 0; i < b.Axes; i++)
			{
				var axis = b.GetAxis(i);
				if (!a.AxisOverlaps(b, axis, out float amount))
					return false;

				if (Math.Abs(amount) < distance)
				{
					pushout = axis * amount;
					distance = Math.Abs(amount);
				}
			}
		}

		return true;
	}
}

