namespace Foster.Framework;

/// <summary>
/// Gamepad Buttons
/// </summary>
public enum Buttons
{
	None = -1,

	/// <summary>
	/// Bottom Face Button
	/// </summary>
	South = 0,

	/// <summary>
	/// Right Face Button
	/// </summary>
	East = 1,

	/// <summary>
	/// Leftt Face Button
	/// </summary>
	West = 2,

	/// <summary>
	/// Top Face Button
	/// </summary>
	North = 3,

	/// <summary>
	/// Xbox360 Back button, Xbox One Change View, PS4/PS5 Share Button, Switch Pro Minus Button, etc
	/// </summary>
	Back = 4,

	/// <summary>
	/// Xbox360 Big Button, Xbox One Home Button, PS4/PS5 Big Button, Switch Pro Home Button, etc
	/// </summary>
	Guide = 5,

	/// <summary>
	/// Xbox360 Start Button, XBox One Select Button, PS4/PS5 Options Button, Switch Pro Plus Button, etc
	/// </summary>
	Start = 6,

	/// <summary>
	/// Left Stick Press
	/// </summary>
	LeftStick = 7,

	/// <summary>
	/// Right Stick Press
	/// </summary>
	RightStick = 8,

	/// <summary>
	/// Left Shoulder Button
	/// </summary>
	LeftShoulder = 9,

	/// <summary>
	/// Right Shoulder Button
	/// </summary>
	RightShoulder = 10,

	/// <summary>
	/// Left D-Pad Up
	/// </summary>
	Up = 11,

	/// <summary>
	/// Left D-Pad Down
	/// </summary>
	Down = 12,

	/// <summary>
	/// Left D-Pad Left
	/// </summary>
	Left = 13,

	/// <summary>
	/// Left D-Pad Right
	/// </summary>
	Right = 14,

	[Obsolete("Use Buttons.South")] A = 0,
	[Obsolete("Use Buttons.East")]  B = 1,
	[Obsolete("Use Buttons.West")]  X = 2,
	[Obsolete("Use Buttons.North")] Y = 3,
	[Obsolete("Use Buttons.Guide")] Select = 5,
}

