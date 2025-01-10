using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// Represents an Input Binding
/// </summary>
[JsonDerivedType(typeof(KeyboardKeyBinding), typeDiscriminator: "Key")]
[JsonDerivedType(typeof(ControllerAxisBinding), typeDiscriminator: "Axis")]
[JsonDerivedType(typeof(ControllerButtonBinding), typeDiscriminator: "Button")]
[JsonDerivedType(typeof(MouseMotionBinding), typeDiscriminator: "MouseButton")]
[JsonDerivedType(typeof(MouseMotionBinding), typeDiscriminator: "MouseMotion")]
public abstract class Binding
{
	public abstract BindingState GetState(Input input, int device);
}