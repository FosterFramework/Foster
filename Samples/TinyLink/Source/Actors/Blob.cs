using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Blob : Actor
{
	public int Health = 3;
	public float JumpTimer = 2;
	private bool grounded = true;

	public Blob()
	{
		Sprite = Assets.GetSprite("blob");
		Hitbox = new(new RectInt(-4, -8, 8, 8));
		Mask = Masks.Enemy;
		Depth = -5;
		Play("fly");
	}

	public override void Update()
	{
		base.Update();

		Velocity.Y += 300 * Time.Delta;

		if (Grounded())
		{
			if (!grounded)
			{
				Play("idle");
				Squish = new Vector2(1.5f, 0.50f);
			}

			Velocity.X = Calc.Approach(Velocity.X, 0, 400 * Time.Delta);
			JumpTimer -= Time.Delta;
			grounded = true;
		}
		else
		{
			grounded = false;
		}

		if (JumpTimer <= 0)
		{
			Play("jump");
			JumpTimer = 2;
			Velocity.Y = -90;
			Squish = new Vector2(0.5f, 1.5f);

			if (Game.GetFirst(Masks.Player) is Actor player)
			{
				var dir = MathF.Sign(player.Position.X - Position.X);
				if (dir == 0) dir = 1;

				Facing = dir;
				Velocity.X = Facing * 40;
			}
		}
	}

	public override void OnWasHit(Actor by)
	{
		Health--;

		if (Health <= 0)
		{
			Game.Create<Pop>(Position + new Point2(0, -4));
			Game.Destroy(this);
		}
		else
		{
			var sign = MathF.Sign(Position.X - by.Position.X);
			Velocity.X = sign * 120;
		}
	}
}