using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

public readonly struct VertexFormat(int stride, params VertexFormat.Element[] elements)
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly record struct Element(
		int Index,
		VertexType Type,
		bool Normalized = true
	);

	public readonly Element[] Elements = elements;
	public readonly int Stride = stride;

	public static VertexFormat Create<T>(params Element[] elements) where T : struct
		=> new(Unsafe.SizeOf<T>(), elements);

	public static bool operator ==(VertexFormat a, VertexFormat b)
		=> a.Elements == b.Elements && a.Stride == b.Stride;

	public static bool operator !=(VertexFormat a, VertexFormat b)
		=> a.Elements != b.Elements || a.Stride != b.Stride;

	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is VertexFormat f && f == this;

	public override int GetHashCode()
		=> HashCode.Combine(Elements, Stride);
}
