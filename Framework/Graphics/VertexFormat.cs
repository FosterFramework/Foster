using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Foster.Framework;

public readonly struct VertexFormat
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Element
	{
		public readonly int Index;
		public readonly VertexType Type;
		public readonly bool Normalized;

		public Element(int index, VertexType type, bool normalized = true)
		{
			Index = index;
			Type = type;
			Normalized = normalized;
		}
	}

	public readonly Element[] Elements;
	public readonly int Stride;

	public VertexFormat(int stride, params Element[] elements)
	{
		Stride = stride;
		Elements = elements;
	}

	public static VertexFormat Create<T>(params Element[] elements) where T : struct
	{
		return new VertexFormat(Unsafe.SizeOf<T>(), elements);
	}

	public static bool operator ==(VertexFormat a, VertexFormat b)
		=> a.Elements == b.Elements && a.Stride == b.Stride;

	public static bool operator !=(VertexFormat a, VertexFormat b)
		=> a.Elements != b.Elements || a.Stride != b.Stride;

	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is VertexFormat f && f == this;

	public override int GetHashCode()
		=> HashCode.Combine(Elements, Stride);
}
