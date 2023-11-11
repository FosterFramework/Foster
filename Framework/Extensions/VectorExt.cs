using System;
using System.Numerics;

namespace Foster.Framework;

public static class VectorExt
{
	/// <summary>
	/// Clamps the vector inside the provided range.
	/// </summary>
	public static Vector2 Clamp(this Vector2 vector, in Vector2 min, in Vector2 max)
		=> new(Calc.Clamp(vector.X, min.X, max.X), Calc.Clamp(vector.Y, min.Y, max.Y));

	/// <summary>
	/// Clamps the vector inside the bounding rectangle.
	/// </summary>
	public static Vector2 Clamp(this Vector2 vector, in Rect bounds)
		=> vector.Clamp(bounds.TopLeft, bounds.BottomRight);

	/// <summary>
	/// Floors the individual components of a Vector2
	/// </summary>
	public static Vector2 Floor(this Vector2 vector)
		=> new (MathF.Floor(vector.X), MathF.Floor(vector.Y));

	/// <summary>
	/// Floors the individual components of a Vector3
	/// </summary>
	public static Vector3 Floor(this Vector3 vector)
		=> new(MathF.Floor(vector.X), MathF.Floor(vector.Y), MathF.Floor(vector.Z));

	/// <summary>
	/// Floors the individual components of a Vector4
	/// </summary>
	public static Vector4 Floor(this Vector4 vector)
		=> new(MathF.Floor(vector.X), MathF.Floor(vector.Y), MathF.Floor(vector.Z), MathF.Floor(vector.W));

	/// <summary>
	/// Rounds the individual components of a Vector2
	/// </summary>
	public static Vector2 Round(this Vector2 vector)
		=> new(MathF.Round(vector.X), MathF.Round(vector.Y));

	/// <summary>
	/// Rounds the individual components of a Vector2
	/// </summary>
	public static Point2 RoundToPoint2(this Vector2 vector)
		=> new((int)MathF.Round(vector.X), (int)MathF.Round(vector.Y));

	/// <summary>
	/// Floors the individual components of a Vector2
	/// </summary>
	public static Point2 FloorToPoint2(this Vector2 vector)
		=> new((int)MathF.Floor(vector.X), (int)MathF.Floor(vector.Y));

	/// <summary>
	/// Rounds the individual components of a Vector3
	/// </summary>
	public static Vector3 Round(this Vector3 vector)
		=> new(MathF.Round(vector.X), MathF.Round(vector.Y), MathF.Round(vector.Z));

	/// <summary>
	/// Rounds the individual components of a Vector4
	/// </summary>
	public static Vector4 Round(this Vector4 vector)
		=> new(MathF.Round(vector.X), MathF.Round(vector.Y), MathF.Round(vector.Z), MathF.Round(vector.W));

	/// <summary>
	/// Ceilings the individual components of a Vector2
	/// </summary>
	public static Vector2 Ceiling(this Vector2 vector)
		=> new(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y));

	/// <summary>
	/// Ceilings the individual components of a Vector3
	/// </summary>
	public static Vector3 Ceiling(this Vector3 vector)
		=> new(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y), MathF.Ceiling(vector.Z));

	/// <summary>
	/// Ceilings the individual components of a Vector4
	/// </summary>
	public static Vector4 Ceiling(this Vector4 vector)
		=> new(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y), MathF.Ceiling(vector.Z), MathF.Ceiling(vector.W));

	/// <summary>
	/// Turns a Vector2 to its right perpendicular
	/// </summary>
	public static Vector2 TurnRight(this Vector2 vector) => new(-vector.Y, vector.X);

	/// <summary>
	/// Turns a Vector2 to its left perpendicular
	/// </summary>
	public static Vector2 TurnLeft(this Vector2 vector) => new(vector.Y, -vector.X);

	/// <summary>
	/// Gets the Angle of a Vector in radians
	/// </summary>
	public static float Angle(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);

	/// <summary>
	/// Normalizes a Vector2 safely (a zero-length Vector2 returns 0)
	/// </summary>
	public static Vector2 Normalized(this Vector2 vector)
	{
		if (vector.X == 0 && vector.Y == 0)
			return Vector2.Zero;
		return Vector2.Normalize(vector);
	}

	/// <summary>
	/// Normalizes a Vector3 safely (a zero-length Vector3 returns 0)
	/// </summary>
	public static Vector3 Normalized(this Vector3 vector)
	{
		if (vector.X == 0 && vector.Y == 0 && vector.Z == 0)
			return Vector3.Zero;
		return Vector3.Normalize(vector);
	}

	/// <summary>
	/// Normalizes a Vector2 and snaps it to the closest of the 4 cardinal directions (a zero-length Vector2 returns 0)
	/// </summary>
	public static Vector2 FourWayNormal(this Vector2 vector)
	{
		if (vector == Vector2.Zero)
			return vector;

		vector = Calc.AngleToVector(Calc.Snap(vector.Angle(), Calc.HalfPI));
		if (MathF.Abs(vector.X) < .1f)
		{
			vector.X = 0;
			vector.Y = MathF.Sign(vector.Y);
		}
		else if (MathF.Abs(vector.Y) < .1f)
		{
			vector.X = MathF.Sign(vector.X);
			vector.Y = 0;
		}

		return vector;
	}

	/// <summary>
	/// Normalizes a Vector2 and snaps it to the closest of the 8 cardinal or diagonal directions (a zero-length Vector2 returns 0)
	/// </summary>
	public static Vector2 EightWayNormal(this Vector2 vector)
	{
		if (vector == Vector2.Zero)
			return vector;

		vector = Calc.AngleToVector(Calc.Snap(vector.Angle(), Calc.PI / 4));
		if (MathF.Abs(vector.X) < .1f)
		{
			vector.X = 0;
			vector.Y = MathF.Sign(vector.Y);
		}
		else if (MathF.Abs(vector.Y) < .1f)
		{
			vector.X = MathF.Sign(vector.X);
			vector.Y = 0;
		}

		return vector;
	}

	/// <summary>
	/// Returns a Vector2 with the X-value of this Vector2, but zero Y
	/// </summary>
	public static Vector2 ZeroY(this Vector2 vector) => new Vector2(vector.X, 0);

	/// <summary>
	/// Returns a Vector2 with the Y-value of this Vector2, but zero X
	/// </summary>
	public static Vector2 ZeroX(this Vector2 vector) => new Vector2(0, vector.Y);
}
