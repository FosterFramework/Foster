using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Ray
/// </summary>
public struct Ray
{
	/// <summary>
	/// Origin of the Ray
	/// </summary>
	public Vector2 Position;

	/// <summary>
	/// Direction of the Ray
	/// </summary>
	public Vector2 Direction;

	public Ray(Vector2 position, Vector2 direction)
	{
		Position = position;
		Direction = direction;
	}

	// TODO: implement intersection tests

}
