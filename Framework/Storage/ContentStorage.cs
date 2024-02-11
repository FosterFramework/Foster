

namespace Foster.Framework.Storage;

/// <summary>
/// Default Content implementation.
/// Implemented using normal .NET IO reading and writing. 
/// Should work well for Desktop Applications.
/// </summary>
public class ContentStorage(string contentPath, bool writable) : Content
{
	public readonly string ContentPath = contentPath;
	public override bool Writable => writable;

	public override IEnumerator<string> EnumerateDirectories(string path, string searchPattern, bool recursive)
	{
		var rec = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		foreach (var dir in Directory.EnumerateDirectories(Path.Combine(ContentPath, path), searchPattern, rec))
			yield return Path.GetRelativePath(ContentPath, dir);
	}

	public override IEnumerator<string> EnumerateFiles(string path, string searchPattern, bool recursive)
	{
		var rec = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
		foreach (var dir in Directory.EnumerateFiles(Path.Combine(ContentPath, path), searchPattern, rec))
			yield return Path.GetRelativePath(ContentPath, dir);
	}

	public override bool DirectoryExists(string path)
		=> Directory.Exists(Path.Combine(ContentPath, path));

	public override bool FileExists(string path)
		=> File.Exists(Path.Combine(ContentPath, path));

	public override Stream OpenRead(string path)
		=> File.OpenRead(Path.Combine(ContentPath, path));

	public override void CreateDirectory(string path)
		=> Directory.CreateDirectory(Path.Combine(ContentPath, path));

	public override void DeleteDirectory(string path)
		=> Directory.Delete(Path.Combine(ContentPath, path));

	public override void DeleteFile(string path)
		=> File.Delete(Path.Combine(ContentPath, path));

	public override Stream Create(string path)
		=> File.Create(Path.Combine(ContentPath, path));
}