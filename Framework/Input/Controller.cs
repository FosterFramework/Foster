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

	internal void Connect(string name, int buttonCount, int axisCount, bool isGamepad, ushort vendor, ushort product, ushort version)
	{
		Name = name;
		Buttons = Math.Min(buttonCount, MaxButtons);
		Axes = Math.Min(axisCount, MaxAxis);
		IsGamepad = isGamepad;
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

		Array.Copy(other.pressed, 0, pressed, 0, pressed.Length);
		Array.Copy(other.down, 0, down, 0, pressed.Length);
		Array.Copy(other.released, 0, released, 0, pressed.Length);
		Array.Copy(other.timestamp, 0, timestamp, 0, pressed.Length);
		Array.Copy(other.axis, 0, axis, 0, axis.Length);
		Array.Copy(other.axisTimestamp, 0, axisTimestamp, 0, axis.Length);
	}

	/// <summary>
	/// The Gamepad type, if known.
	/// </summary>
	public Gamepads Gamepad
	{
		get
		{
			var vendor  = ((Vendor  >> 8) & 0x00FF) | ((Vendor  << 8) & 0xFF00);
			var product = ((Product >> 8) & 0x00FF) | ((Product << 8) & 0xFF00);
			var id = (vendor << 16) | product;

			if (id == 0x4c05c405 || id == 0x4c05cc09)
				return Gamepads.DualShock4;
			
			if (id == 0x4c05e60c)
				return Gamepads.DualSense;

			if (id == 0x7e050920 || id == 0x7e053003)
				return Gamepads.Nintendo;

			return Gamepads.Xbox;
		}
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
