namespace Foster.Framework;

/// <summary>
/// Type of Gamepads
/// This is a 1-1 mapping with SDL_GameControllerType:
/// https://github.com/libsdl-org/SDL/blob/release-2.30.x/include/SDL_gamecontroller.h#L61
/// </summary>
public enum GamepadTypes
{
    Unknown = 0,
    Xbox360,
    XboxOne,
    PS3,
    PS4,
    NintendoSwitchPro,
    Virtual,
    PS5,
    AmazonLuna,
    GoogleStadia,
    NVidiaShield,
    NintendoSwitchJoyconLeft,
    NintendoSwitchJoyconRight,
    NintendoSwitchJoyconPair,
}

/// <summary>
/// Types of GamePad Providers
/// </summary>
public enum GamepadProviders
{
    Unknown,
	Xbox,
	PlayStation,
	Nintendo
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