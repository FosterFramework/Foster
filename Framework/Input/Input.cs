
namespace Foster.Framework;

/// <summary>
/// The Input Manager which can be used to query the previous and current state of Input.
/// </summary>
public sealed class Input
{
	public delegate void TextInputHandlerFn(in ReadOnlySpan<char> value, Window? window);
	public delegate void ControllerConnectedFn(ControllerID id);
	public delegate void ControllerDisconnectedFn(ControllerID id);

	/// <summary>
	/// Default delay before a key or button starts repeating, in seconds
	/// </summary>
	public static float RepeatDelay = 0.4f;

	/// <summary>
	/// Default interval that the repeat is triggered, in seconds
	/// </summary>
	public static float RepeatInterval = 0.03f;

	/// <summary>
	/// The Current Input State
	/// </summary>
	public readonly InputState State = new();

	/// <summary>
	/// The Input State of the previous frame
	/// </summary>
	public readonly InputState LastState = new();

	/// <summary>
	/// The Input State of the next frame
	/// </summary>
	internal readonly InputState NextState = new();

	/// <summary>
	/// The Keyboard of the current State
	/// </summary>
	public KeyboardState Keyboard => State.Keyboard;

	/// <summary>
	/// The Mouse of the Current State
	/// </summary>
	public MouseState Mouse => State.Mouse;

	/// <summary>
	/// The Controllers of the Current State
	/// </summary>
	public ControllerState[] Controllers => State.Controllers;

	/// <summary>
	/// Called whenever keyboard text is typed if <see cref="Window.StartTextInput"/> was called.
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
	/// Input Binding Filters.
	/// These filter the <see cref="Binding.Masks"/> used by <see cref="VirtualInput"/>s.
	/// </summary>
	public readonly HashSet<string> BindingFilters = [];

	/// <summary>
	/// If the Input Module should Receive Events.
	/// If this is false, the Input will essentially not update between frames.
	/// </summary>
	public bool ReceiveEvents = true;

	/// <summary>
	/// Holds references to all VirtualInputs so they can be updated.
	/// </summary>
	private readonly List<WeakReference<VirtualInput>> virtualInputs = [];

	internal readonly InputProvider Provider;

	/// <summary>
	///
	/// </summary>
	internal Input(InputProvider provider)
	{
		Provider = provider;
	}

	/// <summary>
	/// Creates an Input Module that recieves the same events as this one.
	/// </summary>
	public Input CreateEcho()
	{
		var input = new Input(Provider);

		// copy state to this input
		input.State.Copy(State);
		input.LastState.Copy(LastState);
		input.NextState.Copy(NextState);

		Provider.AddEcho(input);
		return input;
	}

	/// <summary>
	/// All created VirtualInputs. These are weak references so be sure to check if they have been disposed
	/// </summary>
	public IReadOnlyList<WeakReference<VirtualInput>> VirtualInputs => virtualInputs;

	/// <summary>
	/// Finds a Connected Controller by the given ID.
	/// If it is not found, or no longer connected, null is returned.
	/// </summary>
	public ControllerState? GetController(ControllerID id) => State.GetController(id);

	/// <summary>
	/// Sets the Clipboard to the given String
	/// </summary>
	public void SetClipboardString(string value)
		=> Provider.SetClipboard(value);

	/// <summary>
	/// Gets the Clipboard String
	/// </summary>
	public string GetClipboardString()
		=> Provider.GetClipboard();

	/// <summary>
	/// Rumbles a Controller for a give duration.
	/// This will cancel any previous rumble effects.
	/// </summary>
	/// <param name="id">The ID of the Controller to Rumble</param>
	/// <param name="intensity">From 0.0 to 1.0 intensity of the Rumble</param>
	/// <param name="duration">How long, in seconds, for the Rumble to last</param>
	public void Rumble(ControllerID id, float intensity, float duration)
		=> Rumble(id, intensity, intensity, duration);

	/// <summary>
	/// Rumbles a Controller for a give duration.
	/// This will cancel any previous rumble effects.
	/// </summary>
	/// <param name="controllerIndex">The Index of the Controller to Rumble</param>
	/// <param name="intensity">From 0.0 to 1.0 intensity of the Rumble</param>
	/// <param name="duration">How long, in seconds, for the Rumble to last</param>
	public void Rumble(int controllerIndex, float intensity, float duration)
		=> Rumble(controllerIndex, intensity, intensity, duration);

	/// <summary>
	/// Rumbles a Controller for a give duration.
	/// This will cancel any previous rumble effects.
	/// </summary>
	/// <param name="id">The ID of the Controller to Rumble</param>
	/// <param name="lowIntensity">From 0.0 to 1.0 intensity of the Low-Intensity Rumble</param>
	/// <param name="highIntensity">From 0.0 to 1.0 intensity of the High-Intensity Rumble</param>
	/// <param name="duration">How long, in seconds, for the Rumble to last</param>
	public void Rumble(ControllerID id, float lowIntensity, float highIntensity, float duration)
	{
		var it = GetController(id);
		if (it != null && it.Connected)
			Provider.Rumble(id, lowIntensity, highIntensity, duration);
	}

	/// <summary>
	/// Rumbles a Controller for a give duration.
	/// This will cancel any previous rumble effects.
	/// </summary>
	/// <param name="controllerIndex">The Index of the Controller to Rumble</param>
	/// <param name="lowIntensity">From 0.0 to 1.0 intensity of the Low-Intensity Rumble</param>
	/// <param name="highIntensity">From 0.0 to 1.0 intensity of the High-Intensity Rumble</param>
	/// <param name="duration">How long, in seconds, for the Rumble to last</param>
	public void Rumble(int controllerIndex, float lowIntensity, float highIntensity, float duration)
	{
		var it = Controllers[controllerIndex];
		if (it.Connected)
			Provider.Rumble(it.ID, lowIntensity, highIntensity, duration);
	}

	/// <summary>
	/// Clears the Input State
	/// </summary>
	public void Clear()
	{
		State.Clear();
		NextState.Clear();
	}

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

	internal void OnText(in ReadOnlySpan<char> text, Window? window)
	{
		NextState.Keyboard.Text.Append(text);
		OnTextEvent?.Invoke(text, window);
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
