using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Foster.Framework;

/// <summary>
/// <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Vector4"/>, and <see cref="Vector128{T}"/> Extension Methods
/// </summary>
public static class VectorExt
{
	#region Vector2

	/// <summary>
	/// Clamps the <see cref="Vector2"/> inside the provided range
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Clamp(this Vector2 vector, in Vector2 min, in Vector2 max)
		=> Vector2.Clamp(vector, min, max);

	/// <summary>
	/// Clamps the <see cref="Vector2"/> inside the bounding <see cref="Rect"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Clamp(this Vector2 vector, in Rect bounds)
		=> vector.Clamp(bounds.TopLeft, bounds.BottomRight);

	/// <summary>
	/// Floors the individual components of a <see cref="Vector2"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Floor(this Vector2 vector)
		=> Vector2.Truncate(vector);

	/// <summary>
	/// Rounds the individual components of a <see cref="Vector2"/> to the nearest even number.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Round(this Vector2 vector)
		=> Vector2.Round(vector);

	/// <summary>
	/// Rounds the individual components of a <see cref="Vector2"/> and returns them as a <see cref="Point2"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point2 RoundToPoint2(this Vector2 vector)
		=> Vector128.ConvertToInt32(Vector128.Round(vector.AsVector128Unsafe())).AsPoint2();

	/// <summary>
	/// Floors the individual components of a <see cref="Vector2"/> and returns them as a <see cref="Point2"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point2 FloorToPoint2(this Vector2 vector)
		=> Vector128.ConvertToInt32(Vector128.Floor(vector.AsVector128Unsafe())).AsPoint2();

	/// <summary>
	/// Ceilings the individual components of a <see cref="Vector2"/> and returns them as a <see cref="Point2"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point2 CeilingToPoint2(this Vector2 vector)
		=> Vector128.ConvertToInt32(Vector128.Ceiling(vector.AsVector128Unsafe())).AsPoint2();

	/// <summary>
	/// Ceilings the individual components of a <see cref="Vector2"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Ceiling(this Vector2 vector) // Can be swapped with Vector2.Ceiling(vector) in .NET 10 
		=> Vector128.Ceiling(vector.AsVector128Unsafe()).AsVector2();

	/// <summary>
	/// Turns a <see cref="Vector2"/> to its right perpendicular
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 TurnRight(this Vector2 vector) => new(-vector.Y, vector.X);

	/// <summary>
	/// Turns a <see cref="Vector2"/> to its left perpendicular
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 TurnLeft(this Vector2 vector) => new(vector.Y, -vector.X);

	/// <summary>
	/// Gets the angle of a <see cref="Vector2"/> in radians
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Angle(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);

	/// <summary>
	/// Normalizes a <see cref="Vector2"/> safely (a zero-length <see cref="Vector2"/> returns the <paramref name="fallbackValue"/>)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Normalized(this Vector2 vector, Vector2 fallbackValue = default)
		=> vector == Vector2.Zero ? fallbackValue : Vector2.Normalize(vector);

