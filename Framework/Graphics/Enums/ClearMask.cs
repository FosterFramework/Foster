namespace Foster.Framework;

/// <summary>
/// Specifies which Textures to clear when rendering to an <see cref="IDrawableTarget"/>
/// </summary>
[Flags]
public enum ClearMask
{
	None = 0,
	Color = 1 << 0,
	Depth = 1 << 1,
	Stencil = 1 << 2,
	All = Color | Depth | Stencil
}
