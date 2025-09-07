namespace Foster.Framework;

/// <summary>
/// Specifies the sample count of a texture when used as a <see cref="Target"/>
/// </summary>
public enum SampleCount
{
	/// <summary>
	/// No Multisampling
	/// </summary>
	One,

	/// <summary>
	/// MSAA x2
	/// </summary>
	Two,

	/// <summary>
	/// MSAA x4
	/// </summary>
	Four,

	/// <summary>
	/// MSAA x8
	/// </summary>
	Eight
}