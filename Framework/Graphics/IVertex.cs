namespace Foster.Framework;

/// <summary>
/// A Vertex struct to be used in a Mesh
/// </summary>
public interface IVertex
{
	/// <summary>
	/// Gets the Format of the Vertex.
	/// This should return a static value, not create a new format every time it's accessed.
	/// </summary>
	public VertexFormat Format { get; }

}
