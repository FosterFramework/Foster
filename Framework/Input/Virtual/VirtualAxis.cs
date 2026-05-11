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
	/// How long before invoking the first Repeated signal
	/// </summary>
	public float RepeatDelay;

	/// <summary>
	/// How frequently to invoke a Repeated signal
	/// </summary>
	public float RepeatInterval;

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

	/// <summary>
	/// The last time the axis was pressed, but not by repeating
	/// </summary>
	public TimeSpan LastPressTimestamp { get; private set; }

	/// <summary>
	/// If the Axis was repeated this frame, which resulted in a press registering
	/// </summary>
	public bool Repeated { get; private set; }

	public override int ControllerIndex { get; set; }

	private int lastDownSign;

	public VirtualAxis(Input input, string name, int controllerIndex = 0)
		: this(input, name, new(), controllerIndex) {}

	internal override void Update(in Time time)
	{
		Value       = Set.Value(Input, ControllerIndex);
		IntValue    = MathF.Sign(Value);
		PressedSign = Set.PressedSign(Input, ControllerIndex);

		// repeating logic
		Repeated = false;
		if (IntValue == 0)
			lastDownSign = 0;
		else if (PressedSign != 0)
		{
			lastDownSign = PressedSign;
			LastPressTimestamp = time.Elapsed;
		}
		else if (lastDownSign == IntValue && PressedSign == 0 && lastDownSign != 0
		&& RepeatInterval > 0 && (time.Elapsed - LastPressTimestamp).TotalSeconds > RepeatDelay)
			if (Calc.OnInterval((time.Elapsed - LastPressTimestamp).TotalSeconds - RepeatDelay, time.Delta, RepeatInterval))
			{
				PressedSign = lastDownSign;
				Repeated    = true;
			}
	}

	/// <summary>
	/// Manually set the state of this <see cref="VirtualAxis"/> for this frame from a single float
	/// </summary>
	public void ManualUpdate(in Time time, float value)
	{
		var prevIntValue = IntValue;

		Value    = value;
		IntValue = MathF.Sign(Value);

		if (IntValue != 0 && IntValue != prevIntValue)
			PressedSign = IntValue;
		else
			PressedSign = 0;

		// repeating logic
		Repeated = false;
		if (IntValue == 0)
			lastDownSign = 0;
		else if (PressedSign != 0)
		{
			lastDownSign      = PressedSign;
			LastPressTimestamp = time.Elapsed;
		}
		else if (lastDownSign == IntValue && lastDownSign != 0 && RepeatInterval > 0
		&& (time.Elapsed - LastPressTimestamp).TotalSeconds > RepeatDelay)
			if (Calc.OnInterval((time.Elapsed - LastPressTimestamp).TotalSeconds - RepeatDelay, time.Delta, RepeatInterval))
			{
				PressedSign = lastDownSign;
				Repeated    = true;
			}
	}

	public void Clear()
	{
		Value = 0;
		IntValue = 0;
		PressedSign = 0;
	}
}
