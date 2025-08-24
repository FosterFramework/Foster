using System.Text;

namespace Foster.Framework;

/// <summary>
/// A File Storage interface
/// </summary>
public abstract class StorageContainer : IDisposable
{
	/// <summary>
	/// If this Storage Container can be written to
	/// </summary>
	public abstract bool Writable { get; }

	/// <summary>
	/// Checks if the given path is a File or Directory
	/// </summary>
	public bool Exists(string path)
		=> FileExists(path) || DirectoryExists(path);

	/// <summary>
	/// Checks if the given File Exists within the Storage Container
	/// </summary>
	public abstract bool FileExists(string path);

	/// <summary>
	/// Checks if the given Directory exists within the Storage Container
	/// </summary>
	public abstract bool DirectoryExists(string path);

	/// <summary>
	/// Opens a Stream for reading
	/// </summary>
	public abstract Stream OpenRead(string path);

	/// <summary>
	/// Enumerates over a path and finds all files and directories that match the search pattern
	/// </summary>
	/// <param name="path">The directory to enumerate</param>
	/// <param name="searchPattern">The search pattern to filter with, which may contain wildcards '*' (match everything) and '?' (match one character).</param>
	/// <param name="searchOption">Whether to search only the given directory, or to search recursively through all directories</param>
	public abstract IEnumerable<string> EnumerateDirectory(
		string? path = null,
		string? searchPattern = null,
		SearchOption searchOption = SearchOption.TopDirectoryOnly
	);

	/// <summary>
	/// Creates a new Directory at the given Path in the Storage Container, if the Storage Container is Writable
	/// </summary>
	public virtual bool CreateDirectory(string path)
		=> throw new InvalidOperationException("This type of Storage Container is not Writable");

	/// <summary>
	/// Opens a Stream to create a new file at the given Path in the Storage Container, if the Storage Container is Writable
	/// </summary>
	public virtual Stream Create(string path)
		=> throw new InvalidOperationException("This type of Storage Container is not Writable");

	/// <summary>
	/// Deletes an existing File or Directory in the Storage Container, if the Storage Container is Writable
	/// </summary>
	public virtual bool Remove(string path)
		=> throw new InvalidOperationException("This type of Storage Container is not Writable");

	/// <summary>
	/// Reads all the contents of the File in the Storage Container at the given path and returns it as a byte array.
	/// </summary>
	public byte[] ReadAllBytes(string path)
	{
		using var stream = OpenRead(path);
		return Calc.ReadAllBytes(stream);
	}

	/// <summary>
	/// Reads all the contents of the File in the Storage Container at the given path and returns it as a string.
	/// </summary>
	public string ReadAllText(string path)
	{
		using var stream = OpenRead(path);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	/// <summary>
	/// Writes all the contents of the byte array to the File in the Storage Container at the given path.
	/// </summary>
	public void WriteAllBytes(string path, byte[] bytes)
	{
		using var stream = Create(path);
		stream.Write(bytes);
		stream.Flush();
	}

	/// <summary>
	/// Writes all the contents of the string to the File in the Storage Container at the given path.
	/// </summary>
	public void WriteAllText(string path, string text)
	{
		using var stream = Create(path);
		stream.Write(Encoding.UTF8.GetBytes(text));
		stream.Flush();
	}

	public abstract void Dispose();
}