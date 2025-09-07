using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static SDL3.SDL;

namespace Foster.Framework;

/// <summary>
/// Application Information struct, to be provided to <seealso cref="App(in AppConfig)"/>
/// <param name="ApplicationName">Application Name used for storing data and representing the Application</param>
/// <param name="WindowTitle">What to display in the Window Title</param>
/// <param name="Width">The Window Width</param>
/// <param name="Height">The Window Height</param>
/// <param name="Fullscreen">If the Window should default to Fullscreen</param>
/// <param name="Resizable">If the Window should be resizable</param>
/// <param name="UpdateMode">An optional default Update Mode to initialize the App with</param>
/// <param name="PreferredGraphicsDriver">The preferred graphics driver, or None to use the platform-default</param>
/// <param name="Flags">Optional App Initialization Flags</param>
/// </summary>
public readonly record struct AppConfig
(
	string ApplicationName,
	string WindowTitle,
	int Width,
	int Height,
	bool Fullscreen = false,
	bool Resizable = true,
	UpdateMode? UpdateMode = null,
	GraphicsDriver PreferredGraphicsDriver = GraphicsDriver.None,
	AppFlags Flags = AppFlags.None
);

/// <summary>
/// App Initialization Flags
/// </summary>
[Flags]
public enum AppFlags
{
	None = 0,

	/// <summary>
	/// Enabled Graphics Debugging properties and validation
	/// </summary>
	GraphicsDebugging = 1 << 0,

	/// <summary>
	/// Enables MultiSampling of the BackBuffer
	/// </summary>
	MultiSampledBackBuffer = 1 << 1,

	/// <summary>
	/// Enabled Graphics Debugging properties and validation
	/// </summary>
	[Obsolete("Use GraphicsDebugging")]
	EnableGraphicsDebugging = 1 << 0,
}

/// <summary>
/// Inherit the App with your game.<br/>
/// Call <see cref="Run"/> to begin the main game update loop.<br/>
/// Note you can only have one App running at a time.
/// </summary>
public abstract class App : IDisposable
{
	/// <summary>
	/// Foster Version Number
	/// </summary>
	public static readonly Version FosterVersion = typeof(App).Assembly.GetName().Version!;

	/// <summary>
	/// The Application Name
	/// </summary>
	public string Name => config.ApplicationName;

	/// <summary>
	/// Timing Module
	/// </summary>
	public Time Time { get; private set; }

	/// <summary>
	/// The real elapsed time since the Application Started
	/// </summary>
	public TimeSpan Now => timer.Elapsed;

	/// <summary>
	/// How the Application should update
	/// </summary>
	public UpdateMode UpdateMode;

	/// <summary>
	/// The Application Window
	/// </summary>
	public readonly Window Window;

	/// <summary>
	/// The Input Module
	/// </summary>
	public readonly Input Input;

	/// <summary>
	/// The GPU Rendering Module
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// The FileSystem Module
	/// </summary>
	public readonly FileSystem FileSystem;

	/// <summary>
	/// If the Application is currently running
	/// </summary>
	public bool Running { get; private set; } = false;

	/// <summary>
	/// If the Application is exiting. Call <see cref="Exit"/> to exit the Application.
	/// </summary>
	public bool Exiting { get; private set; } = false;

	/// <summary>
	/// If the Application is Disposed
	/// </summary>
	public bool Disposed { get; private set; } = false;

	/// <summary>
	/// Gets the path to the User Directory, which is the location where you should
	/// store application data like settings or save data.<br/>
	/// <br/>
	/// The location of this directory is platform and application dependent.
	/// For example on Windows this is usually in `C:/User/{user}/AppData/Roaming/{App.Name}`,
	/// where as on Linux it's usually in `~/.local/share/{App.Name}`.<br/>
	/// <br/>
	/// Note that while using <see cref="System.IO"/> operations on the UserPath is generally
	/// supported across desktop environments, certain platforms require more explicit operations
	/// to mount and read/write data.<br/>
	/// <br/>
	/// If you intend to target non-desktop platforms, you should implement user data
	/// through the <see cref="FileSystem.OpenUserStorage(Action{Storage})"/> API via <see cref="FileSystem"/>
	/// </summary>
	public string UserPath
	{
		get
		{
			// only assign the user path if requested - calling this method may
			// create a directory and we only want to do that if the user
			// actually intends to use it.
			userPath ??= SDL_GetPrefPath(string.Empty, config.ApplicationName);
			return userPath;
		}
	}

