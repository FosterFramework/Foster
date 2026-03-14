namespace Foster.Framework;

/// <summary>
/// Optional Texture Flags used when creating new Textures
/// </summary>
[Flags]
public enum TextureFlags
{
	None = 0,

	/// <summary>
	/// Allows the Texture to be used during Compute Storage Reads
	/// </summary>
	ComputeRead = 1 << 0,

	/// <summary>
	/// Allows the Texture to be used during Compute Storage Writes
	/// </summary>
	ComputeWrite = 1 << 1,
}