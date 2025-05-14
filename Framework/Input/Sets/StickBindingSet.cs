using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input binding that represents a 2D Axis/Stick
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public sealed class StickBindingSet
{
	/// <summary>
	/// Holds a singular stick binding pair
	/// </summary>
	public class StickEntry(Binding left, Binding right, Binding up, Binding down, float deadzone = 0, BindingAxisOverlap? overlap = null, string[]? masks = null)
	{
		/// <summary>
		/// Circular Deadzone applied to the final value
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public float CircularDeadzone { get; set; } = deadzone;

		/// <summary>
		/// How to handle overlapping inputs between negative and positive bindings
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		[JsonConverter(typeof(JsonStringEnumConverter<BindingAxisOverlap>))]
		public BindingAxisOverlap Overlap { get; set; } = overlap ?? default;

		/// <summary>
		/// Left Binding
		/// </summary>
		public Binding Left { get; set; } = left;

		/// <summary>
		/// Right Binding
		/// </summary>
		public Binding Right { get; set; } = right;

		/// <summary>
		/// Up Binding
		/// </summary>
		public Binding Up { get; set; } = up;

		/// <summary>
		/// Down Binding
		/// </summary>
		public Binding Down { get; set; } = down;

		/// <summary>
		/// Optional set of Masks to Filter the binding by.
		/// These are filtered by <see cref="Input.BindingFilters"/>
		/// </summary>
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public string[]? Masks { get; set; } = masks;
	}

	/// <summary>
	/// Stick Binding Pairs
	/// </summary>
	public List<StickEntry> Entries { get; private set; } = [];

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public StickBindingSet Add(Keys left, Keys right, Keys up, Keys down, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new KeyboardKeyBinding(left),
			new KeyboardKeyBinding(right),
			new KeyboardKeyBinding(up),
			new KeyboardKeyBinding(down),
			0,
			overlap
		));
		return this;
	}

	/// <summary>
	/// Adds a Keyboard Key mapping
	/// </summary>
	public StickBindingSet Add(in ReadOnlySpan<string> masks, Keys left, Keys right, Keys up, Keys down, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new KeyboardKeyBinding(left),
			new KeyboardKeyBinding(right),
			new KeyboardKeyBinding(up),
			new KeyboardKeyBinding(down),
			0,
			overlap,
			[..masks]
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public StickBindingSet Add(Buttons left, Buttons right, Buttons up, Buttons down, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerButtonBinding(left),
			new ControllerButtonBinding(right),
			new ControllerButtonBinding(up),
			new ControllerButtonBinding(down),
			0,
			overlap
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Button mapping
	/// </summary>
	public StickBindingSet Add(in ReadOnlySpan<string> masks, Buttons left, Buttons right, Buttons up, Buttons down, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerButtonBinding(left),
			new ControllerButtonBinding(right),
			new ControllerButtonBinding(up),
			new ControllerButtonBinding(down),
			0,
			overlap,
			[..masks]
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public StickBindingSet Add(Axes x, Axes y, float circularDeadzone, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerAxisBinding(x, -1, 0),
			new ControllerAxisBinding(x, 1, 0),
			new ControllerAxisBinding(y, -1, 0),
			new ControllerAxisBinding(y, 1, 0),
			circularDeadzone,
			overlap
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public StickBindingSet Add(in ReadOnlySpan<string> masks, Axes x, Axes y, float circularDeadzone, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerAxisBinding(x, -1, 0),
			new ControllerAxisBinding(x, 1, 0),
			new ControllerAxisBinding(y, -1, 0),
			new ControllerAxisBinding(y, 1, 0),
			circularDeadzone,
			overlap,
			[..masks]
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public StickBindingSet Add(Axes x, float xDeadzone, Axes y, float yDeadzone, float circularDeadzone, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerAxisBinding(x, -1, xDeadzone),
			new ControllerAxisBinding(x, 1, xDeadzone),
			new ControllerAxisBinding(y, -1, yDeadzone),
			new ControllerAxisBinding(y, 1, yDeadzone),
			circularDeadzone,
			overlap
		));
		return this;
	}

	/// <summary>
	/// Adds a GamePad Axis mapping
	/// </summary>
	public StickBindingSet Add(in ReadOnlySpan<string> masks, Axes x, float xDeadzone, Axes y, float yDeadzone, float circularDeadzone, BindingAxisOverlap overlap = default)
	{
		Entries.Add(new(
			new ControllerAxisBinding(x, -1, xDeadzone),
			new ControllerAxisBinding(x, 1, xDeadzone),
			new ControllerAxisBinding(y, -1, yDeadzone),
			new ControllerAxisBinding(y, 1, yDeadzone),
			circularDeadzone,
			overlap,
			[..masks]
		));
		return this;
	}

	/// <summary>
	/// Adds a Mouse Motion mapping
	/// </summary>
	public StickBindingSet AddMouseMotion(float maxMotion = 25)
	{
		Entries.Add(new(
			new MouseMotionBinding(Vector2.UnitX, -1, 0, maxMotion),
			new MouseMotionBinding(Vector2.UnitX, 1, 0, maxMotion),
			new MouseMotionBinding(Vector2.UnitY, -1, 0, maxMotion),
			new MouseMotionBinding(Vector2.UnitY, 1, 0, maxMotion)
		));
		return this;
	}

	/// <summary>
	/// Adds a Mouse Motion mapping
	/// </summary>
	public StickBindingSet AddMouseMotion(in ReadOnlySpan<string> masks, float maxMotion = 25)
	{
		Entries.Add(new(
			new MouseMotionBinding(Vector2.UnitX, -1, 0, maxMotion),
			new MouseMotionBinding(Vector2.UnitX, 1, 0, maxMotion),
			new MouseMotionBinding(Vector2.UnitY, -1, 0, maxMotion),
			new MouseMotionBinding(Vector2.UnitY, 1, 0, maxMotion),
			masks: [..masks]
		));
		return this;
	}

	public StickBindingSet AddArrowKeys(BindingAxisOverlap overlap = default)
		=> Add(Keys.Left, Keys.Right, Keys.Up, Keys.Down, overlap);

	public StickBindingSet AddArrowKeys(in ReadOnlySpan<string> masks, BindingAxisOverlap overlap = default)
		=> Add(masks, Keys.Left, Keys.Right, Keys.Up, Keys.Down, overlap);

	public StickBindingSet AddWasd(BindingAxisOverlap overlap = default)
		=> Add(Keys.A, Keys.D, Keys.W, Keys.S, overlap);

	public StickBindingSet AddWasd(in ReadOnlySpan<string> masks, BindingAxisOverlap overlap = default)
		=> Add(masks, Keys.A, Keys.D, Keys.W, Keys.S, overlap);

	public StickBindingSet AddLeftJoystick(float deadzone)
		=> Add(Axes.LeftX, Axes.LeftY, deadzone);

	public StickBindingSet AddLeftJoystick(in ReadOnlySpan<string> masks, float deadzone)
		=> Add(masks, Axes.LeftX, Axes.LeftY, deadzone);

	public StickBindingSet AddRightJoystick(float deadzone)
		=> Add(Axes.RightX, Axes.RightY, deadzone);

	public StickBindingSet AddRightJoystick(in ReadOnlySpan<string> masks, float deadzone)
		=> Add(masks, Axes.RightX, Axes.RightY, deadzone);

	public StickBindingSet AddDPad(BindingAxisOverlap overlap = default)
		=> Add(Buttons.Left, Buttons.Right, Buttons.Up, Buttons.Down, overlap);

	public StickBindingSet AddDPad(in ReadOnlySpan<string> masks, BindingAxisOverlap overlap = default)
		=> Add(masks, Buttons.Left, Buttons.Right, Buttons.Up, Buttons.Down, overlap);

	public Vector2 Value(Input input, int device)
	{
		var value = Vector2.Zero;

		foreach (var pair in Entries)
		{
			if (!input.IsIncluded(pair.Masks))
				continue;

			var leftState = pair.Left.GetState(input, device);
			var rightState = pair.Right.GetState(input, device);
			var upState = pair.Up.GetState(input, device);
			var downState = pair.Down.GetState(input, device);

			var nextValue = new Vector2(
				pair.Overlap.Resolve(leftState, rightState),
				pair.Overlap.Resolve(upState, downState)
			);

			if (pair.CircularDeadzone > 0 &&
				nextValue.LengthSquared() < pair.CircularDeadzone * pair.CircularDeadzone)
				continue;

			if (nextValue.LengthSquared() > value.LengthSquared())
				value = nextValue;
		}

		return value;
	}
	
	public class JsonConverter : JsonConverter<StickBindingSet>
	{
		public override StickBindingSet? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> new() { Entries = JsonSerializer.Deserialize(ref reader, ListStickBindingSetEntriesJsonContext.Default.ListStickEntry) ?? [] };

		public override void Write(Utf8JsonWriter writer, StickBindingSet value, JsonSerializerOptions options)
			=> JsonSerializer.Serialize(writer, value.Entries, ListStickBindingSetEntriesJsonContext.Default.ListStickEntry);
	}
}

[JsonSerializable(typeof(List<StickBindingSet.StickEntry>))]
internal partial class ListStickBindingSetEntriesJsonContext : JsonSerializerContext {}
