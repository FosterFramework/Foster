using System.Diagnostics;

namespace Foster.Framework;

public struct DrawCommand
{
	/// <summary>
	/// Render Target. If not assigned, will target the Back Buffer
	/// </summary>
	public Target? Target;

	/// <summary>
	/// Material to use
	/// </summary>
	public Shader Shader;

	/// <summary>
	/// Mesh to use
	/// </summary>
	public Mesh Mesh;

	/// <summary>
	/// The Index to begin rendering from the Mesh
	/// </summary>
	public int MeshIndexStart;

	/// <summary>
	/// The total number of Indices to draw from the Mesh
	/// </summary>
	public int MeshIndexCount;

	/// <summary>
	/// The Render State Blend Mode
	/// </summary>
	public BlendMode BlendMode;

	/// <summary>
	/// The Render State Culling Mode
	/// </summary>
	public CullMode CullMode;

	/// <summary>
	/// The Render State Depth comparison Function
	/// </summary>
	public DepthCompare DepthCompare;

	/// <summary>
	/// Render Viewport
	/// </summary>
	public RectInt? Viewport;

	/// <summary>
	/// The Render State Scissor Rectangle
	/// </summary>
	public RectInt? Scissor;

	/// <summary>
	/// Creates a Render Pass based on the given mesh and shader
	/// </summary>
	public DrawCommand(Target? target, Mesh mesh, Shader shader)
	{
		Target = target;
		Mesh = mesh;
		Shader = shader;
		MeshIndexStart = 0;
		MeshIndexCount = mesh.IndexCount;
		BlendMode = BlendMode.Premultiply;
		CullMode = CullMode.None;
		DepthCompare = DepthCompare.None;
		Viewport = null;
		Scissor = null;
	}

	public readonly void Submit()
	{
		Debug.Assert(Target == null || !Target.IsDisposed, "Target is invalid");
		Debug.Assert(Mesh != null && !Mesh.IsDisposed, "Mesh is Invalid");
		Debug.Assert(Shader != null && !Shader.IsDisposed, "Mesh is Invalid");

		Platform.FosterDrawCommand command = new()
		{
			target = (Target != null && !Target.IsDisposed ? Target.resource : IntPtr.Zero),
			mesh = (Mesh != null && !Mesh.IsDisposed ? Mesh.resource : IntPtr.Zero),
			shader = (Shader != null && !Shader.IsDisposed ? Shader.resource : IntPtr.Zero),
			hasViewport = Viewport.HasValue ? 1 : 0,
			hasScissor = Scissor.HasValue ? 1 : 0,
			indexStart = MeshIndexStart,
			indexCount = MeshIndexCount,
			instanceCount = 0,
			compare = DepthCompare,
			cull = CullMode,
			blend = BlendMode,
		};

		if (Viewport.HasValue)
		{
			command.viewport = new () { 
				x = Viewport.Value.X, y = Viewport.Value.Y, 
				w = Viewport.Value.Width, h = Viewport.Value.Height 
			};
		}

		if (Scissor.HasValue)
		{
			command.scissor = new () { 
				x = Scissor.Value.X, y = Scissor.Value.Y, 
				w = Scissor.Value.Width, h = Scissor.Value.Height 
			};
		}

		Platform.FosterDraw(ref command);
	}
}