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
	
	public Facing Reverse => new(-value);

	public static implicit operator Facing(int v) => v < 0 ? Left : Right;
	public static implicit operator int(Facing f) => f.Sign;
	public static explicit operator Point2(Facing f) => Point2.UnitX * f.Sign;
	public static explicit operator Vector2(Facing f) => Vector2.UnitX * f.Sign;

	public static bool operator ==(Facing a, Facing b) => a.Sign == b.Sign;
	public static bool operator !=(Facing a, Facing b) => a.Sign != b.Sign;
	public static int operator *(Facing a, int b) => (int)a * b;
	public static int operator *(int a, Facing b) => a * (int)b;

	public override int GetHashCode() => Sign;
	public override bool Equals(object? obj) =>
		obj != null && obj is Facing f && f == this;

	public bool Equals(Facing other) => this == other;
}
