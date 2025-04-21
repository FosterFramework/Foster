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
	[Obsolete("No longer supported in SDL GPU")] ConstantAlpha,
	[Obsolete("No longer supported in SDL GPU")] OneMinusConstantAlpha,
	SrcAlphaSaturate,
	[Obsolete("No longer supported in SDL GPU")] Src1Color,
	[Obsolete("No longer supported in SDL GPU")] OneMinusSrc1Color,
	[Obsolete("No longer supported in SDL GPU")] Src1Alpha,
	[Obsolete("No longer supported in SDL GPU")] OneMinusSrc1Alpha
}
