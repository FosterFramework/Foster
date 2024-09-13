namespace Foster.Framework;

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
