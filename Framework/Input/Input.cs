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
	internal static void AddDefaultSdlGamepadMappings(string relativePath)
	{
		var path = Path.Combine(relativePath, "gamecontrollerdb.txt");
		if (File.Exists(path))
			AddSdlGamepadMappings(File.ReadAllLines(path));
	}

	/// <summary>
	/// Loads a list of SDL Gamepad Mappings.
	/// You can find more information here: https://github.com/mdqinc/SDL_GameControllerDB
	/// By default, any 'gamecontrollerdb.txt' found adjacent to the application at runtime
	/// will be loaded automatically.
	/// </summary>
	public static void AddSdlGamepadMappings(string[] mappings)
	{
		foreach (var mapping in mappings)
			Platform.SDL_GameControllerAddMapping(mapping);
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

	private static unsafe void OnText(IntPtr cstr)
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

	internal static unsafe void OnFosterEvent(in Platform.FosterEvent ev)
	{
		switch (ev.EventType)
		{
			case Platform.FosterEventType.KeyboardInput:
			{
				fixed (byte* ptr = ev.Keyboard.Text)
					OnText(new IntPtr(ptr));
				break;
			}
			case Platform.FosterEventType.KeyboardKey:
			{
				if (ev.Keyboard.Key >= 0 && ev.Keyboard.Key < Keyboard.MaxKeys)
				{
					if (ev.Keyboard.KeyPressed != 0)
					{
						nextState.Keyboard.down[ev.Keyboard.Key] = true;
						nextState.Keyboard.pressed[ev.Keyboard.Key] = true;
						nextState.Keyboard.timestamp[ev.Keyboard.Key] = Time.Duration;
					}
					else
					{
						nextState.Keyboard.down[ev.Keyboard.Key] = false;
						nextState.Keyboard.released[ev.Keyboard.Key] = true;
					}
				}
				break;
			}
			case Platform.FosterEventType.MouseButton:
			{
				if (ev.Mouse.Button >= 0 && ev.Mouse.Button < Mouse.MaxButtons)
				{
					if (ev.Mouse.ButtonPressed != 0)
					{
						nextState.Mouse.down[ev.Mouse.Button] = true;
						nextState.Mouse.pressed[ev.Mouse.Button] = true;
						nextState.Mouse.timestamp[ev.Mouse.Button] = Time.Duration;
					}
					else
					{
						nextState.Mouse.down[ev.Mouse.Button] = false;
						nextState.Mouse.released[ev.Mouse.Button] = true;
					}
				}
				break;
			}
			case Platform.FosterEventType.MouseMove:
			{
				var size = new Point2(App.Width, App.Height);
				var pixels = new Point2(App.WidthInPixels, App.HeightInPixels);
				nextState.Mouse.Position.X = (ev.Mouse.X / size.X) * pixels.X;
				nextState.Mouse.Position.Y = (ev.Mouse.Y / size.Y) * pixels.Y;
				nextState.Mouse.Delta.X = ev.Mouse.deltaX;
				nextState.Mouse.Delta.Y = ev.Mouse.deltaY;
				break;
			}
			case Platform.FosterEventType.MouseWheel:
			{
				nextState.Mouse.wheelValue = new(ev.Mouse.X, ev.Mouse.Y);
				break;
			}
			case Platform.FosterEventType.ControllerConnect:
			{
				if (ev.Controller.Index >= 0 && ev.Controller.Index < InputState.MaxControllers)
				{
					nextState.Controllers[ev.Controller.Index].Connect(
						Platform.ParseUTF8(ev.Controller.Name), 
						ev.Controller.ButtonCount,
						ev.Controller.AxisCount,
						ev.Controller.IsGamepad != 0,
						ev.Controller.GamepadType,
						ev.Controller.Vendor,
						ev.Controller.Product,
						ev.Controller.Version
					);
				}
				break;
			}
			case Platform.FosterEventType.ControllerDisconnect:
			{
				if (ev.Controller.Index >= 0 && ev.Controller.Index < InputState.MaxControllers)
					nextState.Controllers[ev.Controller.Index].Disconnect();
				break;
			}
			case Platform.FosterEventType.ControllerButton:
			{
				var index = ev.Controller.Index;
				var button = ev.Controller.Button;
				if (index >= 0 && index < InputState.MaxControllers && button >= 0 && button < Controller.MaxButtons)
				{
					if (ev.Controller.ButtonPressed != 0)
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
				break;
			}
			case Platform.FosterEventType.ControllerAxis:
			{
				var index = ev.Controller.Index;
				var axis = ev.Controller.Axis;
				if (index >= 0 && index < InputState.MaxControllers && axis >= 0 && axis < Controller.MaxAxis)
				{
					nextState.Controllers[index].axis[axis] = ev.Controller.AxisValue;
					nextState.Controllers[index].axisTimestamp[axis] = Time.Duration;
				}
				break;
			}
		}
	}
}
