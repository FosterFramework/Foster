using System.Numerics;
using Foster.Framework;

namespace TinyLink;

/// <summary>
/// Hitbox can test overlaps between Rectangles and Grids
/// </summary>
public readonly struct Hitbox
{
	public enum Shapes
	{
		Rect,
		Grid
	}

	public readonly Shapes Shape;
	private readonly RectInt rect;
	private readonly bool[,]? grid;

	public Hitbox()
	{
		Shape = Shapes.Rect;
		rect = new RectInt(0, 0, 0, 0);
		grid = null;
	}

	public Hitbox(in RectInt value)
	{
		Shape = Shapes.Rect;
		rect = value;
		grid = null;
	}

	public Hitbox(in bool[,] value)
	{
		Shape = Shapes.Grid;
		grid = value;
	}

	public bool Overlaps(in RectInt rect)
		=> Overlaps(Point2.Zero, new Hitbox(rect));

	public bool Overlaps(in Hitbox other) 
		=> Overlaps(Point2.Zero, other);

	public bool Overlaps(in Point2 offset, in Hitbox other)
	{
		switch (Shape)
		{
			case Shapes.Rect:
				switch (other.Shape)
				{
					case Shapes.Rect: return RectToRect(rect + offset, other.rect);
					case Shapes.Grid: return RectToGrid(rect + offset, other.grid!);
				}
				break;
			case Shapes.Grid:
				switch (other.Shape)
				{
					case Shapes.Rect: return RectToGrid(other.rect - offset, grid!);
					case Shapes.Grid: throw new NotImplementedException("Grid->Grid overlap not implemented!");
				}
				break;
		}

		throw new NotImplementedException();
	}

	private static bool RectToRect(in RectInt a, in RectInt b)
	{
		return a.Overlaps(b);
	}

	private static bool RectToGrid(in RectInt a, in bool[,] grid)
	{
		int left = Calc.Clamp((int)Math.Floor(a.Left / (float)Game.TileSize), 0, grid.GetLength(0));
		int right = Calc.Clamp((int)Math.Ceiling(a.Right / (float)Game.TileSize), 0, grid.GetLength(0));
		int top = Calc.Clamp((int)Math.Floor(a.Top / (float)Game.TileSize), 0, grid.GetLength(1));
		int bottom = Calc.Clamp((int)Math.Ceiling(a.Bottom / (float)Game.TileSize), 0, grid.GetLength(1));

		for (int x = left; x < right; x ++)
		for (int y = top; y < bottom; y ++)
			if (grid[x, y])
				return true;

		return false;
	}

	public void Render(Batcher batcher, Point2 offset, Color color)
	{
		batcher.PushMatrix(offset);

		if (Shape == Shapes.Rect)
		{
			batcher.RectLine(rect + offset, 1, color);
		}
		else if (Shape == Shapes.Grid && grid != null)
		{
			for (int x = 0; x < grid.GetLength(0); x ++)
			for (int y = 0; y < grid.GetLength(1); y ++)
			{
				if (grid[x, y])
					batcher.RectLine(new RectInt(x, y, 1, 1) * Game.TileSize, 1, color);
			}
		}

		batcher.PopMatrix();
	}

}