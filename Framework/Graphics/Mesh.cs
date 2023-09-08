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
	private VertexFormat currentFormat;

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

	public unsafe void SetIndices<T>(ReadOnlySpan<T> indices) where T : struct
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
			SetIndices(new IntPtr(ptr), indices.Length, format);
		}
	}

	public unsafe void SetIndices(IntPtr data, int count, IndexFormat format)
	{
		Debug.Assert(!IsDisposed, "Mesh is Disposed");

		IndexCount = count;

		int size = format switch
		{
			IndexFormat.Sixteen => 2,
			IndexFormat.ThirtyTwo => 4,
			_ => throw new Exception()
		};

		Platform.FosterMeshSetIndexFormat(resource, format);
		Platform.FosterMeshSetIndexData(
			resource, 
			data,
			size * count
		);
	}

	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices) where T : struct, IVertex
	{
		SetVertices(vertices, default(T).Format);
	}

	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, VertexFormat format) where T : struct
	{
		fixed (byte* ptr = MemoryMarshal.AsBytes(vertices))
		{
			SetVertices(new IntPtr(ptr), vertices.Length, format);
		}
	}

	public unsafe void SetVertices(IntPtr data, int count, VertexFormat format)
	{
		Debug.Assert(!IsDisposed, "Mesh is Disposed");

		VertexCount = count;

		// update vertex format
		if (currentFormat != format)
		{
			currentFormat = format;
			
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
			format.Stride * count
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
