
using System.Collections.ObjectModel;

namespace Foster.Framework;

/// <summary>
/// The Input Manager that stores the current Input State
/// </summary>
public sealed class Input
{
	public delegate void TextInputHandlerFn(char value);
	public delegate void ControllerConnectedFn(ControllerID id);
	public delegate void ControllerDisconnectedFn(ControllerID id);

	/// <summary>
	/// The Current Input State
	/// </summary>
	public readonly InputState State;

	/// <summary>
	/// The Input State of the previous frame
	/// </summary>
	public readonly InputState LastState;

	/// <summary>
	/// The Input State of the next frame
	/// </summary>
	internal readonly InputState NextState;

	/// <summary>
	/// The Keyboard of the current State
	/// </summary>
	public Keyboard Keyboard => State.Keyboard;

	/// <summary>
	/// The Mouse of the Current State
	/// </summary>
	public Mouse Mouse => State.Mouse;

	/// <summary>
	/// The Controllers of the Current State
	/// </summary>
	public ReadOnlyCollection<Controller> Controllers => State.Controllers;

	/// <summary>
	/// Default delay before a key or button starts repeating, in seconds
	/// </summary>
	public static float RepeatDelay = 0.4f;

	/// <summary>
	/// Default interval that the repeat is triggered, in seconds
	/// </summary>
	public static float RepeatInterval = 0.03f;

	/// <summary>
	/// Called whenever keyboard text is typed
	/// </summary>
	public event TextInputHandlerFn? OnTextEvent;

	/// <summary>
	/// Called when a Controller is connected
	/// </summary>
	public event ControllerConnectedFn? OnControllerConnected;

	/// <summary>
	/// Called when a Controller is disconnected
	/// </summary>
	public event ControllerDisconnectedFn? OnControllerDisconnected;

	/// <summary>
	/// Holds references to all Virtual Buttons so they can be updated.
	/// </summary>
	private readonly List<WeakReference<VirtualInput>> virtualInputs = [];

	private readonly InputProvider provider;

	/// <summary>
	/// 
	/// </summary>
	internal Input(InputProvider provider)
	{
		this.provider = provider;
		State = new(provider);
		LastState = new(provider);
		NextState = new(provider);
	}

	/// <summary>
	/// Finds a Connected Controller by the given ID.
	/// If it is not found, or no longer connected, null is returned.
	/// </summary>
	public Controller? GetController(ControllerID id) => State.GetController(id);

	/// <summary>
	/// Sets the Clipboard to the given String
	/// </summary>
	public void SetClipboardString(string value)
		=> provider.SetClipboard(value);

	/// <summary>
	/// Gets the Clipboard String
	/// </summary>
	public string GetClipboardString()
		=> provider.GetClipboard();

	internal void AddVirtualInput(VirtualInput it)
	{
		virtualInputs.Add(new WeakReference<VirtualInput>(it));
	}

	internal void RemoveVirtualInput(VirtualInput it)
	{
		for (int i = virtualInputs.Count - 1; i >= 0; i --)
			if (virtualInputs[i].TryGetTarget(out var input) && input == it)
			{
				virtualInputs.RemoveAt(i);
				break;
			}
	}

	internal void OnText(ReadOnlySpan<char> text)
	{
		foreach (var it in text)
		{
			NextState.Keyboard.Text.Append(it);
			OnTextEvent?.Invoke(it);
		}
	}

	internal void ConnectController(
		ControllerID id,
		string name,
		int buttonCount,
		int axisCount,
		bool isGamepad,
		GamepadTypes type,
		ushort vendor,
		ushort product,
		ushort version)
	{
		for (int i = 0; i < InputState.MaxControllers; i++)
		{
			if (NextState.Controllers[i].Connected)
				continue;

			NextState.Controllers[i].Connect(
				id,
				name,
				buttonCount,
				axisCount,
				isGamepad,
				type,
				vendor,
				product,
				version
			);

			OnControllerConnected?.Invoke(id);
			break;
		}
	}

	internal void DisconnectController(ControllerID id)
	{
		foreach (var it in NextState.Controllers)
			if (it.ID == id)
			{
				it.Disconnect();
				OnControllerDisconnected?.Invoke(id);
			}
	}

	internal void Step(in Time time)
	{
		// step state
		LastState.Copy(State);
		State.Copy(NextState);
		NextState.Step(time);

		// update virtual buttons, remove unreferenced ones
		for (int i = virtualInputs.Count - 1; i >= 0; i--)
		{
			var it = virtualInputs[i];
			if (it.TryGetTarget(out var target))
				target.Update(time);
			else
				virtualInputs.RemoveAt(i);
		}
	}

	/// <summary>
	/// Loads 'gamecontrollerdb.txt' from a local file or falls back to the 
	/// default embedded SDL gamepad mappings
	/// </summary>
	static internal void AddDefaultSDLGamepadMappings(string relativePath)
	{
		var path = Path.Combine(relativePath, "gamecontrollerdb.txt");
		if (File.Exists(path))
			AddSDLGamepadMappings(File.ReadAllLines(path));
	}

	/// <summary>
	/// Loads a list of SDL Gamepad Mappings.
	/// You can find more information here: https://github.com/mdqinc/SDL_GameControllerDB
	/// By default, any 'gamecontrollerdb.txt' found adjacent to the application at runtime
	/// will be loaded automatically.
	/// </summary>
	public static void AddSDLGamepadMappings(string[] mappings)
	{
		foreach (var mapping in mappings)
			SDL3.SDL.SDL_AddGamepadMapping(mapping);
	}
}
