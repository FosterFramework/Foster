using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A virtual Axis input, which detects user input mapped through a <see cref="AxisBindingSet"/>.
/// </summary>
public sealed class VirtualAxis(Input input, string name, AxisBindingSet set, int controllerIndex = 0) : VirtualInput(input, name, controllerIndex)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly AxisBindingSet Set = set;

	/// <summary>
	/// The Binding Axis Entries
	/// </summary>
	public List<AxisBindingSet.AxisEntry> Entries => Set.Entries;

	/// <summary>
	/// The Device Index
	/// </summary>
	[Obsolete("use ControllerIndex instead")]
	public int Device { get => ControllerIndex; set => ControllerIndex = value; }

	/// <summary>
	/// Current Value of the Virtual Axis
	/// </summary>
	public float Value { get; private set; }

	/// <summary>
	/// Current Value of the Virtual Axis as an integer
	/// </summary>
	public int IntValue { get; private set; }

	/// <summary>
	/// If the Axis was pressed this frame (ie. was 0, now non-zero)
	/// </summary>
	public bool Pressed => PressedSign != 0;

	/// <summary>
	/// If a Negative binding was pressed this frame
	/// </summary>
	public bool PressedNegative => PressedSign < 0;

	/// <summary>
	/// If a Positive binding was pressed this frame
	/// </summary>
	public bool PressedPositive => PressedSign > 0;

	/// <summary>
	/// The Sign of the press this frame (or 0 if not pressed this frame)
	/// </summary>
	public int PressedSign { get; private set; }
	
	public override int ControllerIndex { get; set; }

	public VirtualAxis(Input input, string name, int controllerIndex = 0)
		: this(input, name, new(), controllerIndex) {}

	internal override void Update(in Time time)
	{
		Value = Set.Value(Input, ControllerIndex);
		IntValue = MathF.Sign(Value);
		PressedSign = Set.PressedSign(Input, ControllerIndex);
	}

	public void Clear()
	{
		Value = 0;
		IntValue = 0;
		PressedSign = 0;
	}
}
