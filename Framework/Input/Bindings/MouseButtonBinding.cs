using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to a Mouse Button
/// </summary>
public sealed class MouseButtonBinding : Binding
{
	[JsonConverter(typeof(JsonStringEnumConverter<MouseButtons>))]
	public MouseButtons Button { get; set; }

	public MouseButtonBinding() {}

	public MouseButtonBinding(MouseButtons button) => Button = button;

	public override BindingState GetState(Input input, int device) => new(
		Pressed: input.Mouse.Pressed(Button),
		Released: input.Mouse.Released(Button),
		Down: input.Mouse.Down(Button),
		Value: input.Mouse.Down(Button) ? 1 : 0,
		Timestamp: input.Mouse.PressedTimestamp(Button)
	);

	[JsonIgnore]
	public override string Descriptor => $"Mouse Button {Button}";
}
