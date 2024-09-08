using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

public readonly struct VertexFormat(int stride, params VertexFormat.Element[] elements)
{
	public readonly record struct Element(
		int Index,
		VertexType Type,
		bool Normalized = true
	);

	public readonly StackList32<Element> Elements = [..elements];
	public readonly int Stride = stride;

	public static VertexFormat Create<T>(params Element[] elements) where T : struct
		=> new(Unsafe.SizeOf<T>(), elements);

	public static bool operator ==(VertexFormat a, VertexFormat b)
		=> a.Stride == b.Stride && a.Elements.Span.SequenceEqual(b.Elements.Span);

	public static bool operator !=(VertexFormat a, VertexFormat b)
		=> a.Stride != b.Stride || !a.Elements.Span.SequenceEqual(b.Elements.Span);

	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is VertexFormat f && f == this;

	public override int GetHashCode()
		=> HashCode.Combine(Elements, Stride);
}
