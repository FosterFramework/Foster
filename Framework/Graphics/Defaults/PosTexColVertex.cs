using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A Position, TexCoord, Color Vertex
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PosTexColVertex(Vector2 position, Vector2 texcoord, Color color) : IVertex
{
	public Vector2 Pos = position;
	public Vector2 Tex = texcoord;
	public Color Col = color;

	public readonly VertexFormat Format => format;

	private static readonly VertexFormat format = new([
		new(0, VertexType.Float2, false),
		new(1, VertexType.Float2, false),
		new(2, VertexType.UByte4, true)
	]);
}