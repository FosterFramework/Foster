using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Grid : Actor
{
	private readonly bool[,] grid = new bool[Game.Columns, Game.Rows];
	private readonly Subtexture[,] tilemap = new Subtexture[Game.Columns, Game.Rows];

	public Grid()
	{
		Hitbox = new(grid);
	}

	public void Set(int x, int y, Subtexture subtexture)
	{
		tilemap[x, y] = subtexture;
		grid[x, y] = true;
	}

	public override void Render(Batcher batcher)
	{
		for (int x = 0; x < Game.Columns; x ++)
		for (int y = 0; y < Game.Rows; y ++)
		{
			if (grid[x, y])
				batcher.Image(tilemap[x, y], new Vector2(x, y) * Game.TileSize, Color.White);
		}
	}
}