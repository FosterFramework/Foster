namespace Foster.Framework;

/// <summary>
/// Creates a Storage Container around a Path on the normal OS File System.<br/>
/// For Title and User storage, you should use <see cref="ContentStorage"/>
/// </summary>
public sealed class DirectoryStorage(string path) : StorageContainer
{
	public readonly string Path = path;

	public override bool Writable => true;

	public override void Dispose(bool disposing) {}

	public override bool FileExists(string path)
		=> File.Exists(GetPath(path));

	public override bool DirectoryExists(string path)
		=> Directory.Exists(path);

	public override Stream OpenRead(string path)
		=> File.OpenRead(GetPath(path));

	public override IEnumerable<string> EnumerateDirectory(string? path = null, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
		=> Directory.EnumerateFileSystemEntries(GetPath(path), searchPattern ?? string.Empty, searchOption)
		.Select(it => System.IO.Path.GetRelativePath(Path, it));

	public override bool CreateDirectory(string path)
		=> Directory.CreateDirectory(GetPath(path)).Exists;

	public override Stream Create(string path)
		=> File.Create(GetPath(path));

	public override bool Remove(string path)
	{
		path = GetPath(path);

		if (File.Exists(path))
		{
			File.Delete(path);
			return true;
		}
		else if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
			return true;		
		}

		return false;
	}

	private string GetPath(string? subpath)
	{
		if (string.IsNullOrEmpty(subpath))
			return Path;
		return System.IO.Path.Combine(Path, subpath);		
	}
}