using System.Text;

namespace Foster.Framework;

/// <summary>
/// Logging Utility Methods
/// </summary>
public static class Log
{
	/// <summary>
	/// A Logging Function
	/// </summary>
	public delegate void Fn(ReadOnlySpan<char> text);

	private static Fn? onInfo;
	private static Fn? onWarn;
	private static Fn? onError;
	private static readonly StringBuilder logs = new();

	/// <summary>
	/// Sets optional custom logging callbacks.
	/// Callbacks are not guaranteed to be called on the Main thread.
	/// </summary>
	public static void SetCallbacks(Fn? onInfo, Fn? onWarn, Fn? onError)
	{
		Log.onInfo = onInfo;
		Log.onWarn = onWarn;
		Log.onError = onError;
	}

	/// <summary>
	/// Append a string to the Log
	/// </summary>
	public static void Info(ReadOnlySpan<char> message)
	{
		Append(message);

		if (onInfo != null)
			onInfo(message);
		else
			Console.Out.WriteLine(message);
	}


	/// <summary>
	/// Append a UTF8 string to the Log
	/// </summary>
	public static void Info(ReadOnlySpan<byte> utf8)
	{
		var len = Encoding.UTF8.GetCharCount(utf8);
		if (len < 1024)
		{
			Span<char> chars = stackalloc char[len];
			Encoding.UTF8.GetChars(utf8, chars);
			Info(chars);
		}
		else
		{
			Info(Encoding.UTF8.GetString(utf8));
		}
	}

	/// <summary>
	/// Append a UTF8 null-terminated string to the Log
	/// </summary>
	public static unsafe void Info(IntPtr utf8)
	{
		byte* ptr = (byte*)utf8.ToPointer();
		int len = 0;
		while (ptr[len] != 0)
			len++;
		Info(new ReadOnlySpan<byte>(ptr, len));
	}

	/// <summary>
	/// Append an integer value to the Log
	/// </summary>
	public static void Info(int value) => Info(value.ToString());

	/// <summary>
	/// Append a string message to the Log
	/// </summary>
	public static void Info(string message) => Info(message.AsSpan());

	/// <summary>
	/// Append an object value to the Log
	/// </summary>
	public static void Info(object? obj) => Info(obj?.ToString() ?? "null");

	/// <summary>
	/// Append a Warning string to the Log.
	/// </summary>
	public static void Warning(ReadOnlySpan<char> message)
	{
		Append(message);

		if (onWarn != null)
			onWarn(message);
		else
			Console.Out.WriteLine(message);
	}

	/// <summary>
	/// Append a UTF8 Warning string to the Log.
	/// </summary>
	public static void Warning(ReadOnlySpan<byte> utf8)
	{
		var len = Encoding.UTF8.GetCharCount(utf8);
		if (len < 1024)
		{
			Span<char> chars = stackalloc char[len];
			Encoding.UTF8.GetChars(utf8, chars);
			Warning(chars);
		}
		else
		{
			Warning(Encoding.UTF8.GetString(utf8));
		}
	}

	/// <summary>
	/// Append a UTF8 null-terminated Warning string to the Log.
	/// </summary>
	public static unsafe void Warning(IntPtr utf8)
	{
		byte* ptr = (byte*)utf8.ToPointer();
		int len = 0;
		while (ptr[len] != 0)
			len++;
		Warning(new ReadOnlySpan<byte>(ptr, len));
	}

	/// <summary>
	/// Append a Warning to the Log
	/// </summary>
	public static void Warning(string message)
		=> Warning(message.AsSpan());

	/// <summary>
	/// Append an Error string to the Log.
	/// </summary>
	public static void Error(ReadOnlySpan<char> message)
	{
		Append(message);

		if (onError != null)
			onError(message);
		else
			Console.Out.WriteLine(message);
	}

	/// <summary>
	/// Append a UTF8 Error string to the Log.
	/// </summary>
	public static void Error(ReadOnlySpan<byte> utf8)
	{
		var len = Encoding.UTF8.GetCharCount(utf8);
		if (len < 1024)
		{
			Span<char> chars = stackalloc char[len];
			Encoding.UTF8.GetChars(utf8, chars);
			Error(chars);
		}
		else
		{
			Error(Encoding.UTF8.GetString(utf8));
		}
	}

	/// <summary>
	/// Append a UTF8 null-terminated Error string to the Log.
	/// </summary>
	public static unsafe void Error(IntPtr utf8)
	{
		byte* ptr = (byte*)utf8.ToPointer();
		int len = 0;
		while (ptr[len] != 0)
			len++;
		Error(new ReadOnlySpan<byte>(ptr, len));
	}

	/// <summary>
	/// Append an Error to the Log
	/// </summary>
	public static void Error(string message)
		=> Error(message.AsSpan());

	/// <summary>
	/// Appends a line to the Log
	/// </summary>
	public static void Append(ReadOnlySpan<char> message)
	{
		lock (logs)
		{
			logs.Append(message);
			logs.Append('\n');
		}
	}

	/// <summary>
	/// Constructs a string of the Log History
	/// </summary>
	public static string GetHistory()
	{
		string history;
		lock (logs)
			history = logs.ToString();
		return history;
	}

	/// <summary>
	/// Iterates over all the log data, calling the given method for each entry.
	/// </summary>
	public static void GetHistory(Action<ReadOnlyMemory<char>> readChunk)
	{
		lock (logs)
		{
			foreach (var it in logs.GetChunks())
				readChunk(it);
		}
	}

	/// <summary>
	/// Clears the Log history
	/// </summary>
	public static void ClearHistory()
	{
		lock (logs)
			logs.Clear();
	}
}
