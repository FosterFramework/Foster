namespace Foster.Framework;

[Obsolete("Use App Methods instead")]
public static class Graphics
{
	[Obsolete("Origin is never Bottom Left")]
	public const bool OriginBottomLeft = false;

	[Obsolete("No Longer a max texture size")]
	public const int MaxTextureSize = 8192;

	[Obsolete("Use App.Clear")]
	public static void Clear(Color color) 
		=> Renderer.Clear(null, color, 0, 0, ClearMask.Color);

	[Obsolete("Use App.Clear")]
	public static unsafe void Clear(Color color, float depth, int stencil, ClearMask mask)
		=> Renderer.Clear(null, color, depth, stencil, mask);

	[Obsolete("Use App.Draw")]
	public static unsafe void Submit(in DrawCommand command)
		=> Renderer.Draw(command);
}