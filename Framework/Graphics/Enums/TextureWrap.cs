namespace Foster.Framework;

/// <summary>
/// How to wrap Texture Coordinates when sampling from a Texture
/// </summary>
public enum TextureWrap
{
	/// <summary>
	/// Sampling outside the texture repeats it
	/// </summary>
	Repeat,

	/// <summary>
	/// Sampling outside the texture repeats it mirrored
	/// </summary>
	MirroredRepeat,

	/// <summary>
	/// Sampling outside the texture clamps it
	/// </summary>
	Clamp,

	[Obsolete("Use Clamp")] ClampToEdge = Clamp,
	[Obsolete("Use Clamp")] ClampToBorder = Clamp,
}
