using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// Serializable object that holds bindings for an Axis
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public sealed class AxisBindingSet
{
	/// <summary>
	/// Holds a singular axis binding pair
	/// </summary>
	public class AxisEntry(Binding negative, Binding positive, BindingAxisOverlap overlap = default, string[]? masks = null)
	{
		/// <summary>
		/// How to handle overlapping inputs between negative and positive bindings
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		[JsonConverter(typeof(JsonStringEnumConverter<BindingAxisOverlap>))]
		public BindingAxisOverlap Overlap { get; set; } = overlap;

		/// <summary>
		/// Negative Axis Binding
		/// </summary>
		public Binding Negative { get; set; } = negative;

		/// <summary>
		/// Positive Axis Binding
		/// </summary>
		public Binding Positive { get; set; } = positive;

		/// <summary>
		/// Optional set of Masks to Filter the binding by.
		/// These are filtered by <see cref="Input.BindingFilters"/>
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public string[]? Masks { get; set; } = masks;
	}

	/// <summary>
	/// Axis Binding Pairs
	/// </summary>
	public List<AxisEntry> Entries { get; private set; } = [];

	public AxisBindingSet() {}

	public AxisBindingSet(params ReadOnlySpan<(Binding Negative, Binding Positive)> bindings)
	{
		foreach (var (Negative, Positive) in bindings)
			Entries.Add(new(Negative, Positive));
	}

	public AxisBindingSet(params ReadOnlySpan<AxisEntry> bindings)
	{
		foreach (var it in bindings)
			Entries.Add(it);
	}

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public AxisBindingSet Add(Keys negative, Keys positive, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new KeyboardKeyBinding(negative),
			new KeyboardKeyBinding(positive),
			overlap
		));
		return this;
	}

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public AxisBindingSet Add(in ReadOnlySpan<string> masks, Keys negative, Keys positive, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new KeyboardKeyBinding(negative),
			new KeyboardKeyBinding(positive),
			overlap,
			[..masks]
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public AxisBindingSet Add(Buttons negative, Buttons positive, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerButtonBinding(negative),
			new ControllerButtonBinding(positive),
			overlap
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public AxisBindingSet Add(in ReadOnlySpan<string> masks, Buttons negative, Buttons positive, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerButtonBinding(negative),
			new ControllerButtonBinding(positive),
			overlap,
			[..masks]
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public AxisBindingSet Add(Axes axis, float deadzone = 0)
	{
		Entries.Add(new(
			new ControllerAxisBinding(axis, -1, deadzone),
			new ControllerAxisBinding(axis, 1, deadzone)
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public AxisBindingSet Add(in ReadOnlySpan<string> masks, Axes axis, float deadzone = 0)
	{
		Entries.Add(new(
			new ControllerAxisBinding(axis, -1, deadzone),
			new ControllerAxisBinding(axis, 1, deadzone),
			masks: [..masks]
		));
		return this;
	}

	/// <summary>
	/// Gets the current Value of the Axis from the provided Input
	/// </summary>
	public float Value(Input input, int device)
	{
		float value = 0;

		foreach (var pair in Entries)
		{
			if (!input.IsIncluded(pair.Masks))
				continue;
				
			var negative = pair.Negative.GetState(input, device);
			var positive = pair.Positive.GetState(input, device);
			var nextValue = pair.Overlap.Resolve(negative, positive);
			if (MathF.Abs(nextValue) > MathF.Abs(value))
				value = nextValue;
		}

		return value;
	}

	/// <summary>
	/// Gets the Sign of a press that happened this frame
	/// </summary>
	public int PressedSign(Input input, int device)
	{
		float value = 0;

		foreach (var pair in Entries)
		{
			if (!input.IsIncluded(pair.Masks))
				continue;

			var negative = pair.Negative.GetState(input, device);
			var positive = pair.Positive.GetState(input, device);

			if (!negative.Pressed)
				negative = default;
			if (!positive.Pressed)
				positive = default;
			
			var nextValue = pair.Overlap.Resolve(negative, positive);
			if (MathF.Abs(nextValue) > MathF.Abs(value))
				value = nextValue;
		}

		return Math.Sign(value);
	}

	public class JsonConverter : JsonConverter<AxisBindingSet>
	{
		public override AxisBindingSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> new() { Entries = JsonSerializer.Deserialize(ref reader, ListAxisBindingSetEntriesJsonContext.Default.ListAxisEntry) ?? [] };

		public override void Write(Utf8JsonWriter writer, AxisBindingSet value, JsonSerializerOptions options)
			=> JsonSerializer.Serialize(writer, value.Entries, ListAxisBindingSetEntriesJsonContext.Default.ListAxisEntry);
	}
}

[JsonSerializable(typeof(List<AxisBindingSet.AxisEntry>))]
internal partial class ListAxisBindingSetEntriesJsonContext : JsonSerializerContext {}
