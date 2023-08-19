using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TextureSampler : IEquatable<TextureSampler>
{
	public readonly TextureFilter Filter;
	public readonly TextureWrap WrapX;
	public readonly TextureWrap WrapY;

	public TextureSampler(TextureFilter filter, TextureWrap wrapX, TextureWrap wrapY)
	{
		Filter = filter;
		WrapX = wrapX;
		WrapY = wrapY;
	}

	public static bool operator ==(TextureSampler a, TextureSampler b) 
		=> a.Filter == b.Filter && a.WrapX == b.WrapX && a.WrapY == b.WrapY;

	public static bool operator !=(TextureSampler a, TextureSampler b)
		=> !(a == b);

	public override bool Equals(object? obj)
		=> (obj is TextureSampler mode) && (this == mode);

	public bool Equals(TextureSampler other)
		=> (this == other);

	public override int GetHashCode()
		=> HashCode.Combine(Filter, WrapX, WrapY);
}
