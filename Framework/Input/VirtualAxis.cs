namespace Foster.Framework;

/// <summary>
/// A Virtual Input Axis that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualAxis
{
	public enum Overlaps
	{
		/// <summary>
		/// Uses whichever input was pressed most recently
		/// </summary>
		TakeNewer,

		/// <summary>
		/// Uses whichever input was pressed longest ago
		/// </summary>
		TakeOlder,

		/// <summary>
		/// Inputs cancel each other out
		/// </summary>
		CancelOut,
	};

	public interface INode
	{
		float Value(bool deadzone);
		TimeSpan Timestamp { get; }
	}

	public record KeyNode(Keys Key, bool Positive) : INode
	{
		public float Value(bool deadzone) 
			=> Input.Keyboard.Down(Key) ? (Positive ? 1 : -1) : 0;
		public TimeSpan Timestamp
			=> Input.Keyboard.Timestamp(Key);
	}

	public record ButtonNode(int Index, Buttons Button, bool Positive) : INode
	{
		public float Value(bool deadzone)
			=> Input.Controllers[Index].Down(Button) ? (Positive ? 1 : -1) : 0;
		public TimeSpan Timestamp
			=> Input.Controllers[Index].Timestamp(Button);
	}

	public record AxisNode(int Index, Axes Axis, bool Positive, float Deadzone) : INode
	{
		public float Value(bool deadzone)
		{
			if (!deadzone || Math.Abs(Input.Controllers[Index].Axis(Axis)) >= Deadzone)
				return Input.Controllers[Index].Axis(Axis) * (Positive ? 1 : -1);
			return 0f;
		}

		public TimeSpan Timestamp
		{
			get
			{
				if (Math.Abs(Input.Controllers[Index].Axis(Axis)) < Deadzone)
					return TimeSpan.Zero;
				return Input.Controllers[Index].Timestamp(Axis);
			}
		}
	}

	public float Value => GetValue(true);
	public float ValueNoDeadzone => GetValue(false);

	public int IntValue => Math.Sign(Value);
	public int IntValueNoDeadzone => Math.Sign(ValueNoDeadzone);

	public readonly List<INode> Nodes = new();
	public Overlaps OverlapBehaviour = Overlaps.TakeNewer;

	public VirtualAxis(Overlaps overlapBehaviour = Overlaps.TakeNewer)
	{
		OverlapBehaviour = overlapBehaviour;
	}

	private float GetValue(bool deadzone)
	{
		var value = 0f;

		if (OverlapBehaviour == Overlaps.CancelOut)
		{
			foreach (var input in Nodes)
				value += input.Value(deadzone);
			value = Calc.Clamp(value, -1, 1);
		}
		else if (OverlapBehaviour == Overlaps.TakeNewer)
		{
			var timestamp = TimeSpan.Zero;
			for (int i = 0; i < Nodes.Count; i++)
			{
				var time = Nodes[i].Timestamp;
				var val = Nodes[i].Value(deadzone);

				if (time > TimeSpan.Zero && Math.Abs(val) > float.Epsilon && time > timestamp)
				{
					value = val;
					timestamp = time;
				}
			}
		}
		else if (OverlapBehaviour == Overlaps.TakeOlder)
		{
			var timestamp = TimeSpan.Zero;
			for (int i = 0; i < Nodes.Count; i++)
			{
				var time = Nodes[i].Timestamp;
				var val = Nodes[i].Value(deadzone);

				if (time > TimeSpan.Zero && Math.Abs(val) > float.Epsilon && time < timestamp)
				{
					value = val;
					timestamp = time;
				}
			}
		}

		return value;
	}

	public VirtualAxis Add(Keys negative, Keys positive)
	{
		Nodes.Add(new KeyNode(negative, false));
		Nodes.Add(new KeyNode(positive, true));
		return this;
	}

	public VirtualAxis Add(Keys key, bool isPositive)
	{
		Nodes.Add(new KeyNode(key, isPositive));
		return this;
	}

	public VirtualAxis Add(int controller, Buttons negative, Buttons positive)
	{
		Nodes.Add(new ButtonNode(controller, negative, false));
		Nodes.Add(new ButtonNode(controller, positive, true));
		return this;
	}

	public VirtualAxis Add(int controller, Buttons button, bool isPositive)
	{
		Nodes.Add(new ButtonNode(controller, button, isPositive));
		return this;
	}

	public VirtualAxis Add(int controller, Axes axis, float deadzone = 0f)
	{
		Nodes.Add(new AxisNode(controller, axis, true, deadzone));
		return this;
	}

	public VirtualAxis Add(int controller, Axes axis, bool inverse, float deadzone = 0f)
	{
		Nodes.Add(new AxisNode(controller, axis, !inverse, deadzone));
		return this;
	}

	public void Clear()
	{
		Nodes.Clear();
	}
}
