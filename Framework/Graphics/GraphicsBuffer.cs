using System.Runtime.InteropServices;

namespace Foster.Framework;

/// <summary>
/// A buffer that stores graphical data used when drawing.
/// </summary>
public abstract class GraphicsBuffer : IGraphicResource
{
	internal readonly int ElementSizeInBytes;
	internal readonly GraphicsDevice.IHandle Resource;

	/// <summary>
	/// The GraphicsDevice this Mesh was created in
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// Name of the Buffer
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Number of Elements in the Buffer
	/// </summary>
	public int Count { get; private set; }

	/// <summary>
	/// If the Buffer has been disposed
	/// </summary>
	public bool IsDisposed => Resource.Disposed;

	internal GraphicsBuffer(GraphicsDevice graphicsDevice, int elementSizeInBytes, GraphicsDevice.BufferType type, IndexFormat? indexFormat, string? name)
	{
		GraphicsDevice = graphicsDevice;
		Name = name ?? string.Empty;
		ElementSizeInBytes = elementSizeInBytes;
		Resource = graphicsDevice.CreateBuffer(name, type, indexFormat ?? default);
	}

	~GraphicsBuffer()
		=> Dispose();

	/// <summary>
	/// Uploads data to the buffer, and resizes it if required.
	/// </summary>
	public void Upload(nint data, int elementCount, int elementOffset = 0)
	{
		if (IsDisposed)
			throw new Exception("Trying to upload to a disposed DrawBuffer");

		Count = Math.Max(Count, elementOffset + elementCount);
		GraphicsDevice.UploadBufferData(Resource, data, elementCount * ElementSizeInBytes, elementOffset * ElementSizeInBytes);
	}

	/// <summary>
	/// Sets the Buffer Element Count to 0
	/// </summary>
	public void Clear()
	{
		Count = 0;
	}

	/// <summary>
	/// Disposes of the Buffer's resources
	/// </summary>
	public void Dispose()
	{
		GC.SuppressFinalize(this);
		if (!Resource.Disposed)
			GraphicsDevice.DestroyResource(Resource);
	}
}

/// <summary>
/// Holds Vertex Elements for drawing
/// </summary>
public class VertexBuffer(GraphicsDevice graphicsDevice, VertexFormat format, string? name = null)
	: GraphicsBuffer(graphicsDevice, format.Stride, GraphicsDevice.BufferType.Vertex, null, name)
{
	public readonly VertexFormat Format = format;
}

/// <summary>
/// Holds Vertex Elements for drawing
/// </summary>
public class VertexBuffer<T>(GraphicsDevice graphicsDevice, string? name = null)
	: VertexBuffer(graphicsDevice, default(T).Format, name) where T : unmanaged, IVertex
{
	/// <summary>
	/// Uploads data to the buffer, and resizes it if required.
	/// </summary>
	public unsafe void Upload(in ReadOnlySpan<T> data, int offset = 0)
	{
		fixed (T* ptr = data)
			Upload(new nint(ptr), data.Length, offset);
	}
}

/// <summary>
/// Holds Index Elements for drawing
/// </summary>
public class IndexBuffer(GraphicsDevice graphicsDevice, IndexFormat format, string? name = null)
	: GraphicsBuffer(graphicsDevice, format.SizeInBytes(), GraphicsDevice.BufferType.Index, format, name)
{
	/// <summary>
	/// The Index Element Format
	/// </summary>
	public readonly IndexFormat Format = format;
}

/// <summary>
/// Holds Index Elements for drawing
/// </summary>
public class IndexBuffer<T>(GraphicsDevice graphicsDevice, string? name = null)
	: IndexBuffer(graphicsDevice, IndexFormatExt.GetFormatOf<T>(), name) where T : unmanaged
{
	/// <summary>
	/// Uploads data to the buffer, and resizes it if required.
	/// </summary>
	public unsafe void Upload(in ReadOnlySpan<T> data, int offset = 0)
	{
		fixed (T* ptr = data)
			Upload(new nint(ptr), data.Length, offset);
	}
}

/// <summary>
/// Holds Storage Elements for drawing
/// </summary>
public class StorageBuffer(GraphicsDevice graphicsDevice, int elementSizeInBytes, string? name = null)
	: GraphicsBuffer(graphicsDevice, elementSizeInBytes, GraphicsDevice.BufferType.Storage, null, name) {}

/// <summary>
/// Holds Storage Elements for drawing
/// </summary>
public class StorageBuffer<T>(GraphicsDevice graphicsDevice, string? name = null)
	: StorageBuffer(graphicsDevice, Marshal.SizeOf<T>(), name) where T : unmanaged
{
	/// <summary>
	/// Uploads data to the buffer, and resizes it if required.
	/// </summary>
	public unsafe void Upload(in ReadOnlySpan<T> data, int offset = 0)
	{
		fixed (T* ptr = data)
			Upload(new nint(ptr), data.Length, offset);
	}
}