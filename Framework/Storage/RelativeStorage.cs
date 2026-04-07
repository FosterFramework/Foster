namespace Foster.Framework;

/// <summary>
/// Wraps a <see cref="StorageContainer"/> with a relative directory.
/// </summary>
public class RelativeStorage(StorageContainer storage, string path) : StorageContainer
{
	private readonly StorageContainer storage = storage;
	private readonly string relativePath = path;

	public override bool Writable => storage.Writable;

	public override bool DirectoryExists(string path)
		=> storage.DirectoryExists(Path.Combine(relativePath, path));

	public override void Dispose(bool disposing) {}

	public override IEnumerable<string> EnumerateDirectory(string? path = null, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		foreach (var it in storage.EnumerateDirectory(Path.Combine(relativePath, path ?? string.Empty), searchPattern, searchOption))
			yield return Path.GetRelativePath(relativePath, it);
	}

	public override bool FileExists(string path)
		=> storage.FileExists(Path.Combine(relativePath, path));

	public override Stream OpenRead(string path)
		=> storage.OpenRead(Path.Combine(relativePath, path));

	public override bool CreateDirectory(string path)
		=> storage.CreateDirectory(Path.Combine(relativePath, path));

	public override Stream Create(string path)
		=> storage.Create(Path.Combine(relativePath, path));

	public override bool Remove(string path)
		=> storage.Remove(Path.Combine(relativePath, path));
}
