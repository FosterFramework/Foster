using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// Queries and rasterizes characters from a Font File.
/// To draw a font to the screen, use <see cref="SpriteFont"/>.
/// </summary>
public class Font : IDisposable
{
	public struct Character
	{
		public int GlyphIndex;
		public int Width;
		public int Height;
		public float Advance;
		public Vector2 Offset;
		public float Scale;
		public bool Visible;
	}

	private IntPtr fontPtr;
	private IntPtr dataPtr;
	private GCHandle dataHandle;
	private int dataLength;
	private readonly Dictionary<int, int> codepointToGlyphLookup = [];
	private static readonly Exception invalidFontException = new("Attempting to use an invalid/disposed Font");

	public int Ascent { get; private set; }
	public int Descent { get; private set; }
	public int LineGap { get; private set; }
	public int Height => Ascent - Descent;
	public int LineHeight => Ascent - Descent + LineGap;

	public bool Disposed { get; private set; } = false;

	public Font(Stream stream)
	{
		Load(stream);
	}

	public Font(string path)
	{
		using var stream = File.OpenRead(path);
		Load(stream);
	}

	public Font(byte[] bytes)
	{
		MemoryStream stream = new(bytes, false);
		Load(stream);
	}

	~Font() => Dispose();

	private void Load(Stream stream)
	{
		// allocate enough room for the buffer
		byte[] buffer = Calc.ReadAllBytes(stream);

		// pin the buffer
		dataHandle =  GCHandle.Alloc(buffer, GCHandleType.Pinned);
		dataPtr = dataHandle.AddrOfPinnedObject();
		dataLength = buffer.Length;

		// create the font ptr
		fontPtr = Platform.FontInit(dataPtr, dataLength);
		if (fontPtr == IntPtr.Zero)
			throw new Exception("Unable to parse Font Data");

		// get font properties
		Platform.FontGetMetrics(fontPtr, out int ascent, out int descent, out int linegap);
		Ascent = ascent;
		Descent = descent;
		LineGap = linegap;

		// TODO: is this OK to do?
		// some fonts don't seem to use LineGap and rely on Descent
		// so we can override that to make our line gap work properly
		if (LineGap <= 0 && Descent < 0)
		{
			LineGap = -Descent;
			Descent = 0;
		}
	}

	/// <summary>
	/// Gets the Glyph Index of a given Unicode Codepoint
	/// </summary>
	public int GetGlyphIndex(int codepoint)
	{
		if (!codepointToGlyphLookup.TryGetValue(codepoint, out var glyphIndex))
			codepointToGlyphLookup[codepoint] = glyphIndex = Platform.FontGetGlyphIndex(fontPtr, codepoint);

		return glyphIndex;
	}

	/// <summary>
	/// Gets the Glyph Index of the given Char
	/// </summary>
	public int GetGlyphIndex(char ch)
	{
		return GetGlyphIndex((int)ch);
	}

	/// <summary>
	/// Gets the scale value of the Font for a requested size in pixels
	/// </summary>
	public float GetScale(float size)
	{
		if (fontPtr == IntPtr.Zero)
			throw invalidFontException;
		return Platform.FontGetScale(fontPtr, size);
	}

	/// <summary>
	/// Gets the kerning value between two chars at a given scale
	/// </summary>
	public float GetKerning(char ch1, char ch2, float scale)
	{
		var glyph1 = GetGlyphIndex(ch1);
		var glyph2 = GetGlyphIndex(ch2);
		return GetKerningBetweenGlyphs(glyph1, glyph2, scale);
	}

	/// <summary>
	/// Gets the kerning value between two unicode codepoints at a given scale
	/// </summary>
	public float GetKerning(int codepoint1, int codepoint2, float scale)
	{
		var glyph1 = GetGlyphIndex(codepoint1);
		var glyph2 = GetGlyphIndex(codepoint2);
		return GetKerningBetweenGlyphs(glyph1, glyph2, scale);
	}

	/// <summary>
	/// Gets the kerning value between two glyphs at a given scale
	/// </summary>
	public float GetKerningBetweenGlyphs(int glyph1, int glyph2, float scale)
	{
		if (fontPtr == IntPtr.Zero)
			throw invalidFontException;
		return Platform.FontGetKerning(fontPtr, glyph1, glyph2, scale);
	}

	/// <summary>
	/// Gets Character Metrics of a given char at a given scale
	/// </summary>
	public Character GetCharacter(char ch, float scale)
	{
		if (fontPtr == IntPtr.Zero)
			throw invalidFontException;
		return GetCharacterOfGlyph(GetGlyphIndex(ch), scale);
	}

	/// <summary>
	/// Gets Character Metrics of a given unicode codepoint at a given scale
	/// </summary>
	public Character GetCharacter(int codepoint, float scale)
	{
		if (fontPtr == IntPtr.Zero)
			throw invalidFontException;
		return GetCharacterOfGlyph(GetGlyphIndex(codepoint), scale);
	}

	/// <summary>
	/// Gets Character Metrics of a given glyph at a given scale
	/// </summary>
	public Character GetCharacterOfGlyph(int glyphIndex, float scale)
	{
		if (fontPtr == IntPtr.Zero)
			throw invalidFontException;

		Platform.FontGetCharacter(fontPtr, glyphIndex, scale,
			out int width, out int height, out float advance, out float offsetX, out float offsetY, out int visible);

		return new()
		{
			GlyphIndex = glyphIndex,
			Width = width,
			Height = height,
			Advance = advance,
			Offset = new Vector2(offsetX, offsetY),
			Scale = scale,
			Visible = visible != 0,
		};
	}

	/// <summary>
	/// Renders the given character to an Image and returns it
	/// </summary>
	public Image? GetImage(char ch, float scale)
	{
		return GetImage(GetCharacter(ch, scale));
	}

	/// <summary>
	/// Renders a character to an Image and returns it
	/// </summary>
	public Image? GetImage(in Character character)
	{
		if (!character.Visible)
			return null;

		var img = new Image(character.Width, character.Height);
		GetPixels(character, img.Data);
		return img;
	}

	/// <summary>
	/// Renders a character to the given Color buffer.
	/// </summary>
	public bool GetPixels(in Character character, Span<Color> destination)
	{
		if (fontPtr == IntPtr.Zero)
			throw invalidFontException;

		if (!character.Visible)
			return false;

		if (destination.Length < character.Width * character.Height)
			return false;

		unsafe
		{
			fixed (Color* ptr = destination)
				Platform.FontGetPixels(fontPtr, new(ptr), character.GlyphIndex, character.Width, character.Height, character.Scale);
		}

		return true;
	}

	public void Dispose()
	{
		Disposed = true;
		GC.SuppressFinalize(this);

		if (dataPtr != IntPtr.Zero)
		{
			dataHandle.Free();
			dataHandle = new();
			dataPtr = IntPtr.Zero;
		}

		if (fontPtr != IntPtr.Zero)
		{
			Platform.FontFree(fontPtr);
			fontPtr = IntPtr.Zero;
		}
	}
}
