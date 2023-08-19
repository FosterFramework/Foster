using System;
using System.Collections.Generic;

namespace Foster.Framework;

/// <summary>
/// A Virtual Input Button that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualButton
{
	public interface INode
	{
		bool Pressed(float buffer, TimeSpan lastBufferConsumedTime);
		bool Down { get; }
		bool Released { get; }
		bool Repeated(float delay, float interval);
		void Update();
	}

	public class KeyNode : INode
	{
		public Keys Key;

		public bool Pressed(float buffer, TimeSpan lastBufferConsumedTime)
		{
			if (Input.Keyboard.Pressed(Key))
				return true;

			var timestamp = Input.Keyboard.Timestamp(Key);
			var time = Time.Duration;
			
			if (time - timestamp <= TimeSpan.FromSeconds(buffer) && timestamp > lastBufferConsumedTime)
				return true;

			return false;
		}

		public bool Down => Input.Keyboard.Down(Key);
		public bool Released => Input.Keyboard.Released(Key);
		public bool Repeated(float delay, float interval) => Input.Keyboard.Repeated(Key, delay, interval);
		public void Update() { }

		public KeyNode(Keys key)
		{
			Key = key;
		}
	}

	public class MouseButtonNode : INode
	{
		public MouseButtons MouseButton;

		public bool Pressed(float buffer, TimeSpan lastBufferConsumedTime)
		{
			if (Input.Mouse.Pressed(MouseButton))
				return true;

			var timestamp = Input.Mouse.Timestamp(MouseButton);
			var time = Time.Duration;

			if (time - timestamp <= TimeSpan.FromSeconds(buffer) && timestamp > lastBufferConsumedTime)
				return true;

			return false;
		}

		public bool Down => Input.Mouse.Down(MouseButton);
		public bool Released => Input.Mouse.Released(MouseButton);
		public bool Repeated(float delay, float interval) => Input.Mouse.Repeated(MouseButton, delay, interval);
		public void Update() { }

		public MouseButtonNode(MouseButtons mouseButton)
		{
			MouseButton = mouseButton;
		}
	}

	public class ButtonNode : INode
	{
		public int Index;
		public Buttons Button;

		public bool Pressed(float buffer, TimeSpan lastBufferConsumedTime)
		{
			if (Input.Controllers[Index].Pressed(Button))
				return true;

			var timestamp = Input.Controllers[Index].Timestamp(Button);
			var time = Time.Duration;

			if (time - timestamp <= TimeSpan.FromSeconds(buffer) && timestamp > lastBufferConsumedTime)
				return true;

			return false;
		}

		public bool Down => Input.Controllers[Index].Down(Button);
		public bool Released => Input.Controllers[Index].Released(Button);
		public bool Repeated(float delay, float interval) => Input.Controllers[Index].Repeated(Button, delay, interval);
		public void Update() { }

		public ButtonNode(int controller, Buttons button)
		{
			Index = controller;
			Button = button;
		}
	}

	public class AxisNode : INode
	{
		public int Index;
		public Axes Axis;
		public float Threshold;

		private TimeSpan pressedTimestamp;
		private const float AXIS_EPSILON = 0.00001f;

		public bool Pressed(float buffer, TimeSpan lastBufferConsumedTime)
		{
			if (Pressed())
				return true;

			var time = Time.Duration;

			if (time - pressedTimestamp <= TimeSpan.FromSeconds(buffer) && pressedTimestamp > lastBufferConsumedTime)
				return true;

			return false;
		}

		public bool Down
		{
			get
			{
				if (Math.Abs(Threshold) <= AXIS_EPSILON)
					return Math.Abs(Input.Controllers[Index].Axis(Axis)) > AXIS_EPSILON;

				if (Threshold < 0)
					return Input.Controllers[Index].Axis(Axis) <= Threshold;
				
				return Input.Controllers[Index].Axis(Axis) >= Threshold;
			}
		}

		public bool Released
		{
			get
			{
				if (Math.Abs(Threshold) <= AXIS_EPSILON)
					return Math.Abs(Input.LastState.Controllers[Index].Axis(Axis)) > AXIS_EPSILON && Math.Abs(Input.Controllers[Index].Axis(Axis)) < AXIS_EPSILON;

				if (Threshold < 0)
					return Input.LastState.Controllers[Index].Axis(Axis) <= Threshold && Input.Controllers[Index].Axis(Axis) > Threshold;

				return Input.LastState.Controllers[Index].Axis(Axis) >= Threshold && Input.Controllers[Index].Axis(Axis) < Threshold;
			}
		}

		public bool Repeated(float delay, float interval)
		{
			throw new NotImplementedException();
		}

		private bool Pressed()
		{
			if (Math.Abs(Threshold) <= AXIS_EPSILON)
				return (Math.Abs(Input.LastState.Controllers[Index].Axis(Axis)) < AXIS_EPSILON && Math.Abs(Input.Controllers[Index].Axis(Axis)) > AXIS_EPSILON);

			if (Threshold < 0)
				return (Input.LastState.Controllers[Index].Axis(Axis) > Threshold && Input.Controllers[Index].Axis(Axis) <= Threshold);
			
			return (Input.LastState.Controllers[Index].Axis(Axis) < Threshold && Input.Controllers[Index].Axis(Axis) >= Threshold);
		}

		public void Update()
		{
			if (Pressed())
				pressedTimestamp = Input.Controllers[Index].Timestamp(Axis);
		}

		public AxisNode(int controller, Axes axis, float threshold)
		{
			Index = controller;
			Axis = axis;
			Threshold = threshold;
		}
	}

	public readonly List<INode> Nodes = new List<INode>();
	public float RepeatDelay;
	public float RepeatInterval;
	public float Buffer;

	private TimeSpan lastBufferConsumeTime;

	public bool Pressed
	{
		get
		{
			for (int i = 0; i < Nodes.Count; i++)
				if (Nodes[i].Pressed(Buffer, lastBufferConsumeTime))
					return true;

			return false;
		}
	}

	public bool Down
	{
		get
		{
			for (int i = 0; i < Nodes.Count; i++)
				if (Nodes[i].Down)
					return true;

			return false;
		}
	}

	public bool Released
	{
		get
		{
			for (int i = 0; i < Nodes.Count; i++)
				if (Nodes[i].Released)
					return true;

			return false;
		}
	}

	public bool Repeated
	{
		get
		{
			for (int i = 0; i < Nodes.Count; i++)
				if (Nodes[i].Pressed(Buffer, lastBufferConsumeTime) || Nodes[i].Repeated(RepeatDelay, RepeatInterval))
					return true;

			return false;
		}
	}

	public VirtualButton(float buffer = 0f)
	{
		// Using a Weak Reference to subscribe this object to Updates
		// This way it's automatically collected if the user is no longer
		// using it, and we don't require the user to call a Dispose or 
		// Unsubscribe callback
		Input.virtualButtons.Add(new WeakReference<VirtualButton>(this));

		RepeatDelay = Input.RepeatDelay;
		RepeatInterval = Input.RepeatInterval;
		Buffer = buffer;
	}

	public void ConsumeBuffer()
	{
		lastBufferConsumeTime = Time.Duration;
	}

	public VirtualButton Add(params Keys[] keys)
	{
		foreach (var key in keys)
			Nodes.Add(new KeyNode(key));
		return this;
	}

	public VirtualButton Add(params MouseButtons[] buttons)
	{
		foreach (var button in buttons)
			Nodes.Add(new MouseButtonNode(button));
		return this;
	}

	public VirtualButton Add(int controller, params Buttons[] buttons)
	{
		foreach (var button in buttons)
			Nodes.Add(new ButtonNode(controller, button));
		return this;
	}

	public VirtualButton Add(int controller, Axes axis, float threshold)
	{
		Nodes.Add(new AxisNode(controller, axis, threshold));
		return this;
	}

	public void Clear()
	{
		Nodes.Clear();
	}

	internal void Update()
	{
		for (int i = 0; i < Nodes.Count; i ++)
			Nodes[i].Update();
	}

}
