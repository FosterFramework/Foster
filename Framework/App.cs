using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

using static Foster.Framework.SDL3;
using static Foster.Framework.SDL3.Hints;

namespace Foster.Framework;

public static class App
{
	private static readonly List<Module> modules = [];
	private static readonly List<Func<Module>> registrations = [];
	private static readonly Stopwatch timer = new();
	private static bool started = false;
	private static TimeSpan lastTime;
	private static TimeSpan accumulator;
	private static string title = string.Empty;
	private static readonly Exception notRunningException = new("Foster is not Running");
	
	/// <summary>
	/// Foster Version Number
	/// </summary>
	public static readonly Version Version = typeof(App).Assembly.GetName().Version!;

	/// <summary>
	/// If the Application is currently running
	/// </summary>
	public static bool Running { get; private set; } = false;

	/// <summary>
	/// If the Application is exiting. Call <see cref="Exit"/> to exit the Application.
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
		set
		{
			if (!Running)
				throw notRunningException;
			
			if (title != value)
			{
				title = value;
				SDL_SetWindowTitle(Platform.Window, value);
			}
		}
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
	public static bool Focused
	{
		get
		{
			if (!Running)
				throw notRunningException;
			var flags = SDL_WindowFlags.INPUT_FOCUS | SDL_WindowFlags.MOUSE_FOCUS;
			return (SDL_GetWindowFlags(Platform.Window) & flags) != 0;
		}
	}

	/// <summary>
	/// The Window width, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use WidthInPixels to get the drawable size.
	/// </summary>
	public static int Width
	{
		get => Size.X;
		set => Size = new(value, Height);
	}

	/// <summary>
	/// The Window height, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use HeightInPixels to get the drawable size.
	/// </summary>
	public static int Height
	{
		get => Size.Y;
		set => Size = new(Width, value);
	}

	/// <summary>
	/// The Window size, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use SizeInPixels to get the drawable size.
	/// </summary>
	public static Point2 Size
	{
		get
		{
			if (!Running)
				throw notRunningException;
			SDL_GetWindowSize(Platform.Window, out int w, out int h);
			return new(w, h);
		}
		set
		{
			if (!Running)
				throw notRunningException;
			SDL_SetWindowSize(Platform.Window, value.X, value.Y);
		}
	}

	/// <summary>
	/// The Width of the Window in Pixels
	/// </summary>
	public static int WidthInPixels => SizeInPixels.X;

	/// <summary>
	/// The Height of the Window in Pixels
	/// </summary>
	public static int HeightInPixels => SizeInPixels.Y;

	/// <summary>
	/// The Size of the Window in Pixels
	/// </summary>
	public static Point2 SizeInPixels
	{
		get
		{
			if (!Running)
				throw notRunningException;
			SDL_GetWindowSizeInPixels(Platform.Window, out int w, out int h);
			return new(w, h);
		}
	}

