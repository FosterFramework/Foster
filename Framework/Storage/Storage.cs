using static SDL3.SDL;

namespace Foster.Framework;

/// <summary>
/// Default Storage implementation, for Title and User Storage.
/// Call <see cref="OpenUserStorage"/> or <see cref="OpenTitleStorage"/> to begin using the Storage.
/// </summary>
public sealed class Storage : StorageContainer
{
	private nint handle;
	private readonly bool writable;

	public override bool Writable => writable;

	private Storage(nint handle, bool writable)
	{
		this.handle = handle;
		this.writable = writable;
	}

	~Storage()
	{
		Dispose();
	}

	/// <summary>
	/// Opens a user storage, and invokes a callback with the storage object when ready.
	/// Note that User Storage should be disposed as soon as you're done using it.
	/// It should not be left open for an extended amount of time.
	/// User Storage is intended for dynamic save files and application settings.
	/// </summary>
	public static void OpenUserStorage(Action<Storage> onReady)
	{
		if (!App.Running)
			throw new Exception("Application must be running to use Storage");
		
		var handle = SDL_OpenUserStorage(string.Empty, App.Name, 0);
		HandleOpenCallback(new Storage(handle, true), onReady);
	}

	/// <summary>
	/// Opens a user storage and awaits until it is ready.
	/// This should not be awaited on from the Main thread.
	/// Note that this User Storage should be disposed as soon as you're done using it.
	/// It should not be left open for an extended amount of time.
	/// User Storage is intended for dynamic save files and application settings.
	/// </summary>
	public static async Task<Storage> OpenUserStorageAsync()
	{
		if (!App.Running)
			throw new Exception("Application must be running to use Storage");

		var handle = SDL_OpenUserStorage(string.Empty, App.Name, 0);
		return await HandleOpenAsync(new Storage(handle, true));
	}

	/// <summary>
	/// Opens a application title storage, and invokes a callback with the storage object when ready.
	/// Title Storage is intended for static Game Data and Assets.
	/// </summary>
	public static void OpenTitleStorage(Action<Storage> onReady)
	{
		var handle = SDL_OpenTitleStorage(null!, 0);
		HandleOpenCallback(new Storage(handle, false), onReady);
	}

	/// <summary>
	/// Opens a application title storage and awaits until it is ready.
	/// This should not be awaited on from the Main thread.
	/// Title Storage is intended for static Game Data and Assets.
	/// </summary>
	public static async Task<Storage> OpenTitleStorageAsync()
	{
		if (App.IsMainThread())
			throw new Exception("Do not Open Storage and await on it from the Main thread");

		var handle = SDL_OpenTitleStorage(null!, 0);
		return await HandleOpenAsync(new Storage(handle, false));
	}

	private static void HandleOpenCallback(Storage storage, Action<Storage> onReady)
	{
		if (SDL_StorageReady(storage.handle))
		{
			onReady(storage);
		}
		else
		{
			// we wait off the main thread for StorageReady.
			// the SDL docs say not to spinwait on this so that the event loop still receives messages:
			// https://wiki.libsdl.org/SDL3/SDL_StorageReady#remarks
			Task.Run(() =>
			{
				while (!SDL_StorageReady(storage.handle))
					Thread.Sleep(100);
				App.RunOnMainThread(() => onReady(storage));
			});
		}
	}

	private static async Task<Storage> HandleOpenAsync(Storage storage)
	{
		if (App.IsMainThread())
			throw new Exception("Do not Open Storage and await on it from the Main thread");

		if (SDL_StorageReady(storage.handle))
		{
			return storage;
		}
		else
		{
			// we wait off the main thread for StorageReady.
			// the SDL docs say not to spinwait on this so that the event loop still receives messages:
			// https://wiki.libsdl.org/SDL3/SDL_StorageReady#remarks
			await Task.Run(() =>
			{
				while (!SDL_StorageReady(storage.handle))
					Thread.Sleep(100);
			});

			return storage;
		}
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
