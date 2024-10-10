namespace Foster.Framework;

public enum IndexFormat
{
	Sixteen,
	ThirtyTwo
}

public static class IndexFormatExt
{
	public static int SizeInBytes(this IndexFormat format) => format switch
	{
		IndexFormat.Sixteen => 2,
		IndexFormat.ThirtyTwo => 4,
		_ => throw new NotImplementedException(),
	};
}