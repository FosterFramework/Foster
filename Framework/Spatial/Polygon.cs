using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Foster.Framework;

public class Polygon : IEnumerable<Vector2>
{
	private readonly List<Vector2> vertices = new();
	private readonly List<int> triangles = new();
	private bool trianglesDirty;

	/// <summary>
	/// Unsafe Indices - modifying the Polygon will invalidate this Span
	/// </summary>
	public ReadOnlySpan<int> Indices
	{
		get
		{
			Triangulate();
			return CollectionsMarshal.AsSpan(triangles);
		}
	}

	/// <summary>
	/// Polygon Bounds
	/// </summary>
	public Rect Bounds { get; private set; }

	public Polygon()
	{
		vertices.Add(new(-32, -32));
		vertices.Add(new(32, -32));
		vertices.Add(new(32, 32));
		vertices.Add(new(-32, 32));
		trianglesDirty = true;
		CalculateBounds();
	}

	public Polygon(IEnumerable<Vector2> vertices)
	{
		this.vertices.AddRange(vertices);
		trianglesDirty = true;
	}

	public int Count => vertices.Count;

	public void Add(in Vector2 pt)
	{
		vertices.Add(pt);
		trianglesDirty = true;
		CalculateBounds();
	}

	public void Add()
	{
		vertices.Add(new());
		trianglesDirty = true;
		CalculateBounds();
	}

	public void Insert(int index, in Vector2 pt)
	{
		vertices.Insert(index, pt);
		trianglesDirty = true;
		CalculateBounds();
	}

	public void Remove(int index)
	{
		vertices.RemoveAt(index);
		trianglesDirty = true;
		CalculateBounds();
	}

	public void Clear()
	{
		vertices.Clear();
		triangles.Clear();
		trianglesDirty = false;
		Bounds = new();
	}

	public Vector2 this[int index]
	{
		get => vertices[index];
		set
		{
			if (value != vertices[index])
			{
				vertices[index] = value;
				trianglesDirty = true;
				CalculateBounds();
			}
		}
	}

	public Polygon Move(in Vector2 offset)
	{
		if (offset != Vector2.Zero && vertices.Count > 0)
		{
			for (int i = 0; i < vertices.Count; i++)
				vertices[i] += offset;
			trianglesDirty = true;
			CalculateBounds();
		}

		return this;
	}

	public Vector2 Center
	{
		get => Bounds.Center;
		set
		{
			var diff = value - Center;
			if (diff != Vector2.Zero)
			{
				for (int i = 0; i < vertices.Count; i++)
					vertices[i] += diff;
				trianglesDirty = true;
				CalculateBounds();
			}
		}
	}

	public bool Contains(in Vector2 pt)
	{
		foreach (var tri in Triangles)
			if (tri.Contains(pt))
				return true;
		return false;
	}

	public bool Overlaps(in Rect rect)
	{
		foreach (var tri in Triangles)
			if (rect.Overlaps(tri))
				return true;
		return false;
	}

	public bool Overlaps(in Circle circ)
	{
		foreach (var tri in Triangles)
			if (circ.Overlaps(tri))
				return true;
		return false;
	}

	/// <summary>
	/// Find the edge closest to the given point
	/// </summary>
	/// <returns>The index of the first vertex of the edge, the second being the next vertex in the list - or the first vertex if this is the last</returns>
	public int GetClosestEdge(in Vector2 to)
	{
		if (Count <= 2)
			return 0;
		else
		{
			int closestIndex = 0;
			float closestDistSq = new Line(vertices[0], vertices[1]).DistanceSquared(to);

			for (int i = 1; i < Count; i++)
			{
				float distSq = new Line(vertices[i], vertices[(i + 1) % Count]).DistanceSquared(to);
				if (distSq < closestDistSq)
				{
					closestDistSq = distSq;
					closestIndex = i;
				}
			}

			return closestIndex;
		}
	}

	/// <summary>
	/// Enumerate all triangles formed by this polygon. If the polygon's edges cross themselves these may be incorrect. The polygon must triangulate to find these triangles, which is expensive, but it will cache triangles so long as no vertices are changed.
	/// </summary>
	public IEnumerable<Triangle> Triangles
	{
		get
		{
			Triangulate();
			for (int i = 0; i < triangles.Count - 2; i += 3)
				yield return new(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
		}
	}

	/// <summary>
	/// Get the triangles. Do not edit the given lists
	/// </summary>
	public void GetTriangles(out List<Vector2> verts, out List<int> indices)
	{
		Triangulate();
		verts = vertices;
		indices = triangles;
	}

	/// <summary>
	/// Enumerate all edges of the polygon
	/// </summary>
	public IEnumerable<(Vector2 a, Vector2 b)> Edges
	{
		get
		{
			if (vertices.Count > 1)
			{
				for (int i = 1; i < vertices.Count; i++)
					yield return (vertices[i - 1], vertices[i]);
				if (vertices.Count > 2)
					yield return (vertices[^1], vertices[0]);
			}
		}
	}

	private void Triangulate()
	{
		if (trianglesDirty)
		{
			triangles.Clear();
			Calc.Triangulate(vertices, triangles);
			trianglesDirty = false;
		}
	}

	private void CalculateBounds()
	{
		if (vertices.Count == 0)
			Bounds = new();
		else
			Bounds = new()
			{
				X = vertices.Min(v => v.X),
				Y = vertices.Min(v => v.Y),
				Right = vertices.Max(v => v.X),
				Bottom = vertices.Max(v => v.Y),
			};
	}

	IEnumerator<Vector2> IEnumerable<Vector2>.GetEnumerator() => vertices.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => vertices.GetEnumerator();
}
