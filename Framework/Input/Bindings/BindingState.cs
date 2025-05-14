namespace Foster.Framework;

/// <summary>
/// A single-frame of state of a <see cref="Binding"/>
/// </summary>
/// <param name="Pressed">Value is > 0 after being 0 on the last frame</param>
/// <param name="Released">Value is 0 after being > 0 on the last frame</param>
/// <param name="Down">Value is currently > 0</param>
/// <param name="Value">Current state (0 = unpressed, 1 = fully pressed)</param>
/// <param name="Timestamp">When last pressed</param>
public readonly record struct BindingState(
	bool Pressed,
	bool Released,
	bool Down,
	float Value,
	TimeSpan Timestamp
);