	/// <summary>
	/// Normalizes a <see cref="Vector2"/> and snaps it to the closest of the 4 cardinal directions (a zero-length <see cref="Vector2"/> returns the <paramref name="fallbackValue"/>)
	/// </summary>
	public static Vector2 FourWayNormal(this Vector2 vector, Vector2 fallbackValue = default)
	{
		if (vector == Vector2.Zero)
			return fallbackValue;

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
	/// Normalizes a <see cref="Vector2"/> and snaps it to the closest of the 8 cardinal or diagonal directions (a zero-length <see cref="Vector2"/> returns the <paramref name="fallbackValue"/>)
	/// </summary>
	public static Vector2 EightWayNormal(this Vector2 vector, Vector2 fallbackValue = default)
	{
		if (vector == Vector2.Zero)
			return fallbackValue;

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
	/// Returns a <see cref="Vector2"/> with the X-value of this <see cref="Vector2"/>, but zero Y
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 ZeroY(this Vector2 vector) => vector with { Y = 0 };

	/// <summary>
	/// Returns a <see cref="Vector2"/> with the Y-value of this <see cref="Vector2"/>, but zero X
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 ZeroX(this Vector2 vector) => vector with { X = 0 };

	/// <summary>
	/// Returns a <see cref="Vector2"/> with the absolute value of both its components
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Abs(this Vector2 vector)
		=> Vector2.Abs(vector);

	/// <summary>
	/// Move a <see cref="Vector2"/> from one bounds to another
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Map(this Vector2 vector, in Rect fromBounds, in Rect toBounds)
		=> new(
			Calc.Map(vector.X, fromBounds.X, fromBounds.X + fromBounds.Width, toBounds.X, toBounds.X + toBounds.Width),
			Calc.Map(vector.Y, fromBounds.Y, fromBounds.Y + fromBounds.Height, toBounds.Y, toBounds.Y + toBounds.Height)
		);

	#endregion

	#region Vector3

	/// <summary>
	/// Normalizes a <see cref="Vector3"/> safely (a zero-length <see cref="Vector3"/> returns the <paramref name="fallbackValue"/>)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Normalized(this Vector3 vector, Vector3 fallbackValue = default)
		=> vector == Vector3.Zero ? fallbackValue : Vector3.Normalize(vector);

	/// <summary>
	/// Get a <see cref="Vector2"/> from the X- and Y-components of a <see cref="Vector3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 XY(this Vector3 vector)
		=> vector.AsVector128Unsafe().AsVector2();

	/// <summary>
	/// Apply the X- and Y-components of a <see cref="Vector2"/> to a <see cref="Vector3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 WithXY(this Vector3 vector, in Vector2 xy)
		=> new(xy.X, xy.Y, vector.Z);

	/// <summary>
	/// Rounds the individual components of a <see cref="Vector3"/> to the nearest even number.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Round(this Vector3 vector)
		=> Vector3.Round(vector);

	/// <summary>
	/// Floors the individual components of a <see cref="Vector3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Floor(this Vector3 vector)
		=> Vector3.Truncate(vector);

	/// <summary>
	/// Ceilings the individual components of a <see cref="Vector3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 Ceiling(this Vector3 vector) // Can be swapped with Vector3.Ceiling(vector) in .NET 10 
		=> Vector128.Ceiling(vector.AsVector128Unsafe()).AsVector3();

	/// <summary>
	/// Rounds the individual components of a <see cref="Vector3"/> and returns them as a <see cref="Point3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point3 RoundToPoint3(this Vector3 vector)
		=> Vector128.ConvertToInt32(Vector128.Round(vector.AsVector128Unsafe())).AsPoint3();

	/// <summary>
	/// Floors the individual components of a <see cref="Vector3"/> and returns them as a <see cref="Point3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point3 FloorToPoint3(this Vector3 vector)
		=> Vector128.ConvertToInt32(Vector128.Floor(vector.AsVector128Unsafe())).AsPoint3();

	/// <summary>
	/// Ceilings the individual components of a <see cref="Vector3"/> and returns them as a <see cref="Point3"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point3 CeilingToPoint3(this Vector3 vector)
		=> Vector128.ConvertToInt32(Vector128.Ceiling(vector.AsVector128Unsafe())).AsPoint3();
	#endregion

	#region Vector4

	/// <summary>
	/// Rounds the individual components of a <see cref="Vector4"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Round(this Vector4 vector)
		=> Vector4.Round(vector);

	/// <summary>
	/// Floors the individual components of a <see cref="Vector4"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Floor(this Vector4 vector)
		=> Vector4.Truncate(vector);

	/// <summary>
	/// Ceilings the individual components of a <see cref="Vector4"/>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Ceiling(this Vector4 vector)
		=> Vector128.Ceiling(vector.AsVector128()).AsVector4();

	#endregion

	#region Vector128-Point conversions
	// TL;DR: Using a cheeky hack to maintain dotnet consistency with future updates.

	// The action of converting values from a Vector128 to some other struct containing values is consistent across all structs with the same "bit structure".
	// Therefore, we can use some cheeky reinterpret_casts and the .NET-provided intrinsic conversions used specially for System.Numerics, without having a System.Numerics class.
	// This will continue to work as long as .NET is supporting Vector2 and the size of Vector2 in bytes is the same as Point2 in bytes.
	// This compiles into 2 instructions. If Point2/3 are read internally as SIMD vectors, then that will reduce to 1 instruction (identical to AsVector2())
	// https://godbolt.org/z/bMjvj6bYG
#if true
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point2 AsPoint2(this Vector128<int> vector)
	{
		Vector2 v = vector.AsSingle().AsVector2(); // ToVector2(reinterpret_cast(Vector128<int>, Vector128<float>)) - maintains bit values, then reads as Vector2 according to .NET spec
		return Unsafe.BitCast<Vector2, Point2>(v); // reinterpret_cast(Vector2, Point2) - maintains bit values, read now as integer values.
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point3 AsPoint3(this Vector128<int> vector)
	{
		Vector3 v = vector.AsSingle().AsVector3(); // reinterpret_cast(Vector128<int>, Vector128<float>) - maintains bit values, then reads as Vector3 according to .NET spec
		return Unsafe.BitCast<Vector3, Point3>(v); // reinterpret_cast(Vector3, Point3) - maintains bit values, read now as integer values.
	}
#else // If for some reason the above breaks, this is the fallback for dotnet, based on https://github.com/dotnet/runtime/blob/v9.0.10/src/libraries/System.Private.CoreLib/src/System/Runtime/Intrinsics/Vector128.cs#L343
	public static Point2 AsPoint2(this Vector128<int> vector) {
		ref byte address = ref Unsafe.As<Vector128<int>, byte>(ref value);
        return Unsafe.ReadUnaligned<Point2>(ref address);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Point3 AsPoint3(this Vector128<int> vector) {
		ref byte address = ref Unsafe.As<Vector128<int>, byte>(ref value);
        return Unsafe.ReadUnaligned<Point3>(ref address);
	}
#endif
	#endregion
}
