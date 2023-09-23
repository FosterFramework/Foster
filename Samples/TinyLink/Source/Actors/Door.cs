using Foster.Framework;

namespace TinyLink;

public class Door : Actor
{
	private float delay = 0;

	public Door()
	{
		Sprite = Assets.GetSprite("door");
		Depth = -1;
		Visible = false;
		Play("idle");
	}

	public void Appear()
	{
		Hitbox = new (new RectInt(-6, -16, 12, 16));
		Mask = Masks.Solid;
		Visible = true;

		if (Timer > 0.1f)
			Game.Create<Pop>(Position + new Point2(0, -8));
	}

	public override void Update()
	{
		base.Update();

		// wait to appear
		if (!Visible)
		{
			if (Game.GetFirst(Masks.Player) is not Actor player || player.Position.X > Position.X + 12)
				Appear();
		}
		// wait for all enemies to be gone
		else if (Timer > 0.25f)
		{
			bool anyEnemiesAlive = false;

			foreach (var it in Game.Actors)
				if (it.Mask == Masks.Enemy)
				{
					anyEnemiesAlive = true;
					break;
				}

			if (!anyEnemiesAlive)
			{
				if (delay > 0.50f)
				{
					Game.Create<Pop>(Position + new Point2(0, -8));
					Game.Destroy(this);
				}
				delay += Time.Delta;
			}
		}
	}
}