	/// <summary>
	/// What action to perform when the user requests for the Application to exit.<br/>
	/// If not assigned, the default behavior will call <see cref="Exit"/>.<br/>
	/// There is also <seealso cref="Window.OnCloseRequested"/> which will be called if the Window close button is pressed.
	/// </summary>
	public Action? OnExitRequested;

	private readonly AppConfig config;
	private readonly Stopwatch timer = new();
	private TimeSpan lastUpdateTime;
	private TimeSpan fixedAccumulator;
	private readonly int mainThreadID;
	private readonly ConcurrentQueue<Action> mainThreadQueue = [];
	private readonly InputProviderSDL inputProvider;
	private string? userPath = null;

	internal readonly Exception NotRunningException = new("The Application is not Running");
	internal readonly Exception DisposedException = new("The Application is Disposed");

	public App(string name, int width, int height)
		: this(new(name, name, width, height)) {}

	public unsafe App(in AppConfig config)
	{
		this.config = config;

		if (config.Width <= 0 || config.Height <= 0)
			throw new Exception("Width or height is <= 0");
		if (string.IsNullOrEmpty(config.ApplicationName) || string.IsNullOrWhiteSpace(config.ApplicationName))
			throw new Exception("Invalid Application Name");

		// log info
		{
			var sdlv = SDL_GetVersion();
			Log.Info($"Foster: v{FosterVersion.Major}.{FosterVersion.Minor}.{FosterVersion.Build}");
			Log.Info($"SDL: v{sdlv / 1000000}.{(sdlv / 1000) % 1000}.{sdlv % 1000}");
			Log.Info($"Platform: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
			Log.Info($"Framework: {RuntimeInformation.FrameworkDescription}");
		}

		mainThreadID = Environment.CurrentManagedThreadId;

		// set SDL logging method
		SDL_SetLogOutputFunction(Platform.HandleLogFromSDL, IntPtr.Zero);

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
		}

		// Create Modules
		UpdateMode = config.UpdateMode ?? UpdateMode.FixedStep(60);
		inputProvider = new(this);
		Input = inputProvider.Input;
		FileSystem = new(this);
		GraphicsDevice = Platform.CreateGraphicsDevice(this, config.PreferredGraphicsDriver);
		GraphicsDevice.CreateDevice(config.Flags);
		Window = new Window(this, GraphicsDevice, config);
		GraphicsDevice.Startup(Window.Handle);

		// try to load default SDL gamepad mappings
		Input.AddDefaultSDLGamepadMappings(AppContext.BaseDirectory);
	}

	~App()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (Disposed)
			return;
		if (Running)
			throw new Exception("Cannot dispose App while running");

		GC.SuppressFinalize(this);
		Disposed = true;

		GraphicsDevice.Shutdown();
		Window.Close();
		GraphicsDevice.DestroyDevice();
		inputProvider.Dispose();
		mainThreadQueue.Clear();

