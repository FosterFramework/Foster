using System.Collections.ObjectModel;

namespace Foster.Framework;

/// <summary>
/// Stores an Input State
/// </summary>
public class InputState
{
	/// <summary>
	/// The Maximum number of Controllers
	/// </summary>
	public const int MaxControllers = 32;

	/// <summary>
	/// The Keyboard State
	/// </summary>
	public readonly Keyboard Keyboard;

	/// <summary>
	/// The Mouse State
	/// </summary>
	public readonly Mouse Mouse;

	/// <summary>
	/// A list of all the Controllers
	/// </summary>
	private readonly Controller[] controllers;

	/// <summary>
	/// A Read-Only Collection of the Controllers
	/// Note that they aren't necessarily connected
	/// </summary>
	public readonly ReadOnlyCollection<Controller> Controllers;

	public InputState()
	{
		controllers = new Controller[MaxControllers];
		for (int i = 0; i < controllers.Length; i++)
			controllers[i] = new Controller();

		Controllers = new ReadOnlyCollection<Controller>(controllers);
		Keyboard = new Keyboard();
		Mouse = new Mouse();
	}

	internal void Step()
	{
		for (int i = 0; i < Controllers.Count; i++)
		{
			if (Controllers[i].Connected)
				Controllers[i].Step();
		}
		Keyboard.Step();
		Mouse.Step();
	}

	internal void Copy(InputState other)
	{
		for (int i = 0; i < Controllers.Count; i++)
		{
			if (other.Controllers[i].Connected || (Controllers[i].Connected != other.Controllers[i].Connected))
				Controllers[i].Copy(other.Controllers[i]);
		}

		Keyboard.Copy(other.Keyboard);
		Mouse.Copy(other.Mouse);
	}
}
