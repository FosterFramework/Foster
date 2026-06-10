using System.Numerics;
using System.Runtime.CompilerServices;
using static SDL3.SDL;

namespace Foster.Framework;

public sealed class Window : IDrawableTarget
{
	internal nint Handle { get; private set; }
	internal readonly uint ID;

	private string title;
	private readonly App app;
	private readonly Exception closedWindowException = new("The Window has been Closed");
	private readonly Exception notOnMainThreadException = new("This method may only be called from the Main thread");
	object? IDrawableTarget.Surface => this;

	/// <summary>
	/// The Renderer associated with this Window
	/// </summary>
	public GraphicsDevice GraphicsDevice { get; }

	/// <summary>
	/// If this Window is Destroyed
	/// </summary>
	public bool IsDestroyed { get; private set; }

	/// <summary>
	/// The Window Title
	/// </summary>
	public string Title
	{
		get => title;
		set
		{
			// update title
			if (title == value)
				return;
			title = value;

			// apply title
			if (app.IsMainThread())
				ApplyTitle();
			else
				app.RunOnMainThread(ApplyTitle);
		}
	}

	/// <summary>
	/// The Window Position
	/// </summary>
	public Point2 Position
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_GetWindowPosition(Handle, out var x, out var y);
			return new(x, y);
		}
		set
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_SetWindowPosition(Handle, value.X, value.Y);
		}
	}

	/// <summary>
	/// The Window width, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use WidthInPixels to get the drawable size.
	/// </summary>
	public int Width
	{
		get => Size.X;
		set => Size = new(value, Height);
	}

	/// <summary>
	/// The Window height, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use HeightInPixels to get the drawable size.
	/// </summary>
	public int Height
	{
		get => Size.Y;
		set => Size = new(Width, value);
	}

	/// <summary>
	/// The Window size, which isn't necessarily the size in Pixels depending on the Platform.
	/// Use SizeInPixels to get the drawable size.
	/// </summary>
	public Point2 Size
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_GetWindowSize(Handle, out int w, out int h);
			return new(w, h);
		}
		set
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_SetWindowSize(Handle, value.X, value.Y);
		}
	}

	/// <summary>
	/// The Width of the Window in Pixels
	/// </summary>
	public int WidthInPixels => SizeInPixels.X;

	/// <summary>
	/// The Height of the Window in Pixels
	/// </summary>
	public int HeightInPixels => SizeInPixels.Y;

	/// <summary>
	/// The Size of the Window in Pixels
	/// </summary>
	public Point2 SizeInPixels
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_GetWindowSizeInPixels(Handle, out int w, out int h);
			return new(w, h);
		}
	}

	/// <summary>
	/// Gets the Size of the Display that the Window is currently in.
	/// </summary>
	public unsafe Point2 DisplaySize
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			var index = SDL_GetDisplayForWindow(Handle);
			var mode = (SDL_DisplayMode*)SDL_GetCurrentDisplayMode(index);
			if (mode == null)
				return Point2.Zero;
			return new(mode->w, mode->h);
		}
	}

	/// <summary>
	/// Gets the Content Scale for the Window.
	/// </summary>
	public Vector2 ContentScale
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			var scale = SDL_GetWindowDisplayScale(Handle);
			if (scale <= 0)
			{
				Log.Warning($"SDL_GetWindowDisplayScale failed: {SDL_GetError()}");
				return new(WidthInPixels / (float)Width, HeightInPixels / (float)Height);
			}
			return Vector2.One * scale;
		}
	}

	/// <summary>
	/// Calculates the mouse position relative to the Window, in Pixel Coordinates.
	/// </summary>
	public Vector2 MousePosition
	{
		get
		{
			// use global mouse position so we can get it as it moves outside the window
			SDL_GetGlobalMouseState(out var mouseX, out var mouseY);

			// scale it to the pixel coords
			return (new Vector2(mouseX, mouseY) - Position) / (Vector2)Size * (Vector2)SizeInPixels;
		}
	}

	/// <summary>
	/// Whether the Window is Fullscreen or not
	/// </summary>
	public bool Fullscreen
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			return (SDL_GetWindowFlags(Handle) & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;
		}
		set
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_SetWindowFullscreen(Handle, value);
		}
	}

	/// <summary>
	/// Whether the Window is Resizable by the User
	/// </summary>
	public bool Resizable
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			return (SDL_GetWindowFlags(Handle) & SDL_WindowFlags.SDL_WINDOW_RESIZABLE) != 0;
		}
		set
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			SDL_SetWindowResizable(Handle, value);
		}
	}

	/// <summary>
	/// Whether the Window is Maximized
	/// </summary>
	public bool Maximized
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			return (SDL_GetWindowFlags(Handle) & SDL_WindowFlags.SDL_WINDOW_MAXIMIZED) != 0;
		}
		set
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			if (value && !Maximized)
				SDL_MaximizeWindow(Handle);
			else if (!value && Maximized)
				SDL_RestoreWindow(Handle);
		}
	}

	/// <summary>
	/// Returns whether the Window is currently Focused or not.
	/// </summary>
	public bool Focused
	{
		get
		{
			if (Handle == nint.Zero)
				throw closedWindowException;
			var flags = SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS | SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS;
			return (SDL_GetWindowFlags(Handle) & flags) != 0;
		}
	}

	/// <summary>
	/// Called when the Window gains focus
	/// </summary>
	public event Action? OnFocusGain = null;

	/// <summary>
	/// Called when the Window loses focus
	/// </summary>
	public event Action? OnFocusLost = null;

	/// <summary>
	/// Called when the Mouse enters the Window
	/// </summary>
	public event Action? OnMouseEnter = null;

	/// <summary>
	/// Called when the Mouse leaves the Window
	/// </summary>
	public event Action? OnMouseLeave = null;

	/// <summary>
	/// Called when the Window is resized
	/// </summary>
	public event Action? OnResize = null;

	/// <summary>
	/// Called when the Window is restored (after being minimized)
	/// </summary>
	public event Action? OnRestore = null;

	/// <summary>
	/// Called when the Window is maximized
	/// </summary>
	public event Action? OnMaximize = null;

	/// <summary>
	/// Called when the Window is minimized
	/// </summary>
	public event Action? OnMinimize = null;

	/// <summary>
	/// Called when the Window enters full screen mode
	/// </summary>
	public event Action? OnFullscreenEnter = null;

	/// <summary>
	/// Called when the Window exits full screen mode
	/// </summary>
	public event Action? OnFullscreenExit = null;

	/// <summary>
	/// What action(s) to perform when the user requests for the Window to close.
	/// If not assigned, the default behavior will call <see cref="App.Exit"/> on the <see cref="App.Window"/>,
	/// or <see cref="Destroy"/> on other windows.
	/// </summary>
	public Action? OnCloseRequested;

	/// <summary>
	/// Creates a new Window
	/// </summary>
	public Window(App app, string title, int width, int height, bool fullscreen = false, bool resizable = true)
	{
		this.app = app;
		this.title = title;
		GraphicsDevice = app.GraphicsDevice;

		var windowFlags =
			SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
		if (!app.Running)
			windowFlags |= SDL_WindowFlags.SDL_WINDOW_HIDDEN;
		if (fullscreen)
			windowFlags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;
		if (resizable)
			windowFlags |= SDL_WindowFlags.SDL_WINDOW_RESIZABLE;

		Handle = SDL_CreateWindow(title, width, height, windowFlags);
		if (Handle == nint.Zero)
			throw App.CreateExceptionFromSDL(nameof(SDL_CreateWindow));
		ID = SDL_GetWindowID(Handle);

		app.WindowCreated(this);
	}

	/// <summary>
	/// Destroys the Window.
	/// If this is the <see cref="App.Window"/>, this will call <see cref="App.Exit"/>.
	/// </summary>
	public void Destroy()
	{
		if (!IsDestroyed)
		{
			app.WindowMarkedForDestruction(this);
			if (app.Window == this)
				app.Exit();
			IsDestroyed = true;
		}
	}

	internal void Destroyed()
	{
		Handle = nint.Zero;
	}

	/// <summary>
	/// Sets whether the Mouse Cursor should be visible while over the Window
	/// </summary>
	[Obsolete("Use Application SetMouseVisible instead")]
	public void SetMouseVisible(bool enabled)
		=> app.SetMouseVisible(enabled);

	/// <summary>
	/// Sets whether the Mouse is in Relative Mode.
	/// While in Relative Mode, the Mouse Cursor is not visible, and the mouse
	/// is constrained to the Window while still updating Mouse delta.
	/// </summary>
	public void SetMouseRelativeMode(bool enabled)
	{
		if (enabled == SDL_GetWindowRelativeMouseMode(Handle))
			return;

		if (!SDL_SetWindowRelativeMouseMode(Handle, enabled))
			Log.Warning($"Failed to set Mouse Relative Mode: {SDL_GetError()}");

		SDL_WarpMouseInWindow(Handle, Width / 2, Height / 2);
	}

	/// <summary>
	/// Sets the Mouse position within the Window in Pixel Coordinates
	/// </summary>
	public void SetMousePosition(Vector2 position)
	{
		SDL_WarpMouseInWindow(Handle, 
			position.X * (Width / (float)WidthInPixels),
			position.Y * (Height / (float)HeightInPixels)
		);
	}

	/// <summary>
	/// Sets the Mouse Cursor. If null, resets the Cursor to the default OS cursor.
	/// </summary>
	[Obsolete("Use App.SetMouseCursor")]
	public void SetMouseCursor(Cursor? cursor)
		=> app.SetMouseCursor(cursor);

	/// <summary>
	/// This will enable Text input in the Window, by populating keyboard
	/// text in <see cref="KeyboardState.Text"/>.<br/>
	/// <br/>
	/// On some platforms this function will show an on-screen keyboard.
	/// </summary>
	public void SetTextInput(bool enabled)
	{
		if (enabled)
			StartTextInput();
		else
			StopTextInput();
	}

	/// <summary>
	/// This will enable Text input in the Window, by populating keyboard
	/// text in <see cref="KeyboardState.Text"/>.<br/>
	/// <br/>
	/// On some platforms this function will show an on-screen keyboard.
	/// </summary>
	public void StartTextInput()
	{
		if (app.IsMainThread())
		{
			if (!SDL_TextInputActive(Handle))
				SDL_StartTextInput(Handle);
		}
		else
		{
			app.RunOnMainThread(StartTextInput);
		}
	}

	/// <summary>
	/// This disables Text input in the Window
	/// </summary>
	public void StopTextInput()
	{
		if (app.IsMainThread())
		{
			if (SDL_TextInputActive(Handle))
				SDL_StopTextInput(Handle);
		}
		else
		{
			app.RunOnMainThread(StopTextInput);
		}
	}
	
	/// <summary>
	/// Brings focus to the Window
	/// </summary>
	public void Focus()
	{
		SDL_RaiseWindow(Handle);
	}

    public enum DialogType
    {
        Info,
        Warning,
        Error
    }

    public void ShowDialog(DialogType type, string dialogTitle, string message)
    {
        SDL_ShowSimpleMessageBox(type switch
        {
            DialogType.Error => SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
            DialogType.Warning => SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
            _ => SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION
        }, dialogTitle, message, Handle);
    }

	internal void OnEvent(SDL_EventType ev)
	{
		switch (ev)
		{
		case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
			OnFocusGain?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
			OnFocusLost?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
			OnMouseEnter?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
			OnMouseLeave?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
			OnResize?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
			OnRestore?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:
			OnMaximize?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
			OnMinimize?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
			OnFullscreenEnter?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
			OnFullscreenExit?.Invoke();
			break;
		case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
			if (OnCloseRequested != null)
				OnCloseRequested.Invoke();
			else if (app.Window == this)
				app.Exit();
			else
				Destroy();
			break;
		}
	}

	internal void Show()
	{
		SDL_ShowWindow(Handle);
		SDL_SetWindowFullscreenMode(Handle, ref Unsafe.NullRef<SDL_DisplayMode>());
		SDL_SetWindowBordered(Handle, true);
		SDL_RaiseWindow(Handle);
	}

	internal void Hide()
	{
		SDL_HideWindow(Handle);
	}

	private void ApplyTitle()
	{
		if (Handle != nint.Zero)
			SDL_SetWindowTitle(Handle, title);
	}
}
