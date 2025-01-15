namespace Foster.Framework;

/// <summary>
/// Type of Gamepads.
/// This is a 1-1 mapping with SDL_GamepadType.
/// </summary>
public enum GamepadTypes
{
	Unknown = 0,
	Standard,
	Xbox360,
	XboxOne,
	PS3,
	PS4,
	PS5,
	NintendoSwitchPro,
	NintendoSwitchJoyconLeft,
	NintendoSwitchJoyconRight,
	NintendoSwitchJoyconPair,
}

/// <summary>
/// Known popular gamepad types
/// </summary>
[Obsolete("Use GamepadTypes or GamepadProviders instead")]
public enum Gamepads
{
	Xbox,
	DualShock4,
	DualSense,
	Nintendo
}