
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A Subtexture, representing a rectangular segment of a Texture
/// </summary>
public struct Subtexture
{
	public static readonly Subtexture Empty = new();

	/// <summary>
	/// The Texture coordinates. These are set automatically based on the Source rectangle
	/// </summary>
	public Vector2 TexCoords0 = new();
	public Vector2 TexCoords1 = new();
	public Vector2 TexCoords2 = new();
	public Vector2 TexCoords3 = new();

	/// <summary>
	/// The draw coordinates. These are set automatically based on the Source and Frame rectangle
	/// </summary>
	public Vector2 DrawCoords0 = new();
	public Vector2 DrawCoords1 = new();
	public Vector2 DrawCoords2 = new();
	public Vector2 DrawCoords3 = new();

	/// <summary>
	/// The Texture this Subtexture is... a subtexture of
	/// </summary>
	public Texture? Texture;

	/// <summary>
	/// The source rectangle to sample from the Texture
	/// </summary>
	public Rect Source;

	/// <summary>
	/// The frame of the Subtexture. This is useful if you trim transparency and want to store the original size of the image
	/// For example, if the original image was (64, 64), but the trimmed version is (32, 48), the Frame may be (-16, -8, 64, 64)
	/// </summary>
	public Rect Frame;

	/// <summary>
	/// The Draw Width of the Subtexture
	/// </summary>
	public readonly float Width => Frame.Width;

	/// <summary>
	/// The Draw Height of the Subtexture
	/// </summary>
	public readonly float Height => Frame.Height;

	public Subtexture()
	{

	}

	public Subtexture(Texture? texture)
		: this(texture, new Rect(0, 0, texture?.Width ?? 0, texture?.Height ?? 0), new Rect(0, 0, texture?.Width ?? 0, texture?.Height ?? 0))
	{

	}

	public Subtexture(Texture? texture, Rect source)
		: this(texture, source, new Rect(0, 0, source.Width, source.Height))
	{

	}

	public Subtexture(Texture? texture, Rect source, Rect frame)
	{
		Texture = texture;
		Source = source;
		Frame = frame;

		DrawCoords0.X = -frame.X;
		DrawCoords0.Y = -frame.Y;
		DrawCoords1.X = -frame.X + source.Width;
		DrawCoords1.Y = -frame.Y;
		DrawCoords2.X = -frame.X + source.Width;
		DrawCoords2.Y = -frame.Y + source.Height;
		DrawCoords3.X = -frame.X;
		DrawCoords3.Y = -frame.Y + source.Height;

		if (Texture != null)
		{
			var px = 1.0f / Texture.Width;
			var py = 1.0f / Texture.Height;

			var tx0 = source.X * px;
			var ty0 = source.Y * py;
			var tx1 = source.Right * px;
			var ty1 = source.Bottom * py;

			TexCoords0.X = tx0;
			TexCoords0.Y = ty0;
			TexCoords1.X = tx1;
			TexCoords1.Y = ty0;
			TexCoords2.X = tx1;
			TexCoords2.Y = ty1;
			TexCoords3.X = tx0;
			TexCoords3.Y = ty1;
		}
	}

	public readonly (Rect Source, Rect Frame) GetClip(in Rect clip)
	{
		(Rect Source, Rect Frame) result;

		result.Source = (clip + Source.Position + Frame.Position).OverlapRect(Source);

		result.Frame.X = MathF.Min(0, Frame.X + clip.X);
		result.Frame.Y = MathF.Min(0, Frame.Y + clip.Y);
		result.Frame.Width = clip.Width;
		result.Frame.Height = clip.Height;

		return result;
	}

	public readonly (Rect Source, Rect Frame) GetClip(float x, float y, float w, float h)
	{
		return GetClip(new Rect(x, y, w, h));
	}

	public readonly Subtexture GetClipSubtexture(in Rect clip)
	{
		var (source, frame) = GetClip(clip);
		return new Subtexture(Texture, source, frame);
	}
}
