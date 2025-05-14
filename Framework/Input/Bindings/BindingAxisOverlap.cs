using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// How to handle Input Bindings Axis that overlap
/// </summary>
public enum BindingAxisOverlap
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

public static class BindingAxisOverlapExt
{
	public static float Resolve(this BindingAxisOverlap overlap, in BindingState negative, in BindingState positive)
	{
		if (overlap == BindingAxisOverlap.CancelOut)
		{
			return Calc.Clamp(positive.Value - negative.Value, -1, 1);
		}
		else if (overlap == BindingAxisOverlap.TakeNewer)
		{
			if (positive.Down && negative.Down)
				return negative.Timestamp > positive.Timestamp ? -negative.Value : positive.Value;
			else if (positive.Down)
				return positive.Value;
			else if (negative.Down)
				return -negative.Value;
		}
		else if (overlap == BindingAxisOverlap.TakeOlder)
		{
			if (positive.Down && negative.Down)
				return negative.Timestamp < positive.Timestamp ? -negative.Value : positive.Value;
			else if (positive.Down)
				return positive.Value;
			else if (negative.Down)
				return -negative.Value;
		}

		return 0;
	}
}