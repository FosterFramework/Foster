using System.Diagnostics;

namespace Foster.Framework;

public struct DrawCommand()
{
	/// <summary>
	/// Render Target. If not assigned, will target the Back Buffer
	/// </summary>
	public Target? Target;

	/// <summary>
	/// Material to use
	/// </summary>
	public Material Material;

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
	public BlendMode BlendMode = BlendMode.Premultiply;

	/// <summary>
	/// The Render State Culling Mode
	/// </summary>
	public CullMode CullMode = CullMode.None;

	/// <summary>
	/// The Render State Depth comparison Function
	/// </summary>
	public DepthCompare DepthCompare = DepthCompare.None;

	/// <summary>
	/// If Writing to the Depth Buffer is enabled
	/// </summary>
	public bool DepthMask = false;

	/// <summary>
	/// Render Viewport
	/// </summary>
	public RectInt? Viewport = null;

	/// <summary>
	/// The Render State Scissor Rectangle
	/// </summary>
	public RectInt? Scissor = null;

	/// <summary>
	/// Creates a Draw Command based on the given mesh and material
	/// </summary>
	public DrawCommand(Target? target, Mesh mesh, Material material)
		: this()
	{
		Target = target;
		Mesh = mesh;
		Material = material;
		MeshIndexStart = 0;
		MeshIndexCount = mesh.IndexCount;
	}

	public readonly void Submit()
	{
		Graphics.Submit(this);
	}
}