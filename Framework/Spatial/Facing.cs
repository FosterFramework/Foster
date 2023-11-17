using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A left/right struct, where left &lt; 0 and right >= 0.
/// Useful for 2D Platformers.
/// </summary>
public readonly struct Facing : IEquatable<Facing>
{
	public static readonly Facing Right = new(1);
	public static readonly Facing Left = new(-1);

	private readonly int value;

	public int Sign => value < 0 ? -1 : 1;
	public Facing Reverse => new(-value);

	public Facing(int val) => value = val < 0 ? -1 : 1;

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
