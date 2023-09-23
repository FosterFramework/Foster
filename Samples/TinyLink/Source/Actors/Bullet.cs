using Foster.Framework;

namespace TinyLink;

public class Bullet : Actor
{
	public const float Gravity = 130;

	public Bullet()
	{
		Hitbox = new(new RectInt(-4, -4, 8, 8));
		Mask = Masks.Enemy;
		Sprite = Assets.GetSprite("bullet");
		Depth = -5;
	}

	public override void Update()
	{
		base.Update();

		Velocity.Y += Gravity * Time.Delta;

		if (Timer > 2.5f && Time.BetweenInterval(0.05f))
			Visible = !Visible;

		if (Timer > 3.0f)
			Game.Destroy(this);
	}

	public override void OnCollideX() => Game.Destroy(this);
	public override void OnCollideY() => Velocity.Y = -60;

	public override void OnPerformHit(Actor hitting) => Pop();
	public override void OnWasHit(Actor by) => Pop();

	public void Pop()
	{
		Game.Hitstun(0.1f);
		Game.Destroy(this);
		Game.Create<Pop>(Position);
	}
}