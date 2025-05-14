using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// <see cref="Quaternion"/> Extension Methods
/// </summary>
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
}

