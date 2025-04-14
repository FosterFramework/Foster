using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// The Default Vertex for the <seealso cref="Batcher"/>.
/// Similar to <seealso cref="PosTexColVertex"/> but it has an extra "mode" color value for various effects.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BatcherVertex : IVertex
{
	public Vector2 Pos;
	public Vector2 Tex;
	public Color Col;

	/// <summary>
	/// R = Multiply, G = Wash, B = Fill, A = Padding
	/// </summary>
	public Color Mode;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BatcherVertex(in Vector2 position, in Vector2 texcoord, Color color, Color mode)
	{
		Pos = position;
		Tex = texcoord;
		Col = color;
		Mode = mode;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public BatcherVertex(in Vector2 position, Color color, Color mode)
	{
		Pos = position;
		Col = color;
		Mode = mode;
	}

	public readonly VertexFormat Format => format;

	private static readonly VertexFormat format = new([
		new(0, VertexType.Float2, false),
		new(1, VertexType.Float2, false),
		new(2, VertexType.UByte4, true),
		new(3, VertexType.UByte4, true)
	]);
}