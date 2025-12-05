using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Font used to render text to a Sprite <see cref="Batcher"/>.
/// </summary>
public class SpriteFont : IDisposable
{
	private readonly record struct KerningPair(
		int First,
		int Second
	);

	public readonly record struct Character(
		int Codepoint,
		Subtexture Subtexture,
		float Advance,
		Vector2 Offset,
		bool Exists
	);

	/// <summary>
	/// Set of ASCII character unicode values
	/// </summary>
	public static readonly int[] Ascii = [.. Enumerable.Range(32, 128 - 32)];

	/// <summary>
	/// The GraphicsDevice the Sprite Font belongs to
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// Name of the Sprite Font. Not used internally.
	/// </summary>
	public string Name = string.Empty;

	/// <summary>
	/// Font Size
	/// </summary>
	public readonly float Size;

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

	/// <summary>
	/// Newline characters to use during various text measuring and rendering methods.
	/// </summary>
	public readonly List<char> NewlineCharacters = [ '\n' ];

	/// <summary>
	/// Wordbreak characters to use during Text Wrapping calculations and rendering.
	/// </summary>
	public readonly List<char> WordbreakCharacters = [ '\n', ' ' ];

	/// <summary>
	/// Optional Kerning Provider
	/// </summary>
	public IProvideKerning? KerningProvider;

	/// <summary>
	/// Material used when drawing
	/// </summary>
	public Material? Material;

	/// <summary>
	/// Default Sampler used when drawing
	/// </summary>
	public TextureSampler? Sampler;

	private readonly Dictionary<int, Character> characters = [];
	private readonly Dictionary<KerningPair, float> kerning = [];
	private readonly List<Texture> generatedTextures = [];

	public SpriteFont(GraphicsDevice graphicsDevice, Font font, float size, ReadOnlySpan<int> codepoints, bool premultiplyAlpha = true)
	{
		GraphicsDevice = graphicsDevice;
		KerningProvider = font;
		Size = size;

		var fontScale = font.GetScale(size);
		Ascent = font.Ascent * fontScale;
		Descent = font.Descent * fontScale;
		LineGap = font.LineGap * fontScale;

		if (codepoints.Length > 0)
			AddCharacters(font, codepoints, size, premultiplyAlpha);
	}

	public SpriteFont(GraphicsDevice graphicsDevice, Font font, float size, bool premultiplyAlpha = true)
		: this(graphicsDevice, font, size, Ascii, premultiplyAlpha) {}

	public SpriteFont(GraphicsDevice graphicsDevice, string path, float size, ReadOnlySpan<int> codepoints, bool premultiplyAlpha = true)
		: this(graphicsDevice, new Font(path), size, codepoints, premultiplyAlpha) { }

	public SpriteFont(GraphicsDevice graphicsDevice, string path, float size, bool premultiplyAlpha = true)
		: this(graphicsDevice, new Font(path), size, Ascii, premultiplyAlpha) { }

	public SpriteFont(GraphicsDevice graphicsDevice, Stream stream, float size, ReadOnlySpan<int> codepoints, bool premultiplyAlpha = true)
		: this(graphicsDevice, new Font(stream), size, codepoints, premultiplyAlpha) { }

	public SpriteFont(GraphicsDevice graphicsDevice, Stream stream, float size, bool premultiplyAlpha = true)
		: this(graphicsDevice, new Font(stream), size, Ascii, premultiplyAlpha) { }

	public SpriteFont(GraphicsDevice graphicsDevice, float size = 16)
	{
		GraphicsDevice = graphicsDevice;
		Size = size;
	}

	public SpriteFont(GraphicsDevice graphicsDevice, MsdfFont msdf)
	{
		GraphicsDevice = graphicsDevice;
		Size = msdf.Size;
		Ascent = msdf.Ascent;
		Descent = msdf.Descent;
		LineGap = msdf.LineGap;
		Material = graphicsDevice.Defaults.MsdfMaterial.Clone();
		Material.Fragment.SetUniformBuffer([msdf.DistanceRange], 0);
		Sampler = new (TextureFilter.Linear, TextureWrap.Clamp);
		KerningProvider = msdf;

		generatedTextures.Add(new Texture(graphicsDevice, msdf.Image));

		foreach (var ch in msdf.Characters)
		{
			AddCharacter(new Character(
				ch.Codepoint,
				new Subtexture(generatedTextures[0], ch.SourceRect),
				ch.Advance,
				ch.Offset,
				true
			));
		}
	}

