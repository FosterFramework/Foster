namespace Foster.Framework;

/// <summary>
/// A graphical resource that can be drawn to.
/// </summary>
public interface IDrawableTarget
{
	/// <summary>
	/// Internal Surface Object.<br/>
	/// Either a <see cref="Window"/> or <see cref="Target"/>.
	/// </summary>
	protected internal object? Surface { get; }

	/// <summary>
	/// Graphics Device this target is attached to
	/// </summary>
	public GraphicsDevice GraphicsDevice { get; }

	/// <summary>
	/// Width, in pixels, of this Target
	/// </summary>
	/// <value></value>
	public int WidthInPixels { get; }

	/// <summary>
	/// Height, in pixels, of this Target
	/// </summary>
	public int HeightInPixels { get; }
}

/// <summary>
/// <see cref="IDrawableTarget"/> Extension methods
/// </summary>
public static class IDrawableTargetExt
{
	/// <summary>
	/// Size, in pixels, of the Target
	/// </summary>
	public static Point2 SizeInPixels(this IDrawableTarget target)
		=> new(target.WidthInPixels, target.HeightInPixels);

	/// <summary>
	/// Bounds, in pixels, of the Target
	/// </summary>
	public static RectInt BoundsInPixels(this IDrawableTarget target)
		=> new(0, 0, target.WidthInPixels, target.HeightInPixels);

	/// <summary>
	/// Clears a Target's textures to the provided values
	/// </summary>
	public static void Clear(this IDrawableTarget target, ReadOnlySpan<Color> color, float depth, int stencil, ClearMask mask)
	{
		target.GraphicsDevice.Clear(target, color, depth, stencil, mask);
	}

	/// <summary>
	/// Clears a Target's textures to the provided values
	/// </summary>
	public static void Clear(this IDrawableTarget target, Color color, float depth, int stencil, ClearMask mask)
	{
		target.GraphicsDevice.Clear(target, [color], depth, stencil, mask);
	}

	/// <summary>
	/// Clears a Target's color textures to the provided color value
	/// </summary>
	public static void Clear(this IDrawableTarget target, Color color)
	{
		target.Clear(color, 0, 0, ClearMask.Color);
	}
}