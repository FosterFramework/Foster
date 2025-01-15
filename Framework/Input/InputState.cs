using System.Collections.ObjectModel;

namespace Foster.Framework;

/// <summary>
/// Captures a single frame of input.
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
	
	internal InputState(InputProvider provider)
	{
		controllers = new Controller[MaxControllers];
		for (int i = 0; i < controllers.Length; i++)
			controllers[i] = new Controller(provider, i);

		Controllers = new ReadOnlyCollection<Controller>(controllers);
		Keyboard = new Keyboard();
		Mouse = new Mouse();
	}

	/// <summary>
	/// Finds a Connected Controller by the given ID.
	/// If it is not found, or no longer connected, null is returned.
	/// </summary>
	public Controller? GetController(ControllerID id)
	{
		for (int i = 0; i < controllers.Length; i ++)
			if (controllers[i].ID == id)
				return controllers[i];
		return null;
	}

	internal void Step(in Time time)
	{
		for (int i = 0; i < Controllers.Count; i++)
		{
			if (Controllers[i].Connected)
				Controllers[i].Step(time);
		}
		Keyboard.Step(time);
		Mouse.Step(time);
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
