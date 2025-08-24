using System.Numerics;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to Mouse Motion along an axis
/// </summary>
public sealed class MouseMotionBinding : Binding
{
	/// <summary>
	/// The Axis of Mouse Motion to track
	/// </summary>
	[JsonConverter(typeof(JsonConverters.Vector2))]
	public Vector2 Axis { get; set; }

	/// <summary>
	/// The Sign of the Mouse Motion to track
	/// </summary>
	public int Sign { get; set; }

	/// <summary>
	/// The Minimum distance before the mouse motion is tracked
	/// </summary>
	public float Min { get; set; }

	/// <summary>
	/// The Maximum distance before the mouse motion is clamped
	/// </summary>
	public float Max { get; set; }

	public MouseMotionBinding() {}

	public MouseMotionBinding(in Vector2 axis, int sign, float minValue, float maxValue)
	{
		Axis = axis;
		Sign = sign;
		Min = minValue;
		Max = maxValue;
	}

	public override BindingState GetState(Input input, int device) => new(
		Pressed: GetValue(input.State) > 0 && GetValue(input.LastState) <= 0,
		Released: GetValue(input.State) <= 0 && GetValue(input.LastState) > 0,
		Down: GetValue(input.State) > 0,
		Value: GetValue(input.State),
		Timestamp: input.Mouse.MotionTimestamp()
	);

	private float GetValue(InputState state)
	{
		var value = Vector2.Dot(Axis, state.Mouse.Delta);
		return Calc.ClampedMap(value, Sign * Min, Sign * Max, 0, 1);
	}

	[JsonIgnore]
	public override string Descriptor => $"Mouse Motion {Axis}{(Sign > 0 ? '+' : '-')}, {Min}-{Max}";
}
