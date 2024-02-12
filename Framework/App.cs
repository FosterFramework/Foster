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
		Platform.FosterFlags.Resizable |
		Platform.FosterFlags.Vsync |
		Platform.FosterFlags.MouseVisible;

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
	/// Gets the Size of the Display that the Application Window is currently in.
	/// </summary>
	public static Point2 DisplaySize
	{
		get
		{
			Platform.FosterGetDisplaySize(out int w, out int h);
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
		get => flags.Has(Platform.FosterFlags.Fullscreen);
		set
		{
			if (value) flags |= Platform.FosterFlags.Fullscreen;
			else flags &= ~Platform.FosterFlags.Fullscreen;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// Whether the Window is Resizable by the User
	/// </summary>
	public static bool Resizable
	{
		get => flags.Has(Platform.FosterFlags.Resizable);
		set
		{
			if (value) flags |= Platform.FosterFlags.Resizable;
			else flags &= ~Platform.FosterFlags.Resizable;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// If Vertical Synchronization is enabled
	/// </summary>
	public static bool VSync
	{
		get => flags.Has(Platform.FosterFlags.Vsync);
		set
		{
			if (value) flags |= Platform.FosterFlags.Vsync;
			else flags &= ~Platform.FosterFlags.Vsync;
			Platform.FosterSetFlags(flags);
		}
	}

	/// <summary>
	/// If the Mouse is Hidden when over the Window
	/// </summary>
	public static bool MouseVisible
	{
		get => flags.Has(Platform.FosterFlags.MouseVisible);
		set
		{
			if (value) flags |= Platform.FosterFlags.MouseVisible;
			else flags &= ~Platform.FosterFlags.MouseVisible;
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
		if (Exiting)
			throw new Exception("Cannot register new Modules while the Application is shutting down");

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
	/// Functionally the same as calling <see cref="Register{T}"/> followed by <see cref="Run(string, int, int, bool, Renderers)"/>
	/// </summary>
	public static void Run<T>(string applicationName, int width, int height, bool fullscreen = false, Renderers renderer = Renderers.None) where T : Module, new()
	{
		Register<T>();
		Run(applicationName, width, height, fullscreen);
	}

	/// <summary>
	/// Runs the Application
	/// </summary>
	public static void Run(string applicationName, int width, int height, bool fullscreen = false, Renderers renderer = Renderers.None)
	{
		Debug.Assert(!Running, "Application is already running");
		Debug.Assert(!Exiting, "Application is still exiting");
		Debug.Assert(width > 0 && height > 0, "Width or height is <= 0");

		Log.Info($"Foster: v{Version.Major}.{Version.Minor}.{Version.Build}");
		Log.Info($"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
		Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");
		
		MainThreadID = Thread.CurrentThread.ManagedThreadId;

		// toggle fulscreen flag
		if (fullscreen)
			flags |= Platform.FosterFlags.Fullscreen;

		// run the application
		var name = Platform.ToUTF8(applicationName); 
		title = applicationName;
		Name = applicationName;

		Platform.FosterStartup(new()
		{
			windowTitle = name,
			applicationName = name,
			width = width,
			height = height,
			renderer = renderer,
			flags = flags,
		});

		if(Platform.FosterIsRunning() == 0)
			throw new Exception("Platform is not running");

		Running = true;
		UserPath = Platform.ParseUTF8(Platform.FosterGetUserPath());
		Graphics.Initialize();

		// load default input mappings if they exist
		Input.AddDefaultSdlGamepadMappings(AppContext.BaseDirectory);

		// Clear Time
		Time.Frame = 0;
		Time.Duration = new();
		lastTime = TimeSpan.Zero;
		accumulator = TimeSpan.Zero;
		timer.Restart();

		// poll events once, so input has controller state before Startup
		PollEvents();
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
		for (int i = modules.Count - 1; i >= 0; i --)
			modules[i].Shutdown();
		modules.Clear();

		Graphics.Resources.DeleteAllocated();
		Platform.FosterShutdown();
		Platform.FreeUTF8(name);
		started = false;
		Running = false;
		Exiting = false;
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

	private static void Tick()
	{
		static void Update(TimeSpan delta)
		{
			Time.Frame++;
			Time.Advance(delta);

			Graphics.Resources.DeleteRequested();
			Input.Step();
			PollEvents();
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

	private static void PollEvents()
	{
		while (Platform.FosterPollEvents(out var ev) != 0)
		{
			switch (ev.EventType)
			{
			case Platform.FosterEventType.None:
				break;
			case Platform.FosterEventType.ExitRequested:
				if (started)
				{
					if (OnExitRequested != null)
						OnExitRequested();
					else
						Exit();
				}
				break;
			case Platform.FosterEventType.KeyboardInput:
			case Platform.FosterEventType.KeyboardKey:
			case Platform.FosterEventType.MouseButton:
			case Platform.FosterEventType.MouseMove:
			case Platform.FosterEventType.MouseWheel:
			case Platform.FosterEventType.ControllerConnect:
			case Platform.FosterEventType.ControllerDisconnect:
			case Platform.FosterEventType.ControllerButton:
			case Platform.FosterEventType.ControllerAxis:
				Input.OnFosterEvent(ev);
				break;
			}
		}
	}
}
