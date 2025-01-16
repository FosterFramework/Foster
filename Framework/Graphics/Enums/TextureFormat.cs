namespace Foster.Framework;

/// <summary>
/// Available Texture Formats.
/// Some formats are not necessarily supported on every GPU.
/// Check for support with <see cref="GraphicsDevice.IsTextureFormatSupported(TextureFormat)"/> 
/// </summary>
public enum TextureFormat
{
	/// <summary>
	/// 8 bit four channel texture, each component stored in an unsigned normalized format
	/// </summary>
	R8G8B8A8,

	/// <summary>
	/// 8 bit single channel texture, stored in an unsigned normalized format
	/// </summary>
	R8,

	/// <summary>
	/// 24 bit depth, 8 bit stencil texture, with depth stored in an unsigned normalized format and stencil stored in an unsigned int
	/// </summary>
	Depth24Stencil8,

	/// <summary>
	/// 32 bit depth, 8 bit stencil texture, with depth stored in a float format and stencil stored in an unsigned int format
	/// </summary>
	Depth32Stencil8,

	/// <summary>
	/// 16 bit depth texture, stored in an unsigned normalized format
	/// </summary>
	Depth16,

	/// <summary>
	/// 24 bit depth texture, stored in an unsigned normalized format
	/// </summary>
	Depth24,

	/// <summary>
	/// 32 bit depth texture, stored in a float format
	/// </summary>
	Depth32,

	/// <summary>
	/// Shorthand for R8G8B8A8
	/// </summary>
	Color = R8G8B8A8
}

public static class TextureFormatExt
{
	/// <summary>
	/// Gets the size in bytes of a Texture Format
	/// </summary>
	public static int Size(this TextureFormat format)
		=> format switch
		{
			TextureFormat.R8G8B8A8 => 4,
			TextureFormat.R8 => 1,
			TextureFormat.Depth24Stencil8 => 4,
			TextureFormat.Depth32Stencil8 => 5,
			TextureFormat.Depth16 => 2,
			TextureFormat.Depth24 => 3,
			TextureFormat.Depth32 => 4,
			_ => throw new NotImplementedException()
		};

	/// <summary>
	/// Returns true of the given format is a Color format
	/// </summary>
	public static bool IsColorFormat(this TextureFormat format)
		=> format switch
		{
			TextureFormat.R8G8B8A8 => true,
			TextureFormat.R8 => true,
			TextureFormat.Depth24Stencil8 => false,
			TextureFormat.Depth32Stencil8 => false,
			TextureFormat.Depth16 => false,
			TextureFormat.Depth24 => false,
			TextureFormat.Depth32 => false,
			_ => throw new NotImplementedException()
		};

	/// <summary>
	/// Returns true of the given format is a Depth/Stencil format
	/// </summary>
	public static bool IsDepthStencilFormat(this TextureFormat format)
		=> !IsColorFormat(format);
}
