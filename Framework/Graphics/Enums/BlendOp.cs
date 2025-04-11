namespace Foster.Framework;

/// <summary>
/// Specifies the operator used to blend pixels when rendering to an <see cref="IDrawableTarget"/>
/// </summary>
public enum BlendOp
{
	Add,
	Subtract,
	ReverseSubtract,
	Min,
	Max
}
