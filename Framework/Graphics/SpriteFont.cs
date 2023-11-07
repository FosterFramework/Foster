using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Font used to render text in a Sprite Batch
/// </summary>
public class SpriteFont
{
	public struct Character
	{
		public int Codepoint;
		public Subtexture Subtexture;
		public float Advance;
		public Vector2 Offset;

		public Character(int codepoint, in Subtexture subtexture, float advance, in Vector2 offset)
		{
			Codepoint = codepoint;
			Subtexture = subtexture;
			Advance = advance;
			Offset = offset;
		}

		public Character(char ch, in Subtexture subtexture, float advance, in Vector2 offset)
			: this((int)ch, subtexture, advance, offset) { }
	}

	private readonly struct KerningPair : IEquatable<KerningPair>
	{
		public readonly int First;
		public readonly int Second;

		public KerningPair(int first, int second) { First = first; Second = second; }

		public bool Equals(KerningPair other) => First == other.First && Second == other.Second;
		public override int GetHashCode() => HashCode.Combine(First, Second);
	}

	public static readonly int[] Ascii;

	/// <summary>
	/// Name of the Sprite Font. Not used internally.
	/// </summary>
	public string Name = string.Empty;

	/// <summary>
	/// Font Size
	/// </summary>
	public float Size;

	/// <summary>
	/// Font Ascent
	/// </summary>
	public float Ascent;

	/// <summary>
	/// Font Descent
	/// </summary>
	public float Descent;

	/// <summary>
	/// Font Line Gap (space between each line of text)
	/// </summary>
	public float LineGap;

	/// <summary>
	/// Height of the Font
	/// </summary>
	public float Height => Ascent - Descent;

	/// <summary>
	/// Line Height (including Line Gap)
	/// </summary>
	public float LineHeight => Ascent - Descent + LineGap;

	private readonly Dictionary<int, Character> characters = new();
	private readonly Dictionary<KerningPair, float> kerning = new();

	static SpriteFont()
	{
		var ascii = new List<int>();
		for (int i = 32; i < 128; i ++)
			ascii.Add(i);
		Ascii = ascii.ToArray();
	}

	public SpriteFont()
	{

	}

	public SpriteFont(string path, float size)
		: this(path, size, Ascii) { }

	public SpriteFont(string path, float size, ReadOnlySpan<int> codepoints)
	{
		using var font = new Font(path);
		InitializeFromFont(font, size, codepoints);
	}

	public SpriteFont(Stream stream, float size)
		: this(stream, size, Ascii) { }

	public SpriteFont(Stream stream, float size, ReadOnlySpan<int> codepoints)
	{
		using var font = new Font(stream);
		InitializeFromFont(font, size, codepoints);
	}

	public SpriteFont(Font font, float size)
		: this(font, size, Ascii) { }

	public SpriteFont(Font font, float size, ReadOnlySpan<int> codepoints)
	{
		InitializeFromFont(font, size, codepoints);
	}

	private void InitializeFromFont(Font font, float size, ReadOnlySpan<int> codepoints)
	{
		// get font scale
		var scale = font.GetScale(size);

		// setup size based on the font given
		Size = size;
		Ascent = font.Ascent * scale;
		Descent = font.Descent * scale;
		LineGap = font.LineGap * scale;

		// create a buffer that should be large enough for any character
		var buffer = new Color[(int)(size * size)];

		// create sprite packer
		var packer = new Packer
		{
			MaxSize = 4096,
			Trim = false,
			CombineDuplicates = false
		};

		// add each character
		foreach (var codepoint in codepoints)
		{
			var glyph = font.GetGlyphIndex(codepoint);
			var metrics = font.GetCharacterOfGlyph(glyph, scale);

			if (metrics.Visible)
			{
				if (buffer.Length < metrics.Width * metrics.Height)
					Array.Resize(ref buffer, metrics.Width * metrics.Height);

				if (font.GetPixels(metrics, buffer))
				{
					packer.Add(codepoint, string.Empty, metrics.Width, metrics.Height, buffer);
				}
			}

			characters.Add(codepoint, new(
				codepoint,
				new Subtexture(),
				metrics.Advance,
				metrics.Offset
			));
		}

		// pack characters into textures
		var result = packer.Pack();
		var textures = new List<Texture>();
		foreach (var page in result.Pages)
			textures.Add(new(page));

		// update subtextures of all the created characters
		foreach (var packed in result.Entries)
		{
			var codepoint = packed.Index;
			var subtexture = new Subtexture(
				textures[packed.Page],
				packed.Source,
				packed.Frame);

			characters[codepoint] = characters[codepoint] with { Subtexture = subtexture };
		}
	}

	public float WidthOf(ReadOnlySpan<char> text)
	{
		float width = 0;
		float lineWidth = 0;
		int lastCodepoint = 0;

		for (int i = 0; i < text.Length; i ++)
		{
			if (text[i] == '\n')
			{
				lineWidth = 0;
				lastCodepoint = 0;
				continue;
			}

			if (TryGetCharacter(text, i, out var ch, out var step))
			{
				lineWidth += ch.Advance;
				if (lastCodepoint != 0)
					lineWidth += GetKerning(lastCodepoint, ch.Codepoint);
				if (lineWidth > width)
					width = lineWidth;
				lastCodepoint = ch.Codepoint;
				i += step - 1;
			}
		}

		return width;
	}

	public float WidthOfLine(ReadOnlySpan<char> text)
	{
		float lineWidth = 0;
		int lastCodepoint = 0;

		for (int i = 0; i < text.Length; i ++)
		{
			if (text[i] == '\n')
				break;

			if (TryGetCharacter(text, i, out var ch, out var step))
			{
				lineWidth += ch.Advance;
				if (lastCodepoint != 0)
					lineWidth += GetKerning(lastCodepoint, ch.Codepoint);
				lastCodepoint = ch.Codepoint;
				i += step - 1;
			}
		}

		return lineWidth;
	}

	public float HeightOf(ReadOnlySpan<char> text)
	{
		if (text.Length <= 0)
			return 0;

		float height = LineHeight;

		for (int i = 0; i < text.Length; i ++)
		{
			if (text[i] == '\n')
				height += LineHeight;
		}

		return height - LineGap;
	}

	public Character this[int codepoint]  => characters[codepoint];
	public Character this[char ch]  => characters[(int)ch];

	public void AddCharacter(in Character character)
	{
		characters[character.Codepoint] = character;
	}

	public bool TryGetCharacter(int codepoint, out Character character)
	{
		if (characters.TryGetValue(codepoint, out var result))
		{
			character = result;
			return true;
		}

		character = new();
		return false;
	}

	public bool TryGetCharacter(char ch, out Character character)
		=> TryGetCharacter((int)ch, out character);

	public bool TryGetCharacter(ReadOnlySpan<char> text, int index, out Character character, out int length)
	{
		if (index + 1 < text.Length && char.IsSurrogatePair(text[index], text[index + 1]))
		{
			length = 2;
			return TryGetCharacter(char.ConvertToUtf32(text[index], text[index + 1]), out character);
		}
		else
		{
			length = 1;
			return TryGetCharacter(text[index], out character);
		}
	}

	public float GetKerning(char a, char b)
	{
		return GetKerning((int)a, (int)b);
	}

	public float GetKerning(int a, int b)
	{
		if (kerning.TryGetValue(new KerningPair(a, b), out float result))
			return result;
		return 0;
	}

	public void SetKerning(char a, char b, float value)
	{
		SetKerning((int)a, (int)b, value);
	}

	public void SetKerning(int a, int b, float value)
	{
		kerning[new KerningPair(a, b)] = value;
	}
}
