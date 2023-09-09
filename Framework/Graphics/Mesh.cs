using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

public class Mesh : IResource
{
	public string Name { get; set; } = string.Empty;
	public bool IsDisposed => isDisposed;
	public int VertexCount { get; private set; } = 0;
	public int IndexCount { get; private set; } = 0;

	internal IntPtr resource;
	internal bool isDisposed = false;
	private IndexFormat currentIndexFormat;
	private VertexFormat currentVertexFormat;

	public Mesh()
	{
		resource = Platform.FosterMeshCreate();
		if (resource == IntPtr.Zero)
			throw new Exception("Failed to create Mesh");
	}

	~Mesh()
	{
		Dispose();
	}

	/// <summary>
	/// Uploads the Index data to the Mesh.
	/// Offset is an offset into where to upload the data in the underlying GPU buffer.
	/// </summary>
	public unsafe void SetIndices<T>(ReadOnlySpan<T> indices, int offset = 0) where T : struct
	{
		var format = true switch
		{
			true when typeof(T) == typeof(short) => IndexFormat.Sixteen,
			true when typeof(T) == typeof(ushort) => IndexFormat.Sixteen,
			true when typeof(T) == typeof(int) => IndexFormat.ThirtyTwo,
			true when typeof(T) == typeof(uint) => IndexFormat.ThirtyTwo,
			_ => throw new NotImplementedException(),
		};

		fixed (byte* ptr = MemoryMarshal.AsBytes(indices))
		{
			SetIndices(new IntPtr(ptr), indices.Length, offset, format);
		}
	}

	/// <summary>
	/// Uploads the Index data to the Mesh.
	/// Offset is an offset into where to upload the data in the underlying GPU buffer.
	/// </summary>
	public unsafe void SetIndices(IntPtr data, int count, int offset, IndexFormat format)
	{
		Debug.Assert(!IsDisposed, "Mesh is Disposed");

		IndexCount = count;

		int size = format switch
		{
			IndexFormat.Sixteen => 2,
			IndexFormat.ThirtyTwo => 4,
			_ => throw new Exception()
		};

		if (currentIndexFormat != format)
		{
			currentIndexFormat = format;
			Platform.FosterMeshSetIndexFormat(resource, format);
		}
		
		Platform.FosterMeshSetIndexData(
			resource, 
			data,
			size * count,
			size * offset
		);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// Offset is an offset into where to upload the data in the underlying GPU buffer.
	/// </summary>
	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, int offset = 0) where T : struct, IVertex
	{
		SetVertices(vertices, offset, default(T).Format);
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// Offset is an offset into where to upload the data in the underlying GPU buffer.
	/// </summary>
	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, int offset, VertexFormat format) where T : struct
	{
		fixed (byte* ptr = MemoryMarshal.AsBytes(vertices))
		{
			SetVertices(new IntPtr(ptr), vertices.Length, offset, format);
		}
	}

	/// <summary>
	/// Uploads the Vertex data to the Mesh.
	/// Offset is an offset into where to upload the data in the underlying GPU buffer.
	/// </summary>
	public unsafe void SetVertices(IntPtr data, int count, int offset, VertexFormat format)
	{
		Debug.Assert(!IsDisposed, "Mesh is Disposed");

		VertexCount = count;

		// update vertex format
		if (currentVertexFormat != format)
		{
			currentVertexFormat = format;
			
			var elements = stackalloc Platform.FosterVertexElement[format.Elements.Length];
			for (int i = 0; i < format.Elements.Length; i ++)
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
			format.Stride * offset
		);
	}

	public void Dispose()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			Platform.FosterMeshDestroy(resource);
		}
	}
}
