using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

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

	/// <summary>
	/// 
	/// </summary>
	public delegate void TextInputHandler(char value);

	/// <summary>
	/// Called whenever keyboard text is typed
	/// </summary>
	public static event TextInputHandler? OnTextEvent;

	/// <summary>
	/// Holds references to all Virtual Buttons so they can be updated.
	/// </summary>
	internal static readonly List<WeakReference<VirtualButton>> virtualButtons = [];

	/// <summary>
	/// Loads 'gamecontrollerdb.txt' from a local file or falls back to the 
	/// default embedded SDL gamepad mappings
	/// </summary>
	internal static void AddDefaultSDLGamepadMappings(string relativePath)
	{
		var path = Path.Combine(relativePath, "gamecontrollerdb.txt");
		if (File.Exists(path))
			AddSDLGamepadMappings(File.ReadAllLines(path));
	}

	[Obsolete("use AddSDLGamepadMappings")]
	public static void AddSdlGamepadMappings(string[] mappings)
		=> AddSDLGamepadMappings(mappings);

	/// <summary>
	/// Loads a list of SDL Gamepad Mappings.
	/// You can find more information here: https://github.com/mdqinc/SDL_GameControllerDB
	/// By default, any 'gamecontrollerdb.txt' found adjacent to the application at runtime
	/// will be loaded automatically.
	/// </summary>
	public static void AddSDLGamepadMappings(string[] mappings)
	{
		foreach (var mapping in mappings)
			SDL3.SDL_AddGamepadMapping(mapping);
	}

	/// <summary>
	/// Sets the Clipboard to the given String
	/// </summary>
	public static void SetClipboardString(string value)
	{
		SDL3.SDL_SetClipboardText(value);
	}

	/// <summary>
	/// Gets the Clipboard String
	/// </summary>
	public static string GetClipboardString()
	{
		return Platform.ParseUTF8(SDL3.SDL_GetClipboardText());
	}

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

	internal static unsafe void OnText(nint cstr)
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

	internal static void OnKey(int key, bool pressed)
	{
		if (key >= 0 && key < Keyboard.MaxKeys)
		{
			if (pressed)
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
	}

	internal static void OnMouseButton(int button, bool pressed)
	{
		if (button >= 0 && button < Mouse.MaxButtons)
		{
			if (pressed)
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
	}

	internal static void OnMouseMove(Vector2 position, Vector2 delta)
	{
		var size = new Point2(App.Width, App.Height);
		var pixels = new Point2(App.WidthInPixels, App.HeightInPixels);
		nextState.Mouse.Position.X = (position.X / size.X) * pixels.X;
		nextState.Mouse.Position.Y = (position.Y / size.Y) * pixels.Y;
		nextState.Mouse.Delta.X = delta.X;
		nextState.Mouse.Delta.Y = delta.Y;
	}

	internal static void OnMouseWheel(Vector2 wheel)
	{
		nextState.Mouse.wheelValue = wheel;
	}

	internal static void OnControllerConnect(int index, string name, int buttonCount, int axisCount, bool isGamepad, GamepadTypes type, ushort vendor, ushort product, ushort version)
	{
		if (index >= 0 && index < InputState.MaxControllers)
		{
			nextState.Controllers[index].Connect(
				name,
				buttonCount,
				axisCount,
				isGamepad,
				type,
				vendor,
				product,
				version
			);
		}
	}

	internal static void OnControllerDisconnect(int index)
	{
		if (index >= 0 && index < InputState.MaxControllers)
			nextState.Controllers[index].Disconnect();
	}

	internal static void OnControllerButton(int index, int button, bool pressed)
	{
		if (index >= 0 && index < InputState.MaxControllers && button >= 0 && button < Controller.MaxButtons)
		{
			if (pressed)
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

	internal static void OnControllerAxis(int index, int axis, float value)
	{
		if (index >= 0 && index < InputState.MaxControllers && axis >= 0 && axis < Controller.MaxAxis)
		{
			nextState.Controllers[index].axis[axis] = value;
			nextState.Controllers[index].axisTimestamp[axis] = Time.Duration;
		}
	}
}
