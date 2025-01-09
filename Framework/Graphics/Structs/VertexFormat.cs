using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

/// <summary>
/// Describes a Vertex Format used in rendering Meshes.
/// </summary>
public readonly struct VertexFormat
{
	public readonly record struct Element(
		int Index,
		VertexType Type,
		bool Normalized = true
	);

	public readonly StackList32<Element> Elements;
	public readonly int Stride;

	public VertexFormat(in ReadOnlySpan<Element> elements, int stride = 0)
	{
		foreach (var it in elements)
		{
			Elements.Add(it);
			Stride += it.Type.SizeInBytes();
		}

		if (stride != 0)
			Stride = stride;
	}

	public static VertexFormat Create<T>(params Element[] elements) where T : struct
		=> new(elements, Unsafe.SizeOf<T>());

	public static bool operator ==(VertexFormat a, VertexFormat b)
		=> a.Stride == b.Stride && a.Elements.Span.SequenceEqual(b.Elements.Span);

	public static bool operator !=(VertexFormat a, VertexFormat b)
		=> a.Stride != b.Stride || !a.Elements.Span.SequenceEqual(b.Elements.Span);

	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is VertexFormat f && f == this;

	public override int GetHashCode()
	{
		var hash = Stride.GetHashCode();
		foreach (var it in Elements)
			hash = HashCode.Combine(hash, it);
		return hash;
	}
}
