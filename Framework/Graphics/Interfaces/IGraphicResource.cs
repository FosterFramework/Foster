namespace Foster.Framework;

/// <summary>
/// A Graphical Resource
/// </summary>
public interface IGraphicResource : IDisposable
{
	/// <summary>
	/// An optional Name to track the Graphical resource
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// If the Resource has been disposed
	/// </summary>
	public bool IsDisposed { get; }
}