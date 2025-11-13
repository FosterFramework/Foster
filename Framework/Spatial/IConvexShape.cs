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
	extension<TConvexA>(TConvexA convexA) where TConvexA : IConvexShape
	{
		/// <summary>
		/// Checks if the Convex Shape overlaps a Circle, and returns the pushout vector
		/// </summary>
		public bool Overlaps(in Circle circleB, out Vector2 pushout)
		{
			pushout = Vector2.Zero;
			var distance = float.MaxValue;

			// check against axis
			for (int i = 0; i < convexA.Axes; i++)
			{
				var axis = convexA.GetAxis(i);
				if (!convexA.AxisOverlaps(circleB, axis, out float amount))
					return false;

				if (Math.Abs(amount) < distance)
				{
					pushout = axis * amount;
					distance = Math.Abs(amount);
				}
			}

			// check against points
			for (int i = 0; i < convexA.Points; i++)
			{
				var axis = (convexA.GetPoint(i) - circleB.Position).Normalized();

				// if the circle's center exactly overlaps with the point, the axis
				// will be zero which is an invalid state, so just set it to unit-x
				if (axis == Vector2.Zero)
					axis = Vector2.UnitX;

				if (!convexA.AxisOverlaps(circleB, axis, out float amount))
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
		public bool Overlaps<TConvexB>(in TConvexB convexB, out Vector2 pushout) where TConvexB : IConvexShape
		{
			pushout = Vector2.Zero;
			var distance = float.MaxValue;

			// a-axis
			{
				for (int i = 0; i < convexA.Axes; i++)
				{
					var axis = convexA.GetAxis(i);
					if (!convexA.AxisOverlaps(convexB, axis, out float amount))
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
				for (int i = 0; i < convexB.Axes; i++)
				{
					var axis = convexB.GetAxis(i);
					if (!convexA.AxisOverlaps(convexB, axis, out float amount))
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
}

