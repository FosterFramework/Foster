using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// <see cref="Matrix3x2"/> Extension Methods
/// </summary>
public static class Matrix3x2Ext
{
	extension(Matrix3x2 mat)
	{
		/// <summary>
		/// Get the x-scale of the <see cref="Matrix3x2"/>. This will be incorrect if the matrix is rotated or skewed, but it's much cheaper than <see cref="XScale"/>
		/// </summary>
		public float XScaleFast => mat.M11;

		/// <summary>
		/// Get the y-scale of the <see cref="Matrix3x2"/>. This will be incorrect if the matrix is rotated or skewed, but it's much cheaper than <see cref="YScale"/>
		/// </summary>
		public float YScaleFast => mat.M22;

		/// <summary>
		/// Get the scale of the <see cref="Matrix3x2"/>. This will be incorrect if the matrix is rotated or skewed, but it's much cheaper than <see cref="Scale"/>
		/// </summary>
		public Vector2 ScaleFast => new(mat.M11, mat.M22);

		/// <summary>
		/// Get the x-scale of the <see cref="Matrix3x2"/> (this is expensive as it requires a square root - you can use <see cref="XScaleFast"/> if you know the matrix is not rotated or skewed)
		/// </summary>
		public float XScale => MathF.Sqrt(mat.M11 * mat.M11 + mat.M21 * mat.M21);

		/// <summary>
		/// Get the y-scale of the <see cref="Matrix3x2"/> (this is expensive as it requires a square root - you can use <see cref="YScaleFast"/> if you know the matrix is not rotated or skewed)
		/// </summary>
		public float YScale => MathF.Sqrt(mat.M12 * mat.M12 + mat.M22 * mat.M22);

		/// <summary>
		/// Get the scale of the <see cref="Matrix3x2"/> (this is expensive as it requires two square roots - you can use <see cref="ScaleFast"/> if you know the matrix is not rotated or skewed)
		/// </summary>
		public Vector2 Scale => new(mat.XScale, mat.YScale);
	}
}
