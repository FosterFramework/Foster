using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Virtual Input Axis that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualAxis(Input input, string name, VirtualAxis.Overlaps overlapBehavior = VirtualAxis.Overlaps.TakeNewer)
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
	public readonly string Name = name;

	public float Value => GetValue(true);
	public float ValueNoDeadzone => GetValue(false);

	public int IntValue => Math.Sign(Value);
	public int IntValueNoDeadzone => Math.Sign(ValueNoDeadzone);

	public readonly VirtualButton Negative = new(input, $"{name}/Negative");
	public readonly VirtualButton Positive = new(input, $"{name}/Positive");

	public Overlaps OverlapBehaviour = overlapBehavior;

	public VirtualAxis(Input input, Overlaps overlapBehaviour = Overlaps.TakeNewer)
		: this(input, "VirtualAxis", overlapBehaviour) {}

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
			if (Positive.Down && Negative.Down)
				value = Negative.PressTimestamp > Positive.PressTimestamp ? negativeValue : positiveValue;
			else if (Positive.Down)
				value = positiveValue;
			else if (Negative.Down)
				value = negativeValue;
		}
		else if (OverlapBehaviour == Overlaps.TakeOlder)
		{
			if (Positive.Down && Negative.Down)
				value = Negative.PressTimestamp < Positive.PressTimestamp ? negativeValue : positiveValue;
			else if (Positive.Down)
				value = positiveValue;
			else if (Negative.Down)
				value = negativeValue;
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
		Negative.Add(key);
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

	public VirtualAxis AddMouseMotion(Vector2 axis, float maximumValue)
	{
		Negative.AddMouseMotion(axis, -1, maximumValue);
		Positive.AddMouseMotion(axis, 1, maximumValue);
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
