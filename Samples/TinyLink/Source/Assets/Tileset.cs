
using Foster.Framework;

namespace TinyLink;

public class Tileset
{
	public readonly string Name;
	public readonly int Columns;
	public readonly int Rows;
	public readonly List<Subtexture> Tiles = new();

	public Tileset(string name, int columns, int rows)
	{
		Name = name;
		Columns = columns;
		Rows = rows;
		for (int i = 0; i < columns * rows; i ++)
			Tiles.Add(new());
	}

	public Subtexture GetRandomTile(ref Rng rng)
		=> Tiles[rng.Int(Tiles.Count)];
}