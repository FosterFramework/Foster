using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// The Default Vertex for the <seealso cref="Batcher"/>.
/// Similar to <seealso cref="PosTexColVertex"/> but it has an extra "mode" color value for various effects.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BatcherVertex(Vector2 position, Vector2 texcoord, Color color, Color mode) : IVertex
{
	public Vector2 Pos = position;
	public Vector2 Tex = texcoord;
	public Color Col = color;

	/// <summary>
	/// R = Multiply, G = Wash, B = Fill, A = Padding
	/// </summary>
	public Color Mode = mode;  

	public readonly VertexFormat Format => format;

	private static readonly VertexFormat format = new([
		new(0, VertexType.Float2, false),
		new(1, VertexType.Float2, false),
		new(2, VertexType.UByte4, true),
		new(3, VertexType.UByte4, true)
	]);
}