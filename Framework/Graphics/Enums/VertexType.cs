namespace Foster.Framework;

/// <summary>
/// Available Vertex Types used in Vertex Buffer structs
/// </summary>
public enum VertexType
{
	None,
	Float,
	Float2,
	Float3,
	Float4,
	Byte4,
	UByte4,
	Short2,
	UShort2,
	Short4,
	UShort4
}

/// <summary>
/// <see cref="VertexType"/> Extension methods
/// </summary>
public static class VertexTypeExt
{
	public static int SizeInBytes(this VertexType type) => type switch
	{
		VertexType.Float   => 4,
		VertexType.Float2  => 8,
		VertexType.Float3  => 12,
		VertexType.Float4  => 16,
		VertexType.Byte4   => 4,
		VertexType.UByte4  => 4,
		VertexType.Short2  => 4,
		VertexType.UShort2 => 4,
		VertexType.Short4  => 8,
		VertexType.UShort4 => 8,
		_ => throw new NotImplementedException(),
	};
}