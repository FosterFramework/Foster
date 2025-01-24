using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SDL3.SDL;

namespace Foster.Framework;

/// <summary>
/// Application File System Utility Module
/// </summary>
public sealed class FileSystem
{
	private readonly App app;

	internal FileSystem(App app)
	{
		this.app = app;
	}

	/// <summary>
	/// Opens a user storage, and invokes a callback with the storage object when ready.
	/// Note that User Storage should be disposed as soon as you're done using it.
	/// It should not be left open for an extended amount of time.
	/// User Storage is intended for dynamic save files and application settings.
	/// </summary>
	public void OpenUserStorage(Action<Storage> onReady)
	{
		if (app.Disposed)
			throw app.DisposedException;

		HandleOpenCallback(app, Storage.OpenUserStorage(app.Name), onReady);
	}

	/// <summary>
	/// Opens a user storage and awaits until it is ready.
	/// This should not be awaited on from the Main thread.
	/// Note that this User Storage should be disposed as soon as you're done using it.
	/// It should not be left open for an extended amount of time.
	/// User Storage is intended for dynamic save files and application settings.
	/// </summary>
	public async Task<Storage> OpenUserStorageAsync()
	{
		if (app.Disposed)
			throw app.DisposedException;

		return await HandleOpenAsync(Storage.OpenUserStorage(app.Name));
	}

	/// <summary>
	/// Opens a application title storage, and invokes a callback with the storage object when ready.
	/// Title Storage is intended for Game Data and Assets.
	/// </summary>
	public void OpenTitleStorage(Action<Storage> onReady)
		=> OpenTitleStorage(null, onReady);

	/// <summary>
	/// Opens a application title storage, and invokes a callback with the storage object when ready.
	/// Title Storage is intended for Game Data and Assets.
	/// </summary>
	public void OpenTitleStorage(string? path, Action<Storage> onReady)
	{
		if (app.Disposed)
			throw app.DisposedException;

		HandleOpenCallback(app, Storage.OpenTitleStorage(path), onReady);
	}

	/// <summary>
	/// Opens a application title storage and awaits until it is ready.
	/// This should not be awaited on from the Main thread.
	/// Title Storage is intended for Game Data and Assets.
	/// </summary>
	public async Task<Storage> OpenTitleStorageAsync(string? path = null)
	{
		if (app.Disposed)
			throw app.DisposedException;
			
		return await HandleOpenAsync(Storage.OpenTitleStorage(path));
	}

