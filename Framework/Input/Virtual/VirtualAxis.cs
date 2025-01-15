using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A virtual Axis input, which detects user input mapped through a <see cref="AxisBinding"/>.
/// </summary>
public sealed class VirtualAxis(Input input, AxisBinding binding, int controllerIndex = 0) : VirtualInput(input)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly AxisBinding Binding = binding;

	/// <summary>
	/// The Device Index
	/// </summary>
	public readonly int Device = controllerIndex;

	/// <summary>
	/// Current Value of the Virtual Axis
	/// </summary>
	public float Value { get; private set; }

	/// <summary>
	/// Current Value of the Virtual Axis as an integer
	/// </summary>
	public int IntValue { get; private set; }

	/// <summary>
	/// Current Value without deadzones of the Virtual Axis
	/// </summary>
	public float ValueNoDeadzone { get; private set; }

	/// <summary>
	/// Current Value without deadzones of the Virtual Axis as an integer
	/// </summary>
	public int IntValueNoDeadzone { get; private set; }

	/// <summary>
	/// 
	/// </summary>
	public bool Pressed { get; private set; }

	/// <summary>
	/// 
	/// </summary>
	public int PressedSign { get; private set; }

	internal override void Update(in Time time)
	{
		Value = Binding.Value(Input, Device);
		IntValue = (int)Value;
		ValueNoDeadzone = Binding.Value(Input, Device);
		IntValueNoDeadzone = (int)ValueNoDeadzone;
		
		Pressed = Value switch
		{
			> 0 => Binding.Positive.GetState(Input, Device).Pressed,
			< 0 => Binding.Negative.GetState(Input, Device).Pressed,
			_ => false,
		};

		PressedSign = Value switch
		{
			> 0 => (Binding.Positive.GetState(Input, Device).Pressed ? 1 : 0),
			< 0 => (Binding.Negative.GetState(Input, Device).Pressed ? -1 : 0),
			_ => 0,
		};
	}

	public void Clear()
	{
		Value = 0;
		IntValue = 0;
		ValueNoDeadzone = 0;
		IntValueNoDeadzone = 0;
		Pressed = false;
		PressedSign = 0;
	}
}