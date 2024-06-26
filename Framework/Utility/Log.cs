using System.Text;

namespace Foster.Framework;

public static class Log
{
	public delegate void Fn(ReadOnlySpan<char> text);

	[Obsolete("Use Log.GetHistory")]
	public static StringBuilder Logs => logs;
	[Obsolete("Use Log.SetCallbacks")]
	public static Fn? OnInfo { get => onInfo; set => onInfo = value; }
	[Obsolete("Use Log.SetCallbacks")]
	public static Fn? OnWarn { get => onWarn; set => onWarn = value; }
	[Obsolete("Use Log.SetCallbacks")]
	public static Fn? OnError { get => onError; set => onError = value; }

	private static Fn? onInfo;
	private static Fn? onWarn;
	private static Fn? onError;
	private static readonly StringBuilder logs = new();

	/// <summary>
	/// Sets optional custom logging callbacks
	/// </summary>
	public static void SetCallbacks(Fn? onInfo, Fn? onWarn, Fn? onError)
	{
		Log.onInfo = onInfo;
		Log.onWarn = onWarn;
		Log.onError = onError;
	}

	public static void Info(ReadOnlySpan<char> message)
	{
		Append(message);

		if (onInfo != null)
			onInfo(message);
		else
			Console.Out.WriteLine(message);
	}

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

	public static unsafe void Info(IntPtr utf8)
	{
		byte* ptr = (byte*)utf8.ToPointer();
		int len = 0;
		while (ptr[len] != 0)
			len++;
		Info(new ReadOnlySpan<byte>(ptr, len));
	}

	public static void Info(string message)
		=> Info(message.AsSpan());

	public static void Info(object? obj)
		=> Info(obj?.ToString() ?? "null");

	public static void Warning(ReadOnlySpan<char> message)
	{
		Append(message);

		if (onWarn != null)
			onWarn(message);
		else
			Console.Out.WriteLine(message);
	}

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

	public static unsafe void Warning(IntPtr utf8)
	{
		byte* ptr = (byte*)utf8.ToPointer();
		int len = 0;
		while (ptr[len] != 0)
			len++;
		Warning(new ReadOnlySpan<byte>(ptr, len));
	}

	public static void Warning(string message)
		=> Warning(message.AsSpan());

	public static void Error(ReadOnlySpan<char> message)
	{
		Append(message);

		if (onError != null)
			onError(message);
		else
			Console.Out.WriteLine(message);
	}

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

	public static unsafe void Error(IntPtr utf8)
	{
		byte* ptr = (byte*)utf8.ToPointer();
		int len = 0;
		while (ptr[len] != 0)
			len++;
		Error(new ReadOnlySpan<byte>(ptr, len));
	}

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
	/// Iterates over all the chunks in the log history, calling the given method for each entry.
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
