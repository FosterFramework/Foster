using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// The Input Manager that stores the current Input State
/// </summary>
public static class Input
{
	/// <summary>
	/// The Current Input State
	/// </summary>
	public static readonly InputState State = new();

	/// <summary>
	/// The Input State of the previous frame
	/// </summary>
	public static readonly InputState LastState = new();

	/// <summary>
	/// The Input State of the next frame
	/// </summary>
	private static readonly InputState nextState = new();

	/// <summary>
	/// The Keyboard of the current State
	/// </summary>
	public static Keyboard Keyboard => State.Keyboard;

	/// <summary>
	/// The Mouse of the Current State
	/// </summary>
	public static Mouse Mouse => State.Mouse;

	/// <summary>
	/// The Controllers of the Current State
	/// </summary>
	public static ReadOnlyCollection<Controller> Controllers => State.Controllers;

	/// <summary>
	/// Default delay before a key or button starts repeating
	/// </summary>
	public static float RepeatDelay = 0.4f;

	/// <summary>
	/// Default interval that the repeat is triggered, in seconds
	/// </summary>
	public static float RepeatInterval = 0.03f;

	public delegate void TextInputHandler(char value);
	public static event TextInputHandler? OnTextEvent;

	internal static readonly List<WeakReference<VirtualButton>> virtualButtons = new List<WeakReference<VirtualButton>>();

	/// <summary>
	/// Run at the beginning of a frame to step the input state.
	/// After this, the Application will poll the platform for more inputs, which call back
	/// to the various Input.On internal methods.
	/// </summary>
	internal static void Step()
	{
		LastState.Copy(State);
		State.Copy(nextState);
		nextState.Step();

		for (int i = virtualButtons.Count - 1; i >= 0; i--)
		{
			var button = virtualButtons[i];
			if (button.TryGetTarget(out var target))
				target.Update();
			else
				virtualButtons.RemoveAt(i);
		}
	}

	/// <summary>
	/// Sets the Clipboard to the given String
	/// </summary>
	public static void SetClipboardString(string value)
	{
		Platform.FosterSetClipboard(value);
	}

	/// <summary>
	/// Gets the Clipboard String
	/// </summary>
	public static string GetClipboardString()
	{
		var ptr = Platform.FosterGetClipboard();
		return Platform.ParseUTF8(ptr);
	}

	internal static unsafe void OnText(IntPtr cstr)
	{
		byte* ptr = (byte*)cstr;
		if (ptr == null || ptr[0] == 0)
			return;

		// get cstr length
		int len = 0;
		while (ptr[len] != 0)
			len++;

		// convert to chars
		char* chars = stackalloc char[64];
		int written = System.Text.Encoding.UTF8.GetChars(ptr, len, chars, 64);

		// append chars
		for (int i = 0; i < written; i ++)
		{
			OnTextEvent?.Invoke(chars[i]);
			nextState.Keyboard.Text.Append(chars[i]);
		}
	}

	/// <summary>
	/// Invoked by the Application platform when a Key state is changed
	/// </summary>
	internal static void OnKey(int key, byte pressed)
	{
		if (key < 0 || key >= Keyboard.MaxKeys)
			throw new ArgumentOutOfRangeException(nameof(key), "Value is out of Range for supported keys");

		if (pressed != 0)
		{
			nextState.Keyboard.down[key] = true;
			nextState.Keyboard.pressed[key] = true;
			nextState.Keyboard.timestamp[key] = Time.Duration;
		}
		else
		{
			nextState.Keyboard.down[key] = false;
			nextState.Keyboard.released[key] = true;
		}
	}

	/// <summary>
	/// Invoked by the Application platform when a Mouse Button state is changed
	/// </summary>
	internal static void OnMouseButton(int button, byte pressed)
	{
		if (button < 0 || button >= Mouse.MaxButtons)
			throw new ArgumentOutOfRangeException(nameof(button), "Value is out of Range for supported mouse buttons");

		if (pressed != 0)
		{
			nextState.Mouse.down[button] = true;
			nextState.Mouse.pressed[button] = true;
			nextState.Mouse.timestamp[button] = Time.Duration;
		}
		else
		{
			nextState.Mouse.down[button] = false;
			nextState.Mouse.released[button] = true;
		}
	}

	/// <summary>
	/// Invoked by the Application platform when the Mouse Wheel state is changed
	/// </summary>
	internal static void OnMouseMove(float offsetX, float offsetY)
	{
		Point2 size = new Point2(App.Width, App.Height);
		Point2 pixels = new Point2(App.WidthInPixels, App.HeightInPixels);

		nextState.Mouse.Position.X = (offsetX / size.X) * pixels.X;
		nextState.Mouse.Position.Y = (offsetY / size.Y) * pixels.Y;
	}

	/// <summary>
	/// Invoked by the Application platform when the Mouse Wheel state is changed
	/// </summary>
	internal static void OnMouseWheel(float offsetX, float offsetY)
	{
		nextState.Mouse.wheelValue = new Vector2(offsetX, offsetY);
	}

	/// <summary>
	/// Invoked by the Application platform when a Controller is connected
	/// </summary>
	internal static void OnControllerConnect(int index, IntPtr name, int buttonCount, int axisCount, byte isGamepad, ushort vendor, ushort product, ushort version)
	{
		if (index >= 0 && index < InputState.MaxControllers)
			nextState.Controllers[index].Connect(Platform.ParseUTF8(name), buttonCount, axisCount, isGamepad != 0, vendor, product, version);
	}

	/// <summary>
	/// Invoked by the Application platform when a Controller is disconnected
	/// </summary>
	internal static void OnControllerDisconnect(int index)
	{
		if (index >= 0 && index < InputState.MaxControllers)
			nextState.Controllers[index].Disconnect();
	}

	/// <summary>
	/// Invoked by the Application platform when a Controller Button state is changed
	/// </summary>
	internal static void OnControllerButton(int index, int button, byte pressed)
	{
		if (index >= 0 && index < InputState.MaxControllers && button >= 0 && button < Controller.MaxButtons)
		{
			if (pressed != 0)
			{
				nextState.Controllers[index].down[button] = true;
				nextState.Controllers[index].pressed[button] = true;
				nextState.Controllers[index].timestamp[button] = Time.Duration;
			}
			else
			{
				nextState.Controllers[index].down[button] = false;
				nextState.Controllers[index].released[button] = true;
			}
		}
	}

	/// <summary>
	/// Invoked by the Application platform when a Controller Axis state is changed
	/// </summary>
	internal static void OnControllerAxis(int index, int axis, float value)
	{
		if (index >= 0 && index < InputState.MaxControllers && axis >= 0 && axis < Controller.MaxAxis)
		{
			nextState.Controllers[index].axis[axis] = value;
			nextState.Controllers[index].axisTimestamp[axis] = Time.Duration;
		}
	}
}
