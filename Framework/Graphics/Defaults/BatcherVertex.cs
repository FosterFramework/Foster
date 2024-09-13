using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// The Default Vertex for the <seealso cref="Batcher"/>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BatcherVertex(Vector2 position, Vector2 texcoord, Color color, Color mode) : IVertex
{
	public Vector2 Pos = position;
	public Vector2 Tex = texcoord;
	public Color Col = color;
	public Color Mode = mode;  // R = Multiply, G = Wash, B = Fill, A = Padding

	public VertexFormat Format => format;

	private static readonly VertexFormat format = new([
		new(0, VertexType.Float2, false),
		new(1, VertexType.Float2, false),
		new(2, VertexType.UByte4, true),
		new(3, VertexType.UByte4, true)
	]);
}