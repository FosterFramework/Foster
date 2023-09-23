
using Foster.Framework;

namespace TinyLink;

public static class Controls
{
	public static readonly VirtualStick Move = new();
	public static readonly VirtualButton Jump = new();
	public static readonly VirtualButton Attack = new();

	public static void Init()
	{
		Move.AddLeftJoystick(0, 0.2f, 0.2f);
		Move.Add(0, Buttons.Left, Buttons.Right, Buttons.Up, Buttons.Down);
		Move.Add(Keys.Left, Keys.Right, Keys.Up, Keys.Down);

		Jump.Buffer = 0.15f;
		Jump.Add(Keys.X);
		Jump.Add(0, Buttons.A);

		Attack.Buffer = 0.15f;
		Attack.Add(Keys.C);
		Attack.Add(0, Buttons.X);
	}
}