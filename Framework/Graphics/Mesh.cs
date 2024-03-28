using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

public class Mesh : IResource
{
	public string Name { get; set; } = string.Empty;
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

	internal IntPtr resource;
	internal bool disposed = false;

	public Mesh()
	{
		resource = Platform.FosterMeshCreate();
		if (resource == IntPtr.Zero)
			throw new Exception("Failed to create Mesh");
		Graphics.Resources.RegisterAllocated(this, resource, Platform.FosterMeshDestroy);
	}

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

		if (!IndexFormat.HasValue || IndexFormat.Value != format)
		{
			IndexFormat = format;
			Platform.FosterMeshSetIndexFormat(resource, format);
		}

		Platform.FosterMeshSetIndexData(
			resource,
			data,
			GetIndexFormatSize(format) * count,
			0
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

		Platform.FosterMeshSetIndexData(
			resource,
			data,
			size * count,
			size * offset
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
	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, VertexFormat format) where T : struct
	{
		fixed (byte* ptr = MemoryMarshal.AsBytes(vertices))
		{
			SetVertices(new IntPtr(ptr), vertices.Length, format);
		}
	}

	/// <summary>
	/// Recreates the Vertex Data to a given size in the Mesh
	/// </summary>
	public unsafe void SetVertices(int count, VertexFormat format)
	{
		SetVertices(IntPtr.Zero, count, format);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// </summary>
	public unsafe void SetVertices(IntPtr data, int count, VertexFormat format)
	{
		if (IsDisposed)
			throw new Exception("Resource is Disposed");

		VertexCount = count;

		// update vertex format
		if (!VertexFormat.HasValue || VertexFormat.Value != format)
		{
			VertexFormat = format;

			var elements = stackalloc Platform.FosterVertexElement[format.Elements.Length];
			for (int i = 0; i < format.Elements.Length; i++)
			{
				elements[i].index = format.Elements[i].Index;
				elements[i].type = format.Elements[i].Type;
				elements[i].normalized = format.Elements[i].Normalized ? 1 : 0;
			}

			Platform.FosterVertexFormat f = new()
			{
				elements = new IntPtr(elements),
				elementCount = format.Elements.Length,
				stride = format.Stride
			};

			Platform.FosterMeshSetVertexFormat(resource, ref f);
		}

		Platform.FosterMeshSetVertexData(
			resource,
			data,
			format.Stride * count,
			0
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

		Platform.FosterMeshSetVertexData(
			resource,
			data,
			VertexFormat.Value.Stride * count,
			VertexFormat.Value.Stride * offset
		);
	}

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
			Graphics.Resources.RequestDelete(resource);
		}
	}
}
