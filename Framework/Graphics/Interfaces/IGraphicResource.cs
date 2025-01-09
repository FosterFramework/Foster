namespace Foster.Framework;

/// <summary>
/// A Graphical Resource
/// </summary>
public interface IGraphicResource : IDisposable
{
	public string Name { get; set; }
	public bool IsDisposed { get; }
}