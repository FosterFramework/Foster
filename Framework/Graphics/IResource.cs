namespace Foster.Framework;

public interface IResource : IDisposable
{
	public string Name { get; set; }
	public bool IsDisposed { get; }
}