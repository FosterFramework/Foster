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
	/// Optional set of Masks to Filter the binding by.
	/// These are filtered by <see cref="Input.BindingFilters"/> 
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string[]? Masks { get; set; } = null;

	/// <summary>
	/// Gets the current state of the Binding from the provided Input
	/// </summary>
	public abstract BindingState GetState(Input input, int device);

	/// <summary>
	/// If this binding should be included given the proveded Filters
	/// </summary>
	public bool IsIncluded(HashSet<string>? filters)
	{
		if (filters == null || Masks == null || Masks.Length <= 0)
			return true;

		foreach (var it in Masks)
			if (filters.Contains(it))
				return true;

		return false;
	}
}