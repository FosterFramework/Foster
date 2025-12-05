using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// Font Data loaded from a generated MSDF Atlas.<br /><br/>
/// You can create MSDF font atlases using msdf-atlas-gen:<br/>
/// https://github.com/Chlumsky/msdf-atlas-gen <br/><br/>
/// You can see examples of the default generated fonts in Foster's Content Directory:<br />
/// https://github.com/FosterFramework/Foster/tree/main/Framework/Content/Fonts
/// </summary>
public partial class MsdfFont : IProvideKerning
{
	// TODO: include kerning pairs

	private partial class GeneratedMsdfData
	{
		public struct AtlasProperties
		{
			public string Type;
			public float DistanceRange;
			public float DistanceRangeMiddle;
			public float Size;
			public float Width;
			public float Height;
			public string YOrigin;
		}

		public struct MetricsProperties
		{
			public float EmSize;
			public float LineHeight;
			public float Ascender;
			public float Descender;
			public float UnderlineY;
			public float UnderlineThickness;
		}

		public struct Bounds
		{
			public float Left;
			public float Top;
			public float Right;
			public float Bottom;
		}

		public struct Glyph
		{
			public int Unicode;
			public float Advance;
			public Bounds PlaneBounds;
			public Bounds AtlasBounds;
		}

		public AtlasProperties Atlas;
		public MetricsProperties Metrics;
		public List<Glyph> Glyphs = [];
	}

	public readonly record struct Character(
		int Codepoint,
		Rect SourceRect,
		float Advance,
		Vector2 Offset
	);

	public readonly Image Image;
	public readonly float Size;
	public readonly float Ascent;
	public readonly float Descent;
	public readonly float LineGap;
	public readonly float Height;
	public readonly float LineHeight;
	public readonly float DistanceRange;
	public readonly Character[] Characters;

	public MsdfFont(Image atlasImage, byte[] atlasData)
	{
		Image = atlasImage;
		Characters = [];
		if (JsonSerializer.Deserialize<GeneratedMsdfData>(atlasData, GeneratedMsdfDataContext.Default.GeneratedMsdfData) is not {}  data)
			return;

		Size = data.Atlas.Size;
		Ascent = MathF.Abs(Size * data.Metrics.Ascender);
		Descent = -MathF.Abs(Size * data.Metrics.Descender);
		Height = Ascent - Descent;
		LineGap = Size * data.Metrics.LineHeight - Height;
		LineHeight = Ascent - Descent + LineGap;
		DistanceRange = data.Atlas.DistanceRange;

		var characters = new List<Character>();

		foreach (var ch in data.Glyphs)
		{
			characters.Add(new(
				Codepoint: ch.Unicode,
				SourceRect: new Rect(ch.AtlasBounds.Left, ch.AtlasBounds.Top, ch.AtlasBounds.Right - ch.AtlasBounds.Left, ch.AtlasBounds.Bottom - ch.AtlasBounds.Top),
				Advance: ch.Advance * Size,
				Offset: new Vector2(ch.PlaneBounds.Left, ch.PlaneBounds.Top) * Size
			));
		}

		Characters = [..characters];
	}

	public MsdfFont(string atlasImageFile, string atlasDataFile)
		: this(new Image(atlasImageFile), File.ReadAllBytes(atlasDataFile)) {}

	public float GetKerning(int codepointA, int codepointB, float size)
	{
		// TODO: load MSDF kerning data
		return 0f;
	}

	[JsonSerializable(typeof(GeneratedMsdfData))]
	[JsonSourceGenerationOptions(
		IncludeFields = true,
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true
	)]
	private partial class GeneratedMsdfDataContext : JsonSerializerContext {}
}