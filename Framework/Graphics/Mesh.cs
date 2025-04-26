namespace Foster.Framework;

/// <summary>
/// The Mesh contains a vertex buffer, index buffer, and optionally an instance buffer, used for drawing.
/// Use <see cref="Mesh{T}"/> to create a mesh of your given Vertex Format.
/// </summary>
public class Mesh : IGraphicResource
{
	/// <summary>
	/// The GraphicsDevice this Mesh was created in
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// Optional Mesh Name
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Mesh Vertex Buffer
	/// </summary>
	public readonly VertexBuffer VertexBuffer;

	/// <summary>
	/// Mesh Index Buffer
	/// </summary>
	public readonly IndexBuffer IndexBuffer;

	/// <summary>
	/// Mesh Instance Buffer
	/// </summary>
	public readonly VertexBuffer? InstanceBuffer;

	/// <summary>
	/// If the Mesh has been disposed
	/// </summary>
	public bool IsDisposed => VertexBuffer.IsDisposed || (InstanceBuffer?.IsDisposed ?? false) || IndexBuffer.IsDisposed;

	/// <summary>
	/// Number of Vertices in the Mesh
	/// </summary>
	public int VertexCount => VertexBuffer.Count;

	/// <summary>
	/// Number of Instance elements in the Mesh
	/// </summary>
	public int InstanceCount => InstanceBuffer?.Count ?? 0;

	/// <summary>
	/// Number of Indices in the Mesh
	/// </summary>
	public int IndexCount => IndexBuffer.Count;

	/// <summary>
	/// The Mesh's Vertex Format
	/// </summary>
	public VertexFormat VertexFormat => VertexBuffer.Format;

	/// <summary>
	/// The Mesh's Instance Format
	/// </summary>
	public VertexFormat InstanceFormat => VertexBuffer?.Format ?? default;

	/// <summary>
	/// The Mesh's Index Format
	/// </summary>
	public IndexFormat IndexFormat => IndexBuffer.Format;

	internal Mesh(GraphicsDevice graphicsDevice, VertexBuffer vertexBuffer, VertexBuffer? instanceBuffer, IndexBuffer indexBuffer, string? name)
	{
		GraphicsDevice = graphicsDevice;
		Name = name ?? string.Empty;
		VertexBuffer = vertexBuffer;
		InstanceBuffer = instanceBuffer;
		IndexBuffer = indexBuffer;
	}

	public Mesh(GraphicsDevice graphicsDevice, VertexFormat vertexFormat, IndexFormat indexFormat, string? name = null)
		: this(graphicsDevice, 
			new VertexBuffer(graphicsDevice, vertexFormat, name != null ? $"{name}-Vertices" : null),
			null,
			new IndexBuffer(graphicsDevice, indexFormat, name != null ? $"{name}-Indices" : null),
			name
		) {}

	public Mesh(GraphicsDevice graphicsDevice, VertexFormat vertexFormat, VertexFormat instanceFormat, IndexFormat indexFormat, string? name = null)
		: this(graphicsDevice, 
			new VertexBuffer(graphicsDevice, vertexFormat, name != null ? $"{name}-Vertices" : null),
			new VertexBuffer(graphicsDevice, instanceFormat, name != null ? $"{name}-Instances" : null),
			new IndexBuffer(graphicsDevice, indexFormat, name != null ? $"{name}-Indices" : null), 
			name
		) {}

	/// <summary>
	/// Disposes the Mesh resources
	/// </summary>
	~Mesh() => Dispose();

	/// <summary>
	/// Sets the Buffer's Element Counts to 0
	/// </summary>
	public void Clear()
	{
		VertexBuffer.Clear();
		InstanceBuffer?.Clear();
		IndexBuffer.Clear();
	}

	/// <summary>
	/// Resizes the Mesh's index buffer to a given number of indices
	/// </summary>
	public unsafe void SetIndexCount(int count)
	{
		IndexBuffer.Clear();
		IndexBuffer.Upload(nint.Zero, 0, count);
	}

	/// <summary>
	/// Resizes the Mesh's Vertex Buffer to a given element count
	/// </summary>
	public void SetVertexCount(int count)
	{
		VertexBuffer.Clear();
		VertexBuffer.Upload(nint.Zero, 0, count);
	}

	/// <summary>
	/// Resizes the Mesh's Instance Buffer to a given element count
	/// </summary>
	public void SetInstanceCount(int count)
	{
		if (InstanceBuffer == null)
			throw new Exception("Mesh does not contain an instance buffer");
		InstanceBuffer.Clear();
		InstanceBuffer.Upload(nint.Zero, 0, count);
	}

	/// <summary>
	/// Uploads the given data to the Mesh's index buffer, increasing the
	/// size of the underlying index buffer if required.
	/// </summary>
	/// <param name="data">The Index Data to apply</param>
	/// <param name="count">The number of indices to set. Note this is the number, not the size.</param>
	/// <param name="offset">The destination offset to upload the indices to.</param>
	public void SetIndices(nint data, int count, int offset = 0)
		=> IndexBuffer.Upload(data, count, offset);

	/// <summary>
	/// Uploads the given data to the Mesh's vertex buffer, increasing the
	/// size of the underlying vertex buffer if required.
	/// </summary>
	public void SetVertices(nint data, int count, int offset = 0)
		=> VertexBuffer.Upload(data, count, offset);

	/// <summary>
	/// Uploads the given data to the Mesh's instance buffer, increasing the
	/// size of the underlying vertex buffer if required.
	/// </summary>
	public void SetInstances(nint data, int count, int offset = 0)
	{
		if (InstanceBuffer == null)
			throw new Exception("Mesh does not contain an instance buffer");
		InstanceBuffer.Upload(data, count, offset);
	}

	/// <summary>
	/// Disposes the graphical resources of the Mesh.
	/// Once Disposed, the Mesh is no longer usable.
	/// </summary>
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		VertexBuffer.Dispose();
		InstanceBuffer?.Dispose();
		IndexBuffer.Dispose();
	}
}

/// <summary>
/// A Mesh with the given Vertex Buffer and Index Buffer types
/// </summary>
/// <typeparam name="TVertex">The Vertex Buffer Element Type</typeparam>
/// <typeparam name="TIndex">The Index Buffer Element Type, which must be either <see cref="int"/>, <see cref="uint"/>, <see cref="short"/>, or <see cref="ushort"/></typeparam>
public class Mesh<TVertex, TIndex>(GraphicsDevice graphicsDevice, string? name = null)
	: Mesh(graphicsDevice, 
		default(TVertex).Format,
		IndexFormatExt.GetFormatOf<TIndex>(),
		name
	)
	where TVertex : unmanaged, IVertex
	where TIndex : unmanaged
{
	public new VertexBuffer<TVertex> VertexBuffer => (VertexBuffer<TVertex>)base.VertexBuffer;
	public new IndexBuffer<TIndex> IndexBuffer => (IndexBuffer<TIndex>)base.IndexBuffer;

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
/// A Mesh with the given Vertex Buffer, Instance Buffer, and Index Buffer types
/// </summary>
/// <typeparam name="TVertex">The Vertex Buffer Element Type</typeparam>
/// <typeparam name="TInstance">The Instance Buffer Element Type</typeparam>
/// <typeparam name="TIndex">The Index Buffer Element Type, which must be either <see cref="int"/>, <see cref="uint"/>, <see cref="short"/>, or <see cref="ushort"/></typeparam>
public class Mesh<TVertex, TInstance, TIndex>(GraphicsDevice graphicsDevice, string? name = null)
	: Mesh(graphicsDevice, 
		default(TVertex).Format,
		default(TInstance).Format,
		IndexFormatExt.GetFormatOf<TIndex>(),
		name
	)
	where TVertex : unmanaged, IVertex
	where TInstance : unmanaged, IVertex
	where TIndex : unmanaged
{
	public new VertexBuffer<TVertex> VertexBuffer => (VertexBuffer<TVertex>)base.VertexBuffer;
	public new VertexBuffer<TInstance> InstanceBuffer => (VertexBuffer<TInstance>)base.InstanceBuffer!;
	public new IndexBuffer<TIndex> IndexBuffer => (IndexBuffer<TIndex>)base.IndexBuffer;

	/// <inheritdoc cref="Mesh.SetVertices(nint, int, int)"/>
	public unsafe void SetVertices(ReadOnlySpan<TVertex> data, int offset = 0)
	{
		fixed (void* ptr = data)
			SetVertices(new nint(ptr), data.Length, offset);
	}

	/// <inheritdoc cref="Mesh.SetInstances(nint, int, int)"/>
	public unsafe void SetInstances(ReadOnlySpan<TInstance> data, int offset = 0)
	{
		fixed (void* ptr = data)
			SetInstances(new nint(ptr), data.Length, offset);
	}

	/// <inheritdoc cref="Mesh.SetIndices(nint, int, int)"/>
	public unsafe void SetIndices(ReadOnlySpan<TIndex> data, int offset = 0)
	{
		fixed (void* ptr = data)
			SetIndices(new nint(ptr), data.Length, offset);
	}
}

/// <summary>
/// A Mesh with a given Vertex Buffer Element type, and Integer Indices
/// </summary>
/// <typeparam name="TVertex">The Vertex Buffer Element Type</typeparam>
public class Mesh<TVertex>(GraphicsDevice graphicsDevice, string? name = null)
	: Mesh<TVertex, int>(graphicsDevice, name)
	where TVertex : unmanaged, IVertex;