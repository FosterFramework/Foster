using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to a Keyboard Key
/// </summary>
public sealed class KeyboardKeyBinding : Binding
{
	[JsonConverter(typeof(JsonStringEnumConverter<Keys>))]
	public Keys Key { get; set; }

	public KeyboardKeyBinding() {}

	public KeyboardKeyBinding(Keys key)
		=> Key = key;

	public override BindingState GetState(Input input, int device) => new(
		Pressed: input.Keyboard.Pressed(Key),
		Released: input.Keyboard.Released(Key),
		Down: input.Keyboard.Down(Key),
		Value: input.Keyboard.Down(Key) ? 1 : 0,
		Timestamp: input.Keyboard.Timestamp(Key)
	);

	[JsonIgnore]
	public override string Descriptor => $"Key {Key}";
}