	private static void HandleOpenCallback(App app, Storage storage, Action<Storage> onReady)
	{
		if (storage.Ready)
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
				while (!storage.Ready)
					Thread.Sleep(100);
				app.RunOnMainThread(() => onReady(storage));
			});
		}
	}

	private static async Task<Storage> HandleOpenAsync(Storage storage)
	{
		if (storage.Ready)
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
				while (!storage.Ready)
					Thread.Sleep(100);
			});

			return storage;
		}
	}
	
	public enum DialogResult
	{
		/// <summary>
		/// The user successfully selected files.
		/// </summary>
		Success,

		/// <summary>
		/// The user cancelled the selection.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The user tried to select files but there was a system error.
		/// </summary>
		Failed
	}

	/// <summary>
	/// Callback with resulting file paths the user selected
	/// </summary>
	public delegate void DialogCallback(string[] paths, DialogResult result);

	/// <summary>
	/// Callback with the resulting file path the user selected
	/// </summary>
	public delegate void DialogCallbackSingleFile(string path, DialogResult result);
	
	/// <summary>
	/// Dialog File Filter
	/// </summary>
	public readonly record struct DialogFilter(string Name, string Pattern);

	/// <summary>
	/// Shows an "Open File" Dialog
	/// </summary>
	public void OpenFileDialog(DialogCallback callback, bool allowMany = false)
		=> OpenFileDialog(callback, [], null, allowMany);

	/// <summary>
	/// Shows an "Open File" Dialog
	/// </summary>
	public unsafe void OpenFileDialog(DialogCallback callback, DialogFilter[] filters, string? defaultLocation = null, bool allowMany = false)
		=> ShowFileDialog(new(DialogModes.OpenFile, callback, filters, defaultLocation, allowMany));

	/// <summary>
	/// Shows an "Open File" Dialog
	/// </summary>
	public void OpenFolderDialog(DialogCallback callback, bool allowMany = false)
		=> OpenFolderDialog(callback, null, allowMany);

	/// <summary>
	/// Shows an "Open Folder" Dialog
	/// </summary>
	public void OpenFolderDialog(DialogCallback callback, string? defaultLocation = null, bool allowMany = false)
		=> ShowFileDialog(new(DialogModes.OpenFolder, callback, [], defaultLocation, allowMany));

	/// <summary>
	/// Shows a "Save File" Dialog
	/// </summary>
	public void SaveFileDialog(DialogCallbackSingleFile callback)
		=> SaveFileDialog(callback, [], null);

	/// <summary>
	/// Shows a "Save File" Dialog
	/// </summary>
	public unsafe void SaveFileDialog(DialogCallbackSingleFile callback, DialogFilter[] filters, string? defaultLocation = null)
	{
		void Singular(string[] files, DialogResult result)
			=> callback(files.FirstOrDefault() ?? string.Empty, result);
		
		ShowFileDialog(new(DialogModes.SaveFile, Singular, filters, defaultLocation, false));
	}

	private enum DialogModes
	{
		OpenFile,
		OpenFolder,
		SaveFile
	}

	private readonly record struct DialogProperties(
		DialogModes Mode,
		DialogCallback Callback,
		DialogFilter[] Filters,
		string? DefaultLocation,
		bool AllowMany
	);
	
	private readonly record struct DialogUserData(
		App App,
		DialogCallback Callback
	);

	private unsafe void ShowFileDialog(DialogProperties properties)
	{
		[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
		static void CallbackFromSDL(nint userdata, nint files, int filter)
		{
			// get actual callback, release held handle
			var handle = GCHandle.FromIntPtr(userdata);
			var data = (DialogUserData)handle.Target!;
			handle.Free();

			string[] paths;
			DialogResult result;

			// get list of files (list of utf8)
			var ptr = (byte**)files;

			// null means there was a system error
			if (ptr == null)
			{
				Log.Error(SDL_GetError());
				paths = [];
				result = DialogResult.Failed;
			}
			// empty list means the action was cancelled by the user
			else if (ptr[0] == null)
			{
				paths = [];
				result = DialogResult.Cancelled;
			}
			// otherwise return the files
			else
			{
				var list = new List<string>();
				for (int i = 0; ptr[i] != null; i ++)
					list.Add(Platform.ParseUTF8(new nint(ptr[i])));
				paths = [..list];
				result = DialogResult.Success;
			}

			// the callback can be invoked from any thread as per the SDL docs
			// but for ease-of-use we want it to always be called from the Main thread
			data.App.RunOnMainThread(() => data.Callback?.Invoke(paths, result));
		}

		static void Show(App app, DialogProperties properties)
		{
			// fallback to where we think the application is running from
			string? defaultLocation = properties.DefaultLocation;
			if (string.IsNullOrEmpty(defaultLocation))
				defaultLocation = Directory.GetCurrentDirectory();

			// get UTF8 string data for SDL
			Span<SDL_DialogFileFilter> filtersUtf8 = stackalloc SDL_DialogFileFilter[properties.Filters.Length];
			for (int i = 0; i < properties.Filters.Length; i ++)
			{
				filtersUtf8[i].name = (byte*)Platform.AllocateUTF8(properties.Filters[i].Name);
				filtersUtf8[i].pattern = (byte*)Platform.AllocateUTF8(properties.Filters[i].Pattern);
			}

			// create a pointer to our user callback so that SDL can pass it around
			var data = new DialogUserData(app, properties.Callback);
			var handle = GCHandle.Alloc(data);
			var userdata = GCHandle.ToIntPtr(handle);

			// open file dialog
			switch (properties.Mode)
			{
				case DialogModes.OpenFile:
					SDL_ShowOpenFileDialog(
						&CallbackFromSDL, userdata, app.Window.Handle, filtersUtf8, 
						properties.Filters.Length, defaultLocation, properties.AllowMany);
					break;
				case DialogModes.SaveFile:
					SDL_ShowSaveFileDialog(
						&CallbackFromSDL, userdata, app.Window.Handle, filtersUtf8,
						properties.Filters.Length, defaultLocation);
					break;
				case DialogModes.OpenFolder:
					SDL_ShowOpenFolderDialog(
						&CallbackFromSDL, userdata, app.Window.Handle, 
						defaultLocation, properties.AllowMany);
					break;
			}

			// clear UTF8 string memory
			foreach (var it in filtersUtf8)
			{
				Platform.FreeUTF8(new nint(it.name));
				Platform.FreeUTF8(new nint(it.pattern));
			}
		}

		if (app.Disposed)
			throw app.DisposedException;
		
		// SDL docs say that showing file dialogs must be invoked from the Main Thread
		app.RunOnMainThread(() => Show(app, properties));
	}
}