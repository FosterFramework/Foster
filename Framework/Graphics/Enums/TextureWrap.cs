namespace Foster.Framework;

/// <summary>
/// How to wrap Texture Coordinates when sampling from a Texture
/// </summary>
public enum TextureWrap
{
	Repeat,
	MirroredRepeat,
	Clamp,

	[Obsolete("Use Clamp")]
	ClampToEdge = Clamp,
	
	[Obsolete("Use Clamp")]
	ClampToBorder = Clamp,
}
