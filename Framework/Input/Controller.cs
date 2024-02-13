using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// Represents a Gamepad or Joystick
/// </summary>
public class Controller
{
	public const int MaxButtons = 64;
	public const int MaxAxis = 64;

	/// <summary>
	/// Name of the Controller if available
	/// </summary>
	public string Name { get; private set; } = "Unknown";

	/// <summary>
	/// If the Controller is currently connected
	/// </summary>
	public bool Connected { get; private set; } = false;

	/// <summary>
	/// If the Controller is considered a Gamepad (ex. an Xbox Controller).
	/// Otherwise Buttons &amp; Axis enums may not match up.
	/// </summary>
	public bool IsGamepad { get; private set; } = false;

	/// <summary>
	/// The Type of Gamepad
	/// </summary>
	public GamepadTypes GamepadType { get; private set; } = GamepadTypes.Unknown;

	/// <summary>
	/// The Gamepad Provider
	/// </summary>
	public GamepadProviders GamepadProvider => GamepadType switch
	{
		GamepadTypes.Unknown => GamepadProviders.Unknown,
		GamepadTypes.Xbox360 => GamepadProviders.Xbox,
		GamepadTypes.XboxOne => GamepadProviders.Xbox,
		GamepadTypes.PS3 => GamepadProviders.PlayStation,
		GamepadTypes.PS4 => GamepadProviders.PlayStation,
		GamepadTypes.NintendoSwitchPro => GamepadProviders.Nintendo,
		GamepadTypes.Virtual => GamepadProviders.Unknown,
		GamepadTypes.PS5 => GamepadProviders.PlayStation,
		GamepadTypes.AmazonLuna => GamepadProviders.Unknown,
		GamepadTypes.GoogleStadia => GamepadProviders.Unknown,
		GamepadTypes.NVidiaShield => GamepadProviders.Unknown,
		GamepadTypes.NintendoSwitchJoyconLeft => GamepadProviders.Nintendo,
		GamepadTypes.NintendoSwitchJoyconRight => GamepadProviders.Nintendo,
		GamepadTypes.NintendoSwitchJoyconPair => GamepadProviders.Nintendo,
		_ => GamepadProviders.Unknown
	};

	/// <summary>
	/// The Gamepad type, if known.
	/// </summary>
	[Obsolete("Use GamepadType or GamepadProvider instead")]
	public Gamepads Gamepad => Gamepads.Xbox;

	/// <summary>
	/// Number of Buttons
	/// </summary>
	public int Buttons { get; private set; } = 0;

	/// <summary>
	/// Number of Axes
	/// </summary>
	public int Axes { get; private set; } = 0;

	/// <summary>
	/// Vendor ID
	/// </summary>
	public ushort Vendor { get; private set; } = 0;

	/// <summary>
	/// Product ID
	/// </summary>
	public ushort Product { get; private set; } = 0;

	/// <summary>
	/// Version ID
	/// </summary>
	public ushort Version { get; private set; } = 0;

	internal readonly bool[] pressed = new bool[MaxButtons];
	internal readonly bool[] down = new bool[MaxButtons];
	internal readonly bool[] released = new bool[MaxButtons];
	internal readonly TimeSpan[] timestamp = new TimeSpan[MaxButtons];
	internal readonly float[] axis = new float[MaxAxis];
	internal readonly TimeSpan[] axisTimestamp = new TimeSpan[MaxAxis];

	internal void Connect(string name, int buttonCount, int axisCount, bool isGamepad, GamepadTypes type, ushort vendor, ushort product, ushort version)
	{
		Name = name;
		Buttons = Math.Min(buttonCount, MaxButtons);
		Axes = Math.Min(axisCount, MaxAxis);
		IsGamepad = isGamepad;
		GamepadType = type;
		Connected = true;
		Vendor = vendor;
		Product = product;
		Version = version;
	}

	internal void Disconnect()
	{
		Name = "Unknown";
		Connected = false;
		IsGamepad = false;
		GamepadType = GamepadTypes.Unknown;
		Buttons = 0;
		Axes = 0;
		Vendor = 0;
		Product = 0;
		Version = 0;

		Array.Fill(pressed, false);
		Array.Fill(down, false);
		Array.Fill(released, false);
		Array.Fill(timestamp, TimeSpan.Zero);
		Array.Fill(axis, 0);
		Array.Fill(axisTimestamp, TimeSpan.Zero);
	}

	internal void Step()
	{
		Array.Fill(pressed, false);
		Array.Fill(released, false);
	}

	internal void Copy(Controller other)
	{
		Name = other.Name;
		Connected = other.Connected;
		IsGamepad = other.IsGamepad;
		Buttons = other.Buttons;
		Axes = other.Axes;
		Product = other.Product;
		Vendor = other.Vendor;
		Version = other.Version;

		Array.Copy(other.pressed, 0, pressed, 0, pressed.Length);
		Array.Copy(other.down, 0, down, 0, pressed.Length);
		Array.Copy(other.released, 0, released, 0, pressed.Length);
		Array.Copy(other.timestamp, 0, timestamp, 0, pressed.Length);
		Array.Copy(other.axis, 0, axis, 0, axis.Length);
		Array.Copy(other.axisTimestamp, 0, axisTimestamp, 0, axis.Length);
	}

	public bool Pressed(int buttonIndex) => buttonIndex >= 0 && buttonIndex < MaxButtons && pressed[buttonIndex];
	public bool Pressed(Buttons button) => Pressed((int)button);

	public TimeSpan Timestamp(int buttonIndex) => buttonIndex >= 0 && buttonIndex < MaxButtons ? timestamp[buttonIndex] : TimeSpan.Zero;
	public TimeSpan Timestamp(Buttons button) => Timestamp((int)button);
	public TimeSpan Timestamp(Axes axis) => axisTimestamp[(int)axis];

	public bool Down(int buttonIndex) => buttonIndex >= 0 && buttonIndex < MaxButtons && down[buttonIndex];
	public bool Down(Buttons button) => Down((int)button);

	public bool Released(int buttonIndex) => buttonIndex >= 0 && buttonIndex < MaxButtons && released[buttonIndex];
	public bool Released(Buttons button) => Released((int)button);

	public float Axis(int axisIndex) => (axisIndex >= 0 && axisIndex < MaxAxis) ? axis[axisIndex] : 0f;
	public float Axis(Axes axis) => Axis((int)axis);

	public Vector2 Axis(int axisX, int axisY) => new Vector2(Axis(axisX), Axis(axisY));
	public Vector2 Axis(Axes axisX, Axes axisY) => new Vector2(Axis(axisX), Axis(axisY));

	public Vector2 LeftStick => Axis(Foster.Framework.Axes.LeftX, Foster.Framework.Axes.LeftY);
	public Vector2 RightStick => Axis(Foster.Framework.Axes.RightX, Foster.Framework.Axes.RightY);

	public bool Repeated(Buttons button)
	{
		return Repeated(button, Input.RepeatDelay, Input.RepeatInterval);
	}

	public bool Repeated(Buttons button, float delay, float interval)
	{
		if (Pressed(button))
			return true;

		if (Down(button))
		{
			var time = Timestamp(button) / 1000.0;
			return (Time.Duration - time).TotalSeconds > delay && Time.OnInterval(interval, time.TotalSeconds);
		}

		return false;
	}
}
