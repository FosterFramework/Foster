using System.Numerics;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to Mouse Motion along an axis
/// </summary>
public sealed class MouseMotionBinding : Binding
{
	[JsonInclude] public Vector2 Axis;
	[JsonInclude] public int Sign;
	[JsonInclude] public float MinimumValue;
	[JsonInclude] public float MaximumValue;

	public MouseMotionBinding() {}

	public MouseMotionBinding(in Vector2 axis, int sign, float minValue, float maxValue)
	{
		Axis = axis;
		Sign = sign;
		MinimumValue = minValue;
		MaximumValue = maxValue;
	}

	public override BindingState GetState(Input input, int device) => new(
		Pressed: GetValue(input.State) > 0 && GetValue(input.LastState) <= 0,
		Released: GetValue(input.State) <= 0 && GetValue(input.LastState) > 0,
		Down: GetValue(input.State) > 0,
		Value: GetValue(input.State),
		ValueNoDeadzone: GetValue(input.State),
		Timestamp: input.Mouse.MotionTimestamp()
	);

	private float GetValue(InputState state)
	{
		var value = Vector2.Dot(Axis, state.Mouse.Delta);
		return Calc.ClampedMap(value, Sign * MinimumValue, Sign * MaximumValue, 0, 1);
	}
}