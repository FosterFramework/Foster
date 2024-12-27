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

	internal static Storage OpenTitleStorage()
	{
		var handle = SDL_OpenTitleStorage(null!, 0);
		return new Storage(handle, false);
	}

	public override bool DirectoryExists(string path)
	{
		return 
			handle != nint.Zero && 
			SDL_GetStoragePathInfo(handle, path, out var info) && 
			info.type == SDL_PathType.SDL_PATHTYPE_DIRECTORY;
	}

	public override IEnumerable<string> EnumerateDirectory(string path, string? searchPattern = null)
	{
		if (handle == nint.Zero)
			yield break;
		
		var results = SDL_GlobStorageDirectory(handle, path, searchPattern!, (SDL_GlobFlags)0, out int count);
		if (results == nint.Zero)
			yield break;

		var list = new List<string>();

		unsafe
		{
			for (int i = 0; i < count; i ++)
			{
				var ptr = new nint(((byte**)results)[i]);
				if (ptr == nint.Zero)
					break;

				var filepath = Platform.ParseUTF8(ptr);

				// TODO:
				// This is a bug with SDL! It reports full filepaths instead of relative ones
				// to the storage. Thus we need to return a relative path, which is done by
				// hackily detecting two // in the path
				// Relevant issue: https://github.com/libsdl-org/SDL/issues/11427

				var split = filepath.IndexOf("//");
				if (split < 0)
					split = filepath.IndexOf("\\\\");
				if (split >= 0)
					filepath = filepath[(split + 2)..];

				list.Add(filepath);
			}
		}

		SDL_free(results);

		foreach (var it in list)
			yield return it;
	}

	public override bool FileExists(string path)
	{
		return 
			handle != nint.Zero && 
			SDL_GetStoragePathInfo(handle, path, out var info) && 
			info.type == SDL_PathType.SDL_PATHTYPE_FILE;
	}

	public unsafe override Stream OpenRead(string path)
	{
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
		return handle != nint.Zero && SDL_CreateStorageDirectory(handle, path);
	}

	public override bool Remove(string path)
	{
		return handle != nint.Zero && SDL_RemoveStoragePath(handle, path);
	}

	public override Stream Create(string path)
	{
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
				SDL_WriteStorageFile(Storage, Path, new nint(source), (ulong)buffer.Length);
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