	/// <summary>
	/// Gets the Size of the Display that the Application Window is currently in.
	/// </summary>
	public static unsafe Point2 DisplaySize
	{
		get
		{
			if (!Running)
				throw notRunningException;
			var index = SDL_GetDisplayForWindow(Platform.Window);
			var mode = SDL_GetCurrentDisplayMode(index);
			if (mode == null)
				return Point2.Zero;
			return new(mode->w, mode->h);
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
		get
		{
			if (!Running)
				throw notRunningException;
			return (SDL_GetWindowFlags(Platform.Window) & SDL_WindowFlags.FULLSCREEN) != 0;
		}
		set
		{
			if (!Running)
				throw notRunningException;
			SDL_SetWindowFullscreen(Platform.Window, value);
		}
	}

	/// <summary>
	/// Whether the Window is Resizable by the User
	/// </summary>
	public static bool Resizable
	{
		get
		{
			if (!Running)
				throw notRunningException;
			return (SDL_GetWindowFlags(Platform.Window) & SDL_WindowFlags.RESIZABLE) != 0;
		}
		set
		{
			if (!Running)
				throw notRunningException;
			SDL_SetWindowResizable(Platform.Window, value);
		}
	}

	/// <summary>
	/// If Vertical Synchronization is enabled
	/// </summary>
	[Obsolete("Use Graphics.VSync instead")]
	public static bool VSync
	{
		get => Graphics.VSync;
		set => Graphics.VSync = value;
	}

	/// <summary>
	/// If the Mouse is Hidden when over the Window
	/// </summary>
	public static bool MouseVisible
	{
		get
		{
			if (!Running)
				throw notRunningException;
			return SDL_CursorVisible();
		}
		set
		{
			if (!Running)
				throw notRunningException;
			if (value)
				SDL_ShowCursor();
			else
				SDL_HideCursor();
		}
	}

	/// <summary>
	/// What action to perform when the user requests for the Application to exit.
	/// If not assigned, the default behavior is to call <see cref="Exit"/>.
	/// </summary>
	public static Action? OnExitRequested;

	/// <summary>
	/// Called only in DEBUG builds when a hot reload occurs.
	/// Note that this may be called off-thread, depending on when the Hot Reload occurs.
	/// </summary>
	public static Action? OnHotReload;

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
	public static unsafe void Run(string applicationName, int width, int height, bool fullscreen = false, Renderers renderer = Renderers.None)
	{
		Debug.Assert(!Running, "Application is already running");
		Debug.Assert(!Exiting, "Application is still exiting");
		Debug.Assert(width > 0 && height > 0, "Width or height is <= 0");

		// log info
		{
			var sdlv = SDL_GetVersion();
			Log.Info($"Foster: v{Version.Major}.{Version.Minor}.{Version.Build}");
			Log.Info($"SDL: v{sdlv / 1000000}.{((sdlv) / 1000) % 1000}.{(sdlv) % 1000}");
			Log.Info($"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
			Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");
		}

		MainThreadID = Thread.CurrentThread.ManagedThreadId;

		// set SDL logging method
		SDL_SetLogOutputFunction(&Platform.HandleLog, IntPtr.Zero);

		// by default allow controller presses while unfocused, 
		// let game decide if it should handle them
		SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");

		// initialize SDL3
		{
			var initFlags = 
				SDL_InitFlags.VIDEO | SDL_InitFlags.TIMER | SDL_InitFlags.EVENTS |
				SDL_InitFlags.JOYSTICK | SDL_InitFlags.GAMEPAD;

			if (!SDL_Init(initFlags))
			{
				var error = Platform.ParseUTF8(SDL_GetError());
				throw new Exception($"Foster SDL_Init Failed: {error}");
			}

			// get the UserPath
			var name = Platform.ToUTF8(applicationName);
			UserPath = Platform.ParseUTF8(SDL_GetPrefPath(IntPtr.Zero, name));
			Platform.FreeUTF8(name);
		}

		// create the graphics device
		{
			Platform.Device = SDL_CreateGPUDevice(
				formatFlags: SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
				debugMode: 1,
				null);

			if (Platform.Device == IntPtr.Zero)
				throw new Exception("Failed to create GPU Device");
		}

		// create the window
		{
			var windowFlags = 
				SDL_WindowFlags.HIGH_PIXEL_DENSITY | SDL_WindowFlags.RESIZABLE | 
				SDL_WindowFlags.HIDDEN;

			if (fullscreen)
				windowFlags |= SDL_WindowFlags.FULLSCREEN;

			Platform.Window = SDL_CreateWindow(applicationName, width, height, windowFlags);
			if (Platform.Window == IntPtr.Zero)
			{
				var error = Platform.ParseUTF8(SDL_GetError());
				throw new Exception($"Foster SDL_CreateWindow Failed: {error}");
			}

			if (SDL_ClaimWindowForGPUDevice(Platform.Device, Platform.Window) != 1)
				throw new Exception("SDL_GpuClaimWindow failed");
		}

		Renderer.Startup();

		// toggle flags and show window
		SDL_StartTextInput(Platform.Window);
		SDL_SetWindowFullscreenMode(Platform.Window, null);
		SDL_SetWindowBordered(Platform.Window, true);
		SDL_ShowCursor();

		// load default input mappings if they exist
		Input.AddDefaultSdlGamepadMappings(AppContext.BaseDirectory);

		// Clear Time
		Running = true;
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
		
		// Display Window now that we're ready
		SDL_ShowWindow(Platform.Window);

		// begin normal game loop
		started = true;
		while (!Exiting)
			Tick();

		// shutdown
		for (int i = modules.Count - 1; i >= 0; i --)
			modules[i].Shutdown();
		modules.Clear();
		Running = false;

		Renderer.Shutdown();

		SDL_StopTextInput(Platform.Window);
		SDL_ReleaseWindowFromGPUDevice(Platform.Device, Platform.Window);
		SDL_DestroyWindow(Platform.Window);
		SDL_DestroyGPUDevice(Platform.Device);
		SDL_Quit();

		Platform.Window = IntPtr.Zero;
		started = false;
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
		
		Renderer.Present();
	}

	private static unsafe void PollEvents()
	{
		// always perform a mouse-move event
		{
			SDL_GetMouseState(out float mouseX, out float mouseY);
			SDL_GetRelativeMouseState(out float deltaX, out float deltaY);
			Input.OnMouseMove(new Vector2(mouseX, mouseY), new Vector2(deltaX, deltaY));
		}

		SDL_Event ev = default;
		while (SDL_PollEvent(&ev))
		{
			switch (ev.type)
			{
			case SDL_EventType.QUIT:
				if (started)
				{
					if (OnExitRequested != null)
						OnExitRequested();
					else
						Exit();
				}
				break;

			// mouse
			case SDL_EventType.MOUSE_BUTTON_DOWN:
				Input.OnMouseButton((int)Platform.GetMouseFromSDL(ev.button.button), true);
				break;
			case SDL_EventType.MOUSE_BUTTON_UP:
				Input.OnMouseButton((int)Platform.GetMouseFromSDL(ev.button.button), false);
				break;
			case SDL_EventType.MOUSE_WHEEL:
				Input.OnMouseWheel(new(ev.wheel.x, ev.wheel.y));
				break;

			// keyboard
			case SDL_EventType.KEY_DOWN:
				if (ev.key.repeat == 0)
					Input.OnKey((int)Platform.GetKeyFromSDL(ev.key.scancode), true);
				break;
			case SDL_EventType.KEY_UP:
				if (ev.key.repeat == 0)
					Input.OnKey((int)Platform.GetKeyFromSDL(ev.key.scancode), false);
				break;

			case SDL_EventType.TEXT_INPUT:
				Input.OnText(ev.text.text);
				break;

			// joystick

			// gamepad

			default:
				break;
			}
		}
	}
}
