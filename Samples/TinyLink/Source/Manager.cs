
using System.Numerics;
using Foster.Framework;

namespace TinyLink;

class Program
{
	public static void Main()
	{
		App.Register<Manager>();
		App.Run("TinyLink", 1280, 720);
	}
}

/// <summary>
/// Runs the Game and, when the game is not running, displays a tiny tile-based Level Editor
/// </summary>
public class Manager : Module
{
	private const float Scale = 3.0f;
	private const float ButtonSize = 24;
	private const float ButtonSpacing = 2;
	private const float BoxPadding = 4;
	private const float BoxSpacing = 12;
	private const int PaletteColumns = 4;
	private const int ToolbarSpacing = 4;
	private const float ScreenPadding = 4;
	private const float ToolbarHeight = ButtonSize + 4;
	private const float EditorInset = 24;
	private const float PaletteWidth = BoxPadding * 2 + PaletteColumns * ButtonSize + (PaletteColumns - 1) * ButtonSpacing;
	private readonly Color highlight = 0x5fcde4;

	/// <summary>
	/// The current Room Cell we're viewing
	/// </summary>
	private Point2 cell = new(0, 0);

	/// <summary>
	/// Our game, if it's running
	/// </summary>
	private Game? game;

	/// <summary>
	/// Eases in/out as the Game starts/stops plaing
	/// </summary>
	private float gameEase = 1.0f;

	/// <summary>
	/// Sprite Batcher for general Editor visuals
	/// </summary>
	private readonly Batcher batcher = new();

	/// <summary>
	/// Visible Space
	/// </summary>
	private Rect View 
		=> new Rect(0, 0, App.WidthInPixels / Scale, App.HeightInPixels / Scale).Inflate(-ScreenPadding);

	/// <summary>
	/// Toolbar Space
	/// </summary>
	private Rect ToolbarRect
		=> new (View.X, View.Y, View.Width, ToolbarHeight);

	/// <summary>
	/// Workspace, which is the full space below the Toolbar
	/// </summary>
	private Rect WorkRect
		=> new (View.X, View.Y + ToolbarSpacing + ToolbarHeight, View.Width, View.Height - ToolbarHeight - ToolbarSpacing);

	/// <summary>
	/// Palette View when the Editor is Open
	/// </summary>
	private Rect PaletteOpenRect
		=> new (WorkRect.X, WorkRect.Y, PaletteWidth, WorkRect.Height);

	/// <summary>
	/// Edit Workspace when the Editor is Open
	/// </summary>
	private Rect EditorOpenRect
		=> new Rect(WorkRect.X + PaletteWidth + BoxSpacing, WorkRect.Y, WorkRect.Width - PaletteWidth - BoxSpacing, WorkRect.Height).Inflate(-EditorInset);

	/// <summary>
	/// Play Workspace when the Game is Open
	/// </summary>
	private Rect GameOpenRect => WorkRect;

	/// <summary>
	/// Eases between the EditView and the PlayView as the Game opens/closes
	/// </summary>
	private Rect WorkspaceRect
	{
		get
		{
			var a = Vector2.Lerp(EditorOpenRect.TopLeft, GameOpenRect.TopLeft, Ease.CubeInOut(gameEase));
			var b = Vector2.Lerp(EditorOpenRect.BottomRight, GameOpenRect.BottomRight, Ease.CubeInOut(gameEase));
			return new Rect(a, b);
		}
	}

	/// <summary>
	/// Eases the PaletteView out/in as the Game opens/closes
	/// </summary>
	/// <value></value>
	private Rect PaletteSlideRect => 
		PaletteOpenRect - Vector2.UnitX * Ease.CubeInOut(gameEase) * PaletteOpenRect.Right;

	/// <summary>
	/// Mouse Cursor relative to Scale
	/// </summary>
	private Vector2 Cursor => Input.Mouse.Position / Scale;

	/// <summary>
	/// Line Weight for outlines, relative to Scale
	/// </summary>
	private float LineWeight => (1.0f / Scale) * 4;

	/// <summary>
	/// Tries to get the Existing Room
	/// </summary>
	private Room? CurrentRoom => Assets.GetRoom(cell);

	/// <summary>
	/// The current Type we're painting in the Editor
	/// </summary>
	private char brush = '0';

	/// <summary>
	/// If it's a big Brush
	/// </summary>
	private bool brushBig = false;

