using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A utility object to store arbitrary Uniform Buffer data.
/// </summary>
public class UniformBuffer
{
	private byte[] bytes = [];
	private int count;

	/// <summary>
	/// Sets the Data stored in a Uniform Buffer
	/// </summary>
	public unsafe void Set<T>(in T data) where T : unmanaged
	{
		fixed (T* ptr = &data)
			Set(new ReadOnlySpan<byte>((byte*)ptr, Marshal.SizeOf<T>()));
	}

	/// <summary>
	/// Sets the Data stored in a Uniform Buffer
	/// </summary>
	public void Set(in ReadOnlySpan<byte> data)
	{
		if (data.Length > bytes.Length)
			Array.Resize(ref bytes, data.Length);
		data.CopyTo(bytes);
		count = data.Length;
	}

	/// <summary>
	/// Sets the Data stored in a Uniform Buffer
	/// </summary>
	public unsafe void Set(in ReadOnlySpan<float> data)
	{
		fixed (void* ptr = data)
			Set(new ReadOnlySpan<byte>(ptr, data.Length * sizeof(float)));
	}

	/// <summary>
	/// Gets the data stored in a Uniform Buffer, converted to a given Type
	/// </summary>
	public unsafe T Get<T>() where T : unmanaged
	{
		var data = Get();
		if (data.Length < Marshal.SizeOf<T>())
			return new();
		fixed (byte* ptr = data)
			return *(T*)ptr;
	}

	/// <summary>
	/// Gets the data stored in a Uniform Buffer
	/// </summary>
	public ReadOnlySpan<byte> Get()
		=> new ReadOnlySpan<byte>(bytes)[..count];

	/// <summary>
	/// Copies the Data from this Stage to another
	/// </summary>
	public void CopyTo(UniformBuffer to)
	{
		if (to.bytes.Length < bytes.Length)
			Array.Resize(ref to.bytes, bytes.Length);
		Array.Copy(bytes, to.bytes, bytes.Length);
		to.count = count;
	}
}