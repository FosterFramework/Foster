namespace Foster.Framework;

[Flags]
public enum ClearMask
{
	None    = 0,
	Color   = 1,
	Depth   = 2,
	Stencil = 4,
	All     = Color | Depth | Stencil
}