	private Point2 tilePlaceFrom;
	private char tileTypePlacing;
	private bool tilePlacing;

	public override void Startup()
	{
		Assets.Load();
		Controls.Init();
		Factory.RegisterTypes();

		game = new Game(cell);
	}

	public override void Update()
	{
		// Misc. Hotkeys
		if (Input.Keyboard.Pressed(Keys.F1) && game == null)
			Reload();
		if (Input.Keyboard.Pressed(Keys.F4))
			App.Fullscreen = !App.Fullscreen;
		if (Input.Keyboard.Pressed(Keys.Escape))
		{
			CurrentRoom?.Save();
			App.Exit();
		}

		// Run Game
		if (game != null)
		{
			gameEase = Calc.Approach(gameEase, 1.0f, Time.Delta * 4.0f);
			game.Update();
		}
		// Show Editor
		else
		{
			gameEase = Calc.Approach(gameEase, 0.0f, Time.Delta * 4.0f);
		}

		// Build our Sprite Batch
		{
			batcher.Clear();
			batcher.PushMatrix(Matrix3x2.CreateScale(Scale));

			// draw editor/game box
			Box(WorkspaceRect);

			// draw editor stuff if it's open
			if (gameEase < 1.0f)
			{
				Palette();
				Editor();
			}

			Toolbar();

			// draw to screen
			batcher.PopMatrix();
		}
	}

	public override void Render()
	{
		// draw the main UI first
		Graphics.Clear(0x2e1426);
		batcher.Render();

		// draw game on top if it exists
		game?.Render((RectInt)(WorkspaceRect.Inflate(-BoxPadding) * Scale));
	}

	private void Reload()
	{
		Assets.Unload();
		Factory.Clear();

		Assets.Load();
		Factory.RegisterTypes();
	}

	/// <summary>
	/// Shifts to display a new room
	/// </summary>
	private void ShiftRoom(Point2 direction)
	{
		// save last room
		CurrentRoom?.Save();

		// set next room
		cell += direction;
	}

	/// <summary>
	/// Draws a box outline (for palette / workspace)
	/// </summary>
	private void Box(Rect box)
	{
		batcher.Rect(box, 0x000000);
		batcher.RectLine(box, LineWeight, 0xffffff);
	}

	/// <summary>
	/// Draws a Pressable button with an icon and tooltip
	/// </summary>
	private bool Button(Rect button, Subtexture subtexture, string text, bool selected = false)
	{
		var hovering = button.Contains(Cursor);

		batcher.Rect(button, Color.Black);
		batcher.ImageFit(subtexture, button.Inflate(-LineWeight), Vector2.One * 0.50f, Color.White, false, false);
		batcher.RectRoundedLine(button, 4, LineWeight, hovering ? Color.White : (selected ? highlight : Color.Gray));

		if (hovering && Assets.Font != null)
		{
			var at = new Rect(
				Cursor.X + 32, 
				Cursor.Y - Assets.Font.LineHeight / 2, 
				Assets.Font.WidthOf(text), 
				Assets.Font.LineHeight).Inflate(12, 6, 12, 6);

			at.X = Calc.Clamp(at.X, View.Left + 8, View.Right - at.Width - 8);
			at.Y = Calc.Clamp(at.Y, View.Top + 8, View.Bottom - at.Height - 8);

			if (at.Contains(Cursor))
				at.X = Cursor.X - at.Width - 8;

			batcher.PushLayer(-10);
			batcher.RectRounded(at, 4, Color.DarkGray);
			batcher.RectRoundedLine(at, 4, LineWeight, Color.Gray);
			batcher.Text(Assets.Font, text, at.Center, Vector2.One * 0.50f, Color.White);
			batcher.PopLayer();
		}

		return hovering && Input.Mouse.LeftPressed;
	}

