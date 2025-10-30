using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// A 2D Circle
/// </summary>
[StructLayout(LayoutKind.Sequential), JsonConverter(typeof(JsonConverter))]
public struct Circle : IProjectable, IEquatable<Circle>
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
	/// Creates a new Circle at the given position with the given radius
	/// </summary>
	public Circle(Vector2 position, float radius)
	{
		Position = position;
		Radius = radius;
	}

	/// <summary>
	/// Creates a new Circle at the given x and y coordinates with the given radius
	/// </summary>
	public Circle(float x, float y, float radius)
	{
		Position = new(x, y);
		Radius = radius;
	}

	/// <summary>
	/// Creates a new Circle at (0, 0) with the given radius
	/// </summary>
	public Circle(float radius)
	{
		Radius = radius;
	}

	/// <summary>
	/// Calculate the area of the circle
	/// </summary>
	public readonly float Area => MathF.PI * Radius * Radius;

	/// <summary>
	/// Gets the smallest rectangle that contains this circle
	/// </summary>
	public readonly Rect Bounds => Rect.Centered(Position, Radius * 2, Radius * 2);

	/// <summary>
	/// Checks if the Vector2 is in the Circle
	/// </summary>
	public readonly bool Contains(in Vector2 point)
		=> (Position - point).LengthSquared() < (Radius * Radius);

	/// <summary>
	/// Checks if the Point2 is in the Circle
	/// </summary>
	public readonly bool Contains(in Point2 point)
		=> (Position - point).LengthSquared() < (Radius * Radius);

	/// <summary>
	/// Checks if the Circle overlaps with another Circle, and returns their pushout vector
	/// </summary>
	public readonly bool Overlaps(in Circle other, out Vector2 pushout)
	{
		pushout = Vector2.Zero;

		var combinedRadius = (Radius + other.Radius);
		var lengthSqrd = (other.Position - Position).LengthSquared();

		if (lengthSqrd < combinedRadius * combinedRadius)
		{
			var length = MathF.Sqrt(lengthSqrd);

			// they overlap exactly, so there is no "direction" to push out of.
			// instead just push out along the unit-x vector
			if (length <= 0)
				pushout = Vector2.UnitX * combinedRadius;
			else
				pushout = ((Position - other.Position) / length) * (combinedRadius - length);

			return true;
		}

		return false;
	}

	/// <summary>
	/// Checks whether we overlap a circle (as defined by its center and radius)
	/// </summary>
	public readonly bool Overlaps(in Vector2 center, float radius)
		=> Vector2.DistanceSquared(Position, center) < Calc.Squared(radius + Radius);

	/// <summary>
	/// Checks whether we overlap a line segment
	/// </summary>
	public readonly bool Overlaps(in Line line)
		=> Vector2.DistanceSquared(Position, line.ClosestPoint(Position)) < Radius * Radius;

	/// <summary>
	/// Checkers whether we overlap a triangle
	/// </summary>
	public readonly bool Overlaps(in Triangle tri)
		=> tri.Contains(Position) || Overlaps(tri.AB) || Overlaps(tri.BC) || Overlaps(tri.CA);

	/// <summary>
	/// Checks if the Circle overlaps with a Convex Shape, and returns their pushout vector
	/// </summary>
	public readonly bool Overlaps<TConvex>(in TConvex shape, out Vector2 pushout)
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
	public readonly void Project(in Vector2 axis, out float min, out float max)
	{
		min = Vector2.Dot(Position - axis * Radius, axis);
		max = Vector2.Dot(Position + axis * Radius, axis);
	}

	/// <summary>
	/// Return a new circle with the radius increased by the given amount
	/// </summary>
	public readonly Circle Inflate(float addRadius) => new(Position, Radius + addRadius);

	public static bool operator ==(in Circle a, in Circle b) => a.Position == b.Position && a.Radius == b.Radius;
	public static bool operator !=(in Circle a, in Circle b) => !(a == b);

	public static Circle operator +(in Circle a, in Vector2 b) => new(a.Position + b, a.Radius);
	public static Circle operator -(in Circle a, in Vector2 b) => new(a.Position - b, a.Radius);

	public bool Equals(Circle other) => this == other;
	public readonly override bool Equals(object? obj) => obj is Circle circle && circle == this;
	public readonly override int GetHashCode() => HashCode.Combine(Position, Radius);

	public class JsonConverter : JsonConverter<Circle>
	{
		public override Circle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Circle value = new();
			if (reader.TokenType != JsonTokenType.StartObject)
				return value;

			while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
			{
				if (reader.TokenType != JsonTokenType.PropertyName)
					continue;

				var component = reader.ValueSpan;
				if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				{
					reader.Skip();
					continue;
				}

				if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "x"u8))
					value.Position.X = reader.GetSingle();
				else if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "y"u8))
					value.Position.Y = reader.GetSingle();
				else if (Calc.EqualsOrdinalIgnoreCaseUtf8(component, "radius"u8))
					value.Radius = reader.GetSingle();
				else
					reader.Skip();
			}

			return value;
		}

		public override void Write(Utf8JsonWriter writer, Circle value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteNumber("X", value.Position.X);
			writer.WriteNumber("Y", value.Position.Y);
			writer.WriteNumber("Radius", value.Radius);
			writer.WriteEndObject();
		}
	}
}

