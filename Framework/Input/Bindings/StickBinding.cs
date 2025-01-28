using System.Numerics;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input binding that represents a 2D Axis/Stick
/// </summary>
public sealed class StickBinding
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

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public StickBinding Add(Keys left, Keys right, Keys up, Keys down)
	{
		X.Add(left, right);
		Y.Add(up, down);
		return this;
	}

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public StickBinding Add(in ReadOnlySpan<string> masks, Keys left, Keys right, Keys up, Keys down)
	{
		X.Add(masks, left, right);
		Y.Add(masks, up, down);
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public StickBinding Add(Buttons left, Buttons right, Buttons up, Buttons down)
	{
		X.Add(left, right);
		Y.Add(up, down);
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public StickBinding Add(in ReadOnlySpan<string> masks, Buttons left, Buttons right, Buttons up, Buttons down)
	{
		X.Add(masks, left, right);
		Y.Add(masks, up, down);
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public StickBinding Add(Axes x, Axes y, float xDeadzone = 0, float yDeadzone = 0)
	{
		X.Add(x, xDeadzone);
		Y.Add(y, yDeadzone);
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public StickBinding Add(in ReadOnlySpan<string> masks, Axes x, Axes y, float xDeadzone = 0, float yDeadzone = 0)
	{
		X.Add(masks, x, xDeadzone);
		Y.Add(masks, y, yDeadzone);
		return this;
	}

	/// <summary>
	/// Adds a Mouse Motion mapping
	/// </summary>
	public StickBinding AddMouseMotion(float maxMotion = 25)
	{
		X.Negative.Bindings.Add(new MouseMotionBinding(Vector2.UnitX, -1, 0, maxMotion));
		X.Positive.Bindings.Add(new MouseMotionBinding(Vector2.UnitX, 1, 0, maxMotion));
		Y.Negative.Bindings.Add(new MouseMotionBinding(Vector2.UnitY, -1, 0, maxMotion));
		Y.Positive.Bindings.Add(new MouseMotionBinding(Vector2.UnitY, 1, 0, maxMotion));
		return this;
	}

	/// <summary>
	/// Adds a Mouse Motion mapping
	/// </summary>
	public StickBinding AddMouseMotion(in ReadOnlySpan<string> masks, float maxMotion = 25)
	{
		X.Negative.Bindings.Add(new MouseMotionBinding(Vector2.UnitX, -1, 0, maxMotion, masks));
		X.Positive.Bindings.Add(new MouseMotionBinding(Vector2.UnitX, 1, 0, maxMotion, masks));
		Y.Negative.Bindings.Add(new MouseMotionBinding(Vector2.UnitY, -1, 0, maxMotion, masks));
		Y.Positive.Bindings.Add(new MouseMotionBinding(Vector2.UnitY, 1, 0, maxMotion, masks));
		return this;
	}

	public StickBinding AddArrowKeys()
		=> Add(Keys.Left, Keys.Right, Keys.Up, Keys.Down);

	public StickBinding AddArrowKeys(in ReadOnlySpan<string> masks)
		=> Add(masks, Keys.Left, Keys.Right, Keys.Up, Keys.Down);

	public StickBinding AddWasd()
		=> Add(Keys.A, Keys.D, Keys.W, Keys.S);

	public StickBinding AddWasd(in ReadOnlySpan<string> masks)
		=> Add(masks, Keys.A, Keys.D, Keys.W, Keys.S);

	public StickBinding AddLeftJoystick(float xDeadzone = 0, float yDeadzone = 0)
		=> Add(Axes.LeftX, Axes.LeftY, xDeadzone, yDeadzone);

	public StickBinding AddLeftJoystick(in ReadOnlySpan<string> masks, float xDeadzone = 0, float yDeadzone = 0)
		=> Add(masks, Axes.LeftX, Axes.LeftY, xDeadzone, yDeadzone);

	public StickBinding AddRightJoystick(float xDeadzone = 0, float yDeadzone = 0)
		=> Add(Axes.RightX, Axes.RightY, xDeadzone, yDeadzone);

	public StickBinding AddRightJoystick(in ReadOnlySpan<string> masks, float xDeadzone = 0, float yDeadzone = 0)
		=> Add(masks, Axes.RightX, Axes.RightY, xDeadzone, yDeadzone);

	public StickBinding AddDPad()
		=> Add(Buttons.Left, Buttons.Right, Buttons.Up, Buttons.Down);

	public StickBinding AddDPad(in ReadOnlySpan<string> masks)
		=> Add(masks, Buttons.Left, Buttons.Right, Buttons.Up, Buttons.Down);

	public Vector2 Value(Input input, int device, HashSet<string>? filters)
	{
		var it = new Vector2(
			X.Value(input, device, filters),
			Y.Value(input, device, filters));

		if (CircularDeadzone > 0 &&
			it.LengthSquared() < CircularDeadzone * CircularDeadzone)
			return Vector2.Zero;

		return it;
	}
}
