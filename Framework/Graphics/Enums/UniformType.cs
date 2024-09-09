namespace Foster.Framework;

public enum UniformType
{
	None,
	Float,
	Float2,
	Float3,
	Float4,
	Mat3x2,
	Mat4x4
}

public static class UniformTypeExt
{
	public static int SizeInBytes(this UniformType type) => type switch
	{
		UniformType.None =>   0,
		UniformType.Float =>  4,
		UniformType.Float2 => 8,
		UniformType.Float3 => 12,
		UniformType.Float4 => 16,
		UniformType.Mat3x2 => 24,
		UniformType.Mat4x4 => 64,
		_ => throw new NotImplementedException()
	};
}
