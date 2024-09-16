using System.Runtime.InteropServices;
using static Foster.Framework.SDL3;

namespace Foster.Framework;

/// <summary>
/// Shows OS File Dialogs if the platform supports it.
/// Note that all File Dialog operations are only available from the Main thread.
/// </summary>
public static class FileDialog
{
	public enum Result
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
	public delegate void Callback(string[] paths, Result result);

	/// <summary>
	/// Callback with the resulting file path the user selected
	/// </summary>
	public delegate void CallbackSingleFile(string path, Result result);
	
	/// <summary>
	/// File Filter
	/// </summary>
	public readonly record struct Filter(string Name, string Pattern);

	/// <summary>
	/// Shows an "Open File" Dialog
	/// </summary>
	public static void OpenFile(Callback callback, bool allowMany = false)
		=> OpenFile(callback, [], null, allowMany);

	/// <summary>
	/// Shows an "Open File" Dialog
	/// </summary>
	public static unsafe void OpenFile(Callback callback, ReadOnlySpan<Filter> filters, string? defaultLocation = null, bool allowMany = false)
		=> ShowDialogOnMainThread(Modes.OpenFile, callback, filters, defaultLocation, allowMany);

	/// <summary>
	/// Shows an "Open File" Dialog
	/// </summary>
	public static void OpenFolder(Callback callback, bool allowMany = false)
		=> OpenFolder(callback, null, allowMany);

	/// <summary>
	/// Shows an "Open Folder" Dialog
	/// </summary>
	public static void OpenFolder(Callback callback, string? defaultLocation = null, bool allowMany = false)
		=> ShowDialogOnMainThread(Modes.OpenFolder, callback, [], defaultLocation, allowMany);

	/// <summary>
	/// Shows a "Save File" Dialog
	/// </summary>
	public static void SaveFile(CallbackSingleFile callback)
		=> SaveFile(callback, [], null);

	/// <summary>
	/// Shows a "Save File" Dialog
	/// </summary>
	public static unsafe void SaveFile(CallbackSingleFile callback, ReadOnlySpan<Filter> filters, string? defaultLocation = null)
	{
		void Singular(string[] files, Result result)
			=> callback(files.FirstOrDefault() ?? string.Empty, result);
		
		ShowDialogOnMainThread(Modes.SaveFile, Singular, filters, defaultLocation, false);
	}

	private enum Modes
	{
		OpenFile,
		OpenFolder,
		SaveFile
	}

	private static unsafe void ShowDialogOnMainThread(
		Modes mode,
		Callback callback,
		ReadOnlySpan<Filter> filters,
		string? defaultLocation,
		bool allowMany)
	{
		[UnmanagedCallersOnly]
		static void CallbackFromSDL(nint userdata, nint files, int filter)
		{
			// get actual callback, release held handle
			var handle = GCHandle.FromIntPtr(userdata);
			var callback = handle.Target as Callback;
			handle.Free();

			// get list of files (list of utf8)
			var ptr = (byte**)files;

			// null means there was a system error
			if (ptr == null)
			{
				Log.Error(Platform.GetErrorFromSDL());
				callback?.Invoke([], Result.Failed);
			}
			// empty list means the action was cancelled by the user
			else if (ptr[0] == null)
			{
				callback?.Invoke([], Result.Cancelled);
			}
			// otherwise return the files
			else
			{
				var result = new List<string>();
				for (int i = 0; ptr[i] != null; i ++)
					result.Add(Platform.ParseUTF8(new nint(ptr[i])));
				callback?.Invoke([..result], Result.Success);
			}
		}

		// as per SDL docs, these methods can only be called from the main thread
		if (App.IsMainThread())
			throw new Exception("Showing File Dialogs is only supported from the Main thread");

		// fallback to where we think the application is running from
		if (string.IsNullOrEmpty(defaultLocation))
			defaultLocation = Directory.GetCurrentDirectory();

		// get UTF8 string data for SDL
		var defaultLocationUtf8 = Platform.ToUTF8(defaultLocation);
		var filtersUtf8 = stackalloc SDL_DialogFileFilter[filters.Length];
		for (int i = 0; i < filters.Length; i ++)
		{
			filtersUtf8[i].name = Platform.ToUTF8(filters[i].Name);
			filtersUtf8[i].pattern = Platform.ToUTF8(filters[i].Pattern);
		}

		// create a pointer to our user callback so that SDL can pass it around
		var handle = GCHandle.Alloc(callback);
		var userdata = GCHandle.ToIntPtr(handle);

		// open file dialog
		switch (mode)
		{
			case Modes.OpenFile:
				SDL_ShowOpenFileDialog(&CallbackFromSDL, userdata, App.Width, filtersUtf8, filters.Length, defaultLocationUtf8, allowMany);
				break;
			case Modes.SaveFile:
				SDL_ShowSaveFileDialog(&CallbackFromSDL, userdata, App.Width, filtersUtf8, filters.Length, defaultLocationUtf8);
				break;
			case Modes.OpenFolder:
				SDL_ShowOpenFolderDialog(&CallbackFromSDL, userdata, App.Width, defaultLocationUtf8, allowMany);
				break;
		}

		// clear UTF8 string memory
		foreach (var it in new Span<SDL_DialogFileFilter>(filtersUtf8, filters.Length))
		{
			Platform.FreeUTF8(it.name);
			Platform.FreeUTF8(it.pattern);
		}
		Platform.FreeUTF8(defaultLocationUtf8);
	}
}