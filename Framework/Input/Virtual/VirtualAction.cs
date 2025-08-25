namespace Foster.Framework;

/// <summary>
/// A virtual Action/Button input, which detects user input mapped through a <see cref="ActionBindingSet"/>.
/// </summary>
public sealed class VirtualAction(Input input, string name, ActionBindingSet set, int controllerIndex = 0, float buffer = 0)
	: VirtualInput(input, name, controllerIndex)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly ActionBindingSet Set = set;

	/// <summary>
	/// The Binding Action Entries
	/// </summary>
	public List<ActionBindingSet.ActionEntry> Entries => Set.Entries;

	/// <summary>
	/// The Device Index
	/// </summary>
	[Obsolete("use ControllerIndex instead")]
	public int Device { get => ControllerIndex; set => ControllerIndex = value; }

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

	public override int ControllerIndex { get; set; }

	public VirtualAction(Input input, string name, int controllerIndex = 0, float buffer = 0)
		: this(input, name, new(), controllerIndex, buffer) {}

	internal override void Update(in Time time)
	{
		var state = Set.GetState(Input, ControllerIndex);

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
	/// Consumes the press, and <see cref="Pressed"/> will return false until the next Press
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
	/// Essentially Zeros out the state of the Action
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
