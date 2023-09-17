using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Circle
/// </summary>
public struct Circle : IProjectable
{
	/// <summary>
	/// The Position of the Circle
	/// </summary>
	public Vector2 Position;

	/// <summary>
	/// The Radius of the Circle
	/// </summary>
	public float Radius;

	/// <summary>
	/// Creates a new Circle at the given position with the given Radius
	/// </summary>
	public Circle(Vector2 position, float radius)
	{
		Position = position;
		Radius = radius;
	}

	/// <summary>
	/// Creates a new Circle at the given x and y coordinates with the given Radius
	/// </summary>
	public Circle(float x, float y, float radius)
	{
		Position = new Vector2(x, y);
		Radius = radius;
	}

	/// <summary>
	/// Checks if the Vector2 is in the Circle
	/// </summary>
	public bool Contains(in Vector2 point)
	{
		return (Position - point).LengthSquared() < (Radius * Radius);
	}

	/// <summary>
	/// Checks if the Point2 is in the Circle
	/// </summary>
	public bool Contains(in Point2 point)
	{
		return (Position - point).LengthSquared() < (Radius * Radius);
	}

	/// <summary>
	/// Checks if the Circle overlaps with another Circle, and returns their pushout vector
	/// </summary>
	public bool Overlaps(in Circle other, out Vector2 pushout)
	{
		pushout = Vector2.Zero;

		var lengthSqrd = (other.Position - Position).LengthSquared();
		if (lengthSqrd < (Radius + other.Radius) * (Radius + other.Radius))
		{
			var length = MathF.Sqrt(lengthSqrd);
			pushout = ((Position - other.Position) / length) * (Radius + other.Radius - length);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Checks if the Circle overlaps with a Convex Shape, and returns their pushout vector
	/// </summary>
	public bool Overlaps<TConvex>(in TConvex shape, out Vector2 pushout)
		where TConvex : IConvexShape
	{
		pushout = Vector2.Zero;

		if (shape.Overlaps(this, out var p))
		{
			pushout = -p;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Projects the Circle onto an Axis
	/// </summary>
	public void Project(in Vector2 axis, out float min, out float max)
	{
		min = Vector2.Dot(Position - axis * Radius, axis);
		max = Vector2.Dot(Position + axis * Radius, axis);
	}

	public static bool operator ==(in Circle a, in Circle b) => a.Position == b.Position && a.Radius == b.Radius;
	public static bool operator !=(in Circle a, in Circle b) => !(a == b);

	public override bool Equals(object? obj) => obj is Circle circle && circle == this;
	public override int GetHashCode() => HashCode.Combine(Position, Radius);
}

