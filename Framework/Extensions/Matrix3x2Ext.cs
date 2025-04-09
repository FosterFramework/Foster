using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// <see cref="Matrix3x2"/> Extension Methods
/// </summary>
public static class Matrix3x2Ext
{
	public static float ScalingFactor(this Matrix3x2 matrix)
		=> MathF.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
}