using Foster.Framework;

namespace TinyLink;

public class Jumpthru : Actor
{
	public Jumpthru()
	{
		Hitbox = new(new RectInt(0, 0, Game.TileSize, Game.TileSize / 4));
		Mask = Actor.Masks.Jumpthru;
		Sprite = Assets.GetSprite("jumpthru");
		Depth = 5;
		Play("idle");
	}
}