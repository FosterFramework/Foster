using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Foster.Framework;

/// <summary>
/// A 2D Transform
/// </summary>
public struct Transform
{
	private bool matrixDirty;
	private bool matrixInverseDirty;
	private Vector2 position = Vector2.Zero;
	private Vector2 scale = Vector2.One;
	private float rotation = 0f;
	private Matrix3x2 matrix = Matrix3x2.Identity;
	private Matrix3x2 matrixInverse = Matrix3x2.Identity;

	public static readonly Transform Identity = new();

	public Transform() {}

	public Transform(Vector2 position, Vector2 scale, float rotation)
	{
		this.position = position;
		this.scale = scale;
		this.rotation = rotation;
		matrixDirty = true;
	}

	/// <summary>
	/// A value that's updated every the transform is modified
	/// </summary>
	public int TransformIndex;

	/// <summary>
	/// Gets or Sets the Position of the Transform
	/// </summary>
	public Vector2 Position
	{
		get => position;
		set
		{
			if (position != value)
			{
				position = value;
				MakeDirty();
			}
		}
	}

	/// <summary>
	/// Gets or Sets the X Component of the Position of the Transform
	/// </summary>
	public float X
	{
		get => Position.X;
		set => Position = new Vector2(value, Position.Y);
	}

	/// <summary>
	/// Gets or Sets the Y Component of the Position of the Transform
	/// </summary>
	public float Y
	{
		get => Position.Y;
		set => Position = new Vector2(Position.X, value);
	}

	/// <summary>
	/// Gets or Sets the Local Scale of the Transform
	/// </summary>
	public Vector2 Scale
	{
		get => scale;
		set
		{
			if (scale != value)
			{
				scale = value;
				MakeDirty();
			}
		}
	}

	/// <summary>
	/// Gets or Sets the Local Rotation of the Transform
	/// </summary>
	public float Rotation
	{
		get => rotation;
		set
		{
			if (rotation != value)
			{
				rotation = value;
				MakeDirty();
			}
		}
	}

	/// <summary>
	/// Gets the Matrix of the Transform
	/// </summary>
	public Matrix3x2 Matrix
	{
		get
		{
			if (matrixDirty)
			{
				matrixDirty = false;
				matrix = CreateMatrix(position, Vector2.Zero, scale, rotation);
			}

			return matrix;
		}
	}

	/// <summary>
	/// Gets the Inverse of the Matrix of the Transform
	/// </summary>
	public Matrix3x2 MatrixInverse
	{
		get
		{
			if (matrixInverseDirty)
			{
				matrixInverseDirty = false;
				Matrix3x2.Invert(Matrix, out matrixInverse);
			}

			return matrixInverse;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void MakeDirty()
	{
		TransformIndex++;
		matrixDirty = true;
		matrixInverseDirty = true;
	}

	/// <summary>
	/// Creates a Matrix3x2 given the Transform Values
	/// </summary>
	public static Matrix3x2 CreateMatrix(in Vector2 position, in Vector2 origin, in Vector2 scale, in float rotation)
	{
		Matrix3x2 matrix;

		if (origin != Vector2.Zero)
			matrix = Matrix3x2.CreateTranslation(-origin.X, -origin.Y);
		else
			matrix = Matrix3x2.Identity;

		if (scale != Vector2.One)
			matrix *= Matrix3x2.CreateScale(scale.X, scale.Y);

		if (rotation != 0)
			matrix *= Matrix3x2.CreateRotation(rotation);

		if (position != Vector2.Zero)
			matrix *= Matrix3x2.CreateTranslation(position.X, position.Y);

		return matrix;
	}

	public static bool operator ==(Transform a, Transform b) => a.position == b.position && a.scale == b.scale && a.rotation == b.rotation;
	public static bool operator !=(Transform a, Transform b) => !(a == b);

	public override bool Equals(object? obj)
		=> obj is Transform transform && this == transform;

	public override int GetHashCode()
		=> HashCode.Combine(position, scale, rotation);
}
