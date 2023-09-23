using System.Numerics;
using Foster.Framework;

namespace TinyLink;

/// <summary>
/// The Factory holds meta information on how to populate the Game when loading a Room.
/// It describes Actor Spawns and Tile types in a single struct called "Entry".
/// </summary>
public static class Factory
{
	/// <summary>
	/// Used to Spawn an Actor
	/// </summary>
	public delegate void SpawnFn(Point2 position, Game game);

	/// <summary>
	/// Used to Place a Tile
	/// </summary>
	public delegate void TileFn(Point2 tile, Grid fg, Grid bg);

	public readonly struct Entry
	{
		public readonly string Name;
		public readonly Subtexture Image;
		public readonly Vector2 Origin;
		public readonly Point2 Offset;
		public readonly SpawnFn? Spawn;
		public readonly TileFn? Tile;

		public Entry(string name, Subtexture image, Vector2 origin, Point2 offset, SpawnFn spawn)
		{
			Name = name;
			Image = image;
			Origin = origin;
			Offset = offset;
			Spawn = spawn;
		}

		public Entry(string name, Subtexture image, TileFn tile)
		{
			Name = name;
			Image = image;
			Tile = tile;
		}

		public static Entry AsActor<T>(string spriteName, Point2? offset = null, bool exclusive = false, Action<T>? additional = null) where T : Actor, new()
		{
			var sprite = Assets.GetSprite(spriteName);
			if (sprite == null)
				return new();

			return new Entry(typeof(T).Name, sprite.Frames[0].Subtexture, sprite.Origin, offset ?? Point2.Zero, (position, game) =>
			{
				if (!exclusive || game.GetFirst<T>() == null) 
				{
					var it = game.Create<T>(position);
					additional?.Invoke(it);
				}
			});
		}

		public static Entry AsTile(string tilesetName, bool isFg)
		{
			var tileset = Assets.GetTileset(tilesetName);
			if (tileset == null)
				return new();

			return new Entry(tileset.Name, tileset.Tiles[0], (Point2 tile, Grid fg, Grid bg) =>
			{
				Rng rng = new(tile.X + tile.Y * Game.Columns);
				(isFg ? fg : bg).Set(tile.X, tile.Y, tileset.GetRandomTile(ref rng));
			});
		}
	}

	/// <summary>
	/// List of all the Types
	/// </summary>
	public static readonly Dictionary<char, Entry> Entries = new();

	/// <summary>
	/// Registers a new Type
	/// </summary>
	public static void Register(char id, in Entry entry)
	{
		Entries[id] = entry;
	}

	/// <summary>
	/// Finds a given Type entry by its character ID
	/// </summary>
	public static Entry? Find(char id)
	{
		if (Entries.TryGetValue(id, out var entry))
			return entry;

		return null;
	}

	/// <summary>
	/// Registers the Default types built into the game.
	/// </summary>
	public static void RegisterTypes()
	{
		// Empty Type, purely for the Editord
		Register('0', new Entry("Empty", new Subtexture(), null!));

		// Add Tile Types
		Register('1', Entry.AsTile("castle", true));
		Register('G', Entry.AsTile("grass", true));
		Register('g', Entry.AsTile("plants", false));
		Register('w', Entry.AsTile("water", false));
		Register('#', Entry.AsTile("back", false));

		// Add Actor Types
		Register('-', Entry.AsActor<Jumpthru>("jumpthru"));
		Register('P', Entry.AsActor<Player>("player", new Point2(4, 8), true));
		Register('B', Entry.AsActor<Bramble>("bramble", new Point2(4, 8)));
		Register('S', Entry.AsActor<Spitter>("spitter", new Point2(4, 8)));
		Register('M', Entry.AsActor<Mosquito>("mosquito", new Point2(4, 4)));
		Register('D', Entry.AsActor<Door>("door", new Point2(4, 8), false, (it) => it.Appear()));
		Register('C', Entry.AsActor<Door>("door", new Point2(4, 8)));
		Register('b', Entry.AsActor<Blob>("blob", new Point2(4, 8)));
		Register('F', Entry.AsActor<GhostFrog>("ghostfrog", new Point2(4, 8)));
		Register('T', Entry.AsActor<TitleText>("heart", new Point2(4, 4)));
	}

	public static void Clear()
	{
		Entries.Clear();
	}

}