namespace Foster.Framework;

/// <summary>
/// Determines how the SpriteFont should prepare its characters for rendering when requested.
/// </summary>
public enum SpriteFontBlitModes
{
	/// <summary>
	/// Blits the characters immediately upon request, halting the main thread
	/// until they are finished, so that any text rendered will be presented
	/// without any missing characters on the same frame.
	/// </summary>
	Immediate,

	/// <summary>
	/// Queues the characters to be blit in a background thread upon request,
	/// not halting the main thread. This means that characters may not have a
	/// texture until future frames.
	/// </summary>
	Streaming,

	/// <summary>
	/// Does not blit any characters upon request, and instead required the user
	/// to manually prepare the characters they want to use.
	/// </summary>
	Manual
}