
namespace Foster.Framework;

/// <summary>
/// The Mesh contains a Vertex and Index Buffer used for drawing.
/// Used in a <seealso cref="DrawCommand"/>.
/// </summary>
public class Mesh : IResource
{
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
	/// Current Index Format
	/// </summary>
	public IndexFormat? IndexFormat { get; private set; }

	/// <summary>
	/// Current Vertex Format
	/// </summary>
	public VertexFormat? VertexFormat { get; private set; }

	internal Renderer.IHandle? Resource { get; private set; }

	/// <summary>
	/// Disposes the Mesh resources
	/// </summary>
	~Mesh()
	{
		Dispose(false);
	}

	private static IndexFormat GetIndexFormat<T>()
		=> true switch
		{
			true when typeof(T) == typeof(short) => Framework.IndexFormat.Sixteen,
			true when typeof(T) == typeof(ushort) => Framework.IndexFormat.Sixteen,
			true when typeof(T) == typeof(int) => Framework.IndexFormat.ThirtyTwo,
			true when typeof(T) == typeof(uint) => Framework.IndexFormat.ThirtyTwo,
			_ => throw new NotImplementedException(),
		};

	private static int GetIndexFormatSize(Framework.IndexFormat format)
		=> format switch
		{
			Framework.IndexFormat.Sixteen => 2,
			Framework.IndexFormat.ThirtyTwo => 4,
			_ => throw new NotImplementedException(),
		};

	private unsafe void SetIndicesSpan<T>(ReadOnlySpan<T> indices) where T : unmanaged
	{
		fixed (void* ptr = indices)
			SetIndices(new nint(ptr), indices.Length, GetIndexFormat<T>());
	}

	private unsafe void SetSubIndicesSpan<T>(int offset, ReadOnlySpan<T> indices) where T : unmanaged
	{
		if (!IndexFormat.HasValue || IndexFormat.Value != GetIndexFormat<T>())
			throw new Exception("Index Format mismatch; SetSubIndices must use the existing Format set in SetIndices");

		fixed (void* ptr = indices)
			SetSubIndices(offset, new nint(ptr), indices.Length);
	}

	/// <summary>
	/// Recreates the Mesh's Index Buffer to a given number of indices of the given format.
	/// </summary>
	public unsafe void SetIndices(int count, IndexFormat format)
		=> SetIndices(nint.Zero, count, format);

	/// <summary>
	/// Recreates the Mesh's Index Buffer to a given index data.
	/// </summary>
	public unsafe void SetIndices(ReadOnlySpan<ushort> indices)
		=> SetIndicesSpan(indices);

	/// <inheritdoc cref="SetIndices(ReadOnlySpan{ushort})"/>
	public unsafe void SetIndices(ReadOnlySpan<short> indices)
		=> SetIndicesSpan(indices);

	/// <inheritdoc cref="SetIndices(ReadOnlySpan{ushort})"/>
	public unsafe void SetIndices(ReadOnlySpan<uint> indices)
		=> SetIndicesSpan(indices);

	/// <inheritdoc cref="SetIndices(ReadOnlySpan{ushort})"/>
	public unsafe void SetIndices(ReadOnlySpan<int> indices)
		=> SetIndicesSpan(indices);

	/// <inheritdoc cref="SetIndices(ReadOnlySpan{ushort})"/>
	/// <param name="data">The Index Data to apply</param>
	/// <param name="count">The number of indices to set. Note this is the number of vertices, not the size.</param>
	/// <param name="format">The Index Format to use, which is used to calculate the total size in bytes.</param>
	public void SetIndices(nint data, int count, IndexFormat format)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		IndexCount = count;
		Resource ??= App.Renderer.CreateMesh();

		App.Renderer.SetMeshIndexData(
			Resource,
			data,
			GetIndexFormatSize(format) * count,
			0,
			(IndexFormat = format).Value
		);
	}

	/// <summary>
	/// Sets a sub region of the Index Buffer to the given data.
	/// The Mesh must already be able to fit this with a previous call to SetIndices.
	/// This cannot modify the existing Index Format.
	/// </summary>
	public void SetSubIndices(int offset, ReadOnlySpan<ushort> indices)
		=> SetSubIndicesSpan(offset, indices);

	/// <inheritdoc cref="SetSubIndices(int, ReadOnlySpan{ushort})"/>
	public void SetSubIndices(int offset, ReadOnlySpan<short> indices)
		=> SetSubIndicesSpan(offset, indices);

	/// <inheritdoc cref="SetSubIndices(int, ReadOnlySpan{ushort})"/>
	public void SetSubIndices(int offset, ReadOnlySpan<uint> indices)
		=> SetSubIndicesSpan(offset, indices);

	/// <inheritdoc cref="SetSubIndices(int, ReadOnlySpan{ushort})"/>
	public void SetSubIndices(int offset, ReadOnlySpan<int> indices)
		=> SetSubIndicesSpan(offset, indices);

	/// <inheritdoc cref="SetSubIndices(int, ReadOnlySpan{ushort})"/>
	public void SetSubIndices(int offset, nint data, int count)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (!IndexFormat.HasValue)
			throw new Exception("Must call SetIndices before SetSubIndices");

		if (offset + count > IndexCount)
			throw new Exception("SetSubIndices is out of range of the existing Index Buffer");

		var size = GetIndexFormatSize(IndexFormat.Value);

		Resource ??= App.Renderer.CreateMesh();

		App.Renderer.SetMeshIndexData(
			Resource,
			data,
			size * count,
			size * offset,
			IndexFormat.Value
		);
	}

	/// <summary>
	/// Recreates the Mesh's Vertex Buffer to a given index data.
	/// </summary>
	public void SetVertices<T>(ReadOnlySpan<T> vertices) where T : unmanaged, IVertex
		=> SetVertices(vertices, default(T).Format);

	/// <summary>
	/// Recreates the Mesh's Vertex Buffer to a given index data.
	/// </summary>
	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, in VertexFormat format) where T : unmanaged
	{
		fixed (void* ptr = vertices)
			SetVertices(new nint(ptr), vertices.Length, format);
	}

	/// <summary>
	/// Recreates the Mesh's Vertex Buffer to a given vertex count and format
	/// </summary>
	public void SetVertices(int count, in VertexFormat format)
		=> SetVertices(nint.Zero, count, format);

	/// <summary>
	/// Recreates the Mesh's Vertex Buffer to a given index data.
	/// </summary>
	public void SetVertices(nint data, int count, in VertexFormat format)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		VertexCount = count;
		Resource ??= App.Renderer.CreateMesh();

		App.Renderer.SetMeshVertexData(
			Resource,
			data,
			format.Stride * count,
			0,
			(VertexFormat = format).Value
		);
	}

	/// <summary>
	/// Sets a sub region of the Vertex Buffer to the given data.
	/// The Mesh must already be able to fit this with a previous call to SetVertices.
	/// This also cannot modify the existing Vertex Format.
	/// </summary>
	public unsafe void SetSubVertices<T>(int offset, ReadOnlySpan<T> vertices) where T : unmanaged
	{
		fixed (void* ptr = vertices)
			SetSubVertices(offset, new nint(ptr), vertices.Length);
	}

	/// <inheritdoc cref="SetSubVertices{T}"/>
	public unsafe void SetSubVertices(int offset, nint data, int count)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (!VertexFormat.HasValue)
			throw new Exception("Must call SetVertices before SetSubVertices");

		if (offset + count > VertexCount)
			throw new Exception("SetSubVertices is out of range of the existing Vertex Buffer");

		Resource ??= App.Renderer.CreateMesh();

		App.Renderer.SetMeshVertexData(
			Resource,
			data,
			VertexFormat.Value.Stride * count,
			VertexFormat.Value.Stride * offset,
			VertexFormat.Value
		);
	}

	/// <summary>
	/// Disposes the graphical resources of the Mesh. 
	/// Once Disposed, the Mesh is no longer usable.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (Resource != null)
			App.Renderer.DestroyResource(Resource);
	}
}
