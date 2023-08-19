namespace Foster.Framework;

public static class Log
{
	public static Action<string>? OnInfo;
	public static Action<string>? OnWarn;
	public static Action<string>? OnError;
	
	public static void Info(string message)
	{
		if (OnInfo != null)
			OnInfo(message);
		else
			Console.WriteLine(message);
	}

	public static void Warning(string message)
	{
		if (OnWarn != null)
			OnWarn(message);
		else
			Console.WriteLine(message);
	}

	public static void Error(string message)
	{
		if (OnError != null)
			OnError(message);
		else
			Console.WriteLine(message);
	}
}