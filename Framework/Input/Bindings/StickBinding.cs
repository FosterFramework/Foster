using System.Numerics;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input binding that represents a 2D Axis
/// </summary>
public class StickBinding
{
	/// <summary>
	/// Circular Deadzone to apply across the resulting 2D vector
	/// </summary>
	[JsonInclude] public float CircularDeadzone = 0;

	/// <summary>
	/// The X-Axis Binding
	/// </summary>
	[JsonInclude] public readonly AxisBinding X = new();

	/// <summary>
	/// The Y-Axis Binding
	/// </summary>
	[JsonInclude] public readonly AxisBinding Y = new();

	public StickBinding() {}

	public StickBinding(float circularDeadzone, AxisBinding.Overlaps overlapBehavior)
	{
		CircularDeadzone = circularDeadzone;
		X.OverlapBehaviour = overlapBehavior;
		Y.OverlapBehaviour = overlapBehavior;
	}

	public StickBinding Add(Keys left, Keys right, Keys up, Keys down)
	{
		X.Add(left, right);
		Y.Add(up, down);
		return this;
	}

	public StickBinding Add(Buttons left, Buttons right, Buttons up, Buttons down)
	{
		X.Add(left, right);
		Y.Add(up, down);
		return this;
	}

	public StickBinding Add(Axes x, Axes y)
	{
		X.Add(x);
		Y.Add(y);
		return this;
	}

	public Vector2 Value(Input input, int device)
	{
		var it = new Vector2(
			X.Value(input, device),
			Y.Value(input, device));

		if (CircularDeadzone > 0 &&
			it.LengthSquared() < CircularDeadzone * CircularDeadzone)
			return Vector2.Zero;

		return it;
	}

	public Vector2 ValueNoDeadzone(Input input, int device) => new(
		X.ValueNoDeadzone(input, device),
		Y.ValueNoDeadzone(input, device)
	);
}