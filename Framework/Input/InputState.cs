
namespace Foster.Framework;

/// <summary>
/// Captures a single frame of input.
/// </summary>
public sealed class InputState
{
	/// <summary>
	/// The Maximum number of Controllers
	/// </summary>
	public const int MaxControllers = 32;

	/// <summary>
	/// The Keyboard State
	/// </summary>
	public readonly KeyboardState Keyboard = new();

	/// <summary>
	/// The Mouse State
	/// </summary>
	public readonly MouseState Mouse = new();

	/// <summary>
	/// The Controllers state
	/// </summary>
	public readonly ControllerState[] Controllers = 
		[.. Enumerable.Range(0, MaxControllers).Select(it => new ControllerState(it))];

	/// <summary>
	/// Finds a Connected Controller by the given ID.
	/// If it is not found, or no longer connected, null is returned.
	/// </summary>
	public ControllerState? GetController(ControllerID id)
	{
		for (int i = 0; i < Controllers.Length; i ++)
			if (Controllers[i].ID == id)
				return Controllers[i];
		return null;
	}

	/// <summary>
	/// Creates a Snapshot of this Input State and returns it
	/// </summary>
	public InputState Snapshot()
	{
		var result = new InputState();
		result.Copy(this);
		return result;
	}

	/// <summary>
	/// Copies a Snapshot of this Input State into the provided value
	/// </summary>
	public void Snapshot(InputState into)
	{
		into.Copy(this);
	}

	public void Clear()
	{
		Keyboard.Clear();
		Mouse.Clear();
		foreach (var it in Controllers)
			it.Clear();
	}

	internal void Step(in Time time)
	{
		for (int i = 0; i < Controllers.Length; i++)
		{
			if (Controllers[i].Connected)
				Controllers[i].Step(time);
		}
		Keyboard.Step(time);
		Mouse.Step(time);
	}

	internal void Copy(InputState other)
	{
		for (int i = 0; i < Controllers.Length; i++)
		{
			if (other.Controllers[i].Connected || (Controllers[i].Connected != other.Controllers[i].Connected))
				Controllers[i].Copy(other.Controllers[i]);
		}

		Keyboard.Copy(other.Keyboard);
		Mouse.Copy(other.Mouse);
	}
}
