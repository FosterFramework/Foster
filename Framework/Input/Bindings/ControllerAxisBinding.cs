using System.Text.Json.Serialization;

namespace Foster.Framework;

public sealed class ControllerAxisBinding : Binding
{
	[JsonInclude] public Axes Axis;
	[JsonInclude] public int Sign;
	[JsonInclude] public float Deadzone;

	public ControllerAxisBinding() {}
	public ControllerAxisBinding(Axes axis, int sign, float deadzone)
	{
		Axis = axis;
		Sign = sign;
		Deadzone = deadzone;
	}

	public override BindingState GetState(Input input, int device) => new(
		Pressed: GetValue(input.State, device, Deadzone) > 0 && GetValue(input.LastState, device, Deadzone) <= 0,
		Released: GetValue(input.State, device, Deadzone) <= 0 && GetValue(input.LastState, device, Deadzone) > 0,
		Down: GetValue(input.State, device, Deadzone) > 0,
		Value: GetValue(input.State, device, Deadzone),
		ValueNoDeadzone: GetValue(input.State, device, 0),
		Timestamp: input.Controllers[device].Timestamp(Axis)
	);

	private float GetValue(InputState state, int device, float deadzone)
	{
		var value = state.Controllers[device].Axis(Axis);
		return Calc.ClampedMap(value, Sign * deadzone, Sign, 0.0f, 1.0f);
	}
}
