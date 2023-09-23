using System.Numerics;
using Foster.Framework;

namespace TinyLink;

/// <summary>
/// Uses the Room's Text Value to show text.
/// </summary>
public class TitleText : Actor
{
	public override void Render(Batcher batcher)
	{
		if (Assets.Font != null && Game.CurrentRoom != null)
		{
			batcher.Text(Assets.Font, Game.CurrentRoom.Text, Vector2.Zero, Vector2.One * 0.50f, Color.White);
		}
	}
}