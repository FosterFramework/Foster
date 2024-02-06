namespace Foster.Framework.Storage;

/// <summary>
/// A Content that may also be written to.
/// </summary>
public class WritableContent : Content
{
	public WritableContent() { }
	public WritableContent(string currentDirectory) : base(currentDirectory) { }

	#region Directory
	public virtual void CreateDirectory(string path)
	{
		Directory.CreateDirectory(Path.Combine(CurrentDirectory, path));
	}

	public virtual void DeleteDirectory(string path, bool recursive)
	{
		Directory.Delete(Path.Combine(CurrentDirectory, path), recursive);
	}

	public virtual void DeleteFile(string path)
	{
		File.Delete(Path.Combine(CurrentDirectory, path));
	}

	#endregion

	#region File

	public virtual Stream OpenWrite(string path)
	{
		return File.OpenWrite(Path.Combine(CurrentDirectory, path));
	}

	public virtual void WriteAllBytes(string path, byte[] bytes)
	{
		File.WriteAllBytes(Path.Combine(CurrentDirectory, path), bytes);
	}

	public virtual void WriteAllText(string path, string text)
	{
		File.WriteAllText(Path.Combine(CurrentDirectory, path), text);
	}

	#endregion
}