		SDL_Quit();
	}

	/// <summary>
	/// Called when the Application has Started and is ready to be used
	/// </summary>
	protected abstract void Startup();

	/// <summary>
	/// Called as the Application shuts down. This should be used to finalize and dispose any resources.
	/// </summary>
	protected abstract void Shutdown();

	/// <summary>
	/// Called once per frame as the Application updates
	/// </summary>
	protected abstract void Update();

	/// <summary>
	/// Called when the Application renders to the Window
	/// </summary>
	protected abstract void Render();

	/// <summary>
	/// Runs the Application
	/// </summary>
	public void Run()
	{
		if (Disposed)
			throw DisposedException;
		if (Running)
			throw new Exception("Application is already running");
		if (Exiting)
			throw new Exception("Application is still exiting");

		Running = true;
		Time = new();
		lastUpdateTime = TimeSpan.Zero;
		fixedAccumulator = TimeSpan.Zero;
		timer.Restart();

		// poll events once, so input has controller state before Startup
		PollEvents();
		inputProvider.Update(Time);
		Window.Show();
		Startup();

		// begin normal game loop
		while (!Exiting)
			Tick();

		// make sure all queued main thread actions have been run
		while (mainThreadQueue.TryDequeue(out var action))
			action.Invoke();

		// shutdown
		Shutdown();
		Window.Hide();
		Running = false;
		Exiting = false;
	}

	/// <summary>
	/// Notifies the Application to Exit.
	/// The Application may finish the current frame before exiting.
	/// </summary>
	public void Exit()
	{
		if (Running)
			Exiting = true;
	}

	/// <summary>
	/// If the current thread is the Main thread the Application was Run on
	/// </summary>
	public bool IsMainThread()
		=> Environment.CurrentManagedThreadId == mainThreadID;

	/// <summary>
	/// Queues an action to be run on the Main Thread.
	/// If this is called from the main thread, it is invoked immediately.
	/// </summary>
	public void RunOnMainThread(Action action)
	{
		if (Running && IsMainThread())
			action();
		else
			mainThreadQueue.Enqueue(action);
	}

	private void Tick()
	{
		void Step(TimeSpan delta)
		{
			Time = Time.Advance(delta);

			// warp mouse to center of the window if Relative Mode is enabled
			if (SDL_GetWindowRelativeMouseMode(Window.Handle) && Window.Focused)
				SDL_WarpMouseInWindow(Window.Handle, Window.Width / 2, Window.Height / 2);

			inputProvider.Update(Time);
			PollEvents();
			FramePool.NextFrame();

			while (mainThreadQueue.TryDequeue(out var action))
				action.Invoke();

			Update();
		}

		var update = UpdateMode;
		var currentTime = timer.Elapsed;
		var deltaTime = currentTime - lastUpdateTime;
		lastUpdateTime = currentTime;

		// update in Fixed Mode
		if (update.Mode == UpdateMode.Modes.Fixed)
		{
			fixedAccumulator += deltaTime;

			// Do not let us run too fast
			if (update.FixedWaitEnabled)
			{
				while (fixedAccumulator < update.FixedTargetTime)
				{
					int milliseconds = (int)(update.FixedTargetTime - fixedAccumulator).TotalMilliseconds;
					Thread.Sleep(milliseconds);

					currentTime = timer.Elapsed;
					deltaTime = currentTime - lastUpdateTime;
					lastUpdateTime = currentTime;
					fixedAccumulator += deltaTime;
				}
			}

			// Do not allow any update to take longer than our maximum.
			if (fixedAccumulator > update.FixedMaxTime)
			{
				Time.Advance(fixedAccumulator - update.FixedMaxTime);
				fixedAccumulator = update.FixedMaxTime;
			}

			// Do as many fixed updates as we can
			while (fixedAccumulator >= update.FixedTargetTime)
			{
				fixedAccumulator -= update.FixedTargetTime;
				Step(update.FixedTargetTime);
				if (Exiting)
					break;
			}
		}
		// update in Unlocked Mode
		else
		{
			Step(deltaTime);
		}

		// render
		{
			Time = Time.AdvanceRenderFrame();
			Render();
			GraphicsDevice.Present();
		}
	}

	private void PollEvents()
	{
		// we shouldn't need to pump events here, but we've found that if we don't,
		// there are issues on MacOS with it not receiving mouse clicks correctly
		SDL_PumpEvents();

		while (SDL_PollEvent(out var ev) && ev.type != (uint)SDL_EventType.SDL_EVENT_POLL_SENTINEL)
		{
			switch ((SDL_EventType)ev.type)
			{
			case SDL_EventType.SDL_EVENT_QUIT:
				if (Running && !Exiting)
				{
					if (OnExitRequested != null)
						OnExitRequested();
					else
						Exit();
				}
				break;

			// input
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
			case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
			case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
			case SDL_EventType.SDL_EVENT_KEY_DOWN:
			case SDL_EventType.SDL_EVENT_KEY_UP:
			case SDL_EventType.SDL_EVENT_TEXT_INPUT:
			case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
			case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
			case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
			case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
			case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
			case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
			case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
			case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
			case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
			case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
				inputProvider.OnEvent(ev);
				break;

			case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
			case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
			case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
			case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
			case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
			case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
			case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
			case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
			case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
			case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
			case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
				if (ev.window.windowID == Window.ID)
					Window.OnEvent((SDL_EventType)ev.type);
				break;

			default:
				break;
			}
		}
	}
}
