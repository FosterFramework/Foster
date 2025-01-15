using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to a Mouse Button
/// </summary>
public sealed class MouseButtonBinding : Binding
{
	[JsonInclude] public MouseButtons Button;

	public MouseButtonBinding() {}
	public MouseButtonBinding(MouseButtons button) => Button = button;

	public override BindingState GetState(Input input, int device) => new(
		Pressed: input.Mouse.Pressed(Button),
		Released: input.Mouse.Pressed(Button),
		Down: input.Mouse.Down(Button),
		Value: input.Mouse.Down(Button) ? 1 : 0,
		ValueNoDeadzone: input.Mouse.Down(Button) ? 1 : 0,
		Timestamp: input.Mouse.Timestamp(Button)
	);
}
