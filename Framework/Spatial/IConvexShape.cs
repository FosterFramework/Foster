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
	/// The number of axis of the Convex Shape
	/// </summary>
	public int Axis { get; }

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
	public static bool Overlaps(this IConvexShape a, in Circle b, out Vector2 pushout)
	{
		pushout = Vector2.Zero;

		var distance = float.MaxValue;

		// check against axis
		for (int i = 0; i < a.Axis; i++)
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
	public static bool Overlaps(this IConvexShape a, in IConvexShape b, out Vector2 pushout)
	{
		pushout = Vector2.Zero;

		var distance = float.MaxValue;

		// a-axis
		{
			for (int i = 0; i < a.Axis; i++)
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
			for (int i = 0; i < b.Axis; i++)
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

