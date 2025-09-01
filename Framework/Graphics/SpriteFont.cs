using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Font used to render text to a Sprite <see cref="Batcher"/>.<br/>
/// <br/>
/// By default the <see cref="SpriteFont"/> will prepare characters as they are
/// requested, which means there can occasionally be a delay between trying
/// to draw some text and it actually appearing on-screen. To remove this delay,
/// you can call <see cref="PrepareCharacters(ReadOnlySpan{char}, bool)"/> to
/// pre-render all characters that you would like to use.
/// </summary>
public class SpriteFont : IDisposable
{
	public readonly record struct Character(
		int Codepoint,
		Subtexture Subtexture,
		float Advance,
		Vector2 Offset,
		bool Exists
	);

	private readonly record struct KerningPair(int First, int Second);

	/// <summary>
	/// Set of ASCII character unicode values
	/// </summary>
	public static readonly int[] Ascii = [.. Enumerable.Range(32, 128 - 32)];

	/// <summary>
	/// The GraphicsDevice the Sprite Font belongs to
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// The Font being used by the SpriteFont.
	/// This can be null if the SpriteFont was created without a Font, in which
	/// case only custom Characters will be used.
	/// </summary>
	public readonly Font? Font;

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
	/// If the generated character images should premultiply their alpha.
	/// This should be true if you render the SpriteFont with the default
	/// Premultiply BlendMode.
	/// Note that this property does not modify already-created characters.
	/// </summary>
	public bool PremultiplyAlpha = true;

	/// <summary>
	/// If True, the SpriteFont will always wait for all characters to be ready before
	/// drawing anything to the screen. This will potentially cause your game to halt
	/// as characters prepare themselves.
	/// </summary>
	public bool WaitForPendingCharacters = false;

	/// <summary>
	/// Newline characters to use during various text measuring and rendering methods.
	/// </summary>
	public readonly List<char> NewlineCharacters = [ '\n' ];

	/// <summary>
	/// Wordbreak characters to use during Text Wrapping calculations and rendering.
	/// </summary>
	public readonly List<char> WordbreakCharacters = [ '\n', ' ' ];

	private readonly float fontScale = 1.0f;
	private readonly Dictionary<int, Character> characters = [];
	private readonly Dictionary<KerningPair, float> kerning = [];
	private readonly List<Page> pages = [];
	private readonly BlockingCollection<(int Codepoint, Font.Character Metrics)> blittingQueue = [];
	private readonly BlockingCollection<(int Codepoint, int Page, Rect Source, Rect Frame)> blittingResults = [];
	private Task? blittingTask;
	private Color[] blitBuffer = [];

	public SpriteFont(GraphicsDevice graphicsDevice, Font font, float size, ReadOnlySpan<int> prebakedCodepoints = default, bool premultiplyAlpha = true)
	{
		GraphicsDevice = graphicsDevice;
		Font = font;
		Size = size;
		fontScale = font.GetScale(size);
		Ascent = font.Ascent * fontScale;
		Descent = font.Descent * fontScale;
		LineGap = font.LineGap * fontScale;
		PremultiplyAlpha = premultiplyAlpha;

		if (prebakedCodepoints.Length > 0)
			PrepareCharacters(prebakedCodepoints, true);
	}

	public SpriteFont(GraphicsDevice graphicsDevice, string path, float size, ReadOnlySpan<int> prebakedCodepoints = default, bool premultiplyAlpha = true)
		: this(graphicsDevice, new Font(path), size, prebakedCodepoints, premultiplyAlpha)
	{

	}

	public SpriteFont(GraphicsDevice graphicsDevice, Stream stream, float size, ReadOnlySpan<int> prebakedCodepoints = default, bool premultiplyAlpha = true)
		: this(graphicsDevice, new Font(stream), size, prebakedCodepoints, premultiplyAlpha)
	{

	}

	public SpriteFont(GraphicsDevice graphicsDevice, float size = 16)
	{
		GraphicsDevice = graphicsDevice;
		Font = null;
		Size = size;
	}

