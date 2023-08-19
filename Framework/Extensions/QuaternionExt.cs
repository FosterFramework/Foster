using System;
using System.Numerics;

namespace Foster.Framework;

public static class QuaternionExt
{
	public static Quaternion Conjugated(this Quaternion q)
	{
		Quaternion c = q;
		c.X = -c.X;
		c.Y = -c.Y;
		c.Z = -c.Z;
		return c;
	}

	public static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up)
	{
		return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(from, to, up));
	}

	public static Quaternion LookAt(this Quaternion q, Vector3 direction, Vector3 up)
	{
		return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(Vector3.Zero, direction, up));
	}

	public static Vector3 Forward(this Quaternion q)
	{
		return Vector3.Transform(new Vector3(0, 0, -1), q.Conjugated());
	}

	public static Vector3 Left(this Quaternion q)
	{
		return Vector3.Transform(new Vector3(-1, 0, 0), q.Conjugated());
	}

	public static Vector3 Up(this Quaternion q)
	{
		return Vector3.Transform(new Vector3(0, 1, 0), q.Conjugated());
	}
}

