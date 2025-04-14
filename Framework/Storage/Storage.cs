using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using static SDL3.SDL;

namespace Foster.Framework;

/// <summary>
/// Default Storage implementation, for Title and User Storage.
/// </summary>
public sealed class Storage : StorageContainer
{
	private nint handle;
	private readonly bool writable;

	public override bool Writable => writable;

	internal bool Ready => SDL_StorageReady(handle);

	private Storage(nint handle, bool writable)
	{
		this.handle = handle;
		this.writable = writable;
	}

	~Storage()
	{
		Dispose();
	}

	internal static Storage OpenUserStorage(string name)
	{
		var handle = SDL_OpenUserStorage(string.Empty, name, 0);
		return new Storage(handle, true);
	}

	internal static Storage OpenTitleStorage(string? path)
	{
		var handle = SDL_OpenTitleStorage(path!, 0);
		return new Storage(handle, false);
	}

	public override bool DirectoryExists(string path)
	{
		path = Calc.NormalizePath(path);
		return
			handle != nint.Zero &&
			SDL_GetStoragePathInfo(handle, path, out var info) &&
			info.type == SDL_PathType.SDL_PATHTYPE_DIRECTORY;
	}

	private class EnumerateDirectoryUserData(nint handle, string path, Regex? searchPattern, bool recursive)
	{
		public readonly List<string> Entries = [];
		public readonly List<string> SubFolders = [];
		public readonly nint StorageHandle = handle;
		public readonly Regex? SearchPattern = searchPattern;
		public string CurrentPath = path;
		public bool Recursive = recursive;
	}

	public override unsafe IEnumerable<string> EnumerateDirectory(string? path = null, string? searchPattern = null, SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		static bool IsDirectory(nint handle, string path)
		{
			return SDL_GetStoragePathInfo(handle, path, out var info) &&
				info.type == SDL_PathType.SDL_PATHTYPE_DIRECTORY;
		}

		static unsafe void EnumerateSubfolder(string path, EnumerateDirectoryUserData data, nint userdata)
		{
			var start = data.SubFolders.Count;
			data.CurrentPath = path;
			if (!SDL_EnumerateStorageDirectory(data.StorageHandle, path, EnumerateCallback, userdata))
				throw Platform.CreateExceptionFromSDL(nameof(SDL_EnumerateStorageDirectory));
			var end = data.SubFolders.Count;

			if (data.Recursive)
			{
				for (int i = start; i < end; i ++)
					EnumerateSubfolder(data.SubFolders[i], data, userdata);
			}
		}

		static unsafe SDL_EnumerationResult EnumerateCallback(nint userdata, byte* dirname, byte* fname)
		{
			var handle = GCHandle.FromIntPtr(userdata);
			if (handle.Target is not EnumerateDirectoryUserData data)
				return SDL_EnumerationResult.SDL_ENUM_FAILURE;

			string path;
			if (!string.IsNullOrEmpty(data.CurrentPath))
				path = $"{data.CurrentPath}/{Platform.ParseUTF8(new(fname))}";
			else
				path = Platform.ParseUTF8(new(fname));

			// track subfolders if we're recursive
			if (data.Recursive && IsDirectory(data.StorageHandle, path))
				data.SubFolders.Add(path);

			// doesn't pattern match
			if (data.SearchPattern != null && !data.SearchPattern.IsMatch(path))
				return SDL_EnumerationResult.SDL_ENUM_CONTINUE;

			data.Entries.Add(path);
			return SDL_EnumerationResult.SDL_ENUM_CONTINUE;
		}

		if (handle == nint.Zero)
			return [];

		path ??= "";
		path = Calc.NormalizePath(path);

		Regex? pattern = null;
		if (!string.IsNullOrEmpty(searchPattern))
			pattern = new("^" + Regex.Escape(searchPattern).Replace("\\?", ".").Replace("\\*", ".*") + "$");

		var userdata = new EnumerateDirectoryUserData(handle, path, pattern, searchOption == SearchOption.AllDirectories);
		var userdataHandle = GCHandle.Alloc(userdata);
		EnumerateSubfolder(path, userdata, GCHandle.ToIntPtr(userdataHandle));
		userdataHandle.Free();

		return userdata.Entries;
	}

	public override bool FileExists(string path)
	{
		path = Calc.NormalizePath(path);
		return
			handle != nint.Zero &&
			SDL_GetStoragePathInfo(handle, path, out var info) &&
			info.type == SDL_PathType.SDL_PATHTYPE_FILE;
	}

	public unsafe override Stream OpenRead(string path)
	{
		path = Calc.NormalizePath(path);

		// TODO:
		// Is is possible to open a stream somehow with SDL_Storage API?
		// That would be nicer than loading it all in upfront like it is here...

		// get file size
		if (handle == nint.Zero || !SDL_GetStorageFileSize(handle, path, out ulong length))
			throw new Exception($"Failed to open file for reading: {path}");

		// read file
		var buffer = new byte[length];
		fixed (byte* ptr = buffer)
		{
			if (!SDL_ReadStorageFile(handle, path, new nint(ptr), length))
				throw new Exception($"Failed to open file for reading: {path}");
		}

		// return memory stream over result
		return new MemoryStream(buffer);
	}

	public override bool CreateDirectory(string path)
	{
		if (!writable)
			throw new Exception($"{nameof(CreateDirectory)} Failed: the storage can not be written to");
		path = Calc.NormalizePath(path);
		return handle != nint.Zero && SDL_CreateStorageDirectory(handle, path);
	}

	public override bool Remove(string path)
	{
		if (!writable)
			throw new Exception($"{nameof(CreateDirectory)} Failed: the storage can not be written to");
		path = Calc.NormalizePath(path);
		return handle != nint.Zero && SDL_RemoveStoragePath(handle, path);
	}

	public override Stream Create(string path)
	{
		if (!writable)
			throw new Exception($"{nameof(CreateDirectory)} Failed: the storage can not be written to");
		path = Calc.NormalizePath(path);
		return new UserStream(path, handle);
	}

	public override void Dispose()
	{
		if (handle != nint.Zero)
			SDL_CloseStorage(handle);
		handle = nint.Zero;
	}

	/// <summary>
	/// Due to SDL's storage API, we need to write into a memory buffer and then flush
	/// all of that when it's done being written to. If SDL's Storage API ever gets some
	/// kind of stream implementation then we could do that instrad
	/// </summary>
	private class UserStream(string Path, nint Storage) : Stream
	{
		private readonly MemoryStream buffer = new();

		public override bool CanRead => false;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => buffer.Length;

		public override long Position
		{
			get => buffer.Position;
			set => buffer.Position = value;
		}

		public unsafe override void Flush()
		{
			fixed (byte* source = buffer.GetBuffer())
			{
				if (!SDL_WriteStorageFile(Storage, Path, new nint(source), (ulong)buffer.Length))
					throw Platform.CreateExceptionFromSDL(nameof(SDL_WriteStorageFile));
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
			=> this.buffer.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin)
			=> buffer.Seek(offset, origin);

		public override void SetLength(long value)
			=> buffer.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count)
			=> this.buffer.Write(buffer, offset, count);
	}
}
