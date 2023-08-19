namespace Foster.Framework;

public enum BlendMask
{
	None  = 0,
	Red   = 1,
	Green = 2,
	Blue  = 4,
	Alpha = 8,
	RGB   = Red | Green | Blue,
	RGBA  = Red | Green | Blue | Alpha,
}
