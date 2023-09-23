using Foster.Framework;

namespace TinyLink;

public class Orb : Actor
{
	public bool TowardsPlayer = true;
	public float Speed = 40;

	public Orb()
	{
		Hitbox = new(new RectInt(-4, -4, 8, 8));
		Mask = Masks.Hazard;
		Sprite = Assets.GetSprite("bullet");
		Depth = -5;
	}

	public Point2 Target
	{
		get
		{
			var player = Game.GetFirst(Masks.Player);
			var enemy = Game.GetFirst(Masks.Enemy);

			if (player != null && enemy != null)
				return TowardsPlayer ? player.Position : enemy.Position + new Point2(0, -8);

			return Point2.Zero;
		}
	}

	public override void Update()
	{
		base.Update();

		var diff = (Target - Position).Normalized();
		Velocity = diff * Speed;
	}

	public override void Destroyed()
	{
		Game.Create<Pop>(Position);
	}

	public override void OnWasHit(Actor by)
	{
		TowardsPlayer = !TowardsPlayer;
		Speed += 40;
	}

	public override void OnPerformHit(Actor hitting)
	{
		Game.Destroy(this);
	}
}