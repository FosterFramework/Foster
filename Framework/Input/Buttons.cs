namespace Foster.Framework;

/// <summary>
/// Gamepad Buttons
/// </summary>
public enum Buttons
{
	None = -1,

	South = 0,
	East = 1,
	West = 2,
	North = 3,
	Back = 4,
	Select = 5,
	Start = 6,
	LeftStick = 7,
	RightStick = 8,
	LeftShoulder = 9,
	RightShoulder = 10,
	Up = 11,
	Down = 12,
	Left = 13,
	Right = 14,

	[Obsolete("Use Buttons.South")] A = 0,
	[Obsolete("Use Buttons.East")]  B = 1,
	[Obsolete("Use Buttons.West")]  X = 2,
	[Obsolete("Use Buttons.North")] Y = 3,
}