	/// <summary>
	/// Displays the Toolbar
	/// </summary>
	private void Toolbar()
	{
		var rect = new Rect(ToolbarRect.X, ToolbarRect.Y, ButtonSize, ButtonSize);

		if (game == null)
		{
			if (Button(rect, Assets.GetSubtexture("buttons/0"), "Play [Space]") || 
				Input.Keyboard.Pressed(Keys.Space))
			{
				CurrentRoom?.Save();
				if (CurrentRoom != null)
					game = new Game(cell);
			}
			rect.X += rect.Width + ButtonSpacing;

			if (Button(rect, Assets.GetSubtexture("buttons/7"), "Small Brush", !brushBig))
				brushBig = false;
			rect.X += rect.Width + ButtonSpacing;
			if (Button(rect, Assets.GetSubtexture("buttons/8"), "Big Brush", brushBig))
				brushBig = true;
			rect.X += rect.Width + ButtonSpacing;
		}
		else
		{
			if (Button(rect, Assets.GetSubtexture("buttons/1"), "Stop [Space]") || 
				Input.Keyboard.Pressed(Keys.Space))
			{
				cell = game.Cell;
				game = null;
			}
		}

  
		if (Assets.Font != null)
		{
			var cell = (game?.Cell ?? this.cell);
			batcher.Text(Assets.Font, $"Room {cell.X} x {cell.Y}", ToolbarRect.Center, new Vector2(0.50f, 0.50f), Color.White);
		}
	}

	/// <summary>
	/// Displays the Palette type buttons
	/// </summary>
	private void Palette()
	{
		Box(PaletteSlideRect);

		var bounds = PaletteSlideRect.Inflate(-BoxPadding);
		var at = bounds.TopLeft;
		var index = 0;

		foreach (var (id, it) in Factory.Entries)
		{
			var btn = new Rect(at.X, at.Y, ButtonSize, ButtonSize);
			if (Button(btn, it.Image, it.Name, brush == id))
				brush = id;

			at.X += ButtonSize + ButtonSpacing;
			if (index > 0 && (index + 1) % PaletteColumns == 0)
			{
				at.X = bounds.Left;
				at.Y += ButtonSize + ButtonSpacing;
			}
			index++;
		}
	}

