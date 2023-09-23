using System.Numerics;
using Foster.Framework;

namespace TinyLink;

public class Game
{
	public const int Width = 240;
	public const int Height = 135;
	public const int Columns = Width / TileSize;
	public const int Rows = Height / TileSize + 1;
	public const int TileSize = 8;

	public readonly Batcher Batcher = new();
	public readonly Target Screen = new(Width, Height);
	public readonly List<Actor> Actors = new();

	private Room? room;
	private Room? nextRoom;
	private float nextRoomEase;
	private readonly List<Actor> destroying = new();
	private readonly List<Actor> rendering = new();
	private float hitstun = 0;
	private float shaking = 0;
	private Point2 shake;
	private Rng rng = new();

	public Vector2 Camera { get; private set; }
	public RectInt Bounds => room?.WorldBounds ?? new();
	public Point2 Cell => room?.Cell ?? Point2.Zero;
	public Room? CurrentRoom => room;

	public Game(Point2 start)
	{
		if (Assets.Rooms.TryGetValue(start, out room))
		{
			Camera = new Vector2(room.Cell.X * Width, room.Cell.Y * Height);
			LoadRoom(room);
		}
	}

	public void Update()
	{
		// Don't run normal gameplay loop if no room is loaded
		if (room == null)
			return;

		// Run Game normally when not moving to a new room
		if (nextRoom == null)
		{
			// Reload Room Debug
			if (Input.Keyboard.Pressed(Keys.R))
				ReloadRoom();

			// only run normal updates if no hitstun
			if (hitstun <= 0)
			{
				// Remove Destroyed Actors
				for (int i = 0; i < destroying.Count; i ++)
				{
					destroying[i].Destroyed();
					Actors.Remove(destroying[i]);
				}
				destroying.Clear();

				// Update Actors
				for (int i = 0; i < Actors.Count; i ++)
					Actors[i].Update();

				// screen shaking
				if (shaking > 0)
				{
					shaking -= Time.Delta;
					if (Time.OnInterval(0.05f))
						shake = new(rng.Sign(), rng.Sign());
				}
				else
					shake = Point2.Zero;
			}
			else
				hitstun -= Time.Delta;
		}
		// Lerp the Camera to the new Room
		else if (nextRoomEase < 1.0f)
		{
			Camera = Vector2.Lerp(room.WorldBounds.TopLeft, nextRoom.WorldBounds.TopLeft, Ease.CubeInOut(nextRoomEase));
			nextRoomEase = Calc.Approach(nextRoomEase, 1.0f, Time.Delta * 4.0f);
		}
		// Finished Lerping the Camera to the new room, return to normal update
		else
		{
			room = nextRoom;
			nextRoom = null;
			Hitstun(0.1f);
		}
	}

	public void Render(in RectInt viewport)
	{
		// draw gameplay to screen
		{
			Screen.Clear(0x150e22);
			Batcher.PushMatrix(-(Point2)Camera + shake);

			// draw actors
			rendering.AddRange(Actors);
			rendering.Sort((a, b) => b.Depth - a.Depth);
			foreach (var actor in rendering)
			{
				if (!actor.Visible)
					continue;

				Batcher.PushMatrix(actor.Position);
				actor.Render(Batcher);
				Batcher.PopMatrix();
			}
			rendering.Clear();
			Batcher.PopMatrix();

			// draw player HP
			if (GetFirst<Player>() is Player player)
			{
				Point2 pos = new(0, Height - 16);
				Batcher.Rect(new Rect(pos.X, pos.Y + 7, 48, 4), Color.Black);

				for (int i = 0; i < Player.MaxHealth; i++)
				{
					if (player.Health >= i + 1)
						Batcher.Image(Assets.GetSubtexture("heart/0"), pos, Color.White);
					else
						Batcher.Image(Assets.GetSubtexture("heart/1"), pos, Color.White);
					pos.X += 12;
				}
			}

			Batcher.Render(Screen);
			Batcher.Clear();
		}

		// draw screen to window
		{
			var size = viewport.Size;
			var center = viewport.Center;
			var scale = Calc.Min(size.X / (float)Screen.Width, size.Y / (float)Screen.Height);

			Batcher.SetSampler(new(TextureFilter.Nearest, TextureWrap.ClampToEdge, TextureWrap.ClampToEdge));
			Batcher.Image(Screen, center, Screen.Bounds.Size / 2, Vector2.One * scale, 0, Color.White);
			Batcher.Render();
			Batcher.Clear();
		}
	}

	public T Create<T>(Point2? position = null) where T : Actor, new()
	{
		var instance = new T
		{
			Game = this,
			Position = position ?? Point2.Zero
		};
		instance.Added();
		Actors.Add(instance);
		return instance;
	}

	public T? GetFirst<T>() where T : Actor
	{
		foreach (var it in Actors)
			if (it is T instance && !destroying.Contains(it))
				return instance;
		return null;
	}

	public Actor? GetFirst(Actor.Masks mask)
	{
		foreach (var it in Actors)
			if (it.Mask.Has(mask) && !destroying.Contains(it))
				return it;
		return null;
	}

	public void Destroy(Actor actor)
	{
		if (!destroying.Contains(actor))
			destroying.Add(actor);
	}

	public bool Transition(Point2 direction)
	{
		if (room != null && nextRoom == null)
		{
			var nextCell = room.Cell + direction;
			if (Assets.Rooms.TryGetValue(nextCell, out nextRoom))
			{
				foreach (var it in Actors)
					if (it is not Player)
						Destroy(it);

				nextRoomEase = 0.0f;
				LoadRoom(nextRoom);
				return true;
			}
		}

		return false;
	}

	public bool OverlapsAll(in RectInt rect, Actor.Masks mask, List<Actor> results)
	{
		foreach (var actor in Actors)
		{
			var local = rect - actor.Position;
			if (actor.Mask.Has(mask) && actor.Hitbox.Overlaps(local))
				results.Add(actor);
		}

		return results.Count > 0;
	}

	public Actor? OverlapsFirst(in RectInt rect, Actor.Masks mask)
	{
		foreach (var actor in Actors)
		{
			var local = rect - actor.Position;
			if (actor.Mask.Has(mask) && actor.Hitbox.Overlaps(local))
				return actor;
		}

		return null;
	}

	public void ReloadRoom()
	{
		if (room != null)
		{
			foreach (var actor in Actors)
				Destroy(actor);
			LoadRoom(room);
		}
	}

	public void LoadRoom(Room room)
	{
		var offset = room.WorldBounds.TopLeft;

		var bg = Create<Grid>(offset);
		bg.Depth = 10;

		var fg = Create<Grid>(offset);
		fg.Mask = Actor.Masks.Solid;
		fg.Depth = 5;

		// loop over room grid placing objects
		for (int x = 0; x < Columns; x ++)
		for (int y = 0; y < Rows; y ++)
		{
			var tile = new Point2(x, y);
			var at = offset + tile * TileSize;

			if (Factory.Find(room.Tiles[x, y]) is Factory.Entry entry)
			{
				entry.Spawn?.Invoke(at + entry.Offset, this);
				entry.Tile?.Invoke(tile, fg, bg);
			}
		}
	}

	public void Hitstun(float time) => hitstun = MathF.Max(hitstun, time);

	public void Shake(float time) => shaking = MathF.Max(shaking, time);
}