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
	/// Creates a Draw Command based on the given mesh and shader
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
		Graphics.Submit(this);
	}
}