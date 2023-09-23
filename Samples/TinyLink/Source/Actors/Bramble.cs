using Foster.Framework;

namespace TinyLink;

public class Bramble : Actor
{
	public Bramble()
	{
		Hitbox = new(new RectInt(-4, -8, 8, 8));
		Mask = Masks.Hazard;
		Sprite = Assets.GetSprite("bramble");
		Play("idle");
	}

	public override void OnPerformHit(Actor hitting) => Pop();
	public override void OnWasHit(Actor by) => Pop();

	public void Pop()
	{
		Game.Destroy(this);
		Game.Create<Pop>(Position + new Point2(0, -4));
	}
}