	~SpriteFont()
	{
		Dispose();
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (blittingTask != null)
		{
			blittingQueue.CompleteAdding();
			blittingTask?.Wait();
			blittingTask = null;
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
			if (TryGetCharacter(text, length, out var ch, out var step))
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
	/// Prepares the given characters for rendering
	/// </summary>
	/// <param name="codepoints">A list of characters to prepare</param>
	/// <param name="waitForResults">If the function should wait for all characters to be ready</param>
	public void PrepareCharacters(ReadOnlySpan<int> codepoints, bool waitForResults)
	{
		foreach (var codepoint in codepoints)
			BlitEnqueue(codepoint, waitForResults, out _);

		if (waitForResults)
			FlushPendingCharacters(true);
	}

	/// <summary>
	/// Prepares the given characters for rendering
	/// </summary>
	/// <param name="text">A list of characters to prepare</param>
	/// <param name="waitForResults">If the function should wait for all characters to be ready</param>
	public void PrepareCharacters(ReadOnlySpan<char> text, bool waitForResults)
	{
		int index = 0;
		while (index < text.Length)
		{
			int codepoint;
			if (index + 1 < text.Length && char.IsSurrogatePair(text[index], text[index + 1]))
			{
				codepoint = char.ConvertToUtf32(text[index], text[index + 1]);
				index += 2;
			}
			else
			{
				codepoint = text[index];
				index += 1;
			}

			BlitEnqueue(codepoint, waitForResults, out _);
		}

		if (waitForResults)
			FlushPendingCharacters(true);
	}

	/// <summary>
	/// Adds a custom Character to the Sprite Font
	/// </summary>
	public void AddCharacter(int codepoint, in float advance, in Vector2 offset, in Subtexture subtexture)
	{
		characters[codepoint] = new(codepoint, subtexture, advance, offset, true);
	}

	/// <summary>
	/// Adds a custom Character to the Sprite Font
	/// </summary>
	public void AddCharacter(in Character character)
	{
		characters[character.Codepoint] = character;
	}

	/// <summary>
	/// Gets an existing Character from the SpriteFont.
	/// Note that unless you have called <see cref="PrepareCharacters(ReadOnlySpan{char}, bool)"/> and
	/// wait for them to be rendered, they may not have textures yet.
	/// </summary>
	public Character GetCharacter(int codepoint)
	{
		if (!characters.TryGetValue(codepoint, out var value))
			BlitEnqueue(codepoint, false, out value);
		return value;
	}

	public bool TryGetCharacter(int codepoint, out Character character)
	{
		character = GetCharacter(codepoint);
		return character.Exists;
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

	public void SetKerning(int codepointFirst, int codepointSecond, float advance)
	{
		kerning[new(codepointFirst, codepointSecond)] = advance;
	}

	public float GetKerning(int codepointFirst, int codepointSecond)
	{
		var key = new KerningPair(codepointFirst, codepointSecond);

		if (!kerning.TryGetValue(key, out var value))
		{
			if (Font != null)
				kerning[key] = value = Font.GetKerning(codepointFirst, codepointSecond, fontScale);
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

		// apply changes that we have so far that may have been generated off-thread
		if (WaitForPendingCharacters)
			PrepareCharacters(text, true);
		else
			BlitApplyChanges();

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

			if (TryGetCharacter(text, i, out var ch, out var step))
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

	public void FlushPendingCharacters(bool waitForResults)
	{
		if (waitForResults)
			BlitQueued(ref blitBuffer);
		BlitApplyChanges();
	}

	/// <summary>
	/// Enqueus a character to be blitted.
	/// </summary>
	private bool BlitEnqueue(int codepoint, bool isWaitingForResults, out Character ch)
	{
		if (characters.TryGetValue(codepoint, out ch))
			return false;

		var blitting = false;

		if (Font != null)
		{
			var scale = Font.GetScale(Size);
			var glyph = Font.GetGlyphIndex(codepoint);
			var metrics = Font.GetCharacterOfGlyph(glyph, scale);

			characters[codepoint] = ch = new(
				codepoint,
				new Subtexture(null, default, new(0, 0, metrics.Width, metrics.Height)),
				metrics.Advance,
				metrics.Offset,
				glyph != 0
			);

			if (metrics.Visible)
			{
				blittingQueue.Add((codepoint, metrics));
				blitting = true;
			}
		}
		else
		{
			characters[codepoint] = ch = new(
				codepoint,
				default,
				0.0f,
				Vector2.Zero,
				false
			);
		}

		// make sure we have a blitting task if we want to do it offthread
		if (!isWaitingForResults && blitting && blittingTask == null)
		{
			void BlittingTask()
			{
				var blitBuffer = new Color[64];
				while (!blittingQueue.IsAddingCompleted)
				{
					int codepoint = 0;
					Font.Character metrics = default;

					// in case it's emptied between the while loop and reaching this point
					try { (codepoint, metrics) = blittingQueue.Take(); }
					catch {}

					if (codepoint != 0)
						BlitCharacter(ref blitBuffer, codepoint, metrics);
				}
			}

			blittingTask = Task.Run(BlittingTask);
		}

		return blitting;
	}

	/// <summary>
	/// Blits the characters queued in <see cref="blittingQueue"/> and waits for them to finish, populating <see cref="blittingResults"/>
	/// </summary>
	private void BlitQueued(ref Color[] blitBuffer)
	{
		while (blittingQueue.TryTake(out var entry))
			BlitCharacter(ref blitBuffer, entry.Codepoint, entry.Metrics);
	}

	/// <summary>
	/// Blits a single character and waits for it to finish, populating <see cref="blittingResults"/>
	/// </summary>
	private void BlitCharacter(ref Color[] blitBuffer, int codepoint, in Font.Character ch)
	{
		var length = ch.Width * ch.Height;
		if (blitBuffer.Length <= length)
			Array.Resize(ref blitBuffer, length);

		// render the actual character to a buffer
		if (Font == null || !Font.GetPixels(ch, blitBuffer))
			return;

		// premultiply if needed
		if (PremultiplyAlpha)
		{
			for (int i = 0; i < length; i ++)
				blitBuffer[i] = blitBuffer[i].Premultiply();
		}

		// TODO:
		// Ideally the pages could expand if needed.
		// When expanding, all character subtextures would need to be updated.
		var pageSize = (int)Math.Min(4096, Size * 16);

		// unusual case where somehow the character is gigantic and can't fit into a
		// texture page .... in this scenario, throw a warning and don't render it
		if (ch.Width > pageSize || ch.Height > pageSize)
		{
			Log.Warning($"SpriteFont Character was too large to render to a Texture!");
			return;
		}

		// pack into a page
		lock (pages)
		{
			var page = 0;
			while (true)
			{
				if (page >= pages.Count)
					pages.Add(new(GraphicsDevice, Name, pageSize));
				if (pages[page].TryPack(blitBuffer, ch.Width, ch.Height, out var source, out var frame))
				{
					blittingResults.Add((codepoint, page, source, frame));
					break;
				}
				page++;
			}
		}
	}

	/// <summary>
	/// Copies results from <see cref="blittingResults"/> and applies them to the characters.
	/// This also makes sure Texture Pages are updated.
	/// </summary>
	private void BlitApplyChanges()
	{
		// update character subtextures
		while (blittingResults.TryTake(out var result))
		{
			var page = pages[result.Page];
			characters[result.Codepoint] = characters[result.Codepoint] with
			{
				Subtexture = page.GetSubtexture(result.Source, result.Frame)
			};
		}
	}

	private class Page(GraphicsDevice graphicsDevice, string fontName, int size)
	{
		private record struct Node(int Left, int Right, RectInt Bounds);
		private readonly string fontName = fontName;
		private readonly int size = size;
		private readonly Image image = new(size, size);
		private readonly List<Node> nodes = [ new() { Bounds = new(0, 0, size, size) } ];
		private Texture? atlas;
		private bool atlasDirty;

		public Subtexture GetSubtexture(in Rect source, in Rect frame)
		{
			// make sure we have the latest data before returning a subtexture
			if (atlasDirty)
			{
				atlasDirty = false;
				atlas ??= new(graphicsDevice, size, size, TextureFormat.Color, $"SpriteFont[{fontName}]");
				atlas.SetData<Color>(image.Data);
			}

			// get the results
			return new(atlas, source, frame);
		}

		public bool TryPack(Color[] buffer, int width, int height, out Rect source, out Rect frame)
		{
			int index = TryPackNode(0, width + 2, height + 2);

			if (index >= 0)
			{
				var node = nodes[index];
				image.CopyPixels(buffer, width, height, new Point2(node.Bounds.X + 1, node.Bounds.Y + 1));
				source = node.Bounds;
				frame = new Rect(1, 1, width, height);
				atlasDirty = true;
				return true;
			}

			source = frame = default;
			return false;
		}

		private int TryPackNode(int node, int width, int height)
		{
			var it = nodes[node];

			if (it.Left > 0 || it.Right > 0)
			{
				if (it.Left > 0)
				{
					var fit = TryPackNode(it.Left, width, height);
					if (fit > 0)
						return fit;
				}

				if (it.Right > 0)
				{
					var fit = TryPackNode(it.Right, width, height);
					if (fit > 0)
						return fit;
				}

				return -1;
			}

			if (width > it.Bounds.Width || height > it.Bounds.Height)
				return -1;

			var w = it.Bounds.Width - width;
			var h = it.Bounds.Height - height;

			it.Left = nodes.Count;
			nodes.Add(new());
			it.Right = nodes.Count;
			nodes.Add(new());

			if (w <= h)
			{
				nodes[it.Left] = new() { Bounds = new(it.Bounds.X + width, it.Bounds.Y, w, height) };
				nodes[it.Right] = new() { Bounds = new(it.Bounds.X, it.Bounds.Y + height, it.Bounds.Width, h) };
			}
			else
			{
				nodes[it.Left] = new() { Bounds = new(it.Bounds.X, it.Bounds.Y + height, width, h) };
				nodes[it.Right] = new() { Bounds = new(it.Bounds.X + width, it.Bounds.Y, w, it.Bounds.Height) };
			}

			it.Bounds = it.Bounds with { Width = width, Height = height };
			nodes[node] = it;
			return node;
		}
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

	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position,
		Vector2 justify, float scale, Color color)
	{
		batch.PushMatrix(position, Vector2.Zero, Vector2.One * scale, 0);
		batch.Text(font, text, Vector2.Zero, justify, color);
		batch.PopMatrix();
	}
}
