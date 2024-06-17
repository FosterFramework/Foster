using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Ray
/// </summary>
public struct Ray(Vector2 position, Vector2 direction)
{
	/// <summary>
	/// Origin of the Ray
	/// </summary>
	public Vector2 Position = position;

	/// <summary>
	/// Direction of the Ray
	/// </summary>
	public Vector2 Direction = direction;

	// TODO: implement intersection tests

}
