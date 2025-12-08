using System.Runtime.InteropServices;
using System.Text;

namespace Foster.Framework;

internal static partial class Utf8
{
	/// <summary>
	/// Converts a utf8 null-terminating string into a C# string
	/// </summary>
	public static unsafe string FromCStr(nint s)
	{
		if (s == nint.Zero || *(byte*)s == 0)
			return string.Empty;

		var end = (byte*)s;
		while (*end != 0)
			end++;

		return Encoding.UTF8.GetString((byte*)s, (int)(end - (byte*)s));
	}

	/// <summary>
	/// Converts and allocates a null-terminating utf8 string from a C# string.
	/// Call <seealso cref="Free"/> after you're done with the UTF8 string.
	/// </summary>
	public static unsafe nint Allocate(in string str)
	{
		var count = Encoding.UTF8.GetByteCount(str) + 1;
		var ptr = Marshal.AllocHGlobal(count);
		var span = new Span<byte>((byte*)ptr.ToPointer(), count);
		Encoding.UTF8.GetBytes(str, span);
		span[^1] = 0;
		return ptr;
	}

	/// <summary>
	/// Frees a UTF8 string that was allocated from <seealso cref="Allocate"/>
	/// </summary>
	public static void Free(nint ptr)
		=> Marshal.FreeHGlobal(ptr);



}
