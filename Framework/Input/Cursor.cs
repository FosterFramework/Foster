namespace Foster.Framework;

/// <summary>
/// Structure representing an OS Mouse Cursor
/// </summary>
public sealed class Cursor : IDisposable
{
	/// <summary>
	/// Built In System Cursors.
	/// Note that this is 1-1 with SDL's SDL_SystemCursor
	/// </summary>
	public enum SystemTypes
	{
    	Default,
    	Text,
    	Wait,
    	Crosshair,
    	Progress,
    	ResizeNWSE,
    	ResizeNESW,
    	ResizeHorizontal,
    	ResizeVertical,
    	Move,
    	NotAllowed,
    	Pointer,
    	ResizeNW,
    	ResizeN,
    	ResizeNE,
    	ResizeE,
    	ResizeSE,
    	ResizeS,
    	ResizeSW,
    	ResizeW,
	}

	/// <summary>
	/// Reference to SDL's Cursor
	/// </summary>
	internal nint Handle { get; private set; }
	
	/// <summary>
	/// The Focus Point fo the Cursor (where it is considered active)
	/// </summary>
	public readonly Point2 FocusPoint;

	/// <summary>
	/// The Size of the Cursor Image
	/// </summary>
	public readonly Point2 Size;

	/// <summary>
	/// The System Type used to create this cursor, or null if created from an Image
	/// </summary>
	public readonly SystemTypes? SystemType;

	/// <summary>
	/// If the Cursor has been disposed, in which case it can no longer be used.
	/// </summary>
	public bool Disposed => Handle == nint.Zero;

	/// <summary>
	/// Creates a new Cursor
	/// </summary>
	/// <param name="image">The Image for the Cursor to use</param>
	/// <param name="focusPoint">The Focus Point of the Cursor, which is where active point is</param>
	public unsafe Cursor(Image image, Point2 focusPoint)
	{
		FocusPoint = focusPoint;
		Size = image.Size;
		SystemType = null;

		// create SDL surface from image
		var surface = SDL3.SDL_CreateSurfaceFrom(
			image.Width,
			image.Height,
			SDL3.SDL_PixelFormat.SDL_PIXELFORMAT_RGBA8888,
			image.Pointer,
			image.Width * sizeof(Color));
		if (surface == nint.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL3.SDL_CreateSurfaceFrom));

		// create cursor, free surface
		Handle = SDL3.SDL_CreateColorCursor(surface, focusPoint.X, focusPoint.Y);
		SDL3.SDL_DestroySurface(surface);

		// validate that cursor was created successfully
		if (Handle == nint.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL3.SDL_CreateColorCursor));
	}

	/// <summary>
	/// Creates a new cursor from a built in System type.
	/// Note that FocusPoint and Size will not have valid values.
	/// </summary>
	public unsafe Cursor(SystemTypes type)
	{
		FocusPoint = Point2.Zero;
		Size = Point2.Zero;
		SystemType = type;
		Handle = SDL3.SDL_CreateSystemCursor((uint)type);

		// validate that cursor was created successfully
		if (Handle == nint.Zero)
			throw Platform.CreateExceptionFromSDL(nameof(SDL3.SDL_CreateSystemCursor));
	}

	~Cursor()
		=> Dispose();

	public void Dispose()
	{
		if (Handle != nint.Zero)
		{
			SDL3.SDL_DestroyCursor(Handle);
			Handle = nint.Zero;
		}
		GC.SuppressFinalize(this);
	}
}