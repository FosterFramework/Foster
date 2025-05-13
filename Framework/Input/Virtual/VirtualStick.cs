using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A virtual 2D Axis/Stick input, which detects user input mapped through a <see cref="StickBinding"/>.
/// </summary>
public sealed class VirtualStick(Input input, string name, StickBinding binding, int controllerIndex = 0) : VirtualInput(input, name)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly StickBinding Binding = binding;

	/// <summary>
	/// The Device Index
	/// </summary>
	public int Device = controllerIndex;

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

	public VirtualStick(Input input, string name, int controllerIndex = 0)
		: this(input, name, new(), controllerIndex) {}

	internal override void Update(in Time time)
	{
		Value = Binding.Value(Input, Device, Input.BindingFilters);
		IntValue = new(MathF.Sign(Value.X), MathF.Sign(Value.Y));
		PressedLeft = Binding.X.Negative.GetState(Input, Device, Input.BindingFilters).Pressed;
		PressedRight = Binding.X.Positive.GetState(Input, Device, Input.BindingFilters).Pressed;
		PressedUp = Binding.Y.Negative.GetState(Input, Device, Input.BindingFilters).Pressed;
		PressedDown = Binding.Y.Positive.GetState(Input, Device, Input.BindingFilters).Pressed;
	}

	public void Clear()
	{
		Value = Vector2.Zero;
		IntValue = Point2.Zero;
		PressedLeft = PressedRight = PressedUp = PressedDown = false;
	}
}
