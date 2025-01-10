using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input binding that represents an 1D Axis
/// </summary>
public class AxisBinding
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
	/// How to handle overlapping inputs between negative and positive bindings
	/// </summary>
	[JsonInclude] public Overlaps OverlapBehaviour;

	/// <summary>
	/// Negative Value Bindings
	/// </summary>
	[JsonInclude] public readonly ActionBinding Negative = new();

	/// <summary>
	/// Positive Value Bindings
	/// </summary>
	[JsonInclude] public readonly ActionBinding Positive = new();

	public AxisBinding() {}

	public AxisBinding(params ReadOnlySpan<(Binding Negative, Binding Positive)> bindings)
	{
		foreach (var it in bindings)
		{
			Positive.Bindings.Add(it.Positive);
			Negative.Bindings.Add(it.Negative);
		}
	}

	public AxisBinding Add(Keys negative, Keys positive)
	{
		Negative.Add(negative);
		Positive.Add(positive);
		return this;
	}

	public AxisBinding Add(Buttons negative, Buttons positive)
	{
		Negative.Add(negative);
		Positive.Add(positive);
		return this;
	}

	public AxisBinding Add(Axes axis, float deadzone = 0)
	{
		Negative.Add(axis, -1, deadzone);
		Positive.Add(axis, 1, deadzone);
		return this;
	}

	public float Value(Input input, int device)
		=> Value(input, device, true);
	
	public float ValueNoDeadzone(Input input, int device)
		=> Value(input, device, false);

	private float Value(Input input, int device, bool deadzone)
	{
		var negativeState = Negative.GetState(input, device);
		var negativeValue = -(deadzone ? negativeState.Value : negativeState.ValueNoDeadzone);
		var positiveState = Positive.GetState(input, device);
		var positiveValue = deadzone ? positiveState.Value : positiveState.ValueNoDeadzone;

		if (OverlapBehaviour == Overlaps.CancelOut)
		{
			return Calc.Clamp(negativeValue + positiveValue, -1, 1);
		}
		else if (OverlapBehaviour == Overlaps.TakeNewer)
		{
			if (positiveState.Down && negativeState.Down)
				return negativeState.Timestamp > positiveState.Timestamp ? negativeValue : positiveValue;
			else if (positiveState.Down)
				return positiveValue;
			else if (negativeState.Down)
				return negativeValue;
		}
		else if (OverlapBehaviour == Overlaps.TakeOlder)
		{
			if (positiveState.Down && negativeState.Down)
				return negativeState.Timestamp < positiveState.Timestamp ? negativeValue : positiveValue;
			else if (positiveState.Down)
				return positiveValue;
			else if (negativeState.Down)
				return negativeValue;
		}

		return 0;
	}
}