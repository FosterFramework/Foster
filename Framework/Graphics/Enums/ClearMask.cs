namespace Foster.Framework;

[Flags]
public enum ClearMask
{
	None = 0,
	Color = (1 << 0),
	Depth = (1 << 1),
	Stencil = (1 << 2),
	All = Color | Depth | Stencil
}
