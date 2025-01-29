using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to a Mouse Button
/// </summary>
public sealed class MouseButtonBinding : Binding
{
	public MouseButtons Button { get; set; }

	public MouseButtonBinding() {}

	public MouseButtonBinding(MouseButtons button) => Button = button;

	public MouseButtonBinding(MouseButtons button, in ReadOnlySpan<string> masks) : this(button)
		=> Masks = [..masks];

	public override BindingState GetState(Input input, int device) => new(
		Pressed: input.Mouse.Pressed(Button),
		Released: input.Mouse.Pressed(Button),
		Down: input.Mouse.Down(Button),
		Value: input.Mouse.Down(Button) ? 1 : 0,
		Timestamp: input.Mouse.PressedTimestamp(Button)
	);
}
