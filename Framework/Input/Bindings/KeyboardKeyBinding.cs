using System.Text.Json.Serialization;

namespace Foster.Framework;

public sealed class KeyboardKeyBinding : Binding
{
	[JsonInclude] public Keys Key;

	public KeyboardKeyBinding() {}
	public KeyboardKeyBinding(Keys key) => Key = key;

	public override BindingState GetState(Input input, int device) => new(
		Pressed: input.Keyboard.Pressed(Key),
		Released: input.Keyboard.Pressed(Key),
		Down: input.Keyboard.Down(Key),
		Value: input.Keyboard.Down(Key) ? 1 : 0,
		ValueNoDeadzone: input.Keyboard.Down(Key) ? 1 : 0,
		Timestamp: input.Keyboard.Timestamp(Key)
	);
}