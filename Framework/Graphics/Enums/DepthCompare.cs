namespace Foster.Framework;

/// <summary>
/// Specifices the Compare operation when drawing to a Depth <see cref="Target"/>.
/// TODO: rename to just GraphicsCompare as this is also used in Stencil tests.
/// </summary>
public enum DepthCompare
{
	Always,
	Never,
	Less,
	Equal,
	LessOrEqual,
	Greater,
	NotEqual,
	GreatorOrEqual
}
