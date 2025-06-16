﻿using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework;

/// <summary>
/// A 2D Polygon
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public class Polygon : IList<Vector2>, IList
{
	private readonly List<Vector2> vertices = [];
	private readonly List<int> triangles = [];
	private bool boundsDirty = true;
	private bool trianglesDirty = true;
	private Rect bounds;

	/// <summary>
	/// Triangle Indices.
	/// Note: Modifying the Polygon will invalidate this Span
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
	public Rect Bounds
	{
		get
		{
			CalculateBounds();
			return bounds;
		}
	}

	public Polygon() {}

	public Polygon(in Rect rect)
		: this(rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft) {}

	public Polygon(params ReadOnlySpan<Vector2> vertices)
	{
		foreach (var it in vertices)
			this.vertices.Add(it);
		trianglesDirty = true;
		boundsDirty = true;
	}

	public int Count => vertices.Count;

	public int IndexOf(Vector2 item)
		=> vertices.IndexOf(item);

	public void Insert(int index, Vector2 item)
	{
		vertices.Insert(index, item);
		trianglesDirty = boundsDirty = true;
	}

	public void RemoveAt(int index)
	{
		vertices.RemoveAt(index);
		trianglesDirty = boundsDirty = true;
	}

	public void Add(Vector2 item)
	{
		vertices.Add(item);
		trianglesDirty = boundsDirty = true;
	}

	public bool Contains(Vector2 item)
		=> vertices.Contains(item);

	public void CopyTo(Vector2[] array, int arrayIndex)
		=> vertices.CopyTo(array, arrayIndex);

	public bool Remove(Vector2 item)
	{
		if (vertices.Remove(item))
		{
			trianglesDirty = boundsDirty = true;
			return true;
		}
		return false;
	}

	public void Clear()
	{
		vertices.Clear();
		triangles.Clear();
		trianglesDirty = false;
		boundsDirty = false;
		bounds = default;
	}

	public Vector2 this[int index]
	{
		get => vertices[index];
		set
		{
			if (value != vertices[index])
			{
				vertices[index] = value;
				boundsDirty = trianglesDirty = true;
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
			boundsDirty = true;
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
				boundsDirty = trianglesDirty = true;
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
	public IEnumerable<(Vector2 A, Vector2 B)> Edges
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
		if (!trianglesDirty)
			return;
		trianglesDirty = false;
		triangles.Clear();
		Calc.Triangulate(vertices, triangles);
	}

	private void CalculateBounds()
	{
		if (!boundsDirty)
			return;

		boundsDirty = false;
		if (vertices.Count == 0)
		{
			bounds = new();
			return;
		}

		Vector2 min = vertices[0], max = vertices[0];
		for (int i = 1; i < vertices.Count; i ++)
		{
			min = Vector2.Min(min, vertices[i]);
			max = Vector2.Max(max, vertices[i]);
		}
		bounds = Rect.Between(min, max);
	}

	public void Render(Batcher batch, Color color)
	{
		if (Count < 3)
			return;

		var indices = Indices;
		for (int i = 0; i < indices.Length; i ++)
		{
			var a = indices[i];
			var b = indices[(i + 1) % indices.Length];
			var c = indices[(i + 2) % indices.Length];
			batch.Triangle(vertices[a], vertices[b], vertices[c], color);
		}
	}

	public void RenderLine(Batcher batch, float lineWeight, Color color)
	{
		if (Count < 2)
			return;

		for (int i = 0; i < vertices.Count - 1; i ++)
			batch.Line(vertices[i], vertices[i + 1], lineWeight, color);
		batch.Line(vertices[^1], vertices[0], lineWeight, color);
	}

	#region Interfaces

	IEnumerator<Vector2> IEnumerable<Vector2>.GetEnumerator()
		=> vertices.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> vertices.GetEnumerator();

	int IList.Add(object? value)
	{
		int result = ((IList)vertices).Add(value);
		if (result >= 0)
			boundsDirty = trianglesDirty = true;
		return -1;
	}

	bool IList.Contains(object? value)
		=> ((IList)vertices).Contains(value);

	int IList.IndexOf(object? value)
		=> ((IList)vertices).IndexOf(value);

	void IList.Insert(int index, object? value)
	{
		((IList)vertices).Insert(index, value);
		boundsDirty = trianglesDirty = true;
	}

	void IList.Remove(object? value)
	{
		if (value is Vector2 v)
			Remove(v);
	}

	void ICollection.CopyTo(Array array, int index) => ((IList)vertices).CopyTo(array, index);
	bool ICollection<Vector2>.IsReadOnly => ((IList<Vector2>)vertices).IsReadOnly;
	bool IList.IsReadOnly => ((IList)vertices).IsReadOnly;
	bool IList.IsFixedSize => ((IList)vertices).IsFixedSize;
	bool ICollection.IsSynchronized => ((IList)vertices).IsSynchronized;
	object ICollection.SyncRoot => ((IList)vertices).SyncRoot;

	object? IList.this[int index]
	{
		get => ((IList)vertices)[index];
		set
		{
			if (value is Vector2 v)
				this[index] = v;
		}
	}

	#endregion

	#region Json Converter

	public class JsonConverter : JsonConverter<Polygon>
	{
		public override Polygon Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> [.. JsonSerializer.Deserialize(ref reader, PolygonVerticesJsonContext.Default.ListVector2) ?? []];

		public override void Write(Utf8JsonWriter writer, Polygon value, JsonSerializerOptions options)
		{
			if (value != null)
				JsonSerializer.Serialize(writer, value.vertices, PolygonVerticesJsonContext.Default.ListVector2);
			else
				writer.WriteNullValue();
		}
	}

	#endregion
}

[JsonSerializable(typeof(List<Vector2>))]
[JsonSourceGenerationOptions(Converters = [typeof(JsonConverters.Vector2)])]
internal partial class PolygonVerticesJsonContext : JsonSerializerContext {}
