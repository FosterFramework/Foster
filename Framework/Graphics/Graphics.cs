
using System.Diagnostics;

namespace Foster.Framework
{
	public static class Graphics
	{
		/// <summary>
		/// The current Renderer API in use
		/// </summary>
		public static Renderers Renderer { get; private set; } = Renderers.None;

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
		public static int MaxTextureSize { get; private set; }

		/// <summary>
		/// If our (0,0) in our coordinate system is bottom-left.
		/// This is true in OpenGL
		/// </summary>
		public static bool OriginBottomLeft => Renderer == Renderers.OpenGL;

		/// <summary>
		/// Sets up Graphics properties
		/// </summary>
		internal static void Initialize()
		{
			Renderer = Platform.FosterGetRenderer();

			// TODO: actually query the graphics device for this
			MaxTextureSize = 8192;
		}

		/// <summary>
		/// Clears the Back Buffer to a given Color
		/// </summary>
		public static void Clear(Color color)
		{
			Clear(color, 0, 0, ClearMask.Color);
		}

		/// <summary>
		/// Clears the Back Buffer
		/// </summary>
		public static void Clear(Color color, int depth, int stencil, ClearMask mask)
		{
			Platform.FosterClearCommand clear = new()
			{
				target = IntPtr.Zero,
				clip = new(0, 0, Width, Height),
				color = color,
				depth = depth,
				stencil = stencil,
				mask = mask
			};
			Platform.FosterClear(ref clear);
		}

		public static void Submit(in DrawCommand command)
		{
			Debug.Assert(command.Target == null || !command.Target.IsDisposed, "Target is invalid");
			Debug.Assert(command.Mesh != null && !command.Mesh.IsDisposed, "Mesh is Invalid");
			Debug.Assert(command.Shader != null && !command.Shader.IsDisposed, "Mesh is Invalid");

			Platform.FosterDrawCommand fc = new()
			{
				target = (command.Target != null && !command.Target.IsDisposed ? command.Target.resource : IntPtr.Zero),
				mesh = (command.Mesh != null && !command.Mesh.IsDisposed ? command.Mesh.resource : IntPtr.Zero),
				shader = (command.Shader != null && !command.Shader.IsDisposed ? command.Shader.resource : IntPtr.Zero),
				hasViewport = command.Viewport.HasValue ? 1 : 0,
				hasScissor = command.Scissor.HasValue ? 1 : 0,
				indexStart = command.MeshIndexStart,
				indexCount = command.MeshIndexCount,
				instanceCount = 0,
				compare = command.DepthCompare,
				cull = command.CullMode,
				blend = command.BlendMode,
			};

			if (command.Viewport.HasValue)
			{
				fc.viewport = new () { 
					x = command.Viewport.Value.X, y = command.Viewport.Value.Y, 
					w = command.Viewport.Value.Width, h = command.Viewport.Value.Height 
				};
			}

			if (command.Scissor.HasValue)
			{
				fc.scissor = new () { 
					x = command.Scissor.Value.X, y = command.Scissor.Value.Y, 
					w = command.Scissor.Value.Width, h = command.Scissor.Value.Height 
				};
			}

			Platform.FosterDraw(ref fc);
		}
	}
}