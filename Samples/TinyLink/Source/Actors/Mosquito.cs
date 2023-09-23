using Foster.Framework;

namespace TinyLink;

public class Mosquito : Actor
{
	public int Health = 2;

	public Mosquito()
	{
		Sprite = Assets.GetSprite("mosquito");
		Hitbox = new(new RectInt(-5, -5, 10, 10));
		Mask = Masks.Enemy;
		Depth = -5;
		CollidesWithSolids = false;
		Play("fly");
	}

	public override void Update()
	{
		base.Update();

		if (Game.GetFirst(Masks.Player) is Actor player)
		{
			var diff = player.Position.X - Position.X;
			var dist = MathF.Abs(diff);
			var sign = MathF.Sign(diff);

			if (dist < 100)
				Velocity.X += sign * 100 * Time.Delta;
			else
				Velocity.X = Calc.Approach(Velocity.X, 0, 100 * Time.Delta);

			if (MathF.Abs(Velocity.X) > 50)
				Velocity.X = Calc.Approach(Velocity.X, MathF.Sign(Velocity.X) * 50, 800 * Time.Delta);
		}

		Velocity.Y = MathF.Sin(Timer * 4) * 10;
	}

	public override void OnWasHit(Actor by)
	{
		Health--;

		if (Health <= 0)
		{
			Game.Create<Pop>(Position);
			Game.Destroy(this);
		}
		else
		{
			var sign = MathF.Sign(Position.X - by.Position.X);
			Velocity.X = sign * 140;
		}
	}
}