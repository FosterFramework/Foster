using System.Text;

namespace Foster.Framework;

public static class Log
{
	public static readonly StringBuilder Logs = new();

	public delegate void LogFn(ReadOnlySpan<char> text);
	public static LogFn? OnInfo;
	public static LogFn? OnWarn;
	public static LogFn? OnError;

	public static void Info(ReadOnlySpan<char> message)
	{
		Append(message);
		
		if (OnInfo != null)
			OnInfo(message);
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
		
		if (OnWarn != null)
			OnWarn(message);
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
		=> Info(message.AsSpan());

	public static void Error(ReadOnlySpan<char> message)
	{
		Append(message);
		
		if (OnError != null)
			OnError(message);
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
		=> Info(message.AsSpan());

	public static void Append(ReadOnlySpan<char> message)
	{
		lock (Logs)
		{
			Logs.Append(message);
			Logs.Append('\n');
		}
	}
}