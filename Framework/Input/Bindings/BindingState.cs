namespace Foster.Framework;

/// <summary>
/// A single-frame state of an Input Binding
/// </summary>
public readonly record struct BindingState(
	bool Pressed,
	bool Released,
	bool Down,
	float Value,
	float ValueNoDeadzone,
	TimeSpan Timestamp
);