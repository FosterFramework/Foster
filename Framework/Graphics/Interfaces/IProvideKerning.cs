namespace Foster.Framework;

/// <summary>
/// Class that provides kerning data for Sprite Fonts
/// </summary>
public interface IProvideKerning
{
	public float GetKerning(int codepointA, int codepointB, float size);
}