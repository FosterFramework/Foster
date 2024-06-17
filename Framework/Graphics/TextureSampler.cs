using System.Runtime.InteropServices;

namespace Foster.Framework;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TextureSampler(
	TextureFilter Filter,
	TextureWrap WrapX,
	TextureWrap WrapY
);
