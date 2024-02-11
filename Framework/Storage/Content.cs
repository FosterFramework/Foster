using System.Text;

namespace Foster.Framework.Storage;

public abstract class Content
{
	/// <summary>
	/// If this Content can be written to
	/// </summary>
	public abstract bool Writable { get; }

	/// <summary>
	/// Checks if the given File Exists within the Content
	/// </summary>
	public abstract bool FileExists(string path);

	/// <summary>
	/// Checks if the given Directory exists within the Content
	/// </summary>
	public abstract bool DirectoryExists(string path);

	/// <summary>
	/// Checks if the given path is a File or Directory
	/// </summary>
	public bool Exists(string path)
		=> FileExists(path) || DirectoryExists(path);
	public abstract Stream OpenRead(string path);

	/// <summary>
	/// Enumerates over a path and finds all files that match the search pattern
	/// </summary>
	public abstract IEnumerator<string> EnumerateFiles(string path, string searchPattern, bool recursive);

	/// <summary>
	/// Enumerates over a path and finds all directories that match the search pattern
	/// </summary>
	public abstract IEnumerator<string> EnumerateDirectories(string path, string searchPattern, bool recursive);

	/// <summary>
	/// Reads all the contents of the File in the Content at the given path and returns it as a byte array.
	/// </summary>
	public byte[] ReadAllBytes(string path)
	{
		using var stream = OpenRead(path);
		byte[] buffer = new byte[stream.Length];
		stream.Read(buffer);
		return buffer;
	}

	/// <summary>
	/// Reads all the contents of the File in the Content at the given path and returns it as a string.
	/// </summary>
	public string ReadAllText(string path)
	{
		using var stream = OpenRead(path);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	/// <summary>
	/// Creates a new Directory at the given Path in the Content, if the Content is Writable
	/// </summary>
	public virtual void CreateDirectory(string path)
		=> throw new InvalidOperationException("This type of Content is not Writable");

	/// <summary>
	/// Deletes an existing Directory at the given Path in the Content, if the Content is Writable
	/// </summary>
	public virtual void DeleteDirectory(string path)
		=> throw new InvalidOperationException("This type of Content is not Writable");

	/// <summary>
	/// Creates an existing File at the given Path in the Content, if the Content is Writable
	/// </summary>
	public virtual void DeleteFile(string path)
		=> throw new InvalidOperationException("This type of Content is not Writable");

	/// <summary>
	/// Opens a Stream to create a new file at the given Path in the Content, if the Content is Writable
	/// </summary>
	public virtual Stream Create(string path)
		=> throw new InvalidOperationException("This type of Content is not Writable");


	/// <summary>
	/// Writes all the contents of the byte array to the File in the Content at the given path.
	/// </summary>
	public void WriteAllBytes(string path, byte[] bytes)
	{
		using var stream = Create(path);
		stream.Write(bytes);
		stream.Flush();
	}

	/// <summary>
	/// Writes all the contents of the string to the File in the Content at the given path.
	/// </summary>
	public void WriteAllText(string path, string text)
	{
		using var stream = OpenRead(path);
		stream.Write(Encoding.UTF8.GetBytes(text));
		stream.Flush();
	}
}