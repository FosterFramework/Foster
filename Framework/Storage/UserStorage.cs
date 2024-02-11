namespace Foster.Framework.Storage;

/// <summary>
/// A storage location for user data (save files, etc)
/// </summary>
public static class UserStorage
{
	public static Content Provider { get; set; } = new ContentStorage(App.UserPath, true);

	public static bool FileExists(string relativePath)
		=> Provider.FileExists(relativePath);
	public static bool DirectoryExists(string relativePath)
		=> Provider.DirectoryExists(relativePath);
	public static bool Exists(string name)
		=> Provider.Exists(name);

	public static IEnumerator<string> EnumerateFiles(string path, string searchPattern, bool recursive)
		=> Provider.EnumerateFiles(path, searchPattern, recursive);
	public static IEnumerator<string> EnumerateDirectories(string path, string searchPattern, bool recursive)
		=> Provider.EnumerateDirectories(path, searchPattern, recursive);

	public static void CreateDirectory(string path)
		=> Provider.CreateDirectory(path);
	public static void DeleteDirectory(string path)
		=> Provider.DeleteDirectory(path);
	public static void DeleteFile(string path)
		=> Provider.DeleteFile(path);

	public static Stream OpenRead(string relativePath)
		=> Provider.OpenRead(relativePath);
	public static byte[] ReadAllBytes(string relativePath)
		=> Provider.ReadAllBytes(relativePath);
	public static string ReadAllText(string relativePath)
		=> Provider.ReadAllText(relativePath);

	public static Stream Create(string path)
		=> Provider.Create(path);
	public static void WriteAllBytes(string path, byte[] bytes)
		=> Provider.WriteAllBytes(path, bytes);
	public static void WriteAllText(string path, string text)
		=> Provider.WriteAllText(path, text);
}
