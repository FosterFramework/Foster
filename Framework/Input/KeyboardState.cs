﻿using System.Text;

namespace Foster.Framework;

/// <summary>
/// Stores the state of the Keyboard.
/// </summary>
public sealed class KeyboardState
{
	public const int MaxKeys = 512;

	private readonly bool[] pressed = new bool[MaxKeys];
	private readonly bool[] down = new bool[MaxKeys];
	private readonly bool[] released = new bool[MaxKeys];
	private readonly TimeSpan[] timestamp = new TimeSpan[MaxKeys];
	private Time time;

	/// <summary>
	/// Any Text that was typed over the last frame if <see cref="Window.StartTextInput"/> was called.
	/// </summary>
	public readonly StringBuilder Text = new();

	/// <summary>
	/// The last time a key was pressed or held
	/// </summary>
	public TimeSpan InputTimestamp { get; private set; }

	/// <summary>
	/// Checks if the given key was pressed
	/// </summary>
	public bool Pressed(Keys key) => pressed[(int)key];

	/// <summary>
	/// Checks if any of the given keys were pressed
	/// </summary>
	public bool Pressed(Keys key1, Keys key2) => pressed[(int)key1] || pressed[(int)key2];

	/// <summary>
	/// Checks if any of the given keys were pressed
	/// </summary>
	public bool Pressed(Keys key1, Keys key2, Keys key3) => pressed[(int)key1] || pressed[(int)key2] || pressed[(int)key3];

	/// <summary>
	/// Checks if the given key is held
	/// </summary>
	public bool Down(Keys key) => down[(int)key];

	/// <summary>
	/// Checks if any of the given keys were down
	/// </summary>
	public bool Down(Keys key1, Keys key2) => down[(int)key1] || down[(int)key2];

	/// <summary>
	/// Checks if any of the given keys were down
	/// </summary>
	public bool Down(Keys key1, Keys key2, Keys key3) => down[(int)key1] || down[(int)key2] || down[(int)key3];

	/// <summary>
	/// Checks if the given key was released
	/// </summary>
	public bool Released(Keys key) => released[(int)key];

	/// <summary>
	/// Checks if any of the given keys were released
	/// </summary>
	public bool Released(Keys key1, Keys key2) => released[(int)key1] || released[(int)key2];

	/// <summary>
	/// Checks if any of the given keys were released
	/// </summary>
	public bool Released(Keys key1, Keys key2, Keys key3) => released[(int)key1] || released[(int)key2] || released[(int)key3];

	/// <summary>
	/// Checks if any of the given keys were pressed
	/// </summary>
	public bool Pressed(ReadOnlySpan<Keys> keys)
	{
		for (int i = 0; i < keys.Length; i++)
			if (pressed[(int)keys[i]])
				return true;

		return false;
	}

	/// <summary>
	/// Checks if any of the given keys are held
	/// </summary>
	public bool Down(ReadOnlySpan<Keys> keys)
	{
		for (int i = 0; i < keys.Length; i++)
			if (down[(int)keys[i]])
				return true;

		return false;
	}

	/// <summary>
	/// Checks if any of the given keys were released
	/// </summary>
	public bool Released(ReadOnlySpan<Keys> keys)
	{
		for (int i = 0; i < keys.Length; i++)
			if (released[(int)keys[i]])
				return true;

		return false;
	}

	/// <summary>
	/// Checks if the given key was Repeated
	/// </summary>
	public bool Repeated(Keys key)
	{
		return Repeated(key, Input.RepeatDelay, Input.RepeatInterval);
	}

	/// <summary>
	/// Checks if the given key was Repeated, given the delay and interval
	/// </summary>
	public bool Repeated(Keys key, float delay, float interval)
	{
		if (Pressed(key))
			return true;

		var stamp = timestamp[(int)key];
		return Down(key) && (time.Elapsed - stamp).TotalSeconds > delay && Time.OnInterval(time, interval, stamp.TotalSeconds);
	}

	/// <summary>
	/// Checks if the given key was Pressed or held until Repeated
	/// </summary>
	public bool PressedOrRepeated(Keys key)
	{
		return Pressed(key) || Repeated(key);
	}

	/// <summary>
	/// Checks if the given key was Pressed or held until Repeated, given the delay and interval
	/// </summary>
	public bool PressedOrRepeated(Keys key, float repeatDelay, float repeatInterval)
	{
		return Pressed(key) || Repeated(key, repeatDelay, repeatInterval);
	}

	/// <summary>
	/// Gets the Timestamp of when the given key was last pressed, in Ticks
	/// </summary>
	public TimeSpan Timestamp(Keys key)
	{
		return timestamp[(int)key];
	}

	/// <summary>
	/// Returns the first key that is currently down, if any
	/// </summary>
	public Keys? FirstDown()
	{
		for (int i = 0; i < down.Length; i++)
			if (down[i])
				return (Keys)i;
		return null;
	}

	/// <summary>
	/// Returns the first key that is currently pressed, if any
	/// </summary>
	public Keys? FirstPressed()
	{
		for (int i = 0; i < pressed.Length; i++)
			if (pressed[i])
				return (Keys)i;
		return null;
	}

