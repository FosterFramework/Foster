namespace Foster.Framework;

/// <summary>
/// Specifies the a blending factor used when rendering to an <see cref="IDrawableTarget"/>
/// </summary>
public enum BlendFactor
{
	Zero,
	One,
	SrcColor,
	OneMinusSrcColor,
	DstColor,
	OneMinusDstColor,
	SrcAlpha,
	OneMinusSrcAlpha,
	DstAlpha,
	OneMinusDstAlpha,
	ConstantColor,
	OneMinusConstantColor,
	SrcAlphaSaturate,
}
