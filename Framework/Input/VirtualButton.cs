using System;
using System.Collections.Generic;

namespace Foster.Framework;

/// <summary>
/// A Virtual Input Button that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualButton
{
	public interface IBinding
	{
		public bool IsPressed { get; }
		public bool IsDown { get; }
		public bool IsReleased { get; }
		public float Value { get; }
		public float ValueNoDeadzone { get; }
	}

	public record KeyBinding(Keys Key) : IBinding
	{
		public bool IsPressed => Input.Keyboard.Pressed(Key);
		public bool IsDown => Input.Keyboard.Down(Key);
		public bool IsReleased => Input.Keyboard.Released(Key);
		public float Value => IsDown ? 1.0f : 0.0f;
		public float ValueNoDeadzone => IsDown ? 1.0f : 0.0f;
	}

	public record ButtonBinding(int Controller, Buttons Button) : IBinding
	{
		public bool IsPressed => Input.Controllers[Controller].Pressed(Button);
		public bool IsDown => Input.Controllers[Controller].Down(Button);
		public bool IsReleased => Input.Controllers[Controller].Released(Button);
		public float Value => IsDown ? 1.0f : 0.0f;
		public float ValueNoDeadzone => IsDown ? 1.0f : 0.0f;
	}

	public record MouseBinding(MouseButtons Button) : IBinding
	{
		public bool IsPressed => Input.Mouse.Pressed(Button);
		public bool IsDown => Input.Mouse.Down(Button);
		public bool IsReleased => Input.Mouse.Released(Button);
		public float Value => IsDown ? 1.0f : 0.0f;
		public float ValueNoDeadzone => IsDown ? 1.0f : 0.0f;
	}

	public record AxisBinding(int Controller, Axes Axis, int Sign, float Deadzone) : IBinding
	{
		public bool IsPressed => GetValue(Input.State, Deadzone) > 0 && GetValue(Input.LastState, Deadzone) <= 0;
		public bool IsDown => GetValue(Input.State, Deadzone) > 0;
		public bool IsReleased => GetValue(Input.State, Deadzone) <= 0 && GetValue(Input.LastState, Deadzone) > 0;
		public float Value => GetValue(Input.State, Deadzone);
		public float ValueNoDeadzone => GetValue(Input.State, 0);

		private float GetValue(InputState state, float deadzone)
		{
			var value = state.Controllers[Controller].Axis(Axis);
			return Calc.ClampedMap(value, Sign * deadzone, Sign, 0.0f, 1.0f);
		}
	}

	/// <summary>
	/// Optional Virtual Button name
	/// </summary>
	public readonly string Name = string.Empty;

	/// <summary>
	/// Button Bindings
	/// </summary>
	public readonly List<IBinding> Bindings = new();

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
	public float Buffer;

	/// <summary>
	/// If the binding was pressed this frame
	/// </summary>
	public bool Pressed { get; private set; }

	/// <summary>
	/// If the binding is currently held down
	/// </summary>
	public bool Down { get; private set; }

	/// <summary>
	/// If the binding was released this frame
	/// </summary>
	public bool Released { get; private set; }

	/// <summary>
	/// If the binding was repeated this frame
	/// </summary>
	public bool Repeated { get; private set; }

	/// <summary>
	/// Floating value of the binding from 0-1.
	/// For most bindings this is always 0 or 1, 
	/// with the exception of the Axis Binding.
	/// </summary>
	public float Value { get; private set; }

	/// <summary>
	/// Floating value of the binding from -1 to +1
	/// For most bindings this is always 0 or 1, 
	/// with the exception of the Axis Binding.
	/// This ignores AxisBinding.Deadzone
	/// </summary>
	public float ValueNoDeadzone { get; private set; }

	/// <summary>
	/// The time since the button was last pressed
	/// </summary>
	public TimeSpan PressTimestamp { get; private set; }

	/// <summary>
	/// If the current Press was consumed
	/// </summary>
	public bool PressConsumed { get; private set; }

	/// <summary>
	/// The time since the button was last released
	/// </summary>
	public TimeSpan ReleaseTimestamp { get; private set; }

	/// <summary>
	/// If the current Release was consumed
	/// </summary>
	public bool ReleaseConsumed { get; private set; }

	public VirtualButton(string name, float buffer = 0)
	{
		// Using a Weak Reference to subscribe this object to Updates
		// This way it's automatically collected if the user is no longer
		// using it, and we don't require the user to call a Dispose or 
		// Unsubscribe callback
		Input.virtualButtons.Add(new WeakReference<VirtualButton>(this));

		Name = name;
		Buffer = buffer;
		RepeatDelay = Input.RepeatDelay;
		RepeatInterval = Input.RepeatInterval;
	}

	public VirtualButton(float buffer = 0)
		: this("VirtualButton", buffer) {}

	public VirtualButton Add(params Keys[] keys)
	{
		foreach (var key in keys)
			Bindings.Add(new KeyBinding(key));
		return this;
	}

	public VirtualButton Add(params MouseButtons[] buttons)
	{
		foreach (var button in buttons)
			Bindings.Add(new MouseBinding(button));
		return this;
	}

	public VirtualButton Add(int controller, params Buttons[] buttons)
	{
		foreach (var button in buttons)
			Bindings.Add(new ButtonBinding(controller, button));
		return this;
	}

	public VirtualButton Add(int controller, Axes axis, int sign, float threshold)
	{
		Bindings.Add(new AxisBinding(controller, axis, sign, threshold));
		return this;
	}

	/// <summary>
	/// Clears all the Bindings
	/// </summary>
	public void Clear()
	{
		Bindings.Clear();
	}

	/// <summary>
	/// Essentially Zeros out the state of the Button
	/// </summary>
	public void Consume()
	{
		Pressed = false;
		Released = false;
		Down = false;
		Repeated = false;
		Value = 0.0f;
		PressConsumed = true;
		ReleaseConsumed = true;
		PressTimestamp = TimeSpan.Zero;
		ReleaseTimestamp = TimeSpan.Zero;
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
	/// Consumes the release, and VirtualButton.Released will return false until the next Release
	/// </summary>
	/// <returns>True if there was a Release to consume</returns>
	public bool ConsumeRelease()
	{
		if (Released)
		{
			Released = false;
			ReleaseConsumed = true;
			return true;
		}
		else
			return false;
	}

	[Obsolete("Use VirtualButton.ConsumePress() instead")]
	public void ConsumeBuffer() => ConsumePress();

	internal void Update()
	{
		Pressed = GetPressed();
		Released = GetReleased();
		Down = GetDown();
		Value = GetValue();
		Repeated = false;

		if (Pressed)
		{
			PressConsumed = false;
			PressTimestamp = Time.Duration;
		}
		else if (!PressConsumed && PressTimestamp > TimeSpan.Zero && (Time.Duration - PressTimestamp).TotalSeconds < Buffer)
		{
			Pressed = true;
		}

		if (Released)
		{
			ReleaseConsumed = false;
			ReleaseTimestamp = Time.Duration;
		}
		else if (!ReleaseConsumed && ReleaseTimestamp > TimeSpan.Zero && (Time.Duration - ReleaseTimestamp).TotalSeconds < Buffer)
		{
			Released = true;
		}

		if (Down && (Time.Duration - PressTimestamp).TotalSeconds > RepeatDelay)
		{
			if (Time.OnInterval(
				(Time.Duration - PressTimestamp).TotalSeconds - RepeatDelay,
				Time.Delta,
				RepeatInterval, 0))
			{
				Repeated = true;
			}
		}
	}

	private bool GetPressed()
	{
		foreach (var it in Bindings)
			if (it.IsPressed)
				return true;
		return false;
	}

	private bool GetDown()
	{
		foreach (var it in Bindings)
			if (it.IsDown)
				return true;
		return false;
	}

	private bool GetReleased()
	{
		foreach (var it in Bindings)
			if (it.IsReleased)
				return true;
		return false;
	}

	private float GetValue()
	{
		float highest = 0.0f;
		foreach (var it in Bindings)
			highest = MathF.Max(highest, it.Value);
		return highest;
	}
}
