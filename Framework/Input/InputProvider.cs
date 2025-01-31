using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// The Input Provider sends data to an Input Module.
/// Every frame you should call <see cref="Update"/> to step the Input State.
/// </summary>
public abstract class InputProvider
{
	/// <summary>
	/// Our Input Module
	/// </summary>
	public readonly Input Input;

	/// <summary>
	/// Echo Input Modules
	/// </summary>
	private readonly List<WeakReference<Input>> echos = [];

	public InputProvider()
	{
		Input = new(this);
	}

	internal void AddEcho(Input input)
	{
		echos.Add(new(input));
	}

	/// <summary>
	/// What to do when the user tries to set the clipboard
	/// </summary>
	public abstract void SetClipboard(string text);

	/// <summary>
	/// What to do when the user tries to get the clipboard
	/// </summary>
	public abstract string GetClipboard();

	/// <summary>
	/// Rumbles a specific controller
	/// </summary>
	public abstract void Rumble(ControllerID id, float lowIntensity, float highIntensity, float duration);

	/// <summary>
	/// Run at the beginning of a frame to increment the input state.
	/// </summary>
	public virtual void Update(in Time time)
	{
		Input.Step(time);

		for (int i = echos.Count - 1; i >= 0; i--)
		{
			if (echos[i].TryGetTarget(out var target))
				target.Step(time);
			else
				echos.RemoveAt(i);
		}
	}

	/// <summary>
	/// Notifies the Input of the given keyboard text
	/// </summary>
	public void Text(in ReadOnlySpan<char> text, Window? window = null)
	{
		if (Input.ReceiveEvents)
			Input.OnText(text, window);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.OnText(text, window);
	}

	internal unsafe void Text(nint cstr, Window? window)
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
		Text(new ReadOnlySpan<char>(chars, written), window);
	}

	/// <summary>
	/// Notifies the Input of a change in keyboard key state
	/// </summary>
	public void Key(int key, bool pressed, in TimeSpan time)
	{
		if (Input.ReceiveEvents)
			Input.NextState.Keyboard.OnKey(key, pressed, time);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.NextState.Keyboard.OnKey(key, pressed, time);
	}

	/// <summary>
	/// Notifies the Input of a change in mouse button state
	/// </summary>
	public void MouseButton(int button, bool pressed, in TimeSpan time)
	{
		if (Input.ReceiveEvents)
			Input.NextState.Mouse.OnButton(button, pressed, time);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.NextState.Mouse.OnButton(button, pressed, time);
	}

	/// <summary>
	/// Notifies the Input of a change in Mouse position state
	/// </summary>
	public void MouseMove(Vector2 position, Vector2 delta, in TimeSpan time)
	{
		if (Input.ReceiveEvents)
			Input.NextState.Mouse.OnMotion(position, delta, time);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.NextState.Mouse.OnMotion(position, delta, time);
	}

	/// <summary>
	/// Notifies the Input of a change in Mouse Wheel state
	/// </summary>
	public void MouseWheel(Vector2 wheel)
	{
		if (Input.ReceiveEvents)
			Input.NextState.Mouse.OnWheel(wheel);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.NextState.Mouse.OnWheel(wheel);
	}

	/// <summary>
	/// Notifies the Input of a controller connection
	/// </summary>
	public void ConnectController(
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
		Input.ConnectController(id, name, buttonCount, axisCount, isGamepad, type, vendor, product, version);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target))
				target.ConnectController(id, name, buttonCount, axisCount, isGamepad, type, vendor, product, version);
	}

	/// <summary>
	/// Notifies the Input of a controller disconnection
	/// </summary>
	public void DisconnectController(ControllerID id)
	{
		Input.DisconnectController(id);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target))
				target.DisconnectController(id);
	}

	/// <summary>
	/// Notifies the Input of a change in controller button state
	/// </summary>
	public void ControllerButton(ControllerID id, int button, bool pressed, in TimeSpan time)
	{
		if (Input.ReceiveEvents)
			Input.NextState.GetController(id)?.OnButton(button, pressed, time);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.NextState.GetController(id)?.OnButton(button, pressed, time);
	}

	/// <summary>
	/// Notifies the Input of a change in controller axis state
	/// </summary>
	public void ControllerAxis(ControllerID id, int axis, float value, in TimeSpan time)
	{
		if (Input.ReceiveEvents)
			Input.NextState.GetController(id)?.OnAxis(axis, value, time);

		foreach (var it in echos)
			if (it.TryGetTarget(out var target) && target.ReceiveEvents)
				target.NextState.GetController(id)?.OnAxis(axis, value, time);
	}
}