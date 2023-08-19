namespace Foster.Framework;

/// <summary>
/// A Virtual Input Axis that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualAxis
{
	public enum Overlaps
	{
		CancelOut,
		TakeOlder,
		TakeNewer
	};

	public interface INode
	{
		float Value(bool deadzone);
		TimeSpan Timestamp { get; }
	}

	public class KeyNode : INode
	{
		public Keys Key;
		public bool Positive;

		public float Value(bool deadzone) => (Input.Keyboard.Down(Key) ? (Positive ? 1 : -1) : 0);
		public TimeSpan Timestamp => Input.Keyboard.Timestamp(Key);

		public KeyNode(Keys key, bool positive)
		{
			Key = key;
			Positive = positive;
		}
	}

	public class ButtonNode : INode
	{
		public int Index;
		public Buttons Button;
		public bool Positive;

		public float Value(bool deadzone) => (Input.Controllers[Index].Down(Button) ? (Positive ? 1 : -1) : 0);
		public TimeSpan Timestamp => Input.Controllers[Index].Timestamp(Button);

		public ButtonNode(int controller, Buttons button, bool positive)
		{
			Index = controller;
			Button = button;
			Positive = positive;
		}
	}

	public class AxisNode : INode
	{
		public int Index;
		public Axes Axis;
		public bool Positive;
		public float Deadzone;

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

		public AxisNode(int controller, Axes axis, float deadzone, bool positive)
		{
			Index = controller;
			Axis = axis;
			Deadzone = deadzone;
			Positive = positive;
		}
	}

	public float Value => GetValue(true);
	public float ValueNoDeadzone => GetValue(false);

	public int IntValue => Math.Sign(Value);
	public int IntValueNoDeadzone => Math.Sign(ValueNoDeadzone);

	public readonly List<INode> Nodes = new List<INode>();
	public Overlaps OverlapBehaviour;

	private const float EPSILON = 0.00001f;

	public VirtualAxis(Overlaps overlapBehaviour = Overlaps.CancelOut)
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

				if (time > TimeSpan.Zero && Math.Abs(val) > EPSILON && time > timestamp)
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

				if (time > TimeSpan.Zero && Math.Abs(val) > EPSILON && time < timestamp)
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
		Nodes.Add(new AxisNode(controller, axis, deadzone, true));
		return this;
	}

	public VirtualAxis Add(int controller, Axes axis, bool inverse, float deadzone = 0f)
	{
		Nodes.Add(new AxisNode(controller, axis, deadzone, !inverse));
		return this;
	}

	public void Clear()
	{
		Nodes.Clear();
	}

}
