using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

public static class App
{
	private static readonly List<Module> modules = new();
	private static readonly List<Func<Module>> registrations = new();
	private static readonly Stopwatch timer = new();
	private static bool started = false;
	private static TimeSpan lastTime;
	private static TimeSpan accumulator;
	private static string title = string.Empty;
	private static Platform.FosterFlags flags = 
		Platform.FosterFlags.RESIZABLE | 
		Platform.FosterFlags.VSYNC |
		Platform.FosterFlags.MOUSE_VISIBLE;

	/// <summary>
	/// Foster Version Number
	/// </summary>
	public static readonly Version Version = typeof(App).Assembly.GetName().Version!;

	/// <summary>
	/// If the Application is currently running
	/// </summary>
	public static bool Running { get; private set; } = false;

	/// <summary>
	/// If the Application is exiting. Call <see cref="App.Exit"/> to exit the Application.
	/// </summary>
	public static bool Exiting { get; private set; } = false;

	/// <summary>
	/// Gets the Stopwatch used to evaluate Application time.
	/// Note: Modifying this can break your update loop!
	/// </summary>
	public static Stopwatch Timer => timer;

	/// <summary>
	/// The Window Title
	/// </summary>
	public static string Title
	{
		get => title;
		set => Platform.FosterSetTitle(title = value);
	}

	/// <summary>
	/// The Application Name, assigned on Run
	/// </summary>
	public static string Name { get; private set; } = string.Empty;

	/// <summary>
	/// Gets the path to the User Directory, which is the location where you should
	/// store application data like settings or save data.
	/// The location of this directory is platform and application dependent.
	/// For example on Windows this is usually in C:/User/{user}/AppData/Roaming/{App.Name}, 
	/// where as on Linux it's usually in ~/.local/share/{App.Name}
	/// </summary>
	public static string UserPath { get; private set; } = string.Empty;

	/// <summary>
	/// Returns whether the Application Window is currently Focused or not.
	/// </summary>
	public static bool Focused => Platform.FosterGetFocused() != 0;

	/// <summary>
	/// The Window width, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use WidthInPixels to get the drawable size.
	/// </summary>
	public static int Width
	{
		get
		{
			Platform.FosterGetSize(out int w, out _);
			return w;
		}
		set
		{
			if (Width != value)
			{
				Platform.FosterSetSize(value, Height);
				Platform.FosterSetCentered();
			}
		}
	}

	/// <summary>
	/// The Window height, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use HeightInPixels to get the drawable size.
	/// </summary>
	public static int Height
	{
		get
		{
			Platform.FosterGetSize(out _, out int h);
			return h;
		}
		set
		{
			if (Height != value)
			{
				Platform.FosterSetSize(Width, value);
				Platform.FosterSetCentered();
			}
		}
	}

	/// <summary>
	/// The Window size, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use SizeInPixels to get the drawable size.
	/// </summary>
	public static Point2 Size
	{
		get
		{
			Platform.FosterGetSize(out int w, out int h);
			return new(w, h);
		}
		set
		{
			if (Size != value)
			{
				Platform.FosterSetSize(value.X, value.Y);
				Platform.FosterSetCentered();
			}
		}
	}

	/// <summary>
	/// The Width of the Window in Pixels
	/// </summary>
	public static int WidthInPixels
	{
		get
		{
			Platform.FosterGetSizeInPixels(out int w, out _);
			return w;
		}
	}

	/// <summary>
	/// The Height of the Window in Pixels
	/// </summary>
	public static int HeightInPixels
	{
		get
		{
			Platform.FosterGetSizeInPixels(out _, out int h);
			return h;
		}
	}

	/// <summary>
	/// The Size of the Window in Pixels
	/// </summary>
	public static Point2 SizeInPixels
	{
		get
		{
			Platform.FosterGetSizeInPixels(out int w, out int h);
			return new(w, h);
		}
	}

	/// <summary>
	/// Gets the Content Scale for the Application Window.
	/// In the future this should try to use the Display DPI, however the SDL2
	/// implementation doesn't have very reliable values across platforms.
	/// (See here: https://wiki.libsdl.org/SDL2/SDL_GetDisplayDPI#remarks)
	/// Until this has a better underlying solution we are just using the
	/// pixel size compared to the window size as a rough scaling value.
	/// </summary>
	public static Vector2 ContentScale => new Vector2(
		WidthInPixels / (float)Width,
		HeightInPixels / (float)Height);

