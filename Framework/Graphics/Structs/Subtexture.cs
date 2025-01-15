using System.Numerics;
using System.Runtime.CompilerServices;

namespace Foster.Framework;

/// <summary>
/// A Subtexture representing a rectangular segment of a <see cref="Texture"/>
/// </summary>
public readonly struct Subtexture
{
	/// <summary>
	/// Holds 4 Vector2 coordinate components
	/// </summary>
	[InlineArray(4)]
	public struct CoordinateBuffer { private Vector2 element; }

	/// <summary>
	/// An empty (default) Subtexture
	/// </summary>
	public static readonly Subtexture Empty = default;

	/// <summary>
	/// The Texture this Subtexture is... a subtexture of
	/// </summary>
	public readonly Texture? Texture;

	/// <summary>
	/// The source rectangle to sample from the Texture
	/// </summary>
	public readonly Rect Source;

	/// <summary>
	/// The frame of the Subtexture. This is useful if you trim transparency and want to store the original size of the image.
	/// For example, if the original image was (64, 64), but the trimmed version is (32, 48), the Frame may be (-16, -8, 64, 64)
	/// </summary>
	public readonly Rect Frame;

	/// <summary>
	/// The texture UV coordinates. These are set automatically based on the Source rectangle
	/// </summary>
	public readonly CoordinateBuffer TexCoords;

	/// <summary>
	/// The draw coordinates. These are set automatically based on the Source and Frame rectangle
	/// </summary>
	public readonly CoordinateBuffer DrawCoords;

	/// <summary>
	/// The drawable Width of the Subtexture, in pixels
	/// </summary>
	public readonly float Width => Frame.Width;

	/// <summary>
	/// The drawable Height of the Subtexture, in pixels
	/// </summary>
	public readonly float Height => Frame.Height;

	/// <summary>
	/// The drawable size of the Textue, in pixels
	/// </summary>
	public readonly Vector2 Size => new(Width, Height);

	/// <summary>
	/// Constructs an Empty Subtexture
	/// </summary>
	public Subtexture() {}

	/// <summary>
	/// Constructs an Subtexture encompassing a full Texture
	/// </summary>
	public Subtexture(Texture? texture)
		: this(texture, new(0, 0, texture?.Width ?? 0, texture?.Height ?? 0), new(0, 0, texture?.Width ?? 0, texture?.Height ?? 0)) {}

	/// <summary>
	/// Constructs an Subtexture of part of a Texture
	/// </summary>
	public Subtexture(Texture? texture, Rect source)
		: this(texture, source, new(0, 0, source.Width, source.Height)) {}

	/// <summary>
	/// Constructs an Subtexture of part of a Texture
	/// </summary>
	public Subtexture(Texture? texture, Rect source, Rect frame)
	{
		Texture = texture;
		Source = source;
		Frame = frame;

		DrawCoords[0].X = -frame.X;
		DrawCoords[0].Y = -frame.Y;
		DrawCoords[1].X = -frame.X + source.Width;
		DrawCoords[1].Y = -frame.Y;
		DrawCoords[2].X = -frame.X + source.Width;
		DrawCoords[2].Y = -frame.Y + source.Height;
		DrawCoords[3].X = -frame.X;
		DrawCoords[3].Y = -frame.Y + source.Height;

		if (Texture != null && Texture.Width > 0 && Texture.Height > 0)
		{
			var px = 1.0f / Texture.Width;
			var py = 1.0f / Texture.Height;

			var tx0 = source.X * px;
			var ty0 = source.Y * py;
			var tx1 = source.Right * px;
			var ty1 = source.Bottom * py;

			TexCoords[0].X = tx0;
			TexCoords[0].Y = ty0;
			TexCoords[1].X = tx1;
			TexCoords[1].Y = ty0;
			TexCoords[2].X = tx1;
			TexCoords[2].Y = ty1;
			TexCoords[3].X = tx0;
			TexCoords[3].Y = ty1;
		}
	}

	/// <summary>
	/// Gets clipping values from the Subtexture
	/// </summary>
	public readonly (Rect Source, Rect Frame) GetClip(in Rect clip)
	{
		(Rect Source, Rect Frame) result;

		result.Source = (clip + Source.Position + Frame.Position).GetIntersection(Source);

		result.Frame.X = MathF.Min(0, Frame.X + clip.X);
		result.Frame.Y = MathF.Min(0, Frame.Y + clip.Y);
		result.Frame.Width = clip.Width;
		result.Frame.Height = clip.Height;

		return result;
	}

	/// <summary>
	/// Gets clipping values from the Subtexture
	/// </summary>
	public readonly (Rect Source, Rect Frame) GetClip(float x, float y, float w, float h)
	{
		return GetClip(new Rect(x, y, w, h));
	}

	/// <summary>
	/// Gets a subtexture of this Subtexture
	/// </summary>
	public readonly Subtexture GetClipSubtexture(in Rect clip)
	{
		var (source, frame) = GetClip(clip);
		return new Subtexture(Texture, source, frame);
	}
}
