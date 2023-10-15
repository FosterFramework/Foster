using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A 3D Integer Point
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Point3
{

	public static readonly Point3 Zero = new Point3(0, 0, 0);
	public static readonly Point3 One = new Point3(1, 1, 1);
	public static readonly Point3 Left = new Point3(-1, 0, 0);
	public static readonly Point3 Right = new Point3(1, 0, 0);
	public static readonly Point3 Up = new Point3(0, -1, 0);
	public static readonly Point3 Down = new Point3(0, 1, 0);
	public static readonly Point3 Forward = new Point3(0, 0, 1);
	public static readonly Point3 Backward = new Point3(0, 0, -1);

	public int X;
	public int Y;
	public int Z;

	public Point3(int xyz)
	{
		X = Y = Z = xyz;
	}

	public Point3(int x, int y)
	{
		X = x;
		Y = y;
		Z = 0;
	}

	public Point3(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public float Length()
	{
		return new Vector3(X, Y, Z).Length();
	}

	public Vector3 Normalized()
	{
		return new Vector3(X, Y, Z).Normalized();
	}

	public override bool Equals(object? obj) => (obj is Point3 other) && (this == other);

	public override int GetHashCode()
		=> HashCode.Combine(X, Y, Z);

	public override string ToString()
		=> $"[{X}, {Y}, {Z}]";


	public static implicit operator Point3((int X, int Y, int Z) tuple) => new Point3(tuple.X, tuple.Y, tuple.Z);
	public static explicit operator Point3(Vector3 vector) => new Point3((int)vector.X, (int)vector.Y, (int)vector.Z);
	public static implicit operator Vector3(Point3 point) => new Vector3(point.X, point.Y, point.Z);

	public static Point3 operator -(Point3 point) => new Point3(-point.X, -point.Y, -point.Z);
	public static Point3 operator /(Point3 point, int scaler) => new Point3(point.X / scaler, point.Y / scaler, point.Z / scaler);
	public static Point3 operator *(Point3 point, int scaler) => new Point3(point.X * scaler, point.Y * scaler, point.Z * scaler);
	public static Point3 operator %(Point3 point, int scaler) => new Point3(point.X % scaler, point.Y % scaler, point.Z % scaler);

	public static Vector3 operator /(Point3 point, float scaler) => new Vector3(point.X / scaler, point.Y / scaler, point.Z / scaler);
	public static Vector3 operator *(Point3 point, float scaler) => new Vector3(point.X * scaler, point.Y * scaler, point.Z * scaler);
	public static Vector3 operator %(Point3 point, float scaler) => new Vector3(point.X % scaler, point.Y % scaler, point.Z % scaler);

	public static Point3 operator +(Point3 a, Point3 b) => new Point3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	public static Point3 operator -(Point3 a, Point3 b) => new Point3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

	public static bool operator ==(Point3 a, Point3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
	public static bool operator !=(Point3 a, Point3 b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;

}