	/// <summary>
	/// Whether the Window is Fullscreen or not
	/// </summary>
	public static bool Fullscreen
	{
		get => flags.Has(Platform.FosterFlags.FULLSCREEN);
		set
		{
			if (value) flags |= Platform.FosterFlags.FULLSCREEN;
			else flags &= ~Platform.FosterFlags.FULLSCREEN;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// Whether the Window is Resizable by the User
	/// </summary>
	public static bool Resizable
	{
		get => flags.Has(Platform.FosterFlags.RESIZABLE);
		set
		{
			if (value) flags |= Platform.FosterFlags.RESIZABLE;
			else flags &= ~Platform.FosterFlags.RESIZABLE;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// If Vertical Synchronization is enabled
	/// </summary>
	public static bool VSync
	{
		get => flags.Has(Platform.FosterFlags.VSYNC);
		set
		{
			if (value) flags |= Platform.FosterFlags.VSYNC;
			else flags &= ~Platform.FosterFlags.VSYNC;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// If the Mouse is Hidden when over the Window
	/// </summary>
	public static bool MouseVisible
	{
		get => flags.Has(Platform.FosterFlags.MOUSE_VISIBLE);
		set
		{
			if (value) flags |= Platform.FosterFlags.MOUSE_VISIBLE;
			else flags &= ~Platform.FosterFlags.MOUSE_VISIBLE;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// What action to perform when the user requests for the Application to exit.
	/// If not assigned, the default behavior is to call <see cref="App.Exit"/>.
	/// </summary>
	public static Action? OnExitRequested;

	/// <summary>
	/// The Main Thread that the Application was Run on
	/// </summary>
	public static int MainThreadID { get; private set; }

	/// <summary>
	/// Registers a Module that will be run within the Application once it has started.
	/// If the Application is already running, the Module's Startup method will immediately be invoked.
	/// </summary>
	public static void Register<T>() where T : Module, new()
	{
		if (!started)
		{
			registrations.Add(() => new T());
		}
		else
		{
			var it = new T();
			it.Startup();
			modules.Add(it);
		}
	}

	/// <summary>
	/// Runs the Application with the given Module automatically registered.
	/// Functionally the same as calling <see cref="Register{T}"/> followed by <see cref="Run(string, int, int, bool)"/>
	/// </summary>
	public static void Run<T>(string applicationName, int width, int height, bool fullscreen = false) where T : Module, new()
	{
		Register<T>();
		Run(applicationName, width, height, fullscreen);
	}

	/// <summary>
	/// Runs the Application
	/// </summary>
	public static void Run(string applicationName, int width, int height, bool fullscreen = false)
	{
		Debug.Assert(!Running, "Application is already running");
		Debug.Assert(!Exiting, "Application is still exiting");

		Log.Info($"Foster: v{Version.Major}.{Version.Minor}.{Version.Build}");
		Log.Info($"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
		Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");

		Running = true;
		MainThreadID = Thread.CurrentThread.ManagedThreadId;

		if (fullscreen)
			App.flags |= Platform.FosterFlags.FULLSCREEN;

		App.title = applicationName;
		App.Name = applicationName;
		var name = Platform.ToUTF8(applicationName);

		Platform.FosterStartup(new()
		{
			windowTitle = name,
			applicationName = name,
			width = width,
			height = height,
			flags = App.flags,
			onText = Input.OnText,
			onKey = Input.OnKey,
			onMouseButton = Input.OnMouseButton,
			onMouseMove = Input.OnMouseMove,
			onMouseWheel = Input.OnMouseWheel,
			onControllerConnect = Input.OnControllerConnect,
			onControllerDisconnect = Input.OnControllerDisconnect,
			onControllerButton = Input.OnControllerButton,
			onControllerAxis = Input.OnControllerAxis,
			onExitRequest = () =>
			{
				if (OnExitRequested != null)
					OnExitRequested();
				else
					Exit();
			}
		});

		UserPath = Platform.ParseUTF8(Platform.FosterGetUserPath());
		Graphics.Initialize();

		// Clear Time
		Time.Frame = 0;
		Time.Duration = new();
		lastTime = TimeSpan.Zero;
		accumulator = TimeSpan.Zero;
		timer.Restart();

		// poll events once, so input has controller state before Startup
		Platform.FosterPollEvents();
		Input.Step();

		// register & startup all modules in order
		// this is in a loop in case a module registers more modules
		// from within its own constructor/startup call.
		while (registrations.Count > 0)
		{
			int from = modules.Count;

			// create all registered modules
			for (int i = 0; i < registrations.Count; i ++)
				modules.Add(registrations[i].Invoke());
			registrations.Clear();

			// notify all modules we're now running
			for (int i = from; i < modules.Count; i ++)
				modules[i].Startup();
		}

		// begin normal game loop
		started = true;
		while (!Exiting)
			Tick();

		// shutdown
		for (int i = 0; i < modules.Count; i ++)
			modules[i].Shutdown();
		modules.Clear();

		Graphics.Resources.DeleteAllocated();
		Platform.FosterShutdown();
		Platform.FreeUTF8(name);
		started = false;
		Running = false;
		Exiting = false;
	}

	private static void Tick()
	{
		static void Update(TimeSpan delta)
		{
			Time.Frame++;
			Time.Advance(delta);

			Graphics.Resources.DeleteRequested();
			Input.Step();
			Platform.FosterPollEvents();
			FramePool.NextFrame();

			for (int i = 0; i < modules.Count; i ++)
				modules[i].Update();
		}
		
		Platform.FosterBeginFrame();

		var currentTime = timer.Elapsed;
		var deltaTime = currentTime - lastTime;
		lastTime = currentTime;

		if (Time.FixedStep)
		{
			accumulator += deltaTime;

			// Do not let us run too fast
			while (accumulator < Time.FixedStepTarget)
			{
				int milliseconds = (int)(Time.FixedStepTarget - accumulator).TotalMilliseconds;
				Thread.Sleep(milliseconds);

				currentTime = timer.Elapsed;
				deltaTime = currentTime - lastTime;
				lastTime = currentTime;
				accumulator += deltaTime;
			}

			// Do not allow any update to take longer than our maximum.
			if (accumulator > Time.FixedStepMaxElapsedTime)
			{
				Time.Advance(accumulator - Time.FixedStepMaxElapsedTime);
				accumulator = Time.FixedStepMaxElapsedTime;
			}

			// Do as many fixed updates as we can
			while (accumulator >= Time.FixedStepTarget)
			{
				accumulator -= Time.FixedStepTarget;
				Update(Time.FixedStepTarget);
				if (Exiting)
					break;
			}
		}
		else
		{
			Update(deltaTime);
		}

		for (int i = 0; i < modules.Count; i ++)
			modules[i].Render();

		Platform.FosterEndFrame();
	}

	/// <summary>
	/// Notifies the Application to Exit.
	/// The Application may finish the current frame before exiting.
	/// </summary>
	public static void Exit()
	{
		if (Running)
			Exiting = true;
	}
}
