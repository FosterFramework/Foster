using System.Diagnostics;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Font used to render text in a Sprite Batch.
/// By default the SpriteFont will prepare characters as they are requested,
/// which means there can occasionally be a delay between requesting to draw 
/// some text and it actually appearing on-screen. To remove this delay, you
/// can call SpriteFont.PrepareCharacters to pre-render all characters that
/// you would like to use.
/// </summary>
public class SpriteFont
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
	public static readonly int[] Ascii = Enumerable.Range(32, 128 - 32).ToArray();

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
	/// How the SpriteFont should blit characters upon request.
	/// </summary>
	public SpriteFontBlitModes BlitMode = SpriteFontBlitModes.Immediate;

	/// <summary>
	/// If the generated character images should premultiply their alpha.
	/// This should be true if you render the SpriteFont with the default
	/// Premultiply BlendMode.
	/// Note that this property does not modify already-created characters.
	/// </summary>
	public bool PremultiplyAlpha = true;

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
	private readonly List<Page> texturePages = [];
	private Color[] buffer = [];

	public SpriteFont(Font font, float size, ReadOnlySpan<int> prebakedCodepoints = default, bool premultiplyAlpha = true)
	{
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

	public SpriteFont(string path, float size, ReadOnlySpan<int> prebakedCodepoints = default, bool premultiplyAlpha = true)
		: this(new Font(path), size, prebakedCodepoints, premultiplyAlpha)
	{

	}

	public SpriteFont(Stream stream, float size, ReadOnlySpan<int> prebakedCodepoints = default, bool premultiplyAlpha = true)
		: this(new Font(stream), size, prebakedCodepoints, premultiplyAlpha)
	{
		
	}

	public SpriteFont(float size = 16)
	{
		Font = null;
		Size = size;
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
				writeLinesTo.Add((start, text.Length - start));
				break;
			}
		}
	}

	/// <summary>
	/// Prepares the given characters for rendering.
	/// If immediate is true, it will render and update every character immediately.
	/// Otherwise, it queues them off-thread.
	/// </summary>
	public void PrepareCharacters(ReadOnlySpan<int> codepoints, bool immediate)
	{
		foreach (var it in codepoints)
			PrepareCharacter(it, immediate, false);

		if (immediate && codepoints.Length > 0)
		{
			foreach (var page in texturePages)
				page.Upload();
		}
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
	/// Gets a Character from the SpriteFont.
	/// Note that the Character may not yet be ready if BlitMode is set
	/// to SpriteFontBlitModes.Streaming.
	/// </summary>
	public Character GetCharacter(int codepoint)
	{
		if (!characters.TryGetValue(codepoint, out var value))
		{
			// we are not allowed to dynamically create new characters
			if (BlitMode == SpriteFontBlitModes.Manual)
				return new();

			// try to create the character
			value = PrepareCharacter(
				codepoint: codepoint,
				immediate: BlitMode == SpriteFontBlitModes.Immediate,
				immediateUploadToTexture: BlitMode == SpriteFontBlitModes.Immediate
			);
		}

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
			RenderText(batch, text[Start..(Start + Length)], position, justify, color);
			position.Y += LineHeight;
		}

		Pool.Return(lines);
	}

	private Character PrepareCharacter(int codepoint, bool immediate, bool immediateUploadToTexture)
	{
		var advance = 0.0f;
		var offset = Vector2.Zero;
		var subtex = new Subtexture();
		var exists = false;

		if (Font != null)
		{
			var scale = Font.GetScale(Size);
			var glyph = Font.GetGlyphIndex(codepoint);
			var metrics = Font.GetCharacterOfGlyph(glyph, scale);

			advance = metrics.Advance;
			offset = metrics.Offset;
			subtex = new Subtexture(null, default, new Rect(0, 0, metrics.Width, metrics.Height));
			exists = glyph != 0;

			// request that the character be rendered and added to our texture
			if (metrics.Visible)
			{
				if (immediate)
				{
					if (TryBlitCharacter(Font, metrics, ref buffer, PremultiplyAlpha) &&
						TryPackCharacter(buffer, metrics.Width, metrics.Height, immediateUploadToTexture, out var tex))
						subtex = tex;
				}
				else
				{
					if (BlitModule.Instance == null)
						App.Register<BlitModule>();
					BlitModule.Instance?.Queue(this, codepoint, metrics);
				}
			}
		}

		return characters[codepoint] = new(
			codepoint,
			subtex,
			advance,
			offset,
			exists
		);
	}

	private static bool TryBlitCharacter(Font font, in Font.Character ch, ref Color[] buffer, bool premultiply)
	{
		var length = ch.Width * ch.Height;
		if (buffer.Length <= length)
			Array.Resize(ref buffer, length);

		if (font.GetPixels(ch, buffer))
		{
			if (premultiply)
			{
				for (int i = 0; i < length; i ++)
					buffer[i] = buffer[i].Premultiply();
			}

			return true;
		}

		return false;
	}

	private bool TryPackCharacter(Color[] buffer, int width, int height, bool uploadToTexture, out Subtexture subtexture)
	{
		// TODO:
		// Ideally the pages could expand (up to a maximum size) if needed.
		// The reason this can't be done at the moment is because every Character
		// in the sprite font would need their Subtexture re-assigned, so the
		// texture pages would need to keep track of that somehow and update them
		// if it decides to grow its textures.
		// Alternatively Textures could have a Resize method.
		var pageSize = (int)Math.Min(4096, Size * 16);

		// unusual case where somehow the character is gigantic and can't fit into a
		// texture page .... in this scenario, throw a warning and don't render it
		if (width > pageSize || height > pageSize)
		{
			Log.Warning($"SpriteFont Character was too large to render to a Texture!");
			subtexture = default;
			return false;
		}

		var pageIndex = 0;
		while (true)
		{
			if (pageIndex >= texturePages.Count)
				texturePages.Add(new(pageSize));
			
			if (texturePages[pageIndex].TryPack(buffer, width, height, uploadToTexture, out subtexture))
				return true;

			pageIndex++;
		}
	}

	/// <summary>
	/// This is an internal module that blits Font characters, and then uploads
	/// them to textures for the Sprite Font to use. This way the program does
	/// not halt and wait for individual characters to be rendered.
	/// </summary>
	private class BlitModule : Module
	{
		private class BlitTask
		{
			public SpriteFont? SpriteFont;
			public int CodePoint;
			public Color[] Buffer = [];
			public Font.Character Metrics;
			public bool BufferContainsValidData;
		}

		public static BlitModule? Instance = null;

		private readonly Queue<Task<BlitTask>> runningTasks = [];
		private readonly HashSet<SpriteFont> fontsToUpload = [];
		private readonly HashSet<SpriteFont> fontsUploaded = [];
		private readonly Stopwatch uploadTimer = new();

		private const int MaximumMillisecondsPerFrame = 3;

		public override void Startup() => Instance = this;
		public override void Shutdown() => Instance = null;

		public override void Update()
		{
			uploadTimer.Restart();

			// find all finished tasks
			while (runningTasks.TryDequeue(out var task))
			{
				if (uploadTimer.ElapsedMilliseconds >= MaximumMillisecondsPerFrame)
				{
					runningTasks.Enqueue(task);
					break;
				}
				else if (!task.IsCompleted)
				{
					runningTasks.Enqueue(task);
					continue;
				}
				else
				{
					var it = task.Result;
					var width = it.Metrics.Width;
					var height = it.Metrics.Height;

					// pack the character into the page, and reassign its subtexture
					// TODO: should packing the character into the page be part of the thread?
					if (it.SpriteFont != null &&
						it.SpriteFont.TryPackCharacter(it.Buffer, width, height, false, out var subtex))
					{
						it.SpriteFont.AddCharacter(it.SpriteFont.GetCharacter(it.CodePoint) with { Subtexture = subtex });
						fontsToUpload.Add(it.SpriteFont);
					}

					it.SpriteFont = null;
					Pool.Return(it);
				}
			}

			// upload page data to gpu textures
			{
				foreach (var it in fontsToUpload)
				{
					foreach (var page in it.texturePages)
						page.Upload();
					fontsUploaded.Add(it);
					if (uploadTimer.ElapsedMilliseconds >= MaximumMillisecondsPerFrame)
						break;
				}
				
				foreach (var it in fontsUploaded)
					fontsToUpload.Remove(it);
				fontsUploaded.Clear();
			}
		}

		public void Queue(SpriteFont spriteFont, int codepoint, in Font.Character metrics)
		{
			var task = Pool.Get<BlitTask>();
			task.SpriteFont = spriteFont;
			task.CodePoint = codepoint;
			task.Metrics = metrics;
			runningTasks.Enqueue(Task.Factory.StartNew(static (object? state) =>
			{
				var result = (BlitTask)state!;
				result.BufferContainsValidData =
					result.SpriteFont?.Font != null &&
					TryBlitCharacter(result.SpriteFont.Font, result.Metrics, ref result.Buffer, result.SpriteFont.PremultiplyAlpha);
				return result;
			}, task));
		}
	}

	private class Page(int size)
	{
		private record struct Node(int Left, int Right, RectInt Bounds);
		private readonly Image image = new(size, size);
		private readonly Texture texture = new(size, size, TextureFormat.Color);
		private readonly List<Node> nodes = [ new() { Bounds = new(0, 0, size, size) } ];
		private bool textureDirty;

		public bool TryPack(Color[] buffer, int width, int height, bool uploadToTexture, out Subtexture result)
		{
			int index = TryPackNode(0, width + 2, height + 2);

			if (index >= 0)
			{
				var node = nodes[index];
				image.CopyPixels(buffer, width, height, new Point2(node.Bounds.X + 1, node.Bounds.Y + 1));
				result = new Subtexture(texture, node.Bounds, new Rect(1, 1, width, height));

				if (uploadToTexture)
				{
					// TODO: partial rectangle upload
					texture.SetData<Color>(image.Data);
				}
				else
				{
					textureDirty = true;
				}

				return true;
			}

			result = default;
			return false;
		}

		public void Upload()
		{
			if (textureDirty)
			{
				texture.SetData<Color>(image.Data);
				textureDirty = false;
			}
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
}
