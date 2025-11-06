namespace Foster.Framework;

/// <summary>
/// Stores information required to submit a draw command.
/// Call <see cref="Submit"/> or <see cref="GraphicsDevice.Draw"/> to submit.
/// </summary>
public struct DrawCommand
{
	/// <summary>
	/// Render Target. If not assigned, will target the Back Buffer
	/// </summary>
	public IDrawableTarget Target;

	/// <summary>
	/// Material to use
	/// </summary>
	public Material Material;

	/// <summary>
	/// Vertex Buffers to use and their associated input rate.
	/// </summary>
	public StackList4<(VertexBuffer Buffer, bool InstanceInputRate)> VertexBuffers;

	/// <summary>
	/// Vertex Buffers to use
	/// </summary>
	public StackList4<StorageBuffer> VertexStorageBuffers;

	/// <summary>
	/// Fragment storage Buffers to use
	/// </summary>
	public StackList4<StorageBuffer> FragmentStorageBuffers;

	/// <summary>
	/// Index Buffer to use. Set <see cref="IndexCount"/> for the number of indices to draw.
	/// </summary>
	public IndexBuffer? IndexBuffer;

	/// <summary>
	/// The offset into the <see cref="IndexBuffer"/> when using an <see cref="IndexBuffer"/>
	/// </summary>
	public int IndexOffset = 0;

	/// <summary>
	/// The number of indices to draw per instance when using an <see cref="IndexBuffer"/>
	/// </summary>
	public int IndexCount = 0;

	/// <summary>
	/// When using an <see cref="IndexBuffer"/>, this offsets the value of each index.
	/// Otherwise, this is an offset into the Vertex Buffer.
	/// </summary>
	public int VertexOffset = 0;

	/// <summary>
	/// Number of vertices to draw per instance when not using an <see cref="IndexBuffer"/>.
	/// Use <see cref="IndexCount"/> when using an <see cref="IndexBuffer"/>.
	/// </summary>
	public int VertexCount = 0;

	/// <summary>
	/// The number of instances to draw. Should always be at least 1.
	/// </summary>
	public int InstanceCount = 1;

	[Obsolete("Use IndexOffset")]
	public int MeshIndexStart
	{
		readonly get => IndexOffset;
		set => IndexOffset = value;
	}

	[Obsolete("Use IndexCount")]
	public int MeshIndexCount
	{
		readonly get => IndexCount;
		set => IndexCount = value;
	}

	[Obsolete("Use VertexOffset")]
	public int MeshVertexOffset
	{
		readonly get => VertexOffset;
		set => VertexOffset = value;
	}

	/// <summary>
	/// The Render State Blend Mode
	/// </summary>
	public BlendMode BlendMode = BlendMode.Premultiply;

	/// <summary>
	/// The Render State Culling Mode
	/// </summary>
	public CullMode CullMode = CullMode.None;

	/// <summary>
	/// The Depth Comparison Function, only used if DepthTestEnabled is true
	/// </summary>
	public DepthCompare DepthCompare = DepthCompare.Less;

	/// <summary>
	/// If the Depth Test is enabled
	/// </summary>
	public bool DepthTestEnabled = false;

	/// <summary>
	/// If Writing to the Depth Buffer is enabled
	/// </summary>
	public bool DepthWriteEnabled = false;

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
	public DrawCommand(IDrawableTarget target, Mesh mesh, Material material)
		: this()
	{
		Target = target;
		Material = material;

		if (mesh.InstanceBuffer != null)
		{
			VertexBuffers = [
				(mesh.VertexBuffer, false),
				(mesh.InstanceBuffer, true)
			];

			InstanceCount = mesh.InstanceBuffer.Count;
		}
		else
		{
			VertexBuffers = [ (mesh.VertexBuffer, false) ];
			InstanceCount = 1;
		}

		if (mesh.IndexBuffer != null)
		{
			IndexBuffer = mesh.IndexBuffer;
			IndexCount = mesh.IndexBuffer.Count;
		}
		else
		{
			VertexCount = VertexBuffers[0].Buffer.Count;
		}
	}

	public DrawCommand(IDrawableTarget target, VertexBuffer vertexBuffer, Material material)
		: this()
	{
		Target = target;
		Material = material;
		VertexBuffers = [ (vertexBuffer, false) ];
		VertexCount = vertexBuffer.Count;
	}

	public readonly void Submit(GraphicsDevice graphicsDevice)
		=> graphicsDevice.Draw(this);
}