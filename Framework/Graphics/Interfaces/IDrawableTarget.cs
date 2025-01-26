namespace Foster.Framework;

/// <summary>
/// A graphical resource that can be drawn to.<br/>
/// Either a <see cref="Window"/> or a <see cref="Target"/> 
/// </summary>
public interface IDrawableTarget
{
	public GraphicsDevice GraphicsDevice { get; }
	public int WidthInPixels { get; }
	public int HeightInPixels { get; }
}

/// <summary>
/// <see cref="IDrawableTarget"/> Extension methods
/// </summary>
public static class IDrawableTargetExt
{
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