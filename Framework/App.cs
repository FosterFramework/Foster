using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

using static SDL3.SDL;

namespace Foster.Framework;

/// <summary>
/// Application Information struct, to be provided to <seealso cref="App.Run(in AppRunInfo)"/>
/// <param name="ApplicationName">Application Name used for storing data and representing the Application</param>
/// <param name="WindowTitle">What to display in the Window Title</param>
/// <param name="Width">The Window Width</param>
/// <param name="Height">The Window Height</param>
/// <param name="Fullscreen">If the Window should default to Fullscreen</param>
/// <param name="Resizable">If the Window should be resizable</param>
/// </summary>
public readonly record struct AppRunInfo
(
	string ApplicationName,
	string WindowTitle,
	int Width,
	int Height,
	bool Fullscreen = false,
	bool Resizable = true
);

/// <summary>
/// The Application runs and updates your game.
/// </summary>
public static class App
{
	/// <summary>
	/// SDL Window Pointer
	/// </summary>
	internal static nint Window { get; private set; }

	private static readonly List<Module> modules = [];
	private static readonly List<Func<Module>> registrations = [];
	private static readonly Stopwatch timer = new();
	private static bool started = false;
	private static TimeSpan lastUpdateTime;
	private static TimeSpan fixedAccumulator;
	private static string title = string.Empty;
	private static readonly Exception notRunningException = new("Foster is not Running");
	private static readonly List<(uint ID, nint Ptr)> openJoysticks = [];
	private static readonly List<(uint ID, nint Ptr)> openGamepads = [];
	private static int mainThreadID;
	private static readonly ConcurrentQueue<Action> mainThreadQueue = [];

	/// <summary>
	/// How the Application will run its update loop
	/// </summary>
	public enum UpdateModes
	{
		/// <summary>
		/// The Application will update on a fixed time
		/// </summary>
		Fixed,
		
		/// <summary>
		/// How the Application will update as fast as it can
		/// </summary>
		Unlocked,
	}
	
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
				SDL_SetWindowTitle(Window, value);
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
	/// Gets the Timer used to calculcate Application Time.
	/// This is polled immediately when requested.
	/// </summary>
	public static TimeSpan Timer => timer.Elapsed;
	
	/// <summary>
	/// The current Update Mode used by the Application.
	/// This can be changed with <seealso cref="SetFixedUpdate(int, bool)"/> and <seealso cref="SetUnlockedUpdate"/>
	/// </summary>
	public static UpdateModes UpdateMode { get; private set; } = UpdateModes.Fixed;

	/// <summary>
	/// The target time per frame for a Fixed Update
	/// </summary>
	public static TimeSpan FixedUpdateTargetTime { get; private set; }

	/// <summary>
	/// The maximum amount of time a Fixed Update is allowed to take before the Application starts dropping frames.
	/// </summary>
	public static TimeSpan FixedUpdateMaxTime { get; private set; }

	/// <summary>
	/// This will force the main thread to wait until another Fixed update is ready.
	/// This uses less CPU but means that your render loop can not happen any faster than your fixed update rate.
	/// </summary>
	public static bool FixedUpdateWaitEnabled { get; private set; }

	/// <summary>
	/// The current Renderer API in use
	/// </summary>
	public static GraphicsDriver Driver => Renderer.Driver;

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
			SDL_GetWindowSize(Window, out int w, out int h);
			return new(w, h);
		}
		set
		{
			if (!Running)
				throw notRunningException;
			SDL_SetWindowSize(Window, value.X, value.Y);
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
			SDL_GetWindowSizeInPixels(Window, out int w, out int h);
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
			var index = SDL_GetDisplayForWindow(Window);
			var mode = (SDL_DisplayMode*)SDL_GetCurrentDisplayMode(index);
			if (mode == null)
				return Point2.Zero;
			return new(mode->w, mode->h);
		}
	}

	/// <summary>
	/// Gets the Content Scale for the Application Window.
	/// </summary>
	public static Vector2 ContentScale
	{
		get
		{
			var scale = SDL_GetWindowDisplayScale(Window);
			if (scale <= 0)
			{
				Log.Warning($"SDL_GetWindowDisplayScale failed: {Platform.GetErrorFromSDL()}");
				return new(WidthInPixels / Width, HeightInPixels / Height);
			}
			return Vector2.One * scale;
		}
	}

	/// <summary>
	/// Whether the Window is Fullscreen or not
	/// </summary>
	public static bool Fullscreen
	{
		get
		{
			if (!Running)
				throw notRunningException;
			return (SDL_GetWindowFlags(Window) & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;
		}
		set
		{
			if (!Running)
				throw notRunningException;
			SDL_SetWindowFullscreen(Window, value);
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
			return (SDL_GetWindowFlags(Window) & SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0;
		}
		set
		{
			if (!Running)
				throw notRunningException;
			SDL_SetWindowResizable(Window, value);
		}
	}

	/// <summary>
	/// Whether the Window is Maximized
	/// </summary>
	public static bool Maximized
	{
		get
		{
			if (!Running)
				throw notRunningException;
			return (SDL_GetWindowFlags(Window) & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
		}
		set
		{
			if (!Running)
				throw notRunningException;

			if (value && !Maximized)
				SDL_MaximizeWindow(Window);
			else if (!value && Maximized)
				SDL_RestoreWindow(Window);
		}
	}

	/// <summary>
	/// Returns whether the Application Window is currently Focused or not.
	/// </summary>
	public static bool Focused
	{
		get
		{
			if (!Running)
				throw notRunningException;
			var flags = SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS;
			return (SDL_GetWindowFlags(Window) & flags) != 0;
		}
	}

	/// <summary>
	/// If Vertical Synchronization is enabled
	/// </summary>
	public static bool VSync
	{
		get => Renderer.GetVSync();
		set => Renderer.SetVSync(value);
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
	/// Functionally the same as calling <see cref="Register{T}"/> followed by <see cref="Run(string, int, int, bool)"/>
	/// </summary>
	public static void Run<T>(string applicationName, int width, int height, bool fullscreen = false) where T : Module, new()
		=> Run<T>(new(applicationName, applicationName, width, height, fullscreen));

	/// <summary>
	/// Runs the Application with the given Module automatically registered.
	/// Functionally the same as calling <see cref="Register{T}"/> followed by <see cref="Run(in AppRunInfo)"/>
	/// </summary>
	public static void Run<T>(in AppRunInfo info) where T : Module, new()
	{
		Register<T>();
		Run(info);
	}

	/// <summary>
	/// Runs the Application
	/// </summary>
	public static unsafe void Run(string applicationName, int width, int height, bool fullscreen = false)
		=> Run(new(applicationName, applicationName, width, height, fullscreen));

	/// <summary>
	/// Runs the Application
	/// </summary>
	public static unsafe void Run(in AppRunInfo info)
	{
		if (Running)
			throw new Exception("Application is already running");
		if (Exiting)
			throw new Exception("Application is still exiting");
		if (info.Width <= 0 || info.Height <= 0)
			throw new Exception("Width or height is <= 0");
		if (string.IsNullOrEmpty(info.ApplicationName) || string.IsNullOrWhiteSpace(info.ApplicationName))
			throw new Exception("Invalid Application Name");

		// log info
		{
			var sdlv = SDL_GetVersion();
			Log.Info($"Foster: v{Version.Major}.{Version.Minor}.{Version.Build}");
			Log.Info($"SDL: v{sdlv / 1000000}.{(sdlv / 1000) % 1000}.{sdlv % 1000}");
			Log.Info($"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
			Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");
		}

		mainThreadID = Environment.CurrentManagedThreadId;

		// default to fixed update
		SetFixedUpdate(60);

		// set SDL logging method
		SDL_SetLogOutputFunction(&Platform.HandleLogFromSDL, IntPtr.Zero);

		// by default allow controller presses while unfocused, 
		// let game decide if it should handle them
		SDL_SetHint(SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");

		// initialize SDL3
		{
			var initFlags = 
				SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_TIMER | SDL_InitFlags.SDL_INIT_EVENTS |
				SDL_InitFlags.SDL_INIT_JOYSTICK | SDL_InitFlags.SDL_INIT_GAMEPAD;

			if (!SDL_Init(initFlags))
				throw Platform.CreateExceptionFromSDL(nameof(SDL_Init));

			// get the UserPath
			UserPath = SDL_GetPrefPath(string.Empty, info.ApplicationName);
		}

		// create the graphics device
		Renderer.CreateDevice();

		// create the window
		{
			var windowFlags = 
				SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | 
				SDL_WindowFlags.SDL_WINDOW_HIDDEN;

			if (info.Fullscreen)
				windowFlags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

			Window = SDL_CreateWindow(info.WindowTitle, info.Width, info.Height, windowFlags);
			if (Window == IntPtr.Zero)
				throw Platform.CreateExceptionFromSDL(nameof(SDL_CreateWindow));
		}

		Renderer.Startup(Window);

		// toggle flags and show window
		SDL_StartTextInput(Window);
		SDL_SetWindowFullscreenMode(Window, null);
		SDL_SetWindowBordered(Window, true);
		SDL_ShowCursor();

		// load default input mappings if they exist
		Input.AddDefaultSDLGamepadMappings(AppContext.BaseDirectory);

		// Clear Time
		Running = true;
		Time.Frame = 0;
		Time.Delta = 0;
		Time.Duration = new();
		Time.RenderFrame = 0;
		lastUpdateTime = TimeSpan.Zero;
		fixedAccumulator = TimeSpan.Zero;
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
		SDL_ShowWindow(Window);

		// begin normal game loop
		started = true;
		while (!Exiting)
			Tick();

		// make sure all queued main thread actions have been run
		while (mainThreadQueue.TryDequeue(out var action))
			action.Invoke();

		// shutdown
		for (int i = modules.Count - 1; i >= 0; i --)
			modules[i].Shutdown();
		modules.Clear();
		Running = false;

		// release joystick/gamepads
		foreach (var it in openJoysticks)
			SDL_CloseJoystick(it.Ptr);
		foreach (var it in openGamepads)
			SDL_CloseGamepad(it.Ptr);
		openJoysticks.Clear();
		openGamepads.Clear();

		Renderer.Shutdown();
		SDL_StopTextInput(Window);
		SDL_DestroyWindow(Window);
		Renderer.DestroyDevice();
		SDL_Quit();

		mainThreadQueue.Clear();
		Window = IntPtr.Zero;
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

	/// <summary>
	/// Clears the Back Buffer to a given Color
	/// </summary>
	public static void Clear(Color color) 
		=> Renderer.Clear(null, color, 0, 0, ClearMask.Color);

	/// <summary>
	/// Clears the Back Buffer
	/// </summary>
	public static unsafe void Clear(Color color, float depth, int stencil, ClearMask mask)
		=> Renderer.Clear(null, color, depth, stencil, mask);

	/// <summary>
	/// Submits a Draw Command to the GPU
	/// </summary>
	/// <param name="command"></param>
	public static unsafe void Draw(in DrawCommand command)
		=> Renderer.Draw(command);

	/// <summary>
	/// If the current thread is the Main thread the Application was Run on
	/// </summary>
	public static bool IsMainThread()
		=> Environment.CurrentManagedThreadId == mainThreadID;

	/// <summary>
	/// Queues an action to be run on the Main Thread.
	/// If this is called from the main thread, it is invoked immediately.
	/// </summary>
	public static void RunOnMainThread(Action action)
	{
		if (Running && IsMainThread())
			action();
		else
			mainThreadQueue.Enqueue(action);
	}

	/// <summary>
	/// The Update loop will run at a fixed rate.
	/// </summary>
	/// <param name="targetTimePerFrame">The target time per frame</param>
	/// <param name="maxTimePerFrame">The maximum time allowed per frame before the Application intentially starts lagging.</param>
	/// <param name="waitForNextUpdate">The thread will sleep while waiting for a fixed update. This essentially will also lock your render to the Fixed Update rate, but will use less CPU.</param>
	public static void SetFixedUpdate(
		TimeSpan targetTimePerFrame,
		TimeSpan? maxTimePerFrame = null,
		bool waitForNextUpdate = true)
	{
		UpdateMode = UpdateModes.Fixed;
		FixedUpdateTargetTime = targetTimePerFrame;
		FixedUpdateMaxTime = maxTimePerFrame ?? (targetTimePerFrame * 5);
		FixedUpdateWaitEnabled = waitForNextUpdate;
	}

	/// <summary>
	/// The Update loop will run at a fixed rate.
	/// </summary>
	/// <param name="fps">The target frames per second</param>
	/// <param name="waitForNextUpdate">The thread will sleep while waiting for a fixed update. This essentially will also lock your render to the Fixed Update rate, but will use less CPU.</param>
	public static void SetFixedUpdate(int fps, bool waitForNextUpdate = true)
		=> SetFixedUpdate(TimeSpan.FromSeconds(1.0f / fps), null, waitForNextUpdate);

	/// <summary>
	/// The Update loop will run as fast as it can.
	/// This means there will be one update per Render.
	/// </summary>
	public static void SetUnlockedUpdate()
	{
		UpdateMode = UpdateModes.Unlocked;
	}

	private static void Tick()
	{
		static void Update(TimeSpan delta)
		{
			Time.Frame++;
			Time.Advance(delta);
			
			Input.Step();
			PollEvents();
			FramePool.NextFrame();

			while (mainThreadQueue.TryDequeue(out var action))
				action.Invoke();

			for (int i = 0; i < modules.Count; i ++)
				modules[i].Update();
		}
		
		var currentTime = timer.Elapsed;
		var deltaTime = currentTime - lastUpdateTime;
		lastUpdateTime = currentTime;

		// update in Fixed Mode
		if (UpdateMode == UpdateModes.Fixed)
		{
			fixedAccumulator += deltaTime;

			// Do not let us run too fast
			if (FixedUpdateWaitEnabled)
			{
				while (fixedAccumulator < FixedUpdateTargetTime)
				{
					int milliseconds = (int)(FixedUpdateTargetTime - fixedAccumulator).TotalMilliseconds;
					Thread.Sleep(milliseconds);

					currentTime = timer.Elapsed;
					deltaTime = currentTime - lastUpdateTime;
					lastUpdateTime = currentTime;
					fixedAccumulator += deltaTime;
				}
			}

			// Do not allow any update to take longer than our maximum.
			if (fixedAccumulator > FixedUpdateMaxTime)
			{
				Time.Advance(fixedAccumulator - FixedUpdateMaxTime);
				fixedAccumulator = FixedUpdateMaxTime;
			}

			// Do as many fixed updates as we can
			while (fixedAccumulator >= FixedUpdateTargetTime)
			{
				fixedAccumulator -= FixedUpdateTargetTime;
				Update(FixedUpdateTargetTime);
				if (Exiting)
					break;
			}
		}
		// update in Unlocked Mode
		else
		{
			Update(deltaTime);
		}

		// render
		{
			Time.RenderFrame++;
			for (int i = 0; i < modules.Count; i ++)
				modules[i].Render();
			Renderer.Present();
		}
	}

	private static unsafe void PollEvents()
	{
		static void NotifyModules(Events appEvent)
		{
			for (int i = 0; i < modules.Count; i ++)
				modules[i].OnEvent(appEvent);
		}

		// always perform a mouse-move event
		{
			SDL_GetMouseState(out float mouseX, out float mouseY);
			SDL_GetRelativeMouseState(out float deltaX, out float deltaY);
			Input.OnMouseMove(new Vector2(mouseX, mouseY), new Vector2(deltaX, deltaY));
		}

		while (SDL_PollEvent(out var ev) && ev.type != (uint)SDL_EventType.SDL_EVENT_POLL_SENTINEL)
		{
			switch ((SDL_EventType)ev.type)
			{
			case SDL_EventType.SDL_EVENT_QUIT:
				if (started)
				{
					if (OnExitRequested != null)
						OnExitRequested();
					else
						Exit();
				}
				break;

			// mouse
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
				Input.OnMouseButton((int)Platform.GetMouseFromSDL(ev.button.button), true);
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
				Input.OnMouseButton((int)Platform.GetMouseFromSDL(ev.button.button), false);
				break;
			case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
				Input.OnMouseWheel(new(ev.wheel.x, ev.wheel.y));
				break;

			// keyboard
			case SDL_EventType.SDL_EVENT_KEY_DOWN:
				if (!ev.key.repeat)
					Input.OnKey((int)Platform.GetKeyFromSDL(ev.key.scancode), true);
				break;
			case SDL_EventType.SDL_EVENT_KEY_UP:
				if (!ev.key.repeat)
					Input.OnKey((int)Platform.GetKeyFromSDL(ev.key.scancode), false);
				break;

			case SDL_EventType.SDL_EVENT_TEXT_INPUT:
				Input.OnText(new nint(ev.text.text));
				break;

			// joystick
			case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
				{
					var id = ev.jdevice.which;
					if (SDL_IsGamepad(id))
						break;

					var ptr = SDL_OpenJoystick(id);
					openJoysticks.Add((id, ptr));

					Input.OnControllerConnect(
						id: new(id),
						name: SDL_GetJoystickName(ptr),
						buttonCount: SDL_GetNumJoystickButtons(ptr),
						axisCount: SDL_GetNumJoystickAxes(ptr),
						isGamepad: false,
						type: GamepadTypes.Unknown,
						vendor: SDL_GetJoystickVendor(ptr),
						product: SDL_GetJoystickProduct(ptr),
						version: SDL_GetJoystickProductVersion(ptr)
					);
					NotifyModules(Events.ControllerConnect);
					break;
				}
			case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
				{
					var id = ev.jdevice.which;
					if (SDL_IsGamepad(id))
						break;

					for (int i = 0; i < openJoysticks.Count; i ++)
						if (openJoysticks[i].ID == id)
						{
							SDL_CloseJoystick(openJoysticks[i].Ptr);
							openJoysticks.RemoveAt(i);
						}

					Input.OnControllerDisconnect(new(id));
					NotifyModules(Events.ControllerDisconnect);
					break;
				}
			case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
			case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
				{
					var id = ev.jbutton.which;
					if (SDL_IsGamepad(id))
						break;

					Input.OnControllerButton(
						id: new(id),
						button: ev.jbutton.button,
						pressed: ev.type == (uint)SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN);

					break;
				}
			case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
				{
					var id = ev.jaxis.which;
					if (SDL_IsGamepad(id))
						break;

					float value = ev.jaxis.value >= 0
						? ev.jaxis.value / 32767.0f
						: ev.jaxis.value / 32768.0f;

					Input.OnControllerAxis(
						id: new(id),
						axis: ev.jaxis.axis,
						value: value);

					break;
				}

			// gamepad
			case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
				{
					var id = ev.gdevice.which;
					var ptr = SDL_OpenGamepad(id);
					openGamepads.Add((id, ptr));

					Input.OnControllerConnect(
						id: new(id),
						name: SDL_GetGamepadName(ptr),
						buttonCount: 15,
						axisCount: 6,
						isGamepad: true,
						type: (GamepadTypes)SDL_GetGamepadType(ptr),
						vendor: SDL_GetGamepadVendor(ptr),
						product: SDL_GetGamepadProduct(ptr),
						version: SDL_GetGamepadProductVersion(ptr)
					);
					NotifyModules(Events.ControllerConnect);
					break;
				}
			case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
				{
					var id = ev.gdevice.which;
					for (int i = 0; i < openGamepads.Count; i ++)
						if (openGamepads[i].ID == id)
						{
							SDL_CloseGamepad(openGamepads[i].Ptr);
							openGamepads.RemoveAt(i);
						}

					Input.OnControllerDisconnect(new(id));
					NotifyModules(Events.ControllerDisconnect);
					break;
				}
			case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
			case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
				{
					var id = ev.gbutton.which;
					Input.OnControllerButton(
						id: new(id),
						button: (int)Platform.GetButtonFromSDL((SDL_GamepadButton)ev.gbutton.button),
						pressed: ev.type == (uint)SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN);

					break;
				}
			case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
				{
					var id = ev.gbutton.which;
					float value = ev.gaxis.value >= 0
						? ev.gaxis.value / 32767.0f
						: ev.gaxis.value / 32768.0f;

					Input.OnControllerAxis(
						id: new(id),
						axis: (int)Platform.GetAxisFromSDL((SDL_GamepadAxis)ev.gaxis.axis),
						value: value);
						
					break;
				}

			case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
				NotifyModules(Events.FocusGain);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
				NotifyModules(Events.FocusLost);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
				NotifyModules(Events.MouseEnter);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
				NotifyModules(Events.MouseLeave);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
				NotifyModules(Events.Resize);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
				NotifyModules(Events.Restore);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
				NotifyModules(Events.Maximize);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
				NotifyModules(Events.Minimize);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
				NotifyModules(Events.FullscreenEnter);
				break;
			case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
				NotifyModules(Events.FullscreenExit);
				break;

			default:
				break;
			}
		}
	}
}