	~SpriteFont() => Dispose(false);

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (var it in generatedTextures)
				it.Dispose();
			generatedTextures.Clear();
			characters.Clear();
			kerning.Clear();
		}
	}

	public Character this[int codepoint] => GetCharacter(codepoint);
	public Character this[char ch] => GetCharacter(ch);

	/// <summary>
	/// Calculates the width of the given text. If the text has multiple lines,
	/// then the width of the widest line will be returned.
	/// </summary>
	public float WidthOf(ReadOnlySpan<char> text)
	{
		float width = 0;
		float lineWidth = 0;
		int lastCodepoint = 0;

		for (int i = 0; i < text.Length; i ++)
		{
			if (NewlineCharacters.Contains(text[i]))
			{
				lineWidth = 0;
				lastCodepoint = 0;
				continue;
			}

			if (TryGetCharacter(text[i..], out var ch, out var step))
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

	/// <summary>
	/// Calculates the width of the given text, up to the first line-break.
	/// </summary>
	public float WidthOfLine(ReadOnlySpan<char> text)
	{
		float lineWidth = 0;
		int lastCodepoint = 0;

		for (int i = 0; i < text.Length; i ++)
		{
			if (NewlineCharacters.Contains(text[i]))
				break;

			if (TryGetCharacter(text[i..], out var ch, out var step))
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

	/// <summary>
	/// Calculates the width of the next word in the given text
	/// </summary>
	public float WidthOfWord(ReadOnlySpan<char> text, out int length)
	{
		float lineWidth = 0;
		int lastCodepoint = 0;

		length = 0;
		while (length < text.Length)
		{
			if (TryGetCharacter(text[length..], out var ch, out var step))
			{
				lineWidth += ch.Advance;
				if (lastCodepoint != 0)
					lineWidth += GetKerning(lastCodepoint, ch.Codepoint);
				lastCodepoint = ch.Codepoint;
				length += step;
			}
			else
			{
				length++;
			}

			if (length >= text.Length || WordbreakCharacters.Contains(text[length]))
				break;
		}

		return lineWidth;
	}

	/// <summary>
	/// Calculate the height of the given text
	/// </summary>
	public float HeightOf(ReadOnlySpan<char> text)
	{
		if (text.Length <= 0)
			return 0;

		float height = LineHeight;

		for (int i = 0; i < text.Length; i ++)
		{
			if (NewlineCharacters.Contains(text[i]))
				height += LineHeight;
		}

		return height - LineGap;
	}

	/// <summary>
	/// Calculate the size of the given text
	/// </summary>
	public Vector2 SizeOf(ReadOnlySpan<char> text)
	{
		return new Vector2(
			WidthOf(text),
			HeightOf(text)
		);
	}

	/// <summary>
	/// Calculates word-wrapping positions to fit the given text into the maximum line width
	/// </summary>
	public List<(int Start, int Length)> WrapText(ReadOnlySpan<char> text, float maxLineWidth)
	{
		var lines = new List<(int Start, int Length)>();
		WrapText(text, maxLineWidth, lines);
		return lines;
	}

	/// <summary>
	/// Calculates and populates a list with word-wrapping positions to fit the given text into the maximum line width
	/// </summary>
	public void WrapText(ReadOnlySpan<char> text, float maxLineWidth, List<(int Start, int Length)> writeLinesTo)
	{
		var lineWidth = 0.0f;
		var start = 0;
		for (int i = 0; i < text.Length; i ++)
		{
			// mandatory line-break
			if (NewlineCharacters.Contains(text[i]))
			{
				writeLinesTo.Add((start, i - start));
				start = i + 1;
				lineWidth = 0;
				continue;
			}

			var nextWordWidth = WidthOfWord(text[i..], out var nextWordLength);

			// split before the next word if the one being added is too long
			if (lineWidth > 0 && lineWidth + nextWordWidth > maxLineWidth)
			{
				writeLinesTo.Add((start, i - start));
				start = i + 1;
				lineWidth = 0;
			}

			// append word
			lineWidth += nextWordWidth;
			i += nextWordLength - 1;

			// finished
			if (i >= text.Length - 1)
			{
				if (text.Length - start > 0)
					writeLinesTo.Add((start, text.Length - start));
				break;
			}
		}
	}

	/// <summary>
	/// Adds characters from a font to the SpriteFont.
	/// Note that this will render new characters and can be quite slow.
	/// </summary>
	public void AddCharacters(Font font, ReadOnlySpan<int> codepoints, float? size = null, bool premultiplyAlpha = true)
	{
		var scale = font.GetScale(size ?? Size);
		var buffers = new ThreadLocal<Color[]>();
		var tasks = new List<Task>();
		var packer = new Packer()
		{
			MaxSize = 8192,
			Padding = 1,
			Trim = true
		};

		// add all codepoints
		foreach (var codepoint in codepoints)
		{
			var ch = font.GetCharacter(codepoint, scale);
			AddCharacter(new Character(
				codepoint,
				default,
				ch.Advance,
				ch.Offset,
				true
			));

			if (!ch.Visible)
				continue;

			// blit and add to packer
			tasks.Add(Task.Run(() =>
			{
				// make sure our image buffer is big enough
				var buffer = buffers.Value;
				if (buffer == null || buffer.Length < ch.Width * ch.Height)
				{
					buffer = new Color[ch.Width * ch.Height * 2];
					buffers.Value = buffer;
				}
				
				// blit char
				font.GetPixels(ch, buffer);

				// append to packer
				lock(packer)
					packer.Add(codepoint, string.Empty, new RectInt(0, 0, ch.Width, ch.Height), ch.Width, buffer);
			}));
		}

		// wait on all blitting
		Task.WaitAll([..tasks]);
		buffers.Dispose();

		// get packed textures
		var result = packer.Pack();
		var textureIndex = generatedTextures.Count;
		foreach (var page in result.Pages)
		{
			if (premultiplyAlpha)
				page.Premultiply();
			generatedTextures.Add(new(GraphicsDevice, page));
		}

		// update character subtextures
		foreach (var it in result.Entries)
			characters[it.Index] = characters[it.Index] with { 
				Subtexture = new (generatedTextures[textureIndex + it.Page], it.Source, it.Frame)
			};
	}

	/// <summary>
	/// Adds a Character to the Sprite Font
	/// </summary>
	public void AddCharacter(int codepoint, in float advance, in Vector2 offset, in Subtexture subtexture)
	{
		characters[codepoint] = new(codepoint, subtexture, advance, offset, true);
	}

	/// <summary>
	/// Adds a Character to the Sprite Font
	/// </summary>
	public void AddCharacter(in Character character)
	{
		characters[character.Codepoint] = character;
	}

	/// <summary>
	/// Gets an existing Character from the SpriteFont
	/// </summary>
	public Character GetCharacter(int codepoint)
		=> characters.GetValueOrDefault(codepoint, default);

	/// <summary>
	/// Gets an existing Character from the SpriteFont
	/// </summary>
	public bool TryGetCharacter(int codepoint, out Character character)
	{
		character = GetCharacter(codepoint);
		return character.Exists;
	}

	/// <summary>
	/// Gets an existing Character from the SpriteFont
	/// </summary>
	public bool TryGetCharacter(char ch, out Character character)
		=> TryGetCharacter((int)ch, out character);

	/// <summary>
	/// Gets an the first Character from a string, and returns how many chars were consumed
	/// </summary>
	public bool TryGetCharacter(ReadOnlySpan<char> text, out Character character, out int length)
	{
		if (text.Length <= 0)
		{
			character = default;
			length = 0;
			return false;
		}

		if (text.Length > 1 && char.IsSurrogatePair(text[0], text[1]))
		{
			length = 2;
			return TryGetCharacter(char.ConvertToUtf32(text[0], text[1]), out character);
		}

		length = 1;
		return TryGetCharacter(text[0], out character);
	}

	public void SetKerning(int codepointFirst, int codepointSecond, float advance)
	{
		kerning[new(codepointFirst, codepointSecond)] = advance;
	}

	public float GetKerning(int codepointFirst, int codepointSecond)
	{
		var key = new KerningPair(codepointFirst, codepointSecond);

		if (!kerning.TryGetValue(key, out var value))
		{
			if (KerningProvider != null)
				kerning[key] = value = KerningProvider.GetKerning(codepointFirst, codepointSecond, Size);
			else
				value = 0;
		}

		return value;
	}

	public void RenderText(Batcher batch, ReadOnlySpan<char> text, Vector2 position, Color color)
	{
		RenderText(batch, text, position, Vector2.Zero, color);
	}

	public void RenderText(Batcher batch, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
	{
		var at = position + new Vector2(0, Ascent);
		var last = 0;

		if (justify.X != 0)
			at.X -= justify.X * WidthOfLine(text);

		if (justify.Y != 0)
			at.Y -= justify.Y * HeightOf(text);

		// TODO:
		// this is incorrect, this should only happen if the font is a pixel font.
		// (otherwise using matrices and so on will not play nicely with this)
		at.X = Calc.Round(at.X);
		at.Y = Calc.Round(at.Y);

		if (Material != null)
			batch.PushMaterial(Material);

		if (Sampler != null)
			batch.PushSampler(Sampler.Value);

		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				at.X = position.X;
				if (justify.X != 0 && i < text.Length - 1)
					at.X -= justify.X * WidthOfLine(text[(i + 1)..]);
				at.Y += LineHeight;
				last = 0;
				continue;
			}

			if (TryGetCharacter(text[i..], out var ch, out var step))
			{
				if (last != 0)
					at.X += GetKerning(last, ch.Codepoint);

				if (ch.Subtexture.Texture != null)
					batch.Image(ch.Subtexture, at + ch.Offset, color);

				last = ch.Codepoint;
				at.X += ch.Advance;
				i += step - 1;
			}
		}

		if (Sampler != null)
			batch.PopSampler();

		if (Material != null)
			batch.PopMaterial();
	}

	public void RenderText(Batcher batch, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, Color color)
	{
		var lines = Pool.Get<List<(int Start, int Length)>>();
		lines.Clear();

		WrapText(text, maxLineWidth, lines);

		if (justify.Y != 0)
			position.Y -= justify.Y * (Height * lines.Count + LineGap * (lines.Count - 1));

		foreach (var (Start, Length) in lines)
		{
			RenderText(batch, text[Start..(Start + Length)], position, new Vector2(justify.X, 0), color);
			position.Y += LineHeight;
		}

		Pool.Return(lines);
	}
}

public static class SpriteFontBatcherExt
{
	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color)
	{
		font.RenderText(batch, text, position, Vector2.Zero, color);
	}

	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
	{
		font.RenderText(batch, text, position, justify, color);
	}

	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Color color)
	{
		font.RenderText(batch, text, maxLineWidth, position, Vector2.Zero, color);
	}

	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, Color color)
	{
		font.RenderText(batch, text, maxLineWidth, position, justify, color);
	}

	public static void Text(this Batcher batch, SpriteFont font, float size, ReadOnlySpan<char> text, Vector2 position, Color color)
	{
		batch.PushMatrix(position, Vector2.Zero, Vector2.One * (size / font.Size), 0);
		font.RenderText(batch, text, Vector2.Zero, Vector2.Zero, color);
		batch.PopMatrix();
	}

	public static void Text(this Batcher batch, SpriteFont font, float size, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
	{
		batch.PushMatrix(position, Vector2.Zero, Vector2.One * (size / font.Size), 0);
		font.RenderText(batch, text, Vector2.Zero, justify, color);
		batch.PopMatrix();
	}

	public static void Text(this Batcher batch, SpriteFont font, float size, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Color color)
	{
		batch.PushMatrix(position, Vector2.Zero, Vector2.One * (size / font.Size), 0);
		font.RenderText(batch, text, maxLineWidth, Vector2.Zero, Vector2.Zero, color);
		batch.PopMatrix();
	}

	public static void Text(this Batcher batch, SpriteFont font, float size, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, Color color)
	{
		batch.PushMatrix(position, Vector2.Zero, Vector2.One * (size / font.Size), 0);
		font.RenderText(batch, text, maxLineWidth, Vector2.Zero, justify, color);
		batch.PopMatrix();
	}

	public static void Text(this Batcher batch, ReadOnlySpan<char> text, Vector2 position, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, text, position, color);

	public static void Text(this Batcher batch, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, text, position, justify, color);

	public static void Text(this Batcher batch, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, text, maxLineWidth, position, color);

	public static void Text(this Batcher batch, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, text, maxLineWidth, position, justify, color);

	public static void Text(this Batcher batch, float size, ReadOnlySpan<char> text, Vector2 position, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, size, text, position, color);

	public static void Text(this Batcher batch, float size, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, size, text, position, justify, color);

	public static void Text(this Batcher batch, float size, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, size, text, maxLineWidth, position, color);

	public static void Text(this Batcher batch, float size, ReadOnlySpan<char> text, float maxLineWidth, Vector2 position, Vector2 justify, Color color)
		=> Text(batch, batch.GraphicsDevice.Defaults.SpriteFont, size, text, maxLineWidth, position, justify, color);


	[Obsolete("Use explicit text size instead of scale")]
	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, float scale, Color color)
	{
		batch.PushMatrix(position, Vector2.Zero, Vector2.One * scale, 0);
		batch.Text(font, text, Vector2.Zero, justify, color);
		batch.PopMatrix();
	}
}
