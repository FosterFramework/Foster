namespace Foster.Framework;

[Obsolete("Use App Methods instead")]
public static class Graphics
{
	[Obsolete("Use App.Graphics.OriginBottomLeft")]
	public static bool OriginBottomLeft => App.Graphics.OriginBottomLeft;

	[Obsolete("No Longer a max texture size")]
	public const int MaxTextureSize = 8192;

	[Obsolete("Use App.Clear")]
	public static void Clear(Color color) 
		=> App.Renderer.Clear(null, color, 0, 0, ClearMask.Color);

	[Obsolete("Use App.Clear")]
	public static unsafe void Clear(Color color, float depth, int stencil, ClearMask mask)
		=> App.Renderer.Clear(null, color, depth, stencil, mask);

	[Obsolete("Use App.Draw")]
	public static unsafe void Submit(in DrawCommand command)
		=> App.Renderer.Draw(command);
}
