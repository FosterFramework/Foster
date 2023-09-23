
using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public static class Assets
{
	public static SpriteFont? Font { get; private set; }
	public static Texture? Atlas { get; private set; }
	public static readonly Dictionary<string, Sprite> Sprites = new();
	public static readonly Dictionary<string, Tileset> Tilesets = new();
	public static readonly Dictionary<string, Subtexture> Subtextures = new();
	public static readonly Dictionary<Point2, Room> Rooms = new();

	private const string assetsFolderName = "Assets";
	private static string? path = null;

	public static string AssetsPath
	{
		get
		{
			// during development we search up from the build directory to find the Assets folder
			// (instead of copying all the Assets to the build directory).
			if (path == null)
			{
				var up = "";
				while (!Directory.Exists(Path.Join(up, assetsFolderName)) && up.Length < 10)
					up = Path.Join(up, "..");
				path = Path.Join(up, assetsFolderName);
			}

			return path ?? throw new Exception("Unable to find Assets path");
		}
	}

	public static void Load()
	{
		var spritesPath = Path.Join(AssetsPath, "Sprites");
		var tilesetsPath = Path.Join(AssetsPath, "Tilesets");
		var spriteFiles = new Dictionary<string, Aseprite>();
		var tilesetFiles = new Dictionary<string, Aseprite>();

		// load main font file
		Font = new SpriteFont(Path.Join(AssetsPath, "Fonts", "dogica.ttf"), 8);
		Font.LineGap = 4;

		// get sprite files
		foreach (var file in Directory.EnumerateFiles(spritesPath, "*.ase", SearchOption.AllDirectories))
		{
			var name = Path.ChangeExtension(Path.GetRelativePath(spritesPath, file), null);
			var ase = new Aseprite(file);
			if (ase.Frames.Length > 0)
				spriteFiles.Add(name, ase);
		}

		// get tileset files
		foreach (var file in Directory.EnumerateFiles(tilesetsPath, "*.ase", SearchOption.AllDirectories))
		{
			var name = Path.ChangeExtension(Path.GetRelativePath(tilesetsPath, file), null);
			var ase = new Aseprite(file);
			if (ase.Frames.Length > 0)
				tilesetFiles.Add(name, ase);
		}

		// pack all the sprites & tilesets
		Packer.Output output;
		{
			var packer = new Packer();

			foreach (var (name, ase) in spriteFiles)
			{
				var frames = ase.RenderAllFrames();
				for (int i = 0; i < frames.Length; i ++)
					packer.Add($"{name}/{i}", frames[i]);
			}

			foreach (var (name, ase) in tilesetFiles)
			{
				var image = ase.RenderFrame(0);
				var columns = image.Width / Game.TileSize;
				var rows = image.Height / Game.TileSize;

				for (int x = 0; x < columns; x ++)
					for (int y = 0; y < rows; y ++)
						packer.Add($"tilesets/{name}{x}x{y}", image, new RectInt(x, y, 1, 1) * Game.TileSize);
			}

			output = packer.Pack();
		}

		// create texture file
		Atlas = new Texture(output.Pages[0]);

		// create subtextures
		foreach (var it in output.Entries)
			Subtextures.Add(it.Name, new Subtexture(Atlas, it.Source, it.Frame));

		// create sprite assets
		foreach (var (name, ase) in spriteFiles)
		{
			// find origin
			Vector2 origin = Vector2.Zero;
			if (ase.Slices.Count > 0 && ase.Slices[0].Keys.Length > 0 && ase.Slices[0].Keys[0].Pivot.HasValue)
				origin = ase.Slices[0].Keys[0].Pivot!.Value;

			var sprite = new Sprite(name, origin);

			// add frames
			for (int i = 0; i < ase.Frames.Length; i ++)
				sprite.Frames.Add(new(GetSubtexture($"{name}/{i}"), ase.Frames[i].Duration / 1000.0f));

			// add animations
			foreach (var tag in ase.Tags)
			{
				if (!string.IsNullOrEmpty(tag.Name))
					sprite.AddAnimation(tag.Name, tag.From, tag.To - tag.From + 1);
			}

			Sprites.Add(name, sprite);
		}

		// create tileset assets
		foreach (var (name, ase) in tilesetFiles)
		{
			var columns = ase.Width / Game.TileSize;
			var rows = ase.Height / Game.TileSize;
			var tileset = new Tileset(name, columns, rows);

			for (int x = 0; x < columns; x ++)
				for (int y = 0; y < rows; y ++)
					tileset.Tiles[x + y * columns] = GetSubtexture($"tilesets/{name}{x}x{y}");

			Tilesets.Add(name, tileset);
		}

		// load rooms
		foreach (var file in Directory.EnumerateFiles(Path.Join(AssetsPath, "Rooms"), "*.txt"))
		{
			var name = Path.GetFileNameWithoutExtension(file).Split('x');
			if (name.Length <= 1)
				continue;

			if (!int.TryParse(name[0], out var x) || !int.TryParse(name[1], out var y))
				continue;

			var p = new Point2(x, y);
			Rooms.Add(p, new Room(p, file));
		}
	}

	public static void Unload()
	{
		Atlas?.Dispose();
		Atlas = null;
		Font = null;

		Sprites.Clear();
		Tilesets.Clear();
		Subtextures.Clear();
		Rooms.Clear();
	}

	public static Sprite? GetSprite(string name)
	{
		if (Sprites.TryGetValue(name, out var value))
			return value;
		return null;
	}

	public static Tileset? GetTileset(string name)
	{
		if (Tilesets.TryGetValue(name, out var value))
			return value;
		return null;
	}

	public static Room? GetRoom(Point2 cell)
	{
		if (Rooms.TryGetValue(cell, out var value))
			return value;
		return null;
	}

	public static Subtexture GetSubtexture(string name)
	{
		if (Subtextures.TryGetValue(name, out var value))
			return value;
		return new();
	}

}