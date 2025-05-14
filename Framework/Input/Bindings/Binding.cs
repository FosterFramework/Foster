using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// Represents an Input Binding/Mapping
/// </summary>
[JsonDerivedType(typeof(KeyboardKeyBinding), typeDiscriminator: "Key")]
[JsonDerivedType(typeof(ControllerAxisBinding), typeDiscriminator: "Axis")]
[JsonDerivedType(typeof(ControllerButtonBinding), typeDiscriminator: "Button")]
[JsonDerivedType(typeof(MouseButtonBinding), typeDiscriminator: "MouseButton")]
[JsonDerivedType(typeof(MouseMotionBinding), typeDiscriminator: "MouseMotion")]
public abstract class Binding
{
	/// <summary>
	/// Gets the current state of the Binding from the provided Input
	/// </summary>
	public abstract BindingState GetState(Input input, int device);

	/// <summary>
	/// Gets a string descriptor of the binding
	/// </summary>
	public abstract string Descriptor { get; }
}
