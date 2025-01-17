using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input binding that represents an Action
/// </summary>
public sealed class ActionBinding
{
	[JsonInclude] public readonly List<Binding> Bindings = [];

	public ActionBinding() {}

	public ActionBinding(params ReadOnlySpan<Binding> bindings) =>
		Bindings.AddRange(bindings);

	/// <summary>
	/// Adds Keyboard Key mappings to this Binding
	/// </summary>
	public ActionBinding Add(params ReadOnlySpan<Keys> values)
	{
		foreach (var it in values)
			Bindings.Add(new KeyboardKeyBinding(it));
		return this;
	}

	/// <summary>
	/// Adds GamePad Button mappings to this Binding
	/// </summary>
	public ActionBinding Add(params ReadOnlySpan<Buttons> values)
	{
		foreach (var it in values)
			Bindings.Add(new ControllerButtonBinding(it));
		return this;
	}

	/// <summary>
	/// Adds GamePad Axis mappings to this Binding
	/// </summary>
	public ActionBinding Add(Axes axes, int sign, float deadzone = 0)
	{
		Bindings.Add(new ControllerAxisBinding(axes, sign, deadzone));
		return this;
	}

	/// <summary>
	/// Adds Mouse Button mappings to this Binding
	/// </summary>
	public ActionBinding Add(params ReadOnlySpan<MouseButtons> values)
	{
		foreach (var it in values)
			Bindings.Add(new MouseButtonBinding(it));
		return this;
	}

	public ActionBinding AddLeftJoystickLeft(float deadzone = 0)
		=> Add(Axes.LeftX, -1, deadzone);

	public ActionBinding AddLeftJoystickRight(float deadzone = 0)
		=> Add(Axes.LeftX, 1, deadzone);

	public ActionBinding AddLeftJoystickUp(float deadzone = 0)
		=> Add(Axes.LeftY, -1, deadzone);

	public ActionBinding AddLeftJoystickDown(float deadzone = 0)
		=> Add(Axes.LeftY, 1, deadzone);

	public ActionBinding AddRightJoystickLeft(float deadzone = 0)
		=> Add(Axes.RightX, -1, deadzone);

	public ActionBinding AddRightJoystickRight(float deadzone = 0)
		=> Add(Axes.RightX, 1, deadzone);

	public ActionBinding AddRightJoystickUp(float deadzone = 0)
		=> Add(Axes.RightY, -1, deadzone);

	public ActionBinding AddRightJoystickDown(float deadzone = 0)
		=> Add(Axes.RightY, 1, deadzone);

	public BindingState GetState(Input input, int device)
	{
		BindingState result = new();

		foreach (var it in Bindings)
		{
			var state = it.GetState(input, device);
			result = new(
				Pressed: result.Pressed || state.Pressed,
				Released: result.Released || state.Released,
				Down: result.Down || state.Down,
				Value: Math.Max(result.Value, state.Value),
				ValueNoDeadzone: Math.Max(result.ValueNoDeadzone, state.ValueNoDeadzone),
				Timestamp: result.Timestamp > state.Timestamp ? result.Timestamp : state.Timestamp
			);
		}

		return result;
	}
}