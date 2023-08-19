namespace Foster.Framework;

public static class Strings
{
	public static string NormalizePath(string a, string b)
	{
		return NormalizePath(Path.Join(a, b));
	}

	public static string NormalizePath(string a, string b, string c)
	{
		return NormalizePath(Path.Join(a, b, c));
	}

	public static string NormalizePath(string path)
	{
		unsafe
		{
			Span<char> temp = stackalloc char[path.Length];
			for (int i = 0; i < path.Length; i++)
				temp[i] = path[i];
			return NormalizePath(temp).ToString();
		}
	}

	public static Span<char> NormalizePath(Span<char> path)
	{
		for (int i = 0; i < path.Length; i++)
			if (path[i] == '\\') path[i] = '/';

		int length = path.Length;
		for (int i = 1, t = 1, l = length; t < l; i++, t++)
		{
			if (path[t - 1] == '/' && path[t] == '/')
			{
				i--;
				length--;
			}
			else
				path[i] = path[t];
		}

		return path[..length];
	}
}