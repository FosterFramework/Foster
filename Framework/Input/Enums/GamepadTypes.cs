namespace Foster.Framework;

/// <summary>
/// Type of Gamepads.
/// This enum is a one-to-one mapping of SDL_GamepadType:
/// https://wiki.libsdl.org/SDL3/SDL_GamepadType
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

public static class GamepadTypesExt
{
	public static GamepadProviders Provider(this GamepadTypes type) => type switch
	{
		GamepadTypes.Unknown => GamepadProviders.Unknown,
		GamepadTypes.Standard => GamepadProviders.Xbox,
		GamepadTypes.Xbox360 => GamepadProviders.Xbox,
		GamepadTypes.XboxOne => GamepadProviders.Xbox,
		GamepadTypes.PS3 => GamepadProviders.PlayStation,
		GamepadTypes.PS4 => GamepadProviders.PlayStation,
		GamepadTypes.PS5 => GamepadProviders.PlayStation,
		GamepadTypes.NintendoSwitchPro => GamepadProviders.Nintendo,
		GamepadTypes.NintendoSwitchJoyconLeft => GamepadProviders.Nintendo,
		GamepadTypes.NintendoSwitchJoyconRight => GamepadProviders.Nintendo,
		GamepadTypes.NintendoSwitchJoyconPair => GamepadProviders.Nintendo,
		_ => GamepadProviders.Unknown
	};
}