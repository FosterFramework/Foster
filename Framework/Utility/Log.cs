using System.Text;

namespace Foster.Framework;

public static class Log
{
	public static readonly StringBuilder Logs = new();

	public static Action<string>? OnInfo;
	public static Action<string>? OnWarn;
	public static Action<string>? OnError;
	
	public static void Info(string message)
	{
		Append(message);
		
		if (OnInfo != null)
			OnInfo(message);
		else
			Console.WriteLine(message);
	}

	public static void Warning(string message)
	{
		Append($"Warning: {message}");

		if (OnWarn != null)
			OnWarn(message);
		else
			Console.WriteLine(message);
	}

	public static void Error(string message)
	{
		Append($"Error: {message}");

		if (OnError != null)
			OnError(message);
		else
			Console.WriteLine(message);
	}

	public static void Append(string message)
	{
		lock (Logs)
		{
			Logs.AppendLine(message);
		}
	}
}