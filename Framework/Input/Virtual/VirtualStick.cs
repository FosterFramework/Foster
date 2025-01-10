using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A virtual 2D Axis/Stick input.
/// Call <see cref="Input.CreateStick(in StickBinding, int)"/> to instantiate one.
/// </summary>
public sealed class VirtualStick: VirtualInput
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly StickBinding Binding;

	/// <summary>
	/// The Device Index
	/// </summary>
	public readonly int Device;

	/// <summary>
	/// Current Value of the Virtual Stick
	/// </summary>
	public Vector2 Value { get; private set; }

	/// <summary>
	/// Current Value of the Virtual Stick rounded to Integer values
	/// </summary>
	public Point2 IntValue { get; private set; }

	/// <summary>
	/// Current Value without deadzones of the Virtual Stick
	/// </summary>
	public Vector2 ValueNoDeadzone { get; private set; }

	/// <summary>
	/// Current Value without deadzones of the Virtual Stick rounded to Integer values
	/// </summary>
	public Point2 IntValueNoDeadzone { get; private set; }

	internal VirtualStick(Input input, StickBinding binding, int device) : base(input)
	{
		Binding = binding;
		Device = device;
	}

	internal override void Update(in Time time)
	{
		Value = Binding.Value(Input, Device);
		IntValue = Value.RoundToPoint2();
		ValueNoDeadzone = Binding.Value(Input, Device);
		IntValueNoDeadzone = ValueNoDeadzone.RoundToPoint2();
	}
}