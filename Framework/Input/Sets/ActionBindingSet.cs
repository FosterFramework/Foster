using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// Serializable object that holds bindings for a single Action
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public sealed class ActionBindingSet
{
	/// <summary>
	/// Holds a singular Action binding
	/// </summary>
	public class ActionEntry(Binding binding, string[]? masks = null)
	{
		/// <summary>
		/// The Binding to Check
		/// </summary>
		public Binding Binding { get; set; } = binding;

		/// <summary>
		/// Optional set of Masks to Filter the binding by.
		/// These are filtered by <see cref="Input.BindingFilters"/>
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public string[]? Masks { get; set; } = masks;
	}

	/// <summary>
	/// List of Entries in the Binding Set
	/// </summary>
	public List<ActionEntry> Entries { get; private set; } = [];

	public ActionBindingSet() {}

	public ActionBindingSet(params ReadOnlySpan<Binding> bindings)
	{
		foreach (var it in bindings)
			Entries.Add(new (it));
	}

	/// <summary>
	/// Adds Keyboard Key mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(params ReadOnlySpan<Keys> values)
	{
		foreach (var it in values)
			Entries.Add(new(new KeyboardKeyBinding(it)));
		return this;
	}

	/// <summary>
	/// Adds Keyboard Key mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(in ReadOnlySpan<string> masks, params ReadOnlySpan<Keys> values)
	{
		foreach (var it in values)
			Entries.Add(new(new KeyboardKeyBinding(it), [..masks]));
		return this;
	}

	/// <summary>
	/// Adds GamePad Button mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(params ReadOnlySpan<Buttons> values)
	{
		foreach (var it in values)
			Entries.Add(new(new ControllerButtonBinding(it)));
		return this;
	}

	/// <summary>
	/// Adds GamePad Button mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(in ReadOnlySpan<string> masks, params ReadOnlySpan<Buttons> values)
	{
		foreach (var it in values)
			Entries.Add(new(new ControllerButtonBinding(it), [..masks]));
		return this;
	}

	/// <summary>
	/// Adds GamePad Axis mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(Axes axes, int sign, float deadzone = 0)
	{
		Entries.Add(new(new ControllerAxisBinding(axes, sign, deadzone)));
		return this;
	}

	/// <summary>
	/// Adds GamePad Axis mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(in ReadOnlySpan<string> masks, Axes axes, int sign, float deadzone = 0)
	{
		Entries.Add(new(new ControllerAxisBinding(axes, sign, deadzone), [..masks]));
		return this;
	}

	/// <summary>
	/// Adds Mouse Button mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(params ReadOnlySpan<MouseButtons> values)
	{
		foreach (var it in values)
			Entries.Add(new(new MouseButtonBinding(it)));
		return this;
	}

	/// <summary>
	/// Adds Mouse Button mappings to this Binding
	/// </summary>
	public ActionBindingSet Add(in ReadOnlySpan<string> masks, params ReadOnlySpan<MouseButtons> values)
	{
		foreach (var it in values)
			Entries.Add(new(new MouseButtonBinding(it), [..masks]));
		return this;
	}

	public ActionBindingSet AddLeftJoystickLeft(float deadzone = 0)
		=> Add(Axes.LeftX, -1, deadzone);

	public ActionBindingSet AddLeftJoystickLeft(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.LeftX, -1, deadzone);

	public ActionBindingSet AddLeftJoystickRight(float deadzone = 0)
		=> Add(Axes.LeftX, 1, deadzone);

	public ActionBindingSet AddLeftJoystickRight(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.LeftX, 1, deadzone);

	public ActionBindingSet AddLeftJoystickUp(float deadzone = 0)
		=> Add(Axes.LeftY, -1, deadzone);

	public ActionBindingSet AddLeftJoystickUp(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.LeftY, -1, deadzone);

	public ActionBindingSet AddLeftJoystickDown(float deadzone = 0)
		=> Add(Axes.LeftY, 1, deadzone);

	public ActionBindingSet AddLeftJoystickDown(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.LeftY, 1, deadzone);

	public ActionBindingSet AddRightJoystickLeft(float deadzone = 0)
		=> Add(Axes.RightX, -1, deadzone);

	public ActionBindingSet AddRightJoystickLeft(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.RightX, -1, deadzone);

	public ActionBindingSet AddRightJoystickRight(float deadzone = 0)
		=> Add(Axes.RightX, 1, deadzone);

	public ActionBindingSet AddRightJoystickRight(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.RightX, 1, deadzone);

	public ActionBindingSet AddRightJoystickUp(float deadzone = 0)
		=> Add(Axes.RightY, -1, deadzone);

	public ActionBindingSet AddRightJoystickUp(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.RightY, -1, deadzone);

	public ActionBindingSet AddRightJoystickDown(float deadzone = 0)
		=> Add(Axes.RightY, 1, deadzone);

	public ActionBindingSet AddRightJoystickDown(in ReadOnlySpan<string> masks, float deadzone = 0)
		=> Add(masks, Axes.RightY, 1, deadzone);

	public BindingState GetState(Input input, int device)
	{
		BindingState result = new();

		foreach (var it in Entries)
		{
			if (!input.IsIncluded(it.Masks))
				continue;

			var state = it.Binding.GetState(input, device);
			result = new(
				Pressed: result.Pressed || state.Pressed,
				Released: result.Released || state.Released,
				Down: result.Down || state.Down,
				Value: Math.Max(result.Value, state.Value),
				Timestamp: result.Timestamp > state.Timestamp ? result.Timestamp : state.Timestamp
			);
		}

		return result;
	}

	public class JsonConverter : JsonConverter<ActionBindingSet>
	{
		public override ActionBindingSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> new() { Entries = JsonSerializer.Deserialize(ref reader, ListActionBindingSetEntriesJsonContext.Default.ListActionEntry) ?? [] };

		public override void Write(Utf8JsonWriter writer, ActionBindingSet value, JsonSerializerOptions options)
			=> JsonSerializer.Serialize(writer, value.Entries, ListActionBindingSetEntriesJsonContext.Default.ListActionEntry);
	}
}

[JsonSerializable(typeof(List<ActionBindingSet.ActionEntry>))]
internal partial class ListActionBindingSetEntriesJsonContext : JsonSerializerContext {}