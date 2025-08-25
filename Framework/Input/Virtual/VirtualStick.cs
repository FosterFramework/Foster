using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A virtual 2D Axis/Stick input, which detects user input mapped through a <see cref="StickBindingSet"/>.
/// </summary>
public sealed class VirtualStick(Input input, string name, StickBindingSet set, int controllerIndex = 0)
	: VirtualInput(input, name, controllerIndex)
{
	/// <summary>
	/// The Binding Action
	/// </summary>
	public readonly StickBindingSet Set = set;

	/// <summary>
	/// The Binding Stick Entries
	/// </summary>
	public List<StickBindingSet.StickEntry> Entries => Set.Entries;

	/// <summary>
	/// The Device Index
	/// </summary>
	[Obsolete("use ControllerIndex instead")]
	public int Device { get => ControllerIndex; set => ControllerIndex = value; }

	/// <summary>
	/// Current Value of the Virtual Stick
	/// </summary>
	public Vector2 Value { get; private set; }

	/// <summary>
	/// Current Value of the Virtual Stick rounded to Integer values
	/// </summary>
	public Point2 IntValue { get; private set; }

	public bool PressedLeft { get; private set; }

	public bool PressedRight { get; private set; }

	public bool PressedUp { get; private set; }

	public bool PressedDown { get; private set; }

	public override int ControllerIndex { get; set; }

	public VirtualStick(Input input, string name, int controllerIndex = 0)
		: this(input, name, new(), controllerIndex) {}

	internal override void Update(in Time time)
	{
		Value = Set.Value(Input, ControllerIndex);
		IntValue = new(MathF.Sign(Value.X), MathF.Sign(Value.Y));
		PressedLeft = PressedRight = PressedDown = PressedUp = false;

		foreach (var it in Set.Entries)
		{
			if (!Input.IsIncluded(it.Masks))
				continue;

			PressedLeft |= it.Left.GetState(Input, ControllerIndex).Pressed;
			PressedRight |= it.Right.GetState(Input, ControllerIndex).Pressed;
			PressedUp |= it.Up.GetState(Input, ControllerIndex).Pressed;
			PressedDown |= it.Down.GetState(Input, ControllerIndex).Pressed;
		}
	}

	public void Clear()
	{
		Value = Vector2.Zero;
		IntValue = Point2.Zero;
		PressedLeft = PressedRight = PressedUp = PressedDown = false;
	}
}
