
using Foster.Framework;
using System.Numerics;

class Program
{
	public static void Main()
	{
		App.Register<Game>();
		App.Run("Hello World", 1280, 720);
	}
}

class Game : Module
{
	private const float Acceleration = 1200;
	private const float Friction = 500;
	private const float MaxSpeed = 800;

	private readonly Batcher batch = new();
	private readonly Texture texture = new Texture(new Image(128, 128, Color.Blue));
	private Vector2 pos = new();
	private Vector2 speed = new();

	public override void Update()
	{
		App.Title = $"Something Else {App.Width}x{App.Height} : {App.WidthInPixels}x{App.HeightInPixels}";

		if (Input.Keyboard.Down(Keys.Left))
			speed.X -= Acceleration * Time.Delta;
		if (Input.Keyboard.Down(Keys.Right))
			speed.X += Acceleration * Time.Delta;
		if (Input.Keyboard.Down(Keys.Up))
			speed.Y -= Acceleration * Time.Delta;
		if (Input.Keyboard.Down(Keys.Down))
			speed.Y += Acceleration * Time.Delta;

		if (!Input.Keyboard.Down(Keys.Left, Keys.Right))
			speed.X = Calc.Approach(speed.X, 0, Time.Delta * Friction);
		if (!Input.Keyboard.Down(Keys.Up, Keys.Down))
			speed.Y = Calc.Approach(speed.Y, 0, Time.Delta * Friction);

		if (Input.Keyboard.Pressed(Keys.F4))
			App.Fullscreen = !App.Fullscreen;

		if (speed.Length() > MaxSpeed)
			speed = speed.Normalized() * MaxSpeed;

		pos += speed * Time.Delta;
	}

	public override void Render()
	{
		Graphics.Clear(0x44aa77);

		batch.PushMatrix(
			new Vector2(App.WidthInPixels, App.HeightInPixels) / 2,
			Vector2.One,
			new Vector2(texture.Width, texture.Height) / 2,
			(float)Time.Duration.TotalSeconds * 4.0f);
		batch.Image(texture, Vector2.Zero, Color.White);
		batch.PopMatrix();

		batch.Circle(new Circle(pos, 64), 16, Color.Red);
		batch.Circle(new Circle(Input.Mouse.Position, 8), 16, Color.White);

		batch.Render();
		batch.Clear();
	}
}