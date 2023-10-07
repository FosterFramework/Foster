namespace Foster.Framework;

/// <summary>
/// A Virtual Input Axis that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualAxis
{
	public enum Overlaps
	{
		/// <summary>
		/// Uses whichever input was pressed most recently
		/// </summary>
		TakeNewer,

		/// <summary>
		/// Uses whichever input was pressed longest ago
		/// </summary>
		TakeOlder,

		/// <summary>
		/// Inputs cancel each other out
		/// </summary>d
		CancelOut,
	};

	/// <summary>
	/// Optional Virtual Axis name
	/// </summary>
	public readonly string Name;

	public float Value => GetValue(true);
	public float ValueNoDeadzone => GetValue(false);

	public int IntValue => Math.Sign(Value);
	public int IntValueNoDeadzone => Math.Sign(ValueNoDeadzone);

	public readonly VirtualButton Negative;
	public readonly VirtualButton Positive;

	public Overlaps OverlapBehaviour = Overlaps.TakeNewer;

	public VirtualAxis(string name, Overlaps overlapBehavior = Overlaps.TakeNewer)
	{
		Name = name;
		Negative = new VirtualButton($"{name}/Negative");
		Positive = new VirtualButton($"{name}/Positive");
		OverlapBehaviour = overlapBehavior;
	}

	public VirtualAxis(Overlaps overlapBehaviour = Overlaps.TakeNewer)
		: this("VirtualAxis", overlapBehaviour) {}

	private float GetValue(bool deadzone)
	{
		var value = 0f;
		var negativeValue = -(deadzone ? Negative.Value : Negative.ValueNoDeadzone);
		var positiveValue = (deadzone ? Positive.Value : Positive.ValueNoDeadzone);

		if (OverlapBehaviour == Overlaps.CancelOut)
		{
			value = Calc.Clamp(negativeValue + positiveValue, -1, 1);
		}
		else if (OverlapBehaviour == Overlaps.TakeNewer)
		{
			if (Negative.PressTimestamp > Positive.PressTimestamp)
				value = negativeValue;
			else
				value = positiveValue;
		}
		else if (OverlapBehaviour == Overlaps.TakeOlder)
		{
			if (Negative.PressTimestamp < Positive.PressTimestamp)
				value = negativeValue;
			else
				value = positiveValue;
		}

		return value;
	}

	public VirtualAxis Add(Keys negative, Keys positive)
	{
		Negative.Add(negative);
		Positive.Add(positive);
		return this;
	}

	public VirtualAxis AddPositive(Keys key)
	{
		Positive.Add(key);
		return this;
	}

	public VirtualAxis AddNegative(Keys key)
	{
		Positive.Add(key);
		return this;
	}

	public VirtualAxis Add(int controller, Buttons negative, Buttons positive)
	{
		Negative.Add(controller, negative);
		Positive.Add(controller, positive);
		return this;
	}

	public VirtualAxis AddPositive(int controller, Buttons button)
	{
		Positive.Add(controller, button);
		return this;
	}

	public VirtualAxis AddNegative(int controller, Buttons button)
	{
		Negative.Add(controller, button);
		return this;
	}

	public VirtualAxis Add(int controller, Axes axis, float deadzone = 0f)
	{
		Negative.Add(controller, axis, -1, deadzone);
		Positive.Add(controller, axis, 1, deadzone);
		return this;
	}

	public VirtualAxis AddPositive(int controller, Axes axis, int sign, float deadzone = 0f)
	{
		Positive.Add(controller, axis, sign, deadzone);
		return this;
	}

	public VirtualAxis AddNegative(int controller, Axes axis, int sign, float deadzone = 0f)
	{
		Negative.Add(controller, axis, sign, deadzone);
		return this;
	}
	
	public void Consume()
	{
		Negative.Consume();
		Positive.Consume();
	}

	public void ConsumePress()
	{
		Negative.ConsumePress();
		Positive.ConsumePress();
	}

	public void ConsumeRelease()
	{
		Negative.ConsumeRelease();
		Positive.ConsumeRelease();
	}

	public void Clear()
	{
		Negative.Clear();
		Positive.Clear();
	}
}
