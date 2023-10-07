using System;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Virtual Input Stick that can be mapped to different keyboards and gamepads
/// </summary>
public class VirtualStick
{
	/// <summary>
	/// Optional Virtual Stick name
	/// </summary>
	public readonly string Name = string.Empty;

	/// <summary>
	/// The Horizontal Axis
	/// </summary>
	public readonly VirtualAxis Horizontal;

	/// <summary>
	/// The Vertical Axis
	/// </summary>
	public readonly VirtualAxis Vertical;

	/// <summary>
	/// This Deadzone is applied to the Length of the combined Horizontal and Vertical axis values
	/// </summary>
	public float CircularDeadzone = 0f;

	/// <summary>
	/// Gets the current Virtual Stick value
	/// </summary>
	public Vector2 Value
	{
		get
		{
			var result = new Vector2(Horizontal.Value, Vertical.Value);
			if (CircularDeadzone != 0 && result.Length() < CircularDeadzone)
				return Vector2.Zero;
			return result;
		}
	}

	/// <summary>
	/// Gets the current Virtual Stick value, ignoring Deadzones
	/// </summary>
	public Vector2 ValueNoDeadzone => new Vector2(Horizontal.ValueNoDeadzone, Vertical.ValueNoDeadzone);

	/// <summary>
	/// Gets the current Virtual Stick value, clamping to Integer Values
	/// </summary>
	public Point2 IntValue
	{
		get
		{
			var result = Value;
			return new Point2(MathF.Sign(result.X), MathF.Sign(result.Y));
		}
	}

	/// <summary>
	/// Gets the current Virtual Stick value, clamping to Integer values and Ignoring Deadzones
	/// </summary>
	public Point2 IntValueNoDeadzone => new Point2(Horizontal.IntValueNoDeadzone, Vertical.IntValueNoDeadzone);

	public VirtualStick(string name, VirtualAxis.Overlaps overlapBehaviour, float circularDeadzone)
	{
		Name = name;
		Horizontal = new($"{name}/Horizontal", overlapBehaviour);
		Vertical = new($"{name}/Vertical", overlapBehaviour);
		CircularDeadzone = circularDeadzone;
	}

	public VirtualStick(VirtualAxis.Overlaps overlapBehaviour, float circularDeadzone = 0)
		: this("VirtualStick", overlapBehaviour, circularDeadzone)
	{

	}

	public VirtualStick(VirtualAxis.Overlaps overlapBehaviour = VirtualAxis.Overlaps.TakeNewer)
		: this("VirtualStick", overlapBehaviour, 0.0f)
	{
		
	}

	public VirtualStick Add(Keys left, Keys right, Keys up, Keys down)
	{
		Horizontal.Add(left, right);
		Vertical.Add(up, down);
		return this;
	}

	public VirtualStick Add(int controller, Buttons left, Buttons right, Buttons up, Buttons down)
	{
		Horizontal.Add(controller, left, right);
		Vertical.Add(controller, up, down);
		return this;
	}

	public VirtualStick Add(int controller, Axes horizontal, Axes vertical, float deadzoneHorizontal = 0f, float deadzoneVertical = 0f)
	{
		Horizontal.Add(controller, horizontal, deadzoneHorizontal);
		Vertical.Add(controller, vertical, deadzoneVertical);
		return this;
	}

	public VirtualStick AddLeftJoystick(int controller, float deadzoneHorizontal = 0, float deadzoneVertical = 0)
	{
		Horizontal.Add(controller, Axes.LeftX, deadzoneHorizontal);
		Vertical.Add(controller, Axes.LeftY, deadzoneVertical);
		return this;
	}

	public VirtualStick AddRightJoystick(int controller, float deadzoneHorizontal = 0, float deadzoneVertical = 0)
	{
		Horizontal.Add(controller, Axes.RightX, deadzoneHorizontal);
		Vertical.Add(controller, Axes.RightY, deadzoneVertical);
		return this;
	}

	public VirtualStick AddDPad(int controller)
	{
		Horizontal.Add(controller, Buttons.Left, Buttons.Right);
		Vertical.Add(controller, Buttons.Up, Buttons.Down);
		return this;
	}

	public VirtualStick AddArrowKeys()
	{
		Horizontal.Add(Keys.Left, Keys.Right);
		Vertical.Add(Keys.Up, Keys.Down);
		return this;
	}
	
	public void Consume()
	{
		Horizontal.Consume();
		Vertical.Consume();
	}

	public void ConsumePress()
	{
		Horizontal.ConsumePress();
		Vertical.ConsumePress();
	}

	public void ConsumeRelease()
	{
		Horizontal.ConsumeRelease();
		Vertical.ConsumeRelease();
	}

	public void Clear()
	{
		Horizontal.Clear();
		Vertical.Clear();
	}

}
