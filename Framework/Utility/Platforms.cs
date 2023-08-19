namespace Foster.Framework;

public enum Platforms
{
	Unknown,
	Linux,
	Windows,
	MacOS,
	FreeBSD,
	NintendoSwitch,
	PlayStation4,
	PlayStation5,
	XBox,
}

public static class PlatformsExt
{
	public static bool IsUnknown(this Platforms value) => value == Platforms.Unknown;
	public static bool IsDesktop(this Platforms value) => value == Platforms.Windows || value == Platforms.MacOS || value == Platforms.Linux;
	public static bool IsConsole(this Platforms value) => value != Platforms.Unknown && !value.IsDesktop();
	public static bool IsGamepadOnly(this Platforms value) => value.IsConsole();
}