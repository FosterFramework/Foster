using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// An Input Binding mapped to a Controller Axis
/// </summary>
public sealed class ControllerAxisBinding : Binding
{
	[JsonConverter(typeof(JsonStringEnumConverter<Axes>))]
	public Axes Axis { get; set; }
	public int Sign { get; set; }
	public float Deadzone { get; set; }

	public ControllerAxisBinding() {}

	public ControllerAxisBinding(Axes axis, int sign, float deadzone)
	{
		Axis = axis;
		Sign = sign;
		Deadzone = deadzone;
	}

	public override BindingState GetState(Input input, int device)
	{
		var value = GetValue(input.State, device, Deadzone);
		var prevValue = GetValue(input.LastState, device, Deadzone);

		return new(
			Pressed: value > 0 && prevValue <= 0,
			Released: value <= 0 && prevValue > 0,
			Down: value > 0,
			Value: value,
			Timestamp: input.Controllers[device].Timestamp(Axis)
		);
	}

	private float GetValue(InputState state, int device, float deadzone)
	{
		var value = state.Controllers[device].Axis(Axis);
		return Calc.ClampedMap(value, Sign * deadzone, Sign);
	}

	[JsonIgnore]
	public override string Descriptor => $"Axis {Axis}{(Sign > 0 ? '+' : '-')}, >={Deadzone}";
}
