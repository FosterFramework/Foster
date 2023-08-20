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
	public static readonly Version Version = new(0, 1, 0);

	/// <summary>
	/// If the Application is currently running
	/// </summary>
	public static bool Running => Platform.FosterIsRunning();

	/// <summary>
	/// If the Application is exiting. Call App.Exit() to exit the Application.
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
	/// The current Renderer API in use
	/// </summary>
	public static Renderers Renderer { get; private set; } = Renderers.None;

	/// <summary>
	/// Returns whether the Application Window is currently Focused or not.
	/// </summary>
	public static bool Focused => Platform.FosterGetFocused();

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
	/// Gets the Content Scale for the Application Window.
	/// The content scale is the ratio between the current DPI and the platform's default DPI.
	/// </summary>
	public static Vector2 ContentScale => Vector2.One * 2.0f;

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
	/// If not assigned, the default behavior is to call App.Exit();
	/// </summary>
	public static Action? OnExitRequested;

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
	/// Runs the Application
	/// </summary>
	public static void Run(string applicationName, int width, int height, bool fullscreen = false)
	{
		Debug.Assert(!Running, "Application is already running");
		Debug.Assert(!Exiting, "Application is still exiting");

		Log.Info($"Foster: v{Version}");
		Log.Info($"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
		Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");

		if (fullscreen)
			App.flags |= Platform.FosterFlags.FULLSCREEN;

		Platform.FosterStartup(new()
		{
			windowTitle = App.title = applicationName,
			applicationName = App.Name = applicationName,
			width = width,
			height = height,
			flags = App.flags,
			logging = 0,
			onLogInfo = (IntPtr msg) => Log.Info(Platform.ParseUTF8(msg)),
			onLogWarn = (IntPtr msg) => Log.Warning(Platform.ParseUTF8(msg)),
			onLogError = (IntPtr msg) => Log.Error(Platform.ParseUTF8(msg)),
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

		App.UserPath = Platform.ParseUTF8(Platform.FosterGetUserPath());
		App.Renderer = Platform.FosterGetRenderer();

		while (registrations.Count > 0)
		{
			// create all registered modules
			for (int i = 0; i < registrations.Count; i ++)
				modules.Add(registrations[i].Invoke());
			registrations.Clear();

			// notify all modules we're now running
			for (int i = 0; i < modules.Count; i ++)
				modules[i].Startup();
		}

		timer.Restart();
		started = true;

		while (Running && !Exiting)
			Tick();

		for (int i = 0; i < modules.Count; i ++)
			modules[i].Shutdown();

		Platform.FosterShutdown();

		started = false;
		Exiting = false;
	}

	private static void Tick()
	{
		Platform.FosterBeginFrame();

		static void Step(TimeSpan delta)
		{
			Time.Frame++;
			Time.Advance(delta);

			Input.Step();
			Platform.FosterPollEvents();
			FramePool.NextFrame();

			for (int i = 0; i < modules.Count; i ++)
				modules[i].Update();
		}

		var currentTime = timer.Elapsed;
		var deltaTime = currentTime - lastTime;
		lastTime = currentTime;

		if (Time.FixedStep)
		{
			accumulator += deltaTime;

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
				Step(Time.FixedStepTarget);
				if (Exiting)
					break;
			}
		}
		else
		{
			Step(deltaTime);
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

	/// <summary>
	/// Clears the Application Back Buffer to a given Color
	/// </summary>
	public static void Clear(Color color)
	{
		Clear(color, 0, 0, ClearMask.Color);
	}

	/// <summary>
	/// Clears the Application Back Buffer
	/// </summary>
	public static void Clear(Color color, int depth, int stencil, ClearMask mask)
	{
		Platform.FosterClearCommand clear = new()
		{
			target = IntPtr.Zero,
			clip = new(0, 0, WidthInPixels, HeightInPixels),
			color = color,
			depth = depth,
			stencil = stencil,
			mask = mask
		};
		Platform.FosterClear(ref clear);
	}
}