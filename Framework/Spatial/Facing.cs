using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A binary struct, where Left is any negative value and Right is zero or any positive number
/// Useful for 2D Platformers.
/// </summary>
public readonly struct Facing(int val) : IEquatable<Facing>
{
	public static readonly Facing Right = new(1);
	public static readonly Facing Left = new(-1);

	private readonly int value = val < 0 ? -1 : 1;

	/// <summary>
	/// Returns -1 if Left, or +1 if Right
	/// </summary>
	public int Sign => value < 0 ? -1 : 1;

	/// <summary>
	/// The opposite of our value
	/// </summary>
	public Facing Reverse => new(-value);

	/// <summary>
	/// Integers convert to Left if negative, otherwise Right
	/// </summary>
	public static implicit operator Facing(int v) => v < 0 ? Left : Right;

	/// <summary>
	/// -1 for Left, 1 for Right
	/// </summary>
	public static implicit operator int(Facing f) => f.Sign;

	/// <summary>
	/// Cast to a unit Point2 in our direction
	/// </summary>
	public static explicit operator Point2(Facing f) => Point2.UnitX * f.Sign;

	/// <summary>
	/// Cast to a unit Vector2 in our direction
	/// </summary>
	public static explicit operator Vector2(Facing f) => Vector2.UnitX * f.Sign;

	public static bool operator ==(Facing a, Facing b) => a.Sign == b.Sign;
	public static bool operator !=(Facing a, Facing b) => a.Sign != b.Sign;
	public static int operator *(Facing a, int b) => (int)a * b;
	public static int operator *(int a, Facing b) => a * (int)b;

	public static Point2 operator *(Point2 point, Facing facing) => new(point.X * facing.Sign, point.Y);
	public static Point2 operator *(Facing facing, Point2 point) => new(point.X * facing.Sign, point.Y);
	public static Vector2 operator *(Vector2 vec, Facing facing) => new(vec.X * facing.Sign, vec.Y);
	public static Vector2 operator *(Facing facing, Vector2 vec) => new(vec.X * facing.Sign, vec.Y);

	public override int GetHashCode() => Sign;
	public override bool Equals(object? obj) =>
		obj != null && obj is Facing f && f == this;

	public bool Equals(Facing other) => this == other;
}
