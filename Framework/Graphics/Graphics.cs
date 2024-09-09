
namespace Foster.Framework
{
	public static class Graphics
	{
		private static bool vsyncEnabled = true;

		public static bool VSync
		{
			get => vsyncEnabled;
			set
			{
				if (vsyncEnabled != value)
				{
					vsyncEnabled = value;
					throw new NotImplementedException();
				}
			}
		}

		/// <summary>
		/// The current Renderer API in use
		/// </summary>
		public static GraphicsDriver Driver => Renderer.Driver;

		/// <summary>
		/// Width of the Back Buffer, in Pixels
		/// </summary>
		public static int Width => App.WidthInPixels;

		/// <summary>
		/// Height of the Back Buffer, in Pixels
		/// </summary>
		public static int Height => App.HeightInPixels;

		/// <summary>
		/// Maximum Texture Size
		/// </summary>
		public static int MaxTextureSize { get; private set; } = 8192;

		/// <summary>
		/// If our (0,0) in our coordinate system is bottom-left.
		/// </summary>
		public static bool OriginBottomLeft => false;

		/// <summary>
		/// Clears the Back Buffer to a given Color
		/// </summary>
		public static void Clear(Color color) 
			=> Renderer.Clear(null, color, 0, 0, ClearMask.Color);

		/// <summary>
		/// Clears the Back Buffer
		/// </summary>
		public static unsafe void Clear(Color color, float depth, int stencil, ClearMask mask)
			=> Renderer.Clear(null, color, depth, stencil, mask);

		/// <summary>
		/// Submits a Draw Command to the GPU
		/// </summary>
		/// <param name="command"></param>
		public static unsafe void Submit(in DrawCommand command)
			=> Renderer.Draw(command);
	}
}
