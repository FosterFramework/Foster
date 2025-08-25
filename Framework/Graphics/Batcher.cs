using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Foster.Framework;

/// <summary>
/// A 2D Sprite Batcher.<br/>
/// <br/>
/// Constructs a <see cref="Mesh"/> which can be drawn by calling Render.<br/>
/// <br />
/// Note if you intend to re-use the Batcher over multiple frames, be sure to
/// call <see cref="Clear"/> after you have rendered it so it's ready for the
/// next frame.
/// </summary>
public class Batcher : IDisposable
{
	/// <summary>
	/// Sprite Batcher Texture Drawing Modes
	/// </summary>
	public enum Modes
	{
		/// <summary>
		/// Renders Textures normally, Multiplied by the Vertex Color
		/// </summary>
		Normal,

		/// <summary>
		/// Renders Textures washed using Vertex Colors, only using the Texture alpha channel.
		/// </summary>
		Wash,

		/// <summary>
		/// Renders only using Vertex Colors, essentially ignoring the Texture data entirely.
		/// </summary>
		Fill
	}

	/// <summary>
	/// The GraphicsDevice this Batcher was created with
	/// </summary>
	public readonly GraphicsDevice GraphicsDevice;

	/// <summary>
	/// The Default shader used by the Batcher.
	/// TODO: this shouldn't be static, but should be shared between sprite batchers...
	/// </summary>
	private static Shader? DefaultShader;

	/// <summary>
	/// The current Matrix Value of the Batcher
	/// </summary>
	public Matrix3x2 Matrix = Matrix3x2.Identity;

	/// <summary>
	/// The current Scissor Value of the Batcher
	/// </summary>
	public RectInt? Scissor => currentBatch.Scissor;

	/// <summary>
	/// The number of Triangles in the Batcher to be drawn
	/// </summary>
	public int TriangleCount => indexCount / 3;

	/// <summary>
	/// The number of Vertices in the Batcher to be drawn
	/// </summary>
	public int VertexCount => vertexCount;

	/// <summary>
	/// The number of vertex indices in the Batcher to be drawn
	/// </summary>
	public int IndexCount => indexCount;

	/// <summary>
	/// The number of individual batches (draw calls).
	/// </summary>
	public int BatchCount => batches.Count + (currentBatch.Elements > 0 ? 1 : 0);

	private readonly Material defaultMaterial;
	private readonly Stack<Matrix3x2> matrixStack = [];
	private readonly Stack<RectInt?> scissorStack = [];
	private readonly Stack<BlendMode> blendStack = [];
	private readonly Stack<TextureSampler> samplerStack = [];
	private readonly Stack<Material> materialStack = [];
	private readonly Stack<int> layerStack = [];
	private readonly Stack<Color> modeStack = [];
	private readonly List<Batch> batches = [];
	private readonly List<Material> materialsUsed = [];
	private readonly Queue<Material> materialsPool = [];
	private readonly Mesh<BatcherVertex, int> mesh;

	private Color mode = new(255, 0, 0, 0);
	private Batch currentBatch;
	private BatcherVertex[] vertexBuffer = [];
	private int[] indexBuffer = [];
	private int vertexCount = 0;
	private int indexCount = 0;
	private int currentBatchInsert;
	private bool meshDirty;

	private static readonly Color NormalMode = new(255, 0, 0, 0);
	private static readonly Color WashMode = new(0, 255, 0, 0);
	private static readonly Color FillMode = new(0, 0, 255, 0);

	private struct Batch(Material material, BlendMode blend, Texture? texture, TextureSampler sampler, int offset, int elements)
	{
		public int Layer = 0;
		public Material Material = material;
		public BlendMode Blend = blend;
		public Texture? Texture = texture;
		public RectInt? Scissor = null;
		public TextureSampler Sampler = sampler;
		public int Offset = offset;
		public int Elements = elements;
	}

	public Batcher(GraphicsDevice graphicsDevice, string? name = null)
	{
		GraphicsDevice = graphicsDevice;
		defaultMaterial = new();
		mesh = new Mesh<BatcherVertex>(graphicsDevice, name: name);
		Clear();
	}

	~Batcher()
	{
		Dispose();
	}

	/// <summary>
	/// Uploads the current state of the internal Mesh to the GPU
	/// </summary>
	public void Upload()
	{
		if (meshDirty && indexCount > 0 && vertexCount > 0)
		{
			mesh.Clear();
			mesh.SetIndices(indexBuffer.AsSpan(0, indexCount));
			mesh.SetVertices(vertexBuffer.AsSpan(0, vertexCount));
			meshDirty = false;
		}
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Clears the Batcher.
	/// </summary>
	public void Clear()
	{
		vertexCount = 0;
		indexCount = 0;
		currentBatchInsert = 0;
		currentBatch = new Batch(defaultMaterial, BlendMode.Premultiply, null, new(), 0, 0);
		mode = new Color(255, 0, 0, 0);
		batches.Clear();
		matrixStack.Clear();
		scissorStack.Clear();
		blendStack.Clear();
		materialStack.Clear();
		layerStack.Clear();
		samplerStack.Clear();
		modeStack.Clear();

		foreach (var it in materialsUsed)
			materialsPool.Enqueue(it);
		materialsUsed.Clear();

		Matrix = Matrix3x2.Identity;
	}

	#region Rendering

	/// <summary>
	/// Draws the Batcher to the given Target
	/// </summary>
	/// <param name="target">What Target to Draw to.<br/>The value should be either a <see cref="Target"/> or <see cref="Window"/>.</param>
	/// <param name="viewport">Optional Viewport Rectangle</param>
	/// <param name="scissor">Optional Scissor Rectangle, which will clip any Scissor rectangles pushed to the Batcher.</param>
	public void Render(IDrawableTarget target, RectInt? viewport = null, RectInt? scissor = null)
	{
		Point2 size;

		if (viewport.HasValue)
			size = new Point2(viewport.Value.Width, viewport.Value.Height);
		else
			size = new Point2(target.WidthInPixels, target.HeightInPixels);

		var matrix = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, 0, float.MaxValue);
		Render(target, matrix, viewport, scissor);
	}

	/// <summary>
	/// Draws the Batcher to the given Target with the given Matrix Transformation
	/// </summary>
	/// <param name="target">What Target to Draw to.<br/>The value should be either a <see cref="Target"/> or <see cref="Window"/>.</param>
	/// <param name="matrix">Transforms the entire Batch</param>
	/// <param name="viewport">Optional Viewport Rectangle</param>
	/// <param name="scissor">Optional Scissor Rectangle, which will clip any Scissor rectangles pushed to the Batcher.</param>
	public void Render(IDrawableTarget target, Matrix4x4 matrix, RectInt? viewport = null, RectInt? scissor = null)
	{
		if (target == null)
			throw new Exception("Target cannot be null");

		if (vertexCount <= 0 || indexCount <= 0)
			return;

		if (batches.Count <= 0 && currentBatch.Elements <= 0)
			return;

		// upload our data if we've been modified since the last time we rendered
		Upload();

		// make sure default shader and material are valid
		if (DefaultShader == null || DefaultShader.IsDisposed)
			DefaultShader = new BatcherShader(GraphicsDevice);
		defaultMaterial.Shader = DefaultShader;

		// render batches
		for (int i = 0; i < batches.Count; i++)
		{
			// remaining elements in the current batch
			if (currentBatchInsert == i && currentBatch.Elements > 0)
				RenderBatch(target, currentBatch, matrix, viewport, scissor);

			// render the batch
			RenderBatch(target, batches[i], matrix, viewport, scissor);
		}

		// remaining elements in the current batch
		if (currentBatchInsert == batches.Count && currentBatch.Elements > 0)
			RenderBatch(target, currentBatch, matrix, viewport, scissor);
	}

	private void RenderBatch(IDrawableTarget target, in Batch batch, in Matrix4x4 matrix, in RectInt? viewport, in RectInt? scissor)
	{
		// get trimmed scissor value
		var trimmed = scissor;
		if (batch.Scissor.HasValue && trimmed.HasValue)
			trimmed = batch.Scissor.Value.GetIntersection(trimmed.Value);
		else if (batch.Scissor.HasValue)
			trimmed = batch.Scissor;

		// don't render if we're going to clip the entire visible contents
		if (trimmed.HasValue && (trimmed.Value.Width <= 0 || trimmed.Value.Height <= 0))
			return;

		var texture = batch.Texture != null && !batch.Texture.IsDisposed ? batch.Texture : null;
		var mat = batch.Material;

		// set Fragment Sampler 0 to the texture to be drawn
		mat.Fragment.Samplers[0] = new(texture, batch.Sampler);

		// set Vertex Matrix, always assumed to be in slot 0 as the first data
		mat.Vertex.SetUniformBuffer(matrix);

		GraphicsDevice.Draw(new(target, mesh, mat)
		{
			Viewport = viewport,
			Scissor = trimmed,
			BlendMode = batch.Blend,
			IndexOffset = batch.Offset * 3,
			IndexCount = batch.Elements * 3,
			DepthWriteEnabled = false,
			DepthTestEnabled = false,
			CullMode = CullMode.None
		});
	}

	#endregion

	#region Modify State

	private void SetTexture(Texture? texture)
	{
		if (currentBatch.Texture == null || currentBatch.Elements == 0)
		{
			currentBatch.Texture = texture;
		}
		else if (currentBatch.Texture != texture)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Texture = texture;
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
			currentBatchInsert++;
		}
	}

	private void SetSampler(TextureSampler sampler)
	{
		if (currentBatch.Sampler == sampler || currentBatch.Elements == 0)
		{
			currentBatch.Sampler = sampler;
		}
		else if (currentBatch.Sampler != sampler)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Sampler = sampler;
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
			currentBatchInsert++;
		}
	}

	private void SetLayer(int layer)
	{
		if (currentBatch.Layer == layer)
			return;

		// insert last batch
		if (currentBatch.Elements > 0)
		{
			batches.Insert(currentBatchInsert, currentBatch);
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
		}

		// find the point to insert us
		var insert = 0;
		while (insert < batches.Count && batches[insert].Layer >= layer)
			insert++;

		currentBatch.Layer = layer;
		currentBatchInsert = insert;
	}

	private void SetMaterial(Material material)
	{
		if (currentBatch.Elements == 0)
		{
			currentBatch.Material = material;
		}
		else if (currentBatch.Material != material)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Material = material;
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
			currentBatchInsert++;
		}
	}

	private void SetBlend(in BlendMode blend)
	{
		if (currentBatch.Elements == 0)
		{
			currentBatch.Blend = blend;
		}
		else if (currentBatch.Blend != blend)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Blend = blend;
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
			currentBatchInsert++;
		}
	}

	private void SetScissor(RectInt? scissor)
	{
		if (currentBatch.Elements == 0)
		{
			currentBatch.Scissor = scissor;
		}
		else if (currentBatch.Scissor != scissor)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Scissor = scissor;
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
			currentBatchInsert++;
		}
	}

	/// <summary>
	/// Pushes a relative draw layer, with lower values being rendered first.
	/// Note that this is not very performant and should generally be avoided.
	/// </summary>
	public void PushLayer(int delta)
	{
		layerStack.Push(currentBatch.Layer);
		SetLayer(currentBatch.Layer + delta);
	}

	/// <summary>
	/// Pops the current Draw Layer
	/// </summary>
	public void PopLayer()
	{
		SetLayer(layerStack.Pop());
	}

	/// <summary>
	/// Pushes a Material to draw with.<br/>
	/// <br/>
	/// This clones the state of the Material, so changing it after pushing it
	/// will not have an affect on the results.<br/>
	/// <br/>
	/// Note that the Batcher uses the first Fragment Sampler for its texture,
	/// and assumes that the first Vertex Uniform Buffer begins with a Matrix4x4.
	/// </summary>
	public void PushMaterial(Material material)
	{
		if (material.Shader == null)
			throw new Exception("Material must have a Shader assigned");

		materialStack.Push(currentBatch.Material);
		if (!materialsPool.TryDequeue(out var copy))
			copy = new Material();
		materialsUsed.Add(copy);
		material.CopyTo(copy);
		SetMaterial(copy);
	}

	/// <summary>
	/// Pops the current Material
	/// </summary>
	public void PopMaterial()
	{
		SetMaterial(materialStack.Pop());
	}

	/// <summary>
	/// Pushes a Texture Sampler to draw with
	/// </summary>
	public void PushSampler(TextureSampler state)
	{
		samplerStack.Push(currentBatch.Sampler);
		SetSampler(state);
	}

	/// <summary>
	/// Pops the current Texture Sampler
	/// </summary>
	public void PopSampler()
	{
		SetSampler(samplerStack.Pop());
	}

	/// <summary>
	/// Pushes a BlendMode to draw with
	/// </summary>
	public void PushBlend(BlendMode blend)
	{
		blendStack.Push(currentBatch.Blend);
		SetBlend(blend);
	}

	/// <summary>
	/// Pops the current Blend Mode
	/// </summary>
	public void PopBlend()
	{
		SetBlend(blendStack.Pop());
	}

	/// <summary>
	/// Pushes a Scissor Rectangle to draw with.
	/// Note this is in absolute coordinates, and ignores previous
	/// scissors that are in the stack.
	/// </summary>
	public void PushScissor(RectInt? scissor)
	{
		scissorStack.Push(currentBatch.Scissor);
		SetScissor(scissor);
	}

	/// <summary>
	/// Pops the current Scissor Rectangle
	/// </summary>
	public void PopScissor()
	{
		SetScissor(scissorStack.Pop());
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="position"></param>
	/// <param name="scale"></param>
	/// <param name="origin"></param>
	/// <param name="rotation"></param>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	/// <returns></returns>
	public Matrix3x2 PushMatrix(in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, bool relative = true)
	{
		return PushMatrix(Transform.CreateMatrix(position, origin, scale, rotation), relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="position"></param>
	/// <param name="scale"></param>
	/// <param name="rotation"></param>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	/// <returns></returns>
	public Matrix3x2 PushMatrix(in Vector2 position, in Vector2 scale, float rotation, bool relative = true)
	{
		return PushMatrix(Transform.CreateMatrix(position, Vector2.Zero, scale, rotation), relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="transform"></param>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(Transform transform, bool relative = true)
	{
		return PushMatrix(transform.Matrix, relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="position"></param>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(in Vector2 position, bool relative = true)
	{
		return PushMatrix(Matrix3x2.CreateTranslation(position.X, position.Y), relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(in Matrix3x2 matrix, bool relative = true)
	{
		matrixStack.Push(Matrix);

		if (relative)
			Matrix = matrix * Matrix;
		else
			Matrix = matrix;

		return Matrix;
	}

	/// <summary>
	/// Pops the current Matrix used for drawing.
	/// </summary>
	public Matrix3x2 PopMatrix()
	{
		Matrix = matrixStack.Pop();
		return Matrix;
	}

	/// <summary>
	/// Pushes a Texture Color Mode
	/// </summary>
	public void PushMode(Modes mode)
	{
		var value = mode switch
		{
			Modes.Normal => NormalMode,
			Modes.Wash => WashMode,
			Modes.Fill => FillMode,
			_ => throw new NotImplementedException()
		};

		modeStack.Push(this.mode);
		this.mode = value;
	}

	/// <summary>
	/// Pushes a Texture Color Mode, using the Raw value, in case you have a
	/// shader that utilizes this data for something else.
	/// </summary>
	public void PushMode(Color mode)
	{
		modeStack.Push(this.mode);
		this.mode = mode;
	}

	/// <summary>
	/// Pops the current Color Mode
	/// </summary>
	public void PopMode()
	{
		mode = modeStack.Pop();
	}

	#endregion

	#region Line

	public void Line(in Vector2 from, in Vector2 to, float lineWeight, in Color color)
	{
		var normal = (to - from).Normalized();
		var perp = new Vector2(-normal.Y, normal.X) * lineWeight * .5f;
		Quad(from + perp, from - perp, to - perp, to + perp, color);
	}

	public void Line(in Vector2 from, in Vector2 to, float lineWeight, in Color fromColor, in Color toColor)
	{
		var normal = (to - from).Normalized();
		var perp = new Vector2(-normal.Y, normal.X) * lineWeight * .5f;
		Quad(from + perp, from - perp, to - perp, to + perp, fromColor, fromColor, toColor, toColor);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Line(in Line line, float lineWeight, in Color color)
		=> Line(line.From, line.To, lineWeight, color);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Line(in Line line, float lineWeight, in Color fromColor, in Color toColor)
		=> Line(line.From, line.To, lineWeight, fromColor, toColor);

	#endregion

	#region Dashed Line

	public void LineDashed(Vector2 from, Vector2 to, float lineWeight, Color color, float dashLength, float offsetPercent)
	{
		var diff = to - from;
		var dist = diff.Length();
		var axis = diff.Normalized();
		var perp = axis.TurnLeft() * (lineWeight * 0.5f);
		offsetPercent = ((offsetPercent % 1f) + 1f) % 1f;

		var startD = dashLength * offsetPercent * 2f;
		if (startD > dashLength)
			startD -= dashLength * 2f;

		for (float d = startD; d < dist; d += dashLength * 2f)
		{
			var a = from + axis * Math.Max(d, 0f);
			var b = from + axis * Math.Min(d + dashLength, dist);
			Quad(a + perp, b + perp, b - perp, a - perp, color);
		}
	}

	#endregion

	#region Quad

	public void Quad(in Quad quad, Color color)
		=> Quad(quad.A, quad.B, quad.C, quad.D, color);

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Color color)
	{
		Request(4, 6, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[3].Pos = Vector2.Transform(v3, Matrix);
		vertices[0].Col = color;
		vertices[1].Col = color;
		vertices[2].Col = color;
		vertices[3].Col = color;
		vertices[0].Mode = FillMode;
		vertices[1].Mode = FillMode;
		vertices[2].Mode = FillMode;
		vertices[3].Mode = FillMode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
		indices[3] = offset + 0;
		indices[4] = offset + 2;
		indices[5] = offset + 3;
	}

	public void Quad(Texture? texture, in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 t3, in Color color)
	{
		SetTexture(texture);
		Request(4, 6, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[3].Pos = Vector2.Transform(v3, Matrix);
		vertices[0].Tex = t0;
		vertices[1].Tex = t1;
		vertices[2].Tex = t2;
		vertices[3].Tex = t3;
		vertices[0].Col = color;
		vertices[1].Col = color;
		vertices[2].Col = color;
		vertices[3].Col = color;
		vertices[0].Mode = mode;
		vertices[1].Mode = mode;
		vertices[2].Mode = mode;
		vertices[3].Mode = mode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
		indices[3] = offset + 0;
		indices[4] = offset + 2;
		indices[5] = offset + 3;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Color c0, in Color c1, in Color c2, in Color c3)
	{
		Request(4, 6, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[3].Pos = Vector2.Transform(v3, Matrix);
		vertices[0].Col = c0;
		vertices[1].Col = c1;
		vertices[2].Col = c2;
		vertices[3].Col = c3;
		vertices[0].Mode = FillMode;
		vertices[1].Mode = FillMode;
		vertices[2].Mode = FillMode;
		vertices[3].Mode = FillMode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
		indices[3] = offset + 0;
		indices[4] = offset + 2;
		indices[5] = offset + 3;
	}

	public void Quad(Texture? texture, in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 t3, Color c0, Color c1, Color c2, Color c3)
	{
		SetTexture(texture);
		Request(4, 6, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[3].Pos = Vector2.Transform(v3, Matrix);
		vertices[0].Tex = t0;
		vertices[1].Tex = t1;
		vertices[2].Tex = t2;
		vertices[3].Tex = t3;
		vertices[0].Col = c0;
		vertices[1].Col = c1;
		vertices[2].Col = c2;
		vertices[3].Col = c3;
		vertices[0].Mode = mode;
		vertices[1].Mode = mode;
		vertices[2].Mode = mode;
		vertices[3].Mode = mode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
		indices[3] = offset + 0;
		indices[4] = offset + 2;
		indices[5] = offset + 3;
	}

	public void QuadLine(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d, float lineWeight, in Color color)
		=> QuadLine(new Quad(a, b, c, d), lineWeight, color);

	public void QuadLine(in Quad quad, float lineWeight, in Color color)
	{
		var off_ab = quad.NormalAB * lineWeight;
		var off_bc = quad.NormalBC * lineWeight;
		var off_cd = quad.NormalCD * lineWeight;
		var off_da = quad.NormalDA * lineWeight;

		var aa = Intersection(quad.D + off_da, quad.A + off_da, quad.A + off_ab, quad.B + off_ab);
		var bb = Intersection(quad.A + off_ab, quad.B + off_ab, quad.B + off_bc, quad.C + off_bc);
		var cc = Intersection(quad.B + off_bc, quad.C + off_bc, quad.C + off_cd, quad.D + off_cd);
		var dd = Intersection(quad.C + off_cd, quad.D + off_cd, quad.D + off_da, quad.A + off_da);

		Quad(aa, quad.A, quad.B, bb, color);
		Quad(bb, quad.B, quad.C, cc, color);
		Quad(cc, quad.C, quad.D, dd, color);
		Quad(dd, quad.D, quad.A, aa, color);
	}

	[Obsolete("Use QuadLine instead")]
	public void QuadLines(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d, float lineWeight, in Color color)
		=> QuadLine(a, b, c, d, lineWeight, color);

	#endregion

	#region Triangle

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, Color color)
	{
		Request(3, 3, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[0].Col = color;
		vertices[1].Col = color;
		vertices[2].Col = color;
		vertices[0].Mode = FillMode;
		vertices[1].Mode = FillMode;
		vertices[2].Mode = FillMode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
	}

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, Color c0, Color c1, Color c2)
	{
		Request(3, 3, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[0].Col = c0;
		vertices[1].Col = c1;
		vertices[2].Col = c2;
		vertices[0].Mode = FillMode;
		vertices[1].Mode = FillMode;
		vertices[2].Mode = FillMode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Triangle(in Triangle tri, Color color)
		=> Triangle(tri.A, tri.B, tri.C, color);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Triangle(in Triangle tri, Color c0, Color c1, Color c2)
		=> Triangle(tri.A, tri.B, tri.C, c0, c1, c2);

	public void Triangle(Texture? texture, in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 t0, in Vector2 t1, in Vector2 t2, Color color)
	{
		SetTexture(texture);
		Request(3, 3, out var vertices, out var indices, out var offset);

		vertices[0].Pos = Vector2.Transform(v0, Matrix);
		vertices[1].Pos = Vector2.Transform(v1, Matrix);
		vertices[2].Pos = Vector2.Transform(v2, Matrix);
		vertices[0].Tex = t0;
		vertices[1].Tex = t1;
		vertices[2].Tex = t2;
		vertices[0].Col = color;
		vertices[1].Col = color;
		vertices[2].Col = color;
		vertices[0].Mode = mode;
		vertices[1].Mode = mode;
		vertices[2].Mode = mode;

		indices[0] = offset + 0;
		indices[1] = offset + 1;
		indices[2] = offset + 2;
	}

	public void TriangleLine(in Vector2 a, in Vector2 b, in Vector2 c, float lineWeight, in Color color)
	{
		if (lineWeight <= 0)
			return;

		// TODO:
		// Detect if the thickness of the line fills the entire shape
		// (in which case, draw a triangle instead)

		var len_ab = (a - b).Length();
		var len_bc = (b - c).Length();
		var len_ca = (c - a).Length();

		var off_ab = ((b - a) / len_ab).TurnLeft() * lineWeight;
		var off_bc = ((c - b) / len_bc).TurnLeft() * lineWeight;
		var off_ca = ((a - c) / len_ca).TurnLeft() * lineWeight;

		var aa = Intersection(c + off_ca, a + off_ca, a + off_ab, b + off_ab);
		var bb = Intersection(a + off_ab, b + off_ab, b + off_bc, c + off_bc);
		var cc = Intersection(b + off_bc, c + off_bc, c + off_ca, a + off_ca);

		Quad(aa, a, b, bb, color);
		Quad(bb, b, c, cc, color);
		Quad(cc, c, a, aa, color);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TriangleLine(in Triangle tri, float lineWeight, in Color color)
		=> TriangleLine(tri.A, tri.B, tri.C, lineWeight, color);

	#endregion

	#region Rect

	public void Rect(in Rect rect, Color color)
	{
		Quad(
			new(rect.X, rect.Y),
			new(rect.X + rect.Width, rect.Y),
			new(rect.X + rect.Width, rect.Y + rect.Height),
			new(rect.X, rect.Y + rect.Height),
			color);
	}

	public void Rect(in Vector2 position, in Vector2 size, Color color)
	{
		Quad(
			position,
			position + new Vector2(size.X, 0),
			position + new Vector2(size.X, size.Y),
			position + new Vector2(0, size.Y),
			color);
	}

	public void Rect(float x, float y, float width, float height, Color color)
	{
		Quad(
			new(x, y),
			new(x + width, y),
			new(x + width, y + height),
			new(x, y + height), color);
	}

	public void Rect(in Rect rect, Color c0, Color c1, Color c2, Color c3)
	{
		Quad(
			new(rect.X, rect.Y),
			new(rect.X + rect.Width, rect.Y),
			new(rect.X + rect.Width, rect.Y + rect.Height),
			new(rect.X, rect.Y + rect.Height),
			c0, c1, c2, c3);
	}

	public void Rect(in Vector2 position, in Vector2 size, Color c0, Color c1, Color c2, Color c3)
	{
		Quad(
			position,
			position + new Vector2(size.X, 0),
			position + new Vector2(size.X, size.Y),
			position + new Vector2(0, size.Y),
			c0, c1, c2, c3);
	}

	public void Rect(float x, float y, float width, float height, Color c0, Color c1, Color c2, Color c3)
	{
		Quad(
			new(x, y),
			new(x + width, y),
			new(x + width, y + height),
			new(x, y + height),
			c0, c1, c2, c3);
	}

	public void RectLine(in Rect rect, float lineWeight, Color color)
	{
		if (lineWeight >= rect.Width / 2 || lineWeight >= rect.Height / 2)
		{
			Rect(rect, color);
		}
		else if (lineWeight > 0)
		{
			Rect(rect.X, rect.Y, rect.Width, lineWeight, color);
			Rect(rect.X, rect.Bottom - lineWeight, rect.Width, lineWeight, color);
			Rect(rect.X, rect.Y + lineWeight, lineWeight, rect.Height - lineWeight * 2, color);
			Rect(rect.Right - lineWeight, rect.Y + lineWeight, lineWeight, rect.Height - lineWeight * 2, color);
		}
	}

	public void RectDashed(Rect rect, float lineWeight, in Color color, float dashLength, float dashOffset)
	{
		rect = rect.Inflate(-lineWeight / 2);
		LineDashed(rect.TopLeft, rect.TopRight, lineWeight, color, dashLength, dashOffset);
		LineDashed(rect.TopRight, rect.BottomRight, lineWeight, color, dashLength, dashOffset);
		LineDashed(rect.BottomRight, rect.BottomLeft, lineWeight, color, dashLength, dashOffset);
		LineDashed(rect.BottomLeft, rect.TopLeft, lineWeight, color, dashLength, dashOffset);
	}

	#endregion

	#region Rounded Rect

	public void RectRounded(float x, float y, float width, float height, float r0, float r1, float r2, float r3, Color color)
	{
		RectRounded(new Rect(x, y, width, height), r0, r1, r2, r3, color);
	}

	public void RectRounded(float x, float y, float width, float height, float radius, Color color)
	{
		RectRounded(new Rect(x, y, width, height), radius, radius, radius, radius, color);
	}

	public void RectRounded(in Rect rect, float radius, Color color)
	{
		RectRounded(rect, radius, radius, radius, radius, color);
	}

	public void RectRounded(in Rect rect, float r0, float r1, float r2, float r3, Color color)
	{
		// clamp
		r0 = Math.Min(Math.Min(Math.Max(0, r0), rect.Width / 2f), rect.Height / 2f);
		r1 = Math.Min(Math.Min(Math.Max(0, r1), rect.Width / 2f), rect.Height / 2f);
		r2 = Math.Min(Math.Min(Math.Max(0, r2), rect.Width / 2f), rect.Height / 2f);
		r3 = Math.Min(Math.Min(Math.Max(0, r3), rect.Width / 2f), rect.Height / 2f);

		if (r0 <= 0 && r1 <= 0 && r2 <= 0 && r3 <= 0)
		{
			Rect(rect, color);
		}
		else
		{
			// get corners
			var r0_tl = rect.TopLeft;
			var r0_tr = r0_tl + new Vector2(r0, 0);
			var r0_br = r0_tl + new Vector2(r0, r0);
			var r0_bl = r0_tl + new Vector2(0, r0);

			var r1_tl = rect.TopRight + new Vector2(-r1, 0);
			var r1_tr = r1_tl + new Vector2(r1, 0);
			var r1_br = r1_tl + new Vector2(r1, r1);
			var r1_bl = r1_tl + new Vector2(0, r1);

			var r2_tl = rect.BottomRight + new Vector2(-r2, -r2);
			var r2_tr = r2_tl + new Vector2(r2, 0);
			var r2_bl = r2_tl + new Vector2(0, r2);
			var r2_br = r2_tl + new Vector2(r2, r2);

			var r3_tl = rect.BottomLeft + new Vector2(0, -r3);
			var r3_tr = r3_tl + new Vector2(r3, 0);
			var r3_bl = r3_tl + new Vector2(0, r3);
			var r3_br = r3_tl + new Vector2(r3, r3);

			Append(
				vertices: [
					new(Vector2.Transform(r0_tr, Matrix), color, FillMode), // 0
					new(Vector2.Transform(r0_br, Matrix), color, FillMode), // 1
					new(Vector2.Transform(r0_bl, Matrix), color, FillMode), // 2

					new(Vector2.Transform(r1_tl, Matrix), color, FillMode), // 3
					new(Vector2.Transform(r1_br, Matrix), color, FillMode), // 4
					new(Vector2.Transform(r1_bl, Matrix), color, FillMode), // 5

					new(Vector2.Transform(r2_tl, Matrix), color, FillMode), // 6
					new(Vector2.Transform(r2_tr, Matrix), color, FillMode), // 7
					new(Vector2.Transform(r2_bl, Matrix), color, FillMode), // 8

					new(Vector2.Transform(r3_tl, Matrix), color, FillMode), // 9
					new(Vector2.Transform(r3_tr, Matrix), color, FillMode), // 10
					new(Vector2.Transform(r3_br, Matrix), color, FillMode), // 11
				],
				indices: [
					// top quad
						00, /* r0b */ 03, /* r1a */ 05, /* r1d */
						00, /* r0b */ 05, /* r1d */ 01, /* r0c */
					// left quad
						02, /* r0d */ 01, /* r0c */ 10, /* r3b */
						02, /* r0d */ 10, /* r3b */ 09, /* r3a */
					// right quad
						05, /* r1d */ 04, /* r1c */ 07, /* r2b */
						05, /* r1d */ 07, /* r2b */ 06, /* r2a */
					// bottom quad
						10, /* r3b */ 06, /* r2a */ 08, /* r2d */
						10, /* r3b */ 08, /* r2d */ 11, /* r3c */
					// center quad
						01, /* r0c */ 05, /* r1d */ 06, /* r2a */
						01, /* r0c */ 06, /* r2a */ 10, /* r3b */
				]
			);

			var left = Calc.PI;
			var right = 0.0f;
			var up = -Calc.PI / 2;
			var down = Calc.PI / 2;

			// top-left corner
			if (r0 > 0)
				SemiCircle(r0_br, up, -left, r0, Math.Max(3, (int)(r0 / 4)), color);
			else
				Quad(r0_tl, r0_tr, r0_br, r0_bl, color);

			// top-right corner
			if (r1 > 0)
				SemiCircle(r1_bl, up, right, r1, Math.Max(3, (int)(r1 / 4)), color);
			else
				Quad(r1_tl, r1_tr, r1_br, r1_bl, color);

			// bottom-right corner
			if (r2 > 0)
				SemiCircle(r2_tl, right, down, r2, Math.Max(3, (int)(r2 / 4)), color);
			else
				Quad(r2_tl, r2_tr, r2_br, r2_bl, color);

			// bottom-left corner
			if (r3 > 0)
				SemiCircle(r3_tr, down, left, r3, Math.Max(3, (int)(r3 / 4)), color);
			else
				Quad(r3_tl, r3_tr, r3_br, r3_bl, color);
		}

	}

	public void RectRoundedLine(in Rect r, float rounding, float t, Color color)
	{
		RectRoundedLine(r, rounding, rounding, rounding, rounding, t, color);
	}

	public void RectRoundedLine(in Rect r, float rtl, float rtr, float rbr, float rbl, float t, Color color)
	{
		// clamp
		rtl = MathF.Min(MathF.Min(MathF.Max(0.0f, rtl), r.Width / 2.0f), r.Height / 2.0f);
		rtr = MathF.Min(MathF.Min(MathF.Max(0.0f, rtr), r.Width / 2.0f), r.Height / 2.0f);
		rbr = MathF.Min(MathF.Min(MathF.Max(0.0f, rbr), r.Width / 2.0f), r.Height / 2.0f);
		rbl = MathF.Min(MathF.Min(MathF.Max(0.0f, rbl), r.Width / 2.0f), r.Height / 2.0f);

		if (rtl <= 0 && rtr <= 0 && rbr <= 0 && rbl <= 0)
		{
			RectLine(r, t, color);
		}
		else
		{
			var rtlSteps = Math.Max(3, (int)(rtl / 4));
			var rtrSteps = Math.Max(3, (int)(rtr / 4));
			var rbrSteps = Math.Max(3, (int)(rbr / 4));
			var rblSteps = Math.Max(3, (int)(rbl / 4));

			// rounded corners
			SemiCircleLine(new Vector2(r.X + rtl, r.Y + rtl), Calc.Up, Calc.Left, rtl, rtlSteps, t, color);
			SemiCircleLine(new Vector2(r.X + r.Width - rtr, r.Y + rtr), Calc.Up, Calc.Up + Calc.TAU * 0.25f, rtr, rtrSteps, t, color);
			SemiCircleLine(new Vector2(r.X + rbl, r.Y + r.Height - rbl), Calc.Down, Calc.Left, rbl, rblSteps, t, color);
			SemiCircleLine(new Vector2(r.X + r.Width - rbr, r.Y + r.Height - rbr), Calc.Down, Calc.Right, rbr, rbrSteps, t, color);

			// connect sides that aren't touching
			if (r.Height > rtl + rbl)
				Rect(new Rect(r.X, r.Y + rtl, t, r.Height - rtl - rbl), color);
			if (r.Height > rtr + rbr)
				Rect(new Rect(r.X + r.Width - t, r.Y + rtr, t, r.Height - rtr - rbr), color);
			if (r.Width > rtl + rtr)
				Rect(new Rect(r.X + rtl, r.Y, r.Width - rtl - rtr, t), color);
			if (r.Width > rbl + rbr)
				Rect(new Rect(r.X + rbl, r.Y + r.Height - t, r.Width - rbl - rbr, t), color);
		}
	}

	#endregion

	#region Circle

	public void SemiCircle(in Vector2 center, float startRadians, float endRadians, float radius, int steps, in Color color)
	{
		SemiCircle(center, startRadians, endRadians, radius, steps, color, color);
	}

	public void SemiCircle(in Vector2 center, float startRadians, float endRadians, float radius, int steps, in Color centerColor, in Color edgeColor)
	{
		var last = Calc.AngleToVector(startRadians, radius);

		for (int i = 1; i <= steps; i++)
		{
			var next = Calc.AngleToVector(startRadians + (endRadians - startRadians) * (i / (float)steps), radius);
			Triangle(center + last, center + next, center, edgeColor, edgeColor, centerColor);
			last = next;
		}
	}

	public void SemiCircleLine(in Vector2 center, float startRadians, float endRadians, float radius, int steps, float t, Color color)
	{
		if (t >= radius)
		{
			SemiCircle(center, startRadians, endRadians, radius, steps, color, color);
		}
		else
		{
			var last = Calc.AngleToVector(startRadians);
			var r0 = radius - t;
			var r1 = radius;

			for (int i = 1; i <= steps; i++)
			{
				var next = Calc.AngleToVector(startRadians + (endRadians - startRadians) * (i / (float)steps));

				Quad(center + last * r0, center + last * r1, center + next * r1, center + next * r0, color);

				last = next;
			}
		}
	}

	public void Circle(in Vector2 center, float radius, int steps, in Color color)
		=> Circle(center, radius, steps, color, color);

	public void Circle(in Vector2 center, float radius, int steps, in Color centerColor, in Color edgeColor)
	{
		if (steps < 3)
			return;

		Request(steps + 1, steps * 3, out var vertices, out var indices, out var vertexStart);

		// center vertex
		vertices[0] = new(Vector2.Transform(center, Matrix), centerColor, FillMode);

		for (int n = 0, i = 0; n < steps; n++, i += 3)
		{
			var next = Calc.AngleToVector(n / (float)steps * Calc.TAU, radius);
			vertices[n + 1] = new(Vector2.Transform(center + next, Matrix), edgeColor, FillMode);
			indices[i + 0] = vertexStart; // center
			indices[i + 1] = vertexStart + 1 + n;
			indices[i + 2] = vertexStart + 1 + (n + 1) % steps;
		}
	}

	public void Circle(in Circle circle, int steps, in Color color)
		=> Circle(circle.Position, circle.Radius, steps, color, color);

	public void CircleLine(in Vector2 center, float radius, float lineWeight, int steps, in Color color)
	{
		var innerRadius = radius - lineWeight;
		if (innerRadius <= 0)
		{
			Circle(center, radius, steps, color);
			return;
		}

		var last = Calc.AngleToVector(0);
		for (int i = 1; i <= steps; i++)
		{
			var next = Calc.AngleToVector((i / (float)steps) * Calc.TAU);
			Quad(
				center + last * innerRadius, center + last * radius,
				center + next * radius, center + next * innerRadius,
				color);
			last = next;
		}
	}

	public void CircleLine(in Circle circle, float lineWeight, int steps, in Color color)
		=> CircleLine(circle.Position, circle.Radius, lineWeight, steps, color);

	public void CircleDashed(in Vector2 center, float radius, float lineWeight, int steps, in Color color, float dashLength, float dashOffset)
	{
		var last = Calc.AngleToVector(0, radius);
		var segmentLength = (last - Calc.AngleToVector(Calc.TAU / steps, radius)).Length();

		for (int i = 1; i <= steps; i++)
		{
			var next = Calc.AngleToVector((i / (float)steps) * Calc.TAU, radius);
			LineDashed(center + last, center + next, lineWeight, color, dashLength, dashOffset);
			dashOffset += segmentLength;
			last = next;
		}
	}

	public void CircleDashed(in Circle circle, float lineWeight, int steps, in Color color, float dashLength, float dashOffset)
		=> CircleDashed(circle.Position, circle.Radius, lineWeight, steps, color, dashLength, dashOffset);

	#endregion

	#region Radial Bar

	public void RadialBar(in Vector2 position, float percent, float inner_radius, float outer_radius, in Color color)
	{
		if (percent <= 0)
			return;

		const int segments = 64;
		const float single_segment = 1f / segments;
		float bar_radius = (outer_radius - inner_radius) / 2;

		PushMatrix(Matrix3x2.CreateTranslation(position));

		if (percent < single_segment)
		{
			float scale = Calc.ClampedMap(percent, 0, single_segment, 0, 1);
			Circle(Vector2.UnitX * (inner_radius + outer_radius) / 2, bar_radius * scale, 16, color);
		}
		else
		{
			for (int i = 0; i < segments; i++)
			{
				float prev = (i / (float)segments);
				float next = MathF.Min(percent, ((i + 1) / (float)segments));

				Vector2 prev_angle = Calc.AngleToVector(prev * Calc.TAU);
				Vector2 next_angle = Calc.AngleToVector(next * Calc.TAU);

				Vector2 prev_inner = prev_angle * inner_radius;
				Vector2 prev_outer = prev_angle * outer_radius;
				Vector2 next_inner = next_angle * inner_radius;
				Vector2 next_outer = next_angle * outer_radius;

				if (percent < 0.99f)
				{
					if (prev <= 0)
						Circle((prev_inner + prev_outer) / 2, bar_radius, 16, color);
					if (next >= percent)
						Circle((next_inner + next_outer) / 2, bar_radius, 16, color);
				}

				Quad(prev_inner, prev_outer, next_outer, next_inner, color);

				if (next >= percent)
					break;
			}
		}

		PopMatrix();
	}

	#endregion

	#region Image

	public void Image(Texture texture,
		in Vector2 pos0, in Vector2 pos1, in Vector2 pos2, in Vector2 pos3,
		in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 uv3,
		Color col0, Color col1, Color col2, Color col3)
	{
		Quad(texture, pos0, pos1, pos2, pos3, t0, t1, t2, uv3, col0, col1, col2, col3);
	}

	public void Image(Texture texture,
		in Vector2 pos0, in Vector2 pos1, in Vector2 pos2, in Vector2 pos3,
		in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 uv3,
		Color color)
	{
		Quad(texture, pos0, pos1, pos2, pos3, t0, t1, t2, uv3, color);
	}

	public void Image(Texture texture, Color color)
	{
		Quad(texture,
			new Vector2(0, 0),
			new Vector2(texture.Width, 0),
			new Vector2(texture.Width, texture.Height),
			new Vector2(0, texture.Height),
			new Vector2(0, 0),
			Vector2.UnitX,
			new Vector2(1, 1),
			Vector2.UnitY,
			color);
	}

	public void Image(Texture texture, in Vector2 position, Color color)
	{
		Quad(texture,
			position,
			position + new Vector2(texture.Width, 0),
			position + new Vector2(texture.Width, texture.Height),
			position + new Vector2(0, texture.Height),
			new Vector2(0, 0),
			Vector2.UnitX,
			new Vector2(1, 1),
			Vector2.UnitY,
			color);
	}

	public void Image(Texture texture, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		Quad(texture,
			new Vector2(0, 0),
			new Vector2(texture.Width, 0),
			new Vector2(texture.Width, texture.Height),
			new Vector2(0, texture.Height),
			new Vector2(0, 0),
			Vector2.UnitX,
			new Vector2(1, 1),
			Vector2.UnitY,
			color);

		Matrix = was;
	}

	public void Image(Texture texture, in Rect clip, in Vector2 position, Color color)
	{
		var tx0 = clip.X / texture.Width;
		var ty0 = clip.Y / texture.Height;
		var tx1 = clip.Right / texture.Width;
		var ty1 = clip.Bottom / texture.Height;

		Quad(texture,
			position,
			position + new Vector2(clip.Width, 0),
			position + new Vector2(clip.Width, clip.Height),
			position + new Vector2(0, clip.Height),
			new Vector2(tx0, ty0),
			new Vector2(tx1, ty0),
			new Vector2(tx1, ty1),
			new Vector2(tx0, ty1), color);
	}

	public void Image(Texture texture, in Rect clip, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		var tx0 = clip.X / texture.Width;
		var ty0 = clip.Y / texture.Height;
		var tx1 = clip.Right / texture.Width;
		var ty1 = clip.Bottom / texture.Height;

		Quad(texture,
			new Vector2(0, 0),
			new Vector2(clip.Width, 0),
			new Vector2(clip.Width, clip.Height),
			new Vector2(0, clip.Height),
			new Vector2(tx0, ty0),
			new Vector2(tx1, ty0),
			new Vector2(tx1, ty1),
			new Vector2(tx0, ty1),
			color);

		Matrix = was;
	}

	public void Image(in Subtexture subtex, Color color)
	{
		Quad(subtex.Texture,
			subtex.DrawCoords[0], subtex.DrawCoords[1], subtex.DrawCoords[2], subtex.DrawCoords[3],
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			color);
	}

	public void Image(in Subtexture subtex, in Vector2 position, Color color)
	{
		Quad(subtex.Texture,
			position + subtex.DrawCoords[0], position + subtex.DrawCoords[1], position + subtex.DrawCoords[2], position + subtex.DrawCoords[3],
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			color);
	}

	public void Image(in Subtexture subtex, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		Quad(subtex.Texture,
			subtex.DrawCoords[0], subtex.DrawCoords[1], subtex.DrawCoords[2], subtex.DrawCoords[3],
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			color);

		Matrix = was;
	}

	public void Image(in Subtexture subtex, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color c0, Color c1, Color c2, Color c3)
	{
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		Quad(subtex.Texture,
			subtex.DrawCoords[0], subtex.DrawCoords[1], subtex.DrawCoords[2], subtex.DrawCoords[3],
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			c0, c1, c2, c3);

		Matrix = was;
	}

	public void Image(in Subtexture subtex, in Rect clip, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var (source, frame) = subtex.GetClip(clip);
		var tex = subtex.Texture;
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		var px0 = -frame.X;
		var py0 = -frame.Y;
		var px1 = -frame.X + source.Width;
		var py1 = -frame.Y + source.Height;

		var tx0 = 0f;
		var ty0 = 0f;
		var tx1 = 0f;
		var ty1 = 0f;

		if (tex != null)
		{
			tx0 = source.Left / tex.Width;
			ty0 = source.Top / tex.Height;
			tx1 = source.Right / tex.Width;
			ty1 = source.Bottom / tex.Height;
		}

		Quad(subtex.Texture,
			new Vector2(px0, py0), new Vector2(px1, py0), new Vector2(px1, py1), new Vector2(px0, py1),
			new Vector2(tx0, ty0), new Vector2(tx1, ty0), new Vector2(tx1, ty1), new Vector2(tx0, ty1),
			color);

		Matrix = was;
	}

	public void ImageStretch(in Subtexture subtex, in Rect rect, Color color)
	{
		Quad(subtex.Texture,
			rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft,
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			color);
	}

	public void ImageStretch(in Subtexture subtex, in Rect rect, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var was = Matrix;

		var pos = rect.Position;
		Matrix = Transform.CreateMatrix(pos, origin, scale, rotation) * Matrix;

		Quad(subtex.Texture,
			Vector2.Zero, rect.TopRight - pos, rect.BottomRight - pos, rect.BottomLeft - pos,
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			color);

		Matrix = was;
	}

	public void ImageStretch(in Subtexture subtex, in Rect rect, Color c0, Color c1, Color c2, Color c3)
	{
		Quad(subtex.Texture,
			rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft,
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			c0, c1, c2, c3);
	}

	public void ImageStretch(in Subtexture subtex, in Rect rect, in Vector2 origin, in Vector2 scale, float rotation, Color c0, Color c1, Color c2, Color c3)
	{
		var was = Matrix;

		var pos = rect.Position;
		Matrix = Transform.CreateMatrix(pos, origin, scale, rotation) * Matrix;

		Quad(subtex.Texture,
			Vector2.Zero, rect.TopRight - pos, rect.BottomRight - pos, rect.BottomLeft - pos,
			subtex.TexCoords[0], subtex.TexCoords[1], subtex.TexCoords[2], subtex.TexCoords[3],
			c0, c1, c2, c3);

		Matrix = was;
	}

	public void ImageFit(in Subtexture subtex, in Rect rect, in Vector2 justify, Color color, bool flipX, bool flipY)
	{
		if (subtex.Texture == null)
			return;

		var bounds = rect;
		if (bounds.Width == 0)
			bounds.Width = subtex.Width;
		if (bounds.Height == 0)
			bounds.Height = subtex.Height;

		var scale = Calc.Min(bounds.Width / subtex.Width, bounds.Height / subtex.Height);
		var at = new Vector2(bounds.X + bounds.Width * justify.X, bounds.Y + bounds.Height * justify.Y);
		var orig = new Vector2(subtex.Width, subtex.Height) * justify;

		Image(subtex, at, orig, new Vector2(flipX ? -1 : 1, flipY ? -1 : 1) * scale, 0, color);
	}

	public void ImageFit(in Subtexture subtex, in Rect rect, in Vector2 justify, Color c0, Color c1, Color c2, Color c3, bool flipX, bool flipY)
	{
		if (subtex.Texture == null)
			return;

		var bounds = rect;
		if (bounds.Width == 0)
			bounds.Width = subtex.Width;
		if (bounds.Height == 0)
			bounds.Height = subtex.Height;

		var scale = Calc.Min(bounds.Width / subtex.Width, bounds.Height / subtex.Height);
		var at = new Vector2(bounds.X + bounds.Width * justify.X, bounds.Y + bounds.Height * justify.Y);
		var orig = new Vector2(subtex.Width, subtex.Height) * justify;

		Image(subtex, at, orig, new Vector2(flipX ? -1 : 1, flipY ? -1 : 1) * scale, 0, c0, c1, c2, c3);
	}

	#endregion

	#region Misc.

	/// <summary>
	/// Draws a checkered pattern.
	/// This is fine for small a amount of grid cells, but larger areas should use a custom shader for performance.
	/// </summary>
	public void CheckeredPattern(in Rect bounds, float cellWidth, float cellHeight, Color a, Color b)
	{
		var odd = false;

		for (float y = bounds.Top; y < bounds.Bottom; y += cellHeight)
		{
			var cells = 0;
			for (float x = bounds.Left; x < bounds.Right; x += cellWidth)
			{
				var color = (odd ? a : b);
				if (color.A > 0)
					Rect(x, y, Math.Min(bounds.Right - x, cellWidth), Math.Min(bounds.Bottom - y, cellHeight), color);

				odd = !odd;
				cells++;
			}

			if (cells % 2 == 0)
				odd = !odd;
		}
	}

	#endregion

	#region Internal Utils

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private void Append(in ReadOnlySpan<BatcherVertex> vertices, in ReadOnlySpan<int> indices)
	{
		// get spans to insert data
		Request(vertices.Length, indices.Length, out var vertexDest, out var indexDest, out var vertexStart);

		// copy vertices over
		vertices.CopyTo(vertexDest);

		// copy indices over but update them from relative offsets to the offsets in the buffer
		for (int i = 0; i < indices.Length; i ++)
			indexDest[i] = vertexStart + indices[i];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private void Request(int vertexAppendCount, int indexAppendCount, out Span<BatcherVertex> vertices, out Span<int> indices, out int vertexOffset)
	{
		vertexOffset = vertexCount;

		// make sure we have enough vertex space
		if (vertexCount + vertexAppendCount >= vertexBuffer.Length)
		{
			var capacity = Math.Max(8, vertexBuffer.Length);
			while (capacity <= vertexCount + vertexAppendCount)
				capacity *= 2;
			Array.Resize(ref vertexBuffer, capacity);
		}

		// make sure we have enough index space
		if (indexCount + indexAppendCount >= indexBuffer.Length)
		{
			var capacity = Math.Max(8, indexBuffer.Length);
			while (capacity <= indexCount + indexAppendCount)
				capacity *= 2;
			Array.Resize(ref indexBuffer, capacity);
		}

		// get slices
		vertices = vertexBuffer.AsSpan(vertexCount, vertexAppendCount);
		indices = indexBuffer.AsSpan(indexCount, indexAppendCount);

		// increase totals
		indexCount += indexAppendCount;
		vertexCount += vertexAppendCount;
		currentBatch.Elements += indexAppendCount / 3;
		meshDirty = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static Vector2 Intersection(in Vector2 p0, in Vector2 p1, in Vector2 q0, in Vector2 q1)
	{
		var aa = p1 - p0;
		var bb = q0 - q1;
		var cc = q0 - p0;
		var t = (bb.X * cc.Y - bb.Y * cc.X) / (aa.Y * bb.X - aa.X * bb.Y);
		return new(p0.X + t * (p1.X - p0.X), p0.Y + t * (p1.Y - p0.Y));
	}

	#endregion
}
