namespace Foster.Framework;

/// <summary>
/// The Mesh contains a vertex and index buffer used for drawing.
/// Use <see cref="Mesh{T}"/> to create a mesh of your given Vertex Format.
/// Used in a <seealso cref="DrawCommand"/>.
/// </summary>
public class Mesh(GraphicsDevice graphicsDevice, VertexFormat vertexFormat, IndexFormat indexFormat) : IGraphicResource
{
	/// <summary>
	/// The GraphicsDevice this Mesh was created in
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice = graphicsDevice;

	/// <summary>
	/// Optional Mesh Name
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// If the Mesh has been disposed
	/// </summary>
	public bool IsDisposed => Resource != null && Resource.Disposed;

	/// <summary>
	/// Number of Vertices in the Mesh
	/// </summary>
	public int VertexCount { get; private set; } = 0;

	/// <summary>
	/// Number of Indices in the Mesh
	/// </summary>
	public int IndexCount { get; private set; } = 0;

	/// <summary>
	/// The Mesh's Index Format
	/// </summary>
	public readonly IndexFormat IndexFormat = indexFormat;

	/// <summary>
	/// The Mesh's Vertex Format
	/// </summary>
	public readonly VertexFormat VertexFormat = vertexFormat;

	internal GraphicsDevice.IHandle? Resource { get; private set; }

	/// <summary>
	/// Disposes the Mesh resources
	/// </summary>
	~Mesh() => Dispose();

	/// <summary>
	/// Resizes the Mesh's index buffer to a given number of indices of the given format.
	/// </summary>
	public unsafe void SetIndexCount(int count)
		=> SetIndices(nint.Zero, 0, count);

	/// <summary>
	/// Resizes the Mesh's Vertex Buffer to a given vertex count and format
	/// </summary>
	public void SetVertexCount(int count)
		=> SetVertices(nint.Zero, 0, count);

	/// <summary>
	/// Uploads the given data to the Mesh's index buffer, increasing the
	/// size of the underlying index buffer if required.
	/// </summary>
	/// <param name="data">The Index Data to apply</param>
	/// <param name="count">The number of indices to set. Note this is the number, not the size.</param>
	/// <param name="offset">The destination offset to upload the indices to.</param>
	public void SetIndices(nint data, int count, int offset = 0)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		IndexCount = count;
		Resource ??= GraphicsDevice.CreateMesh(VertexFormat, IndexFormat);

		GraphicsDevice.SetMeshIndexData(
			Resource,
			data,
			IndexFormat.SizeInBytes() * count,
			IndexFormat.SizeInBytes() * offset
		);
	}

	/// <summary>
	/// Uploads the given data to the Mesh's vertex buffer, increasing the
	/// size of the underlying vertex buffer if required.
	/// </summary>
	public void SetVertices(nint data, int count, int offset = 0)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		VertexCount = count;
		Resource ??= GraphicsDevice.CreateMesh(VertexFormat, IndexFormat);

		GraphicsDevice.SetMeshVertexData(
			Resource,
			data,
			VertexFormat.Stride * count,
			VertexFormat.Stride * offset
		);
	}

	/// <summary>
	/// Disposes the graphical resources of the Mesh. 
	/// Once Disposed, the Mesh is no longer usable.
	/// </summary>
	public void Dispose()
	{
		if (Resource != null)
			GraphicsDevice.DestroyResource(Resource);
		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// A Mesh with the given Vertex Buffer and Index Buffer types
/// </summary>
/// <typeparam name="TVertex">The Vertex Buffer Type</typeparam>
/// <typeparam name="TIndex">The Index Buffer Type, which must be either <see cref="int"/>, <see cref="uint"/>, <see cref="short"/>, or <see cref="ushort"/></typeparam>
public class Mesh<TVertex, TIndex>(GraphicsDevice graphicsDevice)
	: Mesh(graphicsDevice, new TVertex().Format, IndexFormatExt.GetFormatOf<TIndex>())
	where TVertex : unmanaged, IVertex
	where TIndex : unmanaged
{
	/// <inheritdoc cref="Mesh.SetVertices(nint, int, int)"/>
	public unsafe void SetVertices(ReadOnlySpan<TVertex> data, int offset = 0)
	{
		fixed (void* ptr = data)
			SetVertices(new nint(ptr), data.Length, offset);
	}

	/// <inheritdoc cref="Mesh.SetIndices(nint, int, int)"/>
	public unsafe void SetIndices(ReadOnlySpan<TIndex> data, int offset = 0)
	{
		fixed (void* ptr = data)
			SetIndices(new nint(ptr), data.Length, offset);
	}
}

/// <summary>
/// A Mesh with a given Vertex Buffer type, and Integer Indices
/// </summary>
/// <typeparam name="TVertex">The Vertex Buffer Type</typeparam>
public class Mesh<TVertex>(GraphicsDevice graphicsDevice)
	: Mesh<TVertex, int>(graphicsDevice)
	where TVertex : unmanaged, IVertex;