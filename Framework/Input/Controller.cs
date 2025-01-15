using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// Unique ID per Controller.
/// Every time a Controller is connected/disconnected, it is given a new ID.
/// </summary>
public readonly record struct ControllerID(uint Value);

/// <summary>
/// Stores the state of a GamePad or Joystick Controller.
/// </summary>
public class Controller(InputProvider provider, int index)
{
	public const int MaxButtons = 64;
	public const int MaxAxis = 64;

	/// <summary>
	/// The Input Provider we belong to
	/// </summary>
	private readonly InputProvider provider = provider;

	/// <summary>
	/// The current Controller Slot Index.
	/// This does not change while the Controller is connected, even if there are
	/// empty slots earlier in the list of Controllers.
	/// </summary>
	public readonly int Index = index;

	/// <summary>
	/// A unique ID for the Controller, which persists while it is connected.
	/// If a Controller is disconected and reconnected, it will be given a new ID.
	/// </summary>
	public ControllerID ID { get; private set; }

	/// <summary>
	/// Name of the Controller if available
	/// </summary>
	public string Name { get; private set; } = "Unknown";

	/// <summary>
	/// If the Controller is currently connected
	/// </summary>
	public bool Connected { get; private set; }

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

	private readonly bool[] pressed = new bool[MaxButtons];
	private readonly bool[] down = new bool[MaxButtons];
	private readonly bool[] released = new bool[MaxButtons];
	private readonly TimeSpan[] timestamp = new TimeSpan[MaxButtons];
	private readonly float[] axis = new float[MaxAxis];
	private readonly TimeSpan[] axisTimestamp = new TimeSpan[MaxAxis];
	private Time time;

	internal void Connect(ControllerID id, string name, int buttonCount, int axisCount, bool isGamepad, GamepadTypes type, ushort vendor, ushort product, ushort version)
	{
		ID = id;
		Connected = true;
		Name = name;
		Buttons = Math.Min(buttonCount, MaxButtons);
		Axes = Math.Min(axisCount, MaxAxis);
		IsGamepad = isGamepad;
		GamepadType = type;
		Vendor = vendor;
		Product = product;
		Version = version;
	}

	internal void Disconnect()
	{
		ID = default;
		Connected = false;
		Name = "Unknown";
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

	internal void Step(in Time time)
	{
		Array.Fill(pressed, false);
		Array.Fill(released, false);
		this.time = time;
	}

	internal void Copy(Controller other)
	{
		ID = other.ID;
		Connected = other.Connected;
		Name = other.Name;
		IsGamepad = other.IsGamepad;
		GamepadType = other.GamepadType;
		Buttons = other.Buttons;
		Axes = other.Axes;
		Product = other.Product;
		Vendor = other.Vendor;
		Version = other.Version;
		time = other.time;

		Array.Copy(other.pressed, 0, pressed, 0, pressed.Length);
		Array.Copy(other.down, 0, down, 0, pressed.Length);
		Array.Copy(other.released, 0, released, 0, pressed.Length);
		Array.Copy(other.timestamp, 0, timestamp, 0, pressed.Length);
		Array.Copy(other.axis, 0, axis, 0, axis.Length);
		Array.Copy(other.axisTimestamp, 0, axisTimestamp, 0, axis.Length);
	}

	internal void OnButton(int buttonIndex, bool buttonPressed, in TimeSpan time)
	{
		if (buttonIndex >= 0 && buttonIndex < MaxButtons)
		{
			if (buttonPressed)
			{
				down[buttonIndex] = true;
				pressed[buttonIndex] = true;
				timestamp[buttonIndex] = time;
			}
			else
			{
				down[buttonIndex] = false;
				released[buttonIndex] = true;
			}
		}
	}

	internal void OnAxis(int axisIndex, float axisValue, in TimeSpan time)
	{
		if (axisIndex >= 0 && axisIndex < MaxAxis)
		{
			axis[axisIndex] = axisValue;
			axisTimestamp[axisIndex] = time;
		}
	}

	/// <summary>
	/// Rumbles the Controller for a give duration.
	/// This will cancel any previous rumble effects.
	/// </summary>
	/// <param name="intensity">From 0.0 to 1.0 intensity of the Rumble</param>
	/// <param name="duration">How long, in seconds, for the Rumble to last</param>
	public void Rumble(float intensity, float duration)
		=> Rumble(intensity, intensity, duration);

	/// <summary>
	/// Rumbles the Controller for a give duration.
	/// This will cancel any previous rumble effects.
	/// </summary>
	/// <param name="lowIntensity">From 0.0 to 1.0 intensity of the Low-Intensity Rumble</param>
	/// <param name="highIntensity">From 0.0 to 1.0 intensity of the High-Intensity Rumble</param>
	/// <param name="duration">How long, in seconds, for the Rumble to last</param>
	public void Rumble(float lowIntensity, float highIntensity, float duration)
	{
		if (!Connected)
			return;

		provider.Rumble(ID, lowIntensity, highIntensity, duration);
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

	public Vector2 Axis(int axisX, int axisY) => new(Axis(axisX), Axis(axisY));
	public Vector2 Axis(Axes axisX, Axes axisY) => new(Axis(axisX), Axis(axisY));

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
			var stamp = Timestamp(button) / 1000.0;
			return (time.Elapsed - stamp).TotalSeconds > delay && Time.OnInterval(time, interval, stamp.TotalSeconds);
		}

		return false;
	}
}