	/// <summary>
	/// Returns the first key that is currently released, if any
	/// </summary>
	public Keys? FirstReleased()
	{
		for (int i = 0; i < released.Length; i++)
			if (released[i])
				return (Keys)i;
		return null;
	}

	/// <summary>
	/// Returns True if the Left or Right Control keys are held
	/// </summary>
	public bool Ctrl => Down(Keys.LeftControl, Keys.RightControl);

	/// <summary>
	/// Returns the timestamp in ticks of the most recent Control press
	/// </summary>
	public TimeSpan CtrlTimestamp
		=> Timestamp(Keys.LeftControl) > Timestamp(Keys.RightControl)
			? Timestamp(Keys.LeftControl)
			: Timestamp(Keys.RightControl);

	/// <summary>
	/// Returns True if the Left or Right Control keys are held (or Command on MacOS)
	/// </summary>
	public bool CtrlOrCommand
	{
		get
		{
			if (OperatingSystem.IsMacOS())
				return Down(Keys.LeftOS, Keys.RightOS);
			else
				return Down(Keys.LeftControl, Keys.RightControl);
		}
	}

	/// <summary>
	/// Returns the timestamp in ticks of the most recent Control press (or Command on MacOS)
	/// </summary>
	public TimeSpan CtrlOrCommandTimestamp
	{
		get
		{
			if (OperatingSystem.IsMacOS())
				return Timestamp(Keys.LeftOS) > Timestamp(Keys.RightOS)
					? Timestamp(Keys.LeftOS) : Timestamp(Keys.RightOS);
			else
				return Timestamp(Keys.LeftControl) > Timestamp(Keys.RightControl)
					? Timestamp(Keys.LeftControl) : Timestamp(Keys.RightControl);
		}
	}

	/// <summary>
	/// Returns True if the Left or Right Alt keys are held
	/// </summary>
	public bool Alt => Down(Keys.LeftAlt, Keys.RightAlt);

	/// <summary>
	/// Returns the timestamp in ticks of the most recent Alt press
	/// </summary>
	public TimeSpan AltTimestamp
		=> Timestamp(Keys.LeftAlt) > Timestamp(Keys.RightAlt)
			? Timestamp(Keys.LeftAlt)
			: Timestamp(Keys.RightAlt);

	/// <summary>
	/// Returns True of the Left or Right Shift keys are held
	/// </summary>
	public bool Shift => Down(Keys.LeftShift, Keys.RightShift);

	/// <summary>
	/// Returns the timestamp in ticks of the most recent Shift press
	/// </summary>
	public TimeSpan ShiftTimestamp
		=> Timestamp(Keys.LeftShift) > Timestamp(Keys.RightShift)
			? Timestamp(Keys.LeftShift)
			: Timestamp(Keys.RightShift);

	/// <summary>
	/// Clears the Keyboard State
	/// </summary>
	public void Clear()
	{
		InputTimestamp = TimeSpan.Zero;
		Array.Clear(pressed, 0, MaxKeys);
		Array.Clear(down, 0, MaxKeys);
		Array.Clear(released, 0, MaxKeys);
		Array.Clear(timestamp, 0, MaxKeys);
		Text.Clear();
	}

	/// <summary>
	/// Clears the state of a specific Keyboard Key
	/// </summary>
	public void Clear(Keys key)
	{
		pressed[(int)key] = false;
		down[(int)key] = false;
		released[(int)key] = false;
		timestamp[(int)key] = TimeSpan.Zero;
	}

	/// <summary>
	/// Creates a Snapshot of this Keyboard State and returns it
	/// </summary>
	public KeyboardState Snapshot()
	{
		var result = new KeyboardState();
		result.Copy(this);
		return result;
	}

	/// <summary>
	/// Copies a Snapshot of this Keyboard State into the provided value
	/// </summary>
	public void Snapshot(KeyboardState into)
	{
		into.Copy(this);
	}

	internal void Copy(KeyboardState other)
	{
		Array.Copy(other.pressed, 0, pressed, 0, MaxKeys);
		Array.Copy(other.down, 0, down, 0, MaxKeys);
		Array.Copy(other.released, 0, released, 0, MaxKeys);
		Array.Copy(other.timestamp, 0, timestamp, 0, MaxKeys);

		Text.Clear();
		Text.Append(other.Text);

		time = other.time;
		InputTimestamp = other.InputTimestamp;
	}

	internal void Step(in Time time)
	{
		Array.Fill(pressed, false);
		Array.Fill(released, false);
		Text.Clear();
		this.time = time;
	}

	internal void OnKey(int keyIndex, bool keyPressed, in TimeSpan time)
	{
		if (keyIndex >= 0 && keyIndex < MaxKeys)
		{
			if (keyPressed)
			{
				down[keyIndex] = true;
				pressed[keyIndex] = true;
				timestamp[keyIndex] = time;
				InputTimestamp = time;
			}
			else
			{
				down[keyIndex] = false;
				released[keyIndex] = true;
			}
		}
	}
}
