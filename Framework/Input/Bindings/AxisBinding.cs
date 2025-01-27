using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input binding that represents an 1D Axis
/// </summary>
public sealed class AxisBinding
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

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public AxisBinding Add(Keys negative, Keys positive)
	{
		Negative.Add(negative);
		Positive.Add(positive);
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public AxisBinding Add(Buttons negative, Buttons positive)
	{
		Negative.Add(negative);
		Positive.Add(positive);
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public AxisBinding Add(Axes axis, float deadzone = 0)
	{
		Negative.Add(axis, -1, deadzone);
		Positive.Add(axis, 1, deadzone);
		return this;
	}

	/// <summary>
	/// Gets the current Value of the Axis from the provided Input
	/// </summary>
	public float Value(Input input, int device)
	{
		var negativeState = Negative.GetState(input, device);
		var positiveState = Positive.GetState(input, device);

		if (OverlapBehaviour == Overlaps.CancelOut)
		{
			return Calc.Clamp(positiveState.Value - negativeState.Value, -1, 1);
		}
		else if (OverlapBehaviour == Overlaps.TakeNewer)
		{
			if (positiveState.Down && negativeState.Down)
				return negativeState.Timestamp > positiveState.Timestamp ? -negativeState.Value : positiveState.Value;
			else if (positiveState.Down)
				return positiveState.Value;
			else if (negativeState.Down)
				return -negativeState.Value;
		}
		else if (OverlapBehaviour == Overlaps.TakeOlder)
		{
			if (positiveState.Down && negativeState.Down)
				return negativeState.Timestamp < positiveState.Timestamp ? -negativeState.Value : positiveState.Value;
			else if (positiveState.Down)
				return positiveState.Value;
			else if (negativeState.Down)
				return -negativeState.Value;
		}

		return 0;
	}
}
