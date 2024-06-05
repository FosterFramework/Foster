using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
	public static readonly int[] Ascii;

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
	/// If the SpriteFont is allowed to dynamically blit Characters as they are
	/// requested. If this is false, no new characters that have not already been
	/// rendered will be created.
	/// </summary>
	public bool DynamicBlittingEnabled = true;

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

	private readonly float fontScale = 1.0f;
	private readonly Dictionary<int, Character> characters = [];
	private readonly Dictionary<KerningPair, float> kerning = [];
	private readonly List<Page> texturePages = [];
	private Color[] buffer = [];

	static SpriteFont()
	{
		var ascii = new List<int>();
		for (int i = 32; i < 128; i ++)
			ascii.Add(i);
		Ascii = [.. ascii];
	}

	public SpriteFont(Font font, float size, ReadOnlySpan<int> prebakedCodepoints = default)
	{
		Font = font;
		Size = size;
		fontScale = font.GetScale(size);
		Ascent = font.Ascent * fontScale;
		Descent = font.Descent * fontScale;
		LineGap = font.LineGap * fontScale;

		if (prebakedCodepoints.Length > 0)
			PrepareCharacters(prebakedCodepoints, true);
	}

	public SpriteFont(string path, float size, ReadOnlySpan<int> prebakedCodepoints = default)
		: this(new Font(path), size, prebakedCodepoints)
	{

	}

	public SpriteFont(Stream stream, float size, ReadOnlySpan<int> prebakedCodepoints = default)
		: this(new Font(stream), size, prebakedCodepoints)
	{
		
	}

	public SpriteFont(float size = 16)
	{
		Font = null;
		Size = size;
	}

	public Character this[int codepoint] => GetCharacter(codepoint);
	public Character this[char ch] => GetCharacter(ch);

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

	public Vector2 SizeOf(ReadOnlySpan<char> text)
	{
		return new Vector2(
			WidthOf(text),
			HeightOf(text)
		);
	}

	/// <summary>
	/// Prepares the given characters for rendering.
	/// If immediate is true, it will render and update every character immediately.
	/// Otherwise, it queues them off-thread.
	/// </summary>
	public void PrepareCharacters(ReadOnlySpan<int> codepoints, bool immediate)
	{
		foreach (var it in codepoints)
			PrepareCharacter(it, immediate);

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
	/// Note that the Character may not yet be rendered, in which case its
	/// Subtexture value will have no texture assigned. 
	/// Requesting a Character will queue it to be rendered in another thread,
	/// unless DynamicBlittingEnabled is false, in which case an empty struct
	/// is returned.
	/// </summary>
	public Character GetCharacter(int codepoint)
	{
		if (!characters.TryGetValue(codepoint, out var value))
		{
			// we are not allowed to dynamically create new characters
			if (!DynamicBlittingEnabled)
				return new();

			// try to create the character
			value = PrepareCharacter(codepoint, false);
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
		// TODO:
		// I feel like the vertical alignment is slightly off, but not sure how.

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

	private Character PrepareCharacter(int codepoint, bool immediate)
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
					if (TryBlitCharacter(Font, metrics, ref buffer) &&
						TryPackCharacter(buffer, metrics.Width, metrics.Height, out var tex))
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

	private static bool TryBlitCharacter(Font font, in Font.Character ch, ref Color[] buffer)
	{
		var length = ch.Width * ch.Height;
		if (buffer.Length <= length)
			Array.Resize(ref buffer, length);

		if (font.GetPixels(ch, buffer))
		{
			for (int i = 0; i < length; i ++)
				buffer[i] = buffer[i].Premultiply();
			return true;
		}

		return false;
	}

	private bool TryPackCharacter(Color[] buffer, int width, int height, out Subtexture subtexture)
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
			
			if (texturePages[pageIndex].TryPack(buffer, width, height, out subtexture))
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
				if (!task.IsCompleted)
				{
					runningTasks.Enqueue(task);
					continue;
				}
				else if (uploadTimer.ElapsedMilliseconds > MaximumMillisecondsPerFrame)
				{
					runningTasks.Enqueue(task);
					break;
				}
				else
				{
					var it = task.Result;
					var width = it.Metrics.Width;
					var height = it.Metrics.Height;

					// pack the character into the page, and reassign its subtexture
					// TODO: should packing the character into the page be part of the thread?
					if (it.SpriteFont != null &&
						it.SpriteFont.TryPackCharacter(it.Buffer, width, height, out var subtex))
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
					TryBlitCharacter(result.SpriteFont.Font, result.Metrics, ref result.Buffer);
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

		public bool TryPack(Color[] buffer, int width, int height, out Subtexture result)
		{
			int index = TryPackNode(0, width + 2, height + 2);

			if (index >= 0)
			{
				var node = nodes[index];
				image.CopyPixels(buffer, width, height, new Point2(node.Bounds.X + 1, node.Bounds.Y + 1));
				result = new Subtexture(texture, node.Bounds, new Rect(1, 1, width, height));
				textureDirty = true;
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
		Text(batch, font, text, position, Vector2.Zero, color);
	}

	public static void Text(this Batcher batch, SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
	{
		font.RenderText(batch, text, position, justify, color);
	}
}
