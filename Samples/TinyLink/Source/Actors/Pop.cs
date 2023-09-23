using Foster.Framework;

namespace TinyLink;

public class Pop : Actor
{
	public Pop()
	{
		Sprite = Assets.GetSprite("pop");
		Play("pop", false);
		Depth = -20;
	}

	public override void Update()
	{
		base.Update();

		if (IsFinishedPlaying())
			Game.Destroy(this);
	}
}