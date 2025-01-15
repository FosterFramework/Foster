using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// Stores the state of the Mouse
/// </summary>
public sealed class Mouse
{
	public const int MaxButtons = 5;

	private readonly bool[] pressed = new bool[MaxButtons];
	private readonly bool[] down = new bool[MaxButtons];
	private readonly bool[] released = new bool[MaxButtons];
	private readonly TimeSpan[] timestamp = new TimeSpan[MaxButtons];
	private TimeSpan motionTimestamp;
	private Vector2 wheelValue;
	private Time time;

	/// <summary>
	/// Mouse position, relative to the window, in Pixel Coordinates.
	/// </summary>
	public Vector2 Position;

	/// <summary>
	/// Delta to the previous mouse position, in Pixel Coordinates.
	/// </summary>
	public Vector2 Delta;

	/// <summary>
	/// Shorthand to <see cref="Position"/>'s X component
	/// </summary>
	public float X
	{
		get => Position.X;
		set => Position.X = value;
	}

	/// <summary>
	/// Shorthand to <see cref="Position"/>'s Y component
	/// </summary>
	public float Y
	{
		get => Position.Y;
		set => Position.Y = value;
	}

	/// <summary>
	/// Returns true if the given Mouse Button was pressed
	/// </summary>
	public bool Pressed(MouseButtons button) => pressed[(int)button];

	/// <summary>
	/// Returns true if the given Mouse Button is held down
	/// </summary>
	public bool Down(MouseButtons button) => down[(int)button];

	/// <summary>
	/// Returns true if the given Mouse Button was released
	/// </summary>
	public bool Released(MouseButtons button) => released[(int)button];

	/// <summary>
	/// Returns the timestamp of the last time the given Mouse Button was pressed
	/// </summary>
	public TimeSpan Timestamp(MouseButtons button) => timestamp[(int)button];

	/// <summary>
	/// Returns the timestamp of the last time the mouse was moved
	/// </summary>
	public TimeSpan MotionTimestamp() => motionTimestamp;

	/// <summary>
	/// Returns true if the given Mouse Button is repeating presses
	/// </summary>
	public bool Repeated(MouseButtons button, float delay, float interval)
	{
		if (Pressed(button))
			return true;

		var stamp = timestamp[(int)button];
		return
			Down(button) &&
			(time.Elapsed - stamp).TotalSeconds > delay &&
			Time.OnInterval(time, interval, stamp.TotalSeconds);
	}

	public bool LeftPressed => pressed[(int)MouseButtons.Left];
	public bool LeftDown => down[(int)MouseButtons.Left];
	public bool LeftReleased => released[(int)MouseButtons.Left];

	public bool RightPressed => pressed[(int)MouseButtons.Right];
	public bool RightDown => down[(int)MouseButtons.Right];
	public bool RightReleased => released[(int)MouseButtons.Right];

	public bool MiddlePressed => pressed[(int)MouseButtons.Middle];
	public bool MiddleDown => down[(int)MouseButtons.Middle];
	public bool MiddleReleased => released[(int)MouseButtons.Middle];

	public Vector2 Wheel => wheelValue;

	internal void Copy(Mouse other)
	{
		Array.Copy(other.pressed, 0, pressed, 0, MaxButtons);
		Array.Copy(other.down, 0, down, 0, MaxButtons);
		Array.Copy(other.released, 0, released, 0, MaxButtons);
		Array.Copy(other.timestamp, 0, timestamp, 0, MaxButtons);

		Position = other.Position;
		Delta = other.Delta;
		wheelValue = other.wheelValue;
		time = other.time;
		motionTimestamp = other.motionTimestamp;
	}

	internal void Step(in Time time)
	{
		Array.Fill(pressed, false);
		Array.Fill(released, false);
		wheelValue = Vector2.Zero;
		Delta = Vector2.Zero;
		this.time = time;
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

	internal void OnMotion(in Vector2 position, in Vector2 delta, in TimeSpan time)
	{
		Position = position;
		Delta = delta;
		motionTimestamp = time;
	}

	internal void OnWheel(in Vector2 value)
		=> wheelValue = value;
}
