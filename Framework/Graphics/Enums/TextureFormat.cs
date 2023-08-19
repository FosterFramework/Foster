namespace Foster.Framework;

public enum TextureFormat
{
	/// <summary>
	/// Red = 8, Green = 8, Blue = 8, Alpha = 8
	/// </summary>
	R8G8B8A8,

	/// <summary>
	/// Red = 8
	/// </summary>
	R8,

	/// <summary>
	/// Depth = 24, Stencil = 8
	/// </summary>
	Depth24Stencil8,

	/// <summary>
	/// Shorthand for R8G8B8A8
	/// </summary>
	Color = R8G8B8A8
}

public static class TextureFormatExt
{
	public static int Size(this TextureFormat format)
		=> format switch
		{
			TextureFormat.R8G8B8A8 => 4,
			TextureFormat.R8 => 1,
			TextureFormat.Depth24Stencil8 => 4,
			_ => throw new NotImplementedException()
		};
}