	/// <summary>
	/// Displays the main Editor workspace
	/// </summary>
	private void Editor()
	{
		var bounds = WorkspaceRect;
		var inner = bounds.Inflate(-BoxPadding * 2);
		var arrow = new Rect().Inflate(ButtonSize / 2);
		var lastCell = cell;

		// buttons to move between rooms
		if (game == null)
		{
			var upImg = Assets.GetSubtexture("buttons/6");
			var leftImg = Assets.GetSubtexture("buttons/4");
			var downImg = Assets.GetSubtexture("buttons/3");
			var rightImg = Assets.GetSubtexture("buttons/5");

			var upExits = Assets.GetRoom(cell + Point2.Up) != null;
			var leftExits = Assets.GetRoom(cell + Point2.Left) != null;
			var downExits = Assets.GetRoom(cell + Point2.Down) != null;
			var rightExits = Assets.GetRoom(cell + Point2.Right) != null;

			batcher.PushLayer(-1);
			if (Button(arrow + bounds.TopCenter, upImg, "Move Up Room [W]", upExits) || Input.Keyboard.Pressed(Keys.W))
				ShiftRoom(Point2.Up);
			if (Button(arrow + bounds.CenterLeft, leftImg, "Move Left Room [A]", leftExits) || Input.Keyboard.Pressed(Keys.A))
				ShiftRoom(Point2.Left);
			if (Button(arrow + bounds.BottomCenter, rightImg, "Move Down Room [S]", downExits) || Input.Keyboard.Pressed(Keys.S))
				ShiftRoom(Point2.Down);
			if (Button(arrow + bounds.CenterRight, downImg, "Move Right Room [D]", rightExits) || Input.Keyboard.Pressed(Keys.D))
				ShiftRoom(Point2.Right);
			batcher.PopLayer();
		}

		if (CurrentRoom is Room editing)
		{
			var width = Game.Width;
			var height = Game.Height;
			var scale = Math.Min(inner.Width / width, inner.Height / height);
			var gridColor = Color.White * 0.10f;
			var gridWeight = 1.0f / scale;

			var matrix = 
				Matrix3x2.CreateTranslation(-new Vector2(width, height) / 2) * 
				Matrix3x2.CreateScale(scale) * 
				Matrix3x2.CreateTranslation(inner.Center);

			var localMouse = Vector2.Zero;
			var tileOver = Point2.Zero;
			if (Matrix3x2.Invert(matrix, out var inverse))
			{
				localMouse = Vector2.Transform(Cursor, inverse);
				tileOver = (Point2)(localMouse / Game.TileSize);
			}
			var overGame = new Rect(0, 0, Game.Width, Game.Height).Contains(localMouse);
			
			batcher.PushMatrix(matrix);
			batcher.Rect(new Rect(0, 0, Game.Width, Game.Height), Color.Black);
			batcher.PushScissor((RectInt)(inner * Scale));

			// draw "tiles" first
			for (int x = 0; x < Game.Columns; x ++)
			for (int y = 0; y < Game.Rows; y ++)
			{
				if (Factory.Find(editing.Tiles[x, y]) is not {} it || it.Image.Texture == null)
					continue;

				if (it.Tile == null)
					continue;

				var position = new Vector2(x, y) * Game.TileSize + it.Offset;
				batcher.Image(it.Image, position, it.Origin, Vector2.One, 0, Color.White);
			}

			// draw "actors" second
			for (int x = 0; x < Game.Columns; x ++)
			for (int y = 0; y < Game.Rows; y ++)
			{
				if (Factory.Find(editing.Tiles[x, y]) is not {} it || it.Image.Texture == null)
					continue;

				if (it.Spawn == null)
					continue;

				var position = new Vector2(x, y) * Game.TileSize + it.Offset;
				batcher.Image(it.Image, position, it.Origin, Vector2.One, 0, Color.White);
			}

			// draw grid
			for (int x = 1; x < Game.Columns; x ++)
				batcher.Line(new Vector2(x* Game.TileSize, 0),  new Vector2(x* Game.TileSize, Game.Height), gridWeight, gridColor);
			for (int y = 1; y < Game.Rows; y ++)
				batcher.Line(new Vector2(0, y * Game.TileSize),  new Vector2(Game.Width, y * Game.TileSize), gridWeight, gridColor);

			// begin placing tiles
			if (lastCell == cell && overGame && (Input.Mouse.LeftPressed || Input.Mouse.RightPressed))
			{
				tilePlaceFrom = tileOver;
				tileTypePlacing = Input.Mouse.LeftPressed ? brush : '0';
				tilePlacing = true;
			}

			// return tile placing to selected brush
			if (!Input.Mouse.RightDown)
				tileTypePlacing = brush;
		
			// change shift/size based opn big brush
			var shift = brushBig ? -1 : 0;
			var size = brushBig ? 3 : 1;

			// draw cursor
			if (overGame)
			{
				var cursorRect = (new Rect(tileOver.X + shift, tileOver.Y + shift, size, size) * Game.TileSize).Inflate(gridWeight * 2);
				var cursorColor = tileTypePlacing == '0' ? Color.Red : highlight;
				batcher.RectLine(cursorRect, gridWeight * 2, cursorColor);

				if (Factory.Find(brush) is {} placing)
				{
					var position = tileOver * Game.TileSize + placing.Offset;

					for (int x = shift; x < shift + size; x ++)
						for (int y = shift; y < shift + size; y ++)
							batcher.Image(placing.Image, position + new Point2(x, y) * Game.TileSize, placing.Origin, Vector2.One, 0, Color.White);
				}
			}

			// place tiles
			if (tilePlacing && (Input.Mouse.LeftDown || Input.Mouse.RightDown))
			{
				editing.Set(tilePlaceFrom, tileTypePlacing);

				while (tilePlaceFrom != tileOver)
				{
					var diff = tileOver - tilePlaceFrom;
					if (Math.Abs(diff.X) > Math.Abs(diff.Y))
						diff.Y = 0;
					else
						diff.X = 0;
					tilePlaceFrom.X += Math.Sign(diff.X);
					tilePlaceFrom.Y += Math.Sign(diff.Y);

					for (int x = shift; x < shift + size; x ++)
						for (int y = shift; y < shift + size; y ++)
							editing.Set(tilePlaceFrom + new Point2(x, y), tileTypePlacing);
				}
			}

			batcher.PopScissor();
			batcher.RectLine(new Rect(0, 0, Game.Width, Game.Height), LineWeight, Color.Black);
			batcher.PopMatrix();
		}
		else
		{
			if (Button(arrow + bounds.Center, Assets.GetSubtexture("buttons/2"), "Create New Room Here"))
			{
				var it = new Room(cell);
				Assets.Rooms.Add(cell, it);
				it.Save();
			}
		}

		if (!Input.Mouse.LeftDown && !Input.Mouse.RightDown)
		{
			tileTypePlacing = brush;
			tilePlacing = false;
		}
	}
}
