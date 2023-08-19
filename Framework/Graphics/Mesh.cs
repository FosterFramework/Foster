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
	private bool isDisposed = false;

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
		Debug.Assert(!IsDisposed, "Mesh is Disposed");

		var format = true switch
		{
			true when typeof(T) == typeof(short) => IndexFormat.Sixteen,
			true when typeof(T) == typeof(ushort) => IndexFormat.Sixteen,
			true when typeof(T) == typeof(int) => IndexFormat.ThirtyTwo,
			true when typeof(T) == typeof(uint) => IndexFormat.ThirtyTwo,
			_ => throw new NotImplementedException(),
		};
		
		IndexCount = indices.Length;

		Platform.FosterMeshSetIndexFormat(resource, format);

		fixed (byte* ptr = MemoryMarshal.AsBytes(indices))
		{
			Platform.FosterMeshSetIndexData(
				resource, 
				new IntPtr(ptr),
				Marshal.SizeOf<T>() * indices.Length
			);
		}
	}

	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices) where T : struct, IVertex
	{
		SetVertices(vertices, default(T).Format);
	}

	public unsafe void SetVertices<T>(ReadOnlySpan<T> vertices, VertexFormat format) where T : struct
	{
		Debug.Assert(!IsDisposed, "Mesh is Disposed");
		
		VertexCount = vertices.Length;

		// update vertex format
		{
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

		fixed (byte* ptr = MemoryMarshal.AsBytes(vertices))
		{
			Platform.FosterMeshSetVertexData(
				resource,
				new IntPtr(ptr),
				Marshal.SizeOf<T>() * vertices.Length
			);
		}
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
