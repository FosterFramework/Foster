
using System.Text;
using Foster.Framework;

namespace TinyLink;

public class Room
{
	/// <summary>
	/// World Location of the Room
	/// </summary>
	public readonly Point2 Cell;

	/// <summary>
	/// Grid of Tiles
	/// </summary>
	public readonly char[,] Tiles = new char[Game.Columns, Game.Rows];

	/// <summary>
	/// Optional Text string. There's no way to edit this in-game, but
	/// it is used to display title/ending text.
	/// </summary>
	public string Text = string.Empty;

	/// <summary>
	/// Bounds in World Space in Pixels
	/// </summary>
	public RectInt WorldBounds => new(Cell.X * Game.Width, Cell.Y * Game.Height, Game.Width, Game.Height);

	public Room(Point2 cell)
	{
		Cell = cell;
		Text = string.Empty;
		for (int x = 0; x < Game.Columns; x ++)
		for (int y = 0; y < Game.Rows; y ++)
			Tiles[x, y] = '0';
	}

	public Room(Point2 cell, string path)
	{
		Cell = cell;

		var lines = File.ReadAllLines(path);
		var text = new StringBuilder();
		var y = 0;

		foreach (var line in lines)
		{
			// parse text line
			if (line.StartsWith(":"))
			{
				text.AppendLine(line[1..]);
			}
			// parse row of tiles
			else if (y < Game.Rows)
			{
				for (int x = 0; x < line.Length && x < Game.Columns; x ++)
					Tiles[x, y] = line[x];
				y++;
			}
		}

		Text = text.ToString();
	}

	public void Set(Point2 tile, char ch)
	{
		if (tile.X >= 0 && tile.Y >= 0 && tile.X < Game.Columns && tile.Y < Game.Rows)
			Tiles[tile.X, tile.Y] = ch;
	}

	public void Save()
	{
		StringBuilder result = new();

		// write text lines first
		foreach (var line in Text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
			result.AppendLine($":{line}");

		// write tile grid next
		for (int y = 0; y < Game.Rows; y ++)
		{
			for (int x = 0; x < Game.Columns; x ++)
				result.Append(Tiles[x, y]);
			result.AppendLine();
		}

		// output to file
		File.WriteAllText(
			Path.Join(Assets.AssetsPath, "Rooms", $"{Cell.X}x{Cell.Y}.txt"),
			result.ToString());
	}
}