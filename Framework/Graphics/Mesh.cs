using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// The Mesh contains a buffer of Vertices and optionally a buffer of Indices
/// used during a DrawCommand.
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
	public bool IsDisposed => disposed;

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

	internal IntPtr resource { get; private set; }
	private bool disposed = false;

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

	/// <summary>
	/// Uploads the Index Data to the Mesh
	/// </summary>
	public unsafe void SetIndices<T>(ReadOnlySpan<T> indices) where T : struct
	{
		fixed (byte* ptr = MemoryMarshal.AsBytes(indices))
		{
			SetIndices(new IntPtr(ptr), indices.Length, GetIndexFormat<T>());
		}
	}

	/// <summary>
	/// Recreates the Index Data to a given size in the Mesh
	/// </summary>
	public unsafe void SetIndices<T>(int count, IndexFormat format) where T : struct
	{
		SetIndices(IntPtr.Zero, count, format);
	}

	/// <summary>
	/// Uploads the Index data to the Mesh.
	/// </summary>
	public void SetIndices(nint data, int count, IndexFormat format)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		IndexCount = count;

		if (resource == nint.Zero)
			resource = Renderer.CreateMesh();

		Renderer.SetMeshIndexData(
			resource,
			data,
			GetIndexFormatSize(format) * count,
			0,
			(IndexFormat = format).Value
		);
	}

	/// <summary>
	/// Uploads a sub area of index data to the Mesh.
	/// The Mesh must already be able to fit this with a previous call to SetIndices.
	/// This also cannot modify the existing Index Format.
	/// </summary>
	public unsafe void SetSubIndices<T>(int offset, ReadOnlySpan<T> indices) where T : struct
	{
		if (!IndexFormat.HasValue || IndexFormat.Value != GetIndexFormat<T>())
			throw new Exception("Index Format mismatch; SetSubIndices must use the existing Format set in SetIndices");

		fixed (byte* ptr = MemoryMarshal.AsBytes(indices))
		{
			SetSubIndices(offset, new IntPtr(ptr), indices.Length);
		}
	}

	/// <summary>
	/// Uploads the Index data to the Mesh.
	/// The Mesh must already be able to fit this with a previous call to SetIndices.
	/// This also cannot modify the existing Index Format.
	/// </summary>
	public void SetSubIndices(int offset, nint data, int count)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (!IndexFormat.HasValue)
			throw new Exception("Must call SetIndices before SetSubIndices");

		if (offset + count > IndexCount)
			throw new Exception("SetSubIndices is out of range of the existing Index Buffer");

		var size = GetIndexFormatSize(IndexFormat.Value);

		if (resource == nint.Zero)
			resource = Renderer.CreateMesh();

		Renderer.SetMeshIndexData(
			resource,
			data,
			size * count,
			size * offset,
			IndexFormat.Value
		);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// </summary>
	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices) where T : struct, IVertex
	{
		SetVertices(vertices, default(T).Format);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// </summary>
	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, in VertexFormat format) where T : struct
	{
		fixed (byte* ptr = MemoryMarshal.AsBytes(vertices))
		{
			SetVertices(new IntPtr(ptr), vertices.Length, format);
		}
	}

	/// <summary>
	/// Recreates the Vertex Data to a given size in the Mesh
	/// </summary>
	public unsafe void SetVertices(int count, in VertexFormat format)
	{
		SetVertices(IntPtr.Zero, count, format);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// </summary>
	public unsafe void SetVertices(IntPtr data, int count, in VertexFormat format)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		VertexCount = count;

		if (resource == nint.Zero)
			resource = Renderer.CreateMesh();

		Renderer.SetMeshVertexData(
			resource,
			data,
			format.Stride * count,
			0,
			(VertexFormat = format).Value
		);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// The Mesh must already be able to fit this with a previous call to SetVertices.
	/// This also cannot modify the existing Vertex Format.
	/// </summary>
	public unsafe void SetSubVertices<T>(int offset, ReadOnlySpan<T> vertices) where T : struct
	{
		fixed (byte* ptr = MemoryMarshal.AsBytes(vertices))
		{
			SetSubVertices(offset, new IntPtr(ptr), vertices.Length);
		}
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// The Mesh must already be able to fit this with a previous call to SetVertices.
	/// This also cannot modify the existing Vertex Format.
	/// </summary>
	public unsafe void SetSubVertices(int offset, IntPtr data, int count)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		if (!VertexFormat.HasValue)
			throw new Exception("Must call SetVertices before SetSubVertices");

		if (offset + count > VertexCount)
			throw new Exception("SetSubVertices is out of range of the existing Vertex Buffer");

		if (resource == nint.Zero)
			resource = Renderer.CreateMesh();

		Renderer.SetMeshVertexData(
			resource,
			data,
			VertexFormat.Value.Stride * count,
			VertexFormat.Value.Stride * offset,
			VertexFormat.Value
		);
	}

	/// <summary>
	/// Disposes the graphical resources of the Mesh. Once Disposed, the Mesh
	/// is no longer usable.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			disposed = true;
			if (resource != nint.Zero)
				Renderer.DestroyMesh(resource);
			resource = nint.Zero;
		}
	}
}
