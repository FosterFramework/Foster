namespace Foster.Framework;

/// <summary>
/// Various Application Events that will get passed to registered <seealso cref="Module"/>.
/// Register Modules with <seealso cref="App.Register{T}"/>
/// </summary>
public enum Events
{
	/// <summary>
	/// The Application Window gained focus
	/// </summary>
	FocusGain,

	/// <summary>
	/// The Application Window lost focus
	/// </summary>
	FocusLost,

	/// <summary>
	/// The Mouse entered the Application Window
	/// </summary>
	MouseEnter,

	/// <summary>
	/// The Mouse exited the Application Window
	/// </summary>
	MouseLeave,

	/// <summary>
	/// The Application Window was resized
	/// </summary>
	Resize,

	/// <summary>
	/// The Application Window was restored (from being minimized or maximized)
	/// </summary>
	Restore,

	/// <summary>
	/// The Application Window was maximized
	/// </summary>
	Maximize,

	/// <summary>
	/// The Application Window was minimized
	/// </summary>
	Minimize,

	/// <summary>
	/// The Application Window entered Fullscreen Mode
	/// </summary>
	FullscreenEnter,

	/// <summary>
	/// The Application Window exited Fullscreen Mode
	/// </summary>
	FullscreenExit,

	/// <summary>
	/// A Controller was Connected
	/// </summary>
	ControllerConnect,

	/// <summary>
	/// A Controller was Disconnected
	/// </summary>
	ControllerDisconnect
}