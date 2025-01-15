namespace Foster.Framework;

/// <summary>
/// A virtual Action/Button input, which detects user input mapped through a <see cref="ActionBinding"/>.
/// </summary>
public sealed class VirtualAction(Input input, ActionBinding action, int controllerIndex = 0, float buffer = 0) : VirtualInput(input)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly ActionBinding Binding = action;

	/// <summary>
	/// The Device Index
	/// </summary>
	public readonly int Device = controllerIndex;

	/// <summary>
	/// How long before invoking the first Repeated signal
	/// </summary>
	public float RepeatDelay;

	/// <summary>
	/// How frequently to invoke a Repeated signal
	/// </summary>
	public float RepeatInterval;

	/// <summary>
	/// Input buffer
	/// </summary>
	public float Buffer = buffer;

	/// <summary>
	/// If the Action was pressed this frame
	/// </summary>
	public bool Pressed { get; private set; }

	/// <summary>
	/// If the current Press was consumed
	/// </summary>
	public bool PressConsumed { get; private set; }

	/// <summary>
	/// If the Action is currently held down
	/// </summary>
	public bool Down { get; private set; }

	/// <summary>
	/// If the Action was released this frame
	/// </summary>
	public bool Released { get; private set; }

	/// <summary>
	/// If the Action was repeated this frame
	/// </summary>
	public bool Repeated { get; private set; }

	/// <summary>
	/// Floating value of the Action from 0-1.
	/// For most Actions this is always 0 or 1, 
	/// with the exception of the Axis Binding.
	/// </summary>
	public float Value { get; private set; }

	/// <summary>
	/// Floating value of the Action from -1 to +1
	/// For most Actions this is always 0 or 1, 
	/// with the exception of the Axis Binding.
	/// This ignores AxisBinding.Deadzone
	/// </summary>
	public float ValueNoDeadzone { get; private set; }

	/// <summary>
	/// The time since the Action was last pressed
	/// </summary>
	public TimeSpan Timestamp { get; private set; }

	internal override void Update(in Time time)
	{
		var state = Binding.GetState(Input, Device);

		Pressed = state.Pressed;
		Released = state.Released;
		Down = state.Down;
		Value = state.Value;
		Repeated = false;

		if (Pressed)
		{
			PressConsumed = false;
			Timestamp = time.Elapsed;
		}
		else if (!PressConsumed && Timestamp > TimeSpan.Zero && (time.Elapsed - Timestamp).TotalSeconds < Buffer)
		{
			Pressed = true;
		}

		if (Down && (time.Elapsed - Timestamp).TotalSeconds > RepeatDelay)
		{
			if (Time.OnInterval(
				(time.Elapsed - Timestamp).TotalSeconds - RepeatDelay,
				time.Delta,
				RepeatInterval, 0))
			{
				Repeated = true;
			}
		}
	}

	/// <summary>
	/// Consumes the press, and VirtualButton.Pressed will return false until the next Press
	/// </summary>
	/// <returns>True if there was a Press to consume</returns>
	public bool ConsumePress()
	{
		if (Pressed)
		{
			Pressed = false;
			PressConsumed = true;
			return true;
		}
		else
			return false;
	}

	/// <summary>
	/// Essentially Zeros out the state of the Button
	/// </summary>
	public void Clear()
	{
		Pressed = false;
		Released = false;
		PressConsumed = true;
		Down = false;
		Repeated = false;
		Value = 0.0f;
	}
}