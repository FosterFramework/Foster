using System.Numerics;

namespace Foster.Framework;

public readonly struct TriangulationEnumerable(IReadOnlyList<Vector2> Vertices, IReadOnlyList<int> Triangles)
{
	public TriangulationEnumerator GetEnumerator() => new(Vertices, Triangles);
}

public struct TriangulationEnumerator(IReadOnlyList<Vector2> Vertices, IReadOnlyList<int> Triangles)
{
	private int index = -3;

	public bool MoveNext() => (index += 3) < Triangles.Count - 2;

	public Triangle Current => new(
		Vertices[Triangles[index]],
		Vertices[Triangles[index + 1]],
		Vertices[Triangles[index + 2]]
	);
}
