using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to a Keyboard Key
/// </summary>
public sealed class KeyboardKeyBinding : Binding
{
	[JsonInclude] public Keys Key;

	public KeyboardKeyBinding() {}

	public KeyboardKeyBinding(Keys key)
		=> Key = key;

	public KeyboardKeyBinding(Keys key, in ReadOnlySpan<string> masks) : this(key)
		=> Masks.AddRange(masks);

	public override BindingState GetState(Input input, int device) => new(
		Pressed: input.Keyboard.Pressed(Key),
		Released: input.Keyboard.Pressed(Key),
		Down: input.Keyboard.Down(Key),
		Value: input.Keyboard.Down(Key) ? 1 : 0,
		Timestamp: input.Keyboard.Timestamp(Key)
	);
}
