
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
		public static unsafe void Clear(Color color, float depth, int stencil, ClearMask mask)
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
			Platform.FosterClear(&clear);
		}

		public static unsafe void Submit(in DrawCommand command)
		{
			IntPtr shader = IntPtr.Zero;
			if (command.Material != null && command.Material.Shader != null && !command.Material.Shader.IsDisposed)
				shader = command.Material.Shader.resource;

			if (shader == IntPtr.Zero)
				throw new Exception("Material Shader is Invalid");

			if (command.Target != null && command.Target.IsDisposed)
				throw new Exception("Mesh is Invalid");

			if (command.Mesh == null || command.Mesh.IsDisposed)
				throw new Exception("Target is invalid");

			Platform.FosterDrawCommand fc = new()
			{
				target = (command.Target != null && !command.Target.IsDisposed ? command.Target.resource : IntPtr.Zero),
				mesh = (command.Mesh != null && !command.Mesh.IsDisposed ? command.Mesh.resource : IntPtr.Zero),
				shader = shader,
				hasViewport = command.Viewport.HasValue ? 1 : 0,
				hasScissor = command.Scissor.HasValue ? 1 : 0,
				indexStart = command.MeshIndexStart,
				indexCount = command.MeshIndexCount,
				instanceCount = 0,
				compare = command.DepthCompare,
				depthMask = command.DepthMask ? 1 : 0,
				cull = command.CullMode,
				blend = command.BlendMode,
			};

			if (command.Viewport.HasValue)
			{
				fc.viewport = new()
				{
					x = command.Viewport.Value.X,
					y = command.Viewport.Value.Y,
					w = command.Viewport.Value.Width,
					h = command.Viewport.Value.Height
				};
			}

			if (command.Scissor.HasValue)
			{
				fc.scissor = new()
				{
					x = command.Scissor.Value.X,
					y = command.Scissor.Value.Y,
					w = command.Scissor.Value.Width,
					h = command.Scissor.Value.Height
				};
			}

			// apply material values before drawing
			command.Material?.Apply();

			// perform draw
			Platform.FosterDraw(&fc);
		}

		internal static class Resources
		{
			public delegate void FreeFn(IntPtr resource);

			private readonly record struct Allocated(WeakReference<IResource> Managed, IntPtr Handle, FreeFn Free);
			private static readonly Dictionary<IntPtr, Allocated> allocated = new();
			private static readonly Queue<IntPtr> freeing = new();

			/// <summary>
			/// Registers a graphical resource so that it can be claned up later
			/// </summary>
			public static void RegisterAllocated(IResource managed, IntPtr handle, FreeFn free)
			{
				Allocated alloc = new(new(managed), handle, free);
				lock (allocated)
					allocated.Add(handle, alloc);
			}

			/// <summary>
			/// Requests that a graphical resource be deleted.
			/// Running on the main thread performs the resource deletion
			/// immediately, otherwise it is deferred to be run later
			/// on the main thread.
			/// </summary>
			public static void RequestDelete(IntPtr handle)
			{
				if (Thread.CurrentThread.ManagedThreadId == App.MainThreadID)
				{
					PerformDelete(handle);
				}
				else
				{
					lock (freeing)
						freeing.Enqueue(handle);
				}
			}

			/// <summary>
			/// Deletes all resources that have requested deletion.
			/// This should only be run from the Main thread.
			/// </summary>
			public static void DeleteRequested()
			{
				Debug.Assert(Thread.CurrentThread.ManagedThreadId == App.MainThreadID);

				lock (freeing)
				{
					while (freeing.Count > 0)
						PerformDelete(freeing.Dequeue());
				}
			}

			/// <summary>
			/// Deletes all remaining allocated resources.
			/// This should only be run from the Main thread during Application shutdown.
			/// </summary>
			public static void DeleteAllocated()
			{
				Debug.Assert(Thread.CurrentThread.ManagedThreadId == App.MainThreadID);

				var disposing = new List<IResource>();

				// Find all managed objects and make sure they have been disposed.
				lock (allocated)
				{
					foreach (var alloc in allocated.Values)
					{
						if (alloc.Managed.TryGetTarget(out var target))
							disposing.Add(target);
					}
				}

				// We intentionally call their Dispose method (instead of just
				// calling PerformDelete on their handle) as this makes sure the
				// managed object itself knows that it is Disposed. This way, if
				// you run the Application multiple times but reference a managed
				// resource from a previous run, it will be properly marked as Disposed.
				foreach (var it in disposing)
					it.Dispose();

				DeleteRequested();
			}

			/// <summary>
			/// Performs the actual resource deletion if it has not already run.
			/// </summary>
			private static void PerformDelete(IntPtr handle)
			{
				Allocated? alloc = null;

				// remove from the allocated list
				lock (allocated)
				{
					if (allocated.TryGetValue(handle, out var value))
					{
						alloc = value;
						allocated.Remove(handle);
					}
				}

				if (alloc is Allocated it)
					it.Free(it.Handle);
			}
		}
	}
}
