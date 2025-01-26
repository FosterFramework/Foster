namespace Foster.Framework;

/// <summary>
/// Index Buffer Formats used in <see cref="Mesh"/> index buffers
/// </summary>
public enum IndexFormat
{
	/// <summary>
	/// 16-bit integer format (<see cref="short"/> or <see cref="ushort"/>)
	/// </summary>
	Sixteen,

	/// <summary>
	/// 32-bit integer format (<see cref="int"/> or <see cref="uint"/>)
	/// </summary>
	ThirtyTwo
}

/// <summary>
/// <see cref="IndexFormat"/> Extension Methods
/// </summary>
public static class IndexFormatExt
{
	/// <summary>
	/// Gets the Size of the IndexFormat in bytes
	/// </summary>
	public static int SizeInBytes(this IndexFormat format) => format switch
	{
		IndexFormat.Sixteen => 2,
		IndexFormat.ThirtyTwo => 4,
		_ => throw new NotImplementedException(),
	};

	/// <summary>
	/// Gets the associated <see cref="IndexFormat"/> of the given integer type.
	/// </summary>
	/// <typeparam name="T">Must be either <see cref="int"/>, <see cref="uint"/>, <see cref="short"/>, or <see cref="ushort"/> </typeparam>
	public static IndexFormat GetFormatOf<T>() where T : unmanaged
	{
		if (typeof(T) == typeof(ushort)) return IndexFormat.Sixteen;
		if (typeof(T) == typeof(short)) return IndexFormat.Sixteen;
		if (typeof(T) == typeof(uint)) return IndexFormat.ThirtyTwo;
		if (typeof(T) == typeof(int)) return IndexFormat.ThirtyTwo;

		throw new Exception("Invalid Index Format Type");
	}
}