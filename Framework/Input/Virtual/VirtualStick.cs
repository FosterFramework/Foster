using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A virtual 2D Axis/Stick input, which detects user input mapped through a <see cref="StickBinding"/>.
/// </summary>
public sealed class VirtualStick(Input input, StickBinding binding, int controllerIndex = 0) : VirtualInput(input)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly StickBinding Binding = binding;

	/// <summary>
	/// The Device Index
	/// </summary>
	public readonly int Device = controllerIndex;

	/// <summary>
	/// Current Value of the Virtual Stick
	/// </summary>
	public Vector2 Value { get; private set; }

	/// <summary>
	/// Current Value of the Virtual Stick rounded to Integer values
	/// </summary>
	public Point2 IntValue { get; private set; }

	public bool PressedLeft { get; private set; }

	public bool PressedRight { get; private set; }

	public bool PressedUp { get; private set; }

	public bool PressedDown { get; private set; }

	public VirtualStick(Input input, int controllerIndex = 0)
		: this(input, new(), controllerIndex) {}

	internal override void Update(in Time time)
	{
		Value = Binding.Value(Input, Device);
		IntValue = Value.RoundToPoint2();
		PressedLeft = Binding.X.Negative.GetState(Input, Device).Pressed;
		PressedRight = Binding.X.Positive.GetState(Input, Device).Pressed;
		PressedUp = Binding.Y.Negative.GetState(Input, Device).Pressed;
		PressedDown = Binding.Y.Positive.GetState(Input, Device).Pressed;
	}

	public void Clear()
	{
		Value = Vector2.Zero;
		IntValue = Point2.Zero;
		PressedLeft = PressedRight = PressedUp = PressedDown = false;
	}
}
