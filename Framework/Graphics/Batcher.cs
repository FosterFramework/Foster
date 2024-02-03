using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Foster.Framework;

public class Batcher : IDisposable
{
	/// <summary>
	/// Vertex Format of Batcher.Vertex
	/// </summary>
	private static readonly VertexFormat VertexFormat = VertexFormat.Create<Vertex>(
		new VertexFormat.Element(0, VertexType.Float2, false),
		new VertexFormat.Element(1, VertexType.Float2, false),
		new VertexFormat.Element(2, VertexType.UByte4, true),
		new VertexFormat.Element(3, VertexType.UByte4, true)
	);

	/// <summary>
	/// The Vertex Layout used for Sprite Batching
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex(Vector2 position, Vector2 texcoord, Color color, Color mode) : IVertex
	{
		public Vector2 Pos = position;
		public Vector2 Tex = texcoord;
		public Color Col = color;
		public Color Mode = mode;  // R = Multiply, G = Wash, B = Fill, A = Padding

		public readonly VertexFormat Format => VertexFormat;
	}

	/// <summary>
	/// The Default shader used by the Batcher.
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

	private readonly MaterialState defaultMaterialState = new();
	private readonly Stack<Matrix3x2> matrixStack = new();
	private readonly Stack<RectInt?> scissorStack = new();
	private readonly Stack<BlendMode> blendStack = new();
	private readonly Stack<TextureSampler> samplerStack = new();
	private readonly Stack<MaterialState> materialStack = new();
	private readonly Stack<int> layerStack = new();
	private readonly Stack<Color> modeStack = new();
	private readonly List<Batch> batches = new();
	private readonly Mesh mesh = new();
	private Batch currentBatch;
	private int currentBatchInsert;
	private Color mode = new(255, 0, 0, 0);
	private bool dirty;

	private readonly List<Material> materialPool = new();
	private int materialPoolIndex;

	private IntPtr vertexPtr = IntPtr.Zero;
	private int vertexCount = 0;
	private int vertexCapacity = 0;

	private IntPtr indexPtr = IntPtr.Zero;
	private int indexCount = 0;
	private int indexCapacity = 0;

	private readonly record struct MaterialState(
		Material Material,
		string MatrixUniform,
		string TextureUniform,
		string SamplerUniform
	);

	private struct Batch
	{
		public int Layer;
		public MaterialState MaterialState;
		public BlendMode Blend;
		public Texture? Texture;
		public RectInt? Scissor;
		public TextureSampler Sampler;
		public int Offset;
		public int Elements;
		public bool FlipVerticalUV;

		public Batch(MaterialState material, BlendMode blend, Texture? texture, TextureSampler sampler, int offset, int elements)
		{
			Layer = 0;
			MaterialState = material;
			Blend = blend;
			Texture = texture;
			Sampler = sampler;
			Scissor = null;
			Offset = offset;
			Elements = elements;
			FlipVerticalUV = (texture?.IsTargetAttachment ?? false) && Graphics.OriginBottomLeft;
		}
	}

	public Batcher()
	{
		if (DefaultShader == null || DefaultShader.IsDisposed)
			DefaultShader = new Shader(ShaderDefaults.Batcher[Graphics.Renderer]);
		defaultMaterialState = new(new Material(DefaultShader), "u_matrix", "u_texture", "u_texture_sampler");
		Clear();
	}

	~Batcher()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (vertexPtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(vertexPtr);
			vertexPtr = IntPtr.Zero;
			vertexCapacity = 0;
		}

		if (indexPtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(indexPtr);
			indexPtr = IntPtr.Zero;
			indexCapacity = 0;
		}

		materialPool.Clear();
		materialPoolIndex = 0;
	}

	/// <summary>
	/// Clears the Batcher.
	/// </summary>
	public void Clear()
	{
		vertexCount = 0;
		indexCount = 0;
		currentBatchInsert = 0;
		materialPoolIndex = 0;
		currentBatch = new Batch(defaultMaterialState, BlendMode.Premultiply, null, new(), 0, 0);
		mode = new Color(255, 0, 0, 0);
		batches.Clear();
		matrixStack.Clear();
		scissorStack.Clear();
		blendStack.Clear();
		materialStack.Clear();
		layerStack.Clear();
		samplerStack.Clear();
		modeStack.Clear();
		Matrix = Matrix3x2.Identity;
	}

	#region Rendering

	/// <summary>
	/// Draws the Batcher to the given Target
	/// </summary>
	/// <param name="target">What Target to Draw to, or null for the Window's backbuffer</param>
	/// <param name="viewport">Optional Viewport Rectangle</param>
	/// <param name="scissor">Optional Scissor Rectangle, which will clip any Scissor rectangles pushed to the Batcher.</param>
	public void Render(Target? target = null, RectInt? viewport = null, RectInt? scissor = null)
	{
		Point2 size;

		if (viewport.HasValue)
			size = new Point2(viewport.Value.Width, viewport.Value.Height);
		else if (target != null)
			size = new Point2(target.Width,target.Height);
		else
			size = new Point2(App.WidthInPixels, App.HeightInPixels);

		var matrix = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, 0, float.MaxValue);
		Render(target, matrix, viewport, scissor);
	}

	/// <summary>
	/// Draws the Batcher to the given Target with the given Matrix Transformation
	/// </summary>
	/// <param name="target">What Target to Draw to, or null for the Window's backbuffer</param>
	/// <param name="matrix">Transforms the entire Batch</param>
	/// <param name="viewport">Optional Viewport Rectangle</param>
	/// <param name="scissor">Optional Scissor Rectangle, which will clip any Scissor rectangles pushed to the Batcher.</param>
	public void Render(Target? target, Matrix4x4 matrix, RectInt? viewport = null, RectInt? scissor = null)
	{
		if (target != null && target.IsDisposed)
			throw new Exception("Target is disposed");

		if (indexPtr == IntPtr.Zero || vertexPtr == IntPtr.Zero)
			return;

		if (batches.Count <= 0 && currentBatch.Elements <= 0)
			return;

		// upload our data if we've been modified since the last time we rendered
		if (dirty)
		{
			mesh.SetIndices(indexPtr, indexCount, IndexFormat.ThirtyTwo);
			mesh.SetVertices(vertexPtr, vertexCount, VertexFormat);
			dirty = false;
		}

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

	private void RenderBatch(Target? target, in Batch batch, in Matrix4x4 matrix, in RectInt? viewport, in RectInt? scissor)
	{
		var trimmed = scissor;
		if (batch.Scissor.HasValue && trimmed.HasValue)
			trimmed = batch.Scissor.Value.OverlapRect(trimmed.Value);
		else if (batch.Scissor.HasValue)
			trimmed = batch.Scissor;

		var texture = batch.Texture != null && !batch.Texture.IsDisposed ? batch.Texture : null;

		var mat = batch.MaterialState.Material;
		mat.Set(batch.MaterialState.MatrixUniform, matrix);
		mat.Set(batch.MaterialState.TextureUniform, texture);
		mat.Set(batch.MaterialState.SamplerUniform, batch.Sampler);

		DrawCommand command = new(target, mesh, mat)
		{
			Viewport = viewport,
			Scissor = trimmed,
			BlendMode = batch.Blend,
			MeshIndexStart = batch.Offset * 3,
			MeshIndexCount = batch.Elements * 3,
			DepthMask = false,
			DepthCompare = DepthCompare.None,
			CullMode = CullMode.None
		};
		command.Submit();
	}

	#endregion

	#region Modify State

	/// <summary>
	/// Sets the Current Texture being drawn
	/// </summary>
	public void SetTexture(Texture? texture)
	{
		if (currentBatch.Texture == null || currentBatch.Elements == 0)
		{
			currentBatch.Texture = texture;
			currentBatch.FlipVerticalUV = (texture?.IsTargetAttachment ?? false) && Graphics.OriginBottomLeft;
		}
		else if (currentBatch.Texture != texture)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Texture = texture;
			currentBatch.FlipVerticalUV = (texture?.IsTargetAttachment ?? false) && Graphics.OriginBottomLeft;
			currentBatch.Offset += currentBatch.Elements;
			currentBatch.Elements = 0;
			currentBatchInsert++;
		}
	}

	/// <summary>
	/// Sets the Current Texture Sampler being used
	/// </summary>
	public void SetSampler(TextureSampler sampler)
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

	/// <summary>
	/// Sets the current Layer to draw at.
	/// Note that this is not very performant and should generally be avoided.
	/// </summary>
	public void SetLayer(int layer)
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

	private void SetMaterial(MaterialState materialState)
	{
		if (currentBatch.Elements == 0)
		{
			currentBatch.MaterialState = materialState;
		}
		else if (currentBatch.MaterialState != materialState)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.MaterialState = materialState;
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
	/// Pushes a Material to draw with
	/// This clones the state of the Material, so changing it after pushing it
	/// will not have an effect on the resulting draw.
	/// </summary>
	public void PushMaterial(Material material)
	{
		PushMaterial(material, 
			defaultMaterialState.MatrixUniform, 
			defaultMaterialState.TextureUniform, 
			defaultMaterialState.SamplerUniform
		);
	}

	/// <summary>
	/// Pushes a Material to draw with.
	/// This clones the state of the Material, so changing it after pushing it
	/// will not have an effect on the resulting draw.
	/// </summary>
	public void PushMaterial(Material material, string matrixUniform, string textureUniform, string samplerUniform)
	{
		materialStack.Push(currentBatch.MaterialState);

		// get a pooled material, or create a new one
		Material? copy;
		if (materialPoolIndex < materialPool.Count)
			copy = materialPool[materialPoolIndex];
		else
			materialPool.Add(copy = new Material());
		materialPoolIndex++;

		// copy the values to our internal material & set it
		material.CopyTo(copy);
		SetMaterial(new(copy, matrixUniform, textureUniform, samplerUniform));
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
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(in Vector2 position, in Vector2 scale, in Vector2 origin, float rotation, bool relative = true)
	{
		return PushMatrix(Transform.CreateMatrix(position, origin, scale, rotation), relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(Transform transform, bool relative = true)
	{
		return PushMatrix(transform.Matrix, relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(in Vector2 position, bool relative = true)
	{
		return PushMatrix(Matrix3x2.CreateTranslation(position.X, position.Y), relative);
	}

	/// <summary>
	/// Pushes a Matrix that will transform all future data
	/// </summary>
	/// <param name="relative">If the Matrix should be relative to the previously pushed transformations</param>
	public Matrix3x2 PushMatrix(in Matrix3x2 matrix, bool relative = true)
	{
		matrixStack.Push(Matrix);

		if (relative)
		{
			Matrix = matrix * Matrix;
		}
		else
		{
			Matrix = matrix;
		}

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
	/// Pushes the Normal drawing mode.
	/// This mode is used for drawing textures normally.
	/// </summary>
	public void PushModeNormal()
	{
		modeStack.Push(mode);
		mode = new Color(255, 0, 0, 0);
	}

	/// <summary>
	/// Pushes the Wash drawing mode, where only texture transparency is used and vertex color is the resulting output.
	/// </summary>
	public void PushModeWash()
	{
		modeStack.Push(mode);
		mode = new Color(0, 255, 0, 0);
	}

	/// <summary>
	/// Pushes the Fill drawing mode, where the texture is entirely ignored and the vertex color and alpha will be the resulting output.
	/// This mode is used for drawing shapes.
	/// </summary>
	public void PushModeFill()
	{
		modeStack.Push(mode);
		mode = new Color(0, 0, 255, 0);
	}

	/// <summary>
	/// Pushes a custom Mode value
	/// </summary>
	public void PushMode(Color value)
	{
		modeStack.Push(mode);
		mode = value;
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

	public void Line(in Vector2 from, in Vector2 to, float thickness, in Color color)
	{
		var normal = (to - from).Normalized();
		var perp = new Vector2(-normal.Y, normal.X) * thickness * .5f;
		Quad(from + perp, from - perp, to - perp, to + perp, color);
	}

	public void Line(in Vector2 from, in Vector2 to, float thickness, in Color fromColor, in Color toColor)
	{
		var normal = (to - from).Normalized();
		var perp = new Vector2(-normal.Y, normal.X) * thickness * .5f;
		Quad(from + perp, from - perp, to - perp, to + perp, fromColor, fromColor, toColor, toColor);
	}

	#endregion

	#region Dashed Line

	public void LineDashed(Vector2 from, Vector2 to, float thickness, Color color, float dashLength, float offsetPercent)
	{
		var diff = to - from;
		var dist = diff.Length();
		var axis = diff.Normalized();
		var perp = axis.TurnLeft() * (thickness * 0.5f);
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
		PushQuad();
		EnsureVertexCapacity(vertexCount + 4);

		unsafe
		{
			var mode = new Color(0, 0, 255, 0);
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 4);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[3].Pos = Vector2.Transform(v3, Matrix);
			vertexArray[0].Col = color;
			vertexArray[1].Col = color;
			vertexArray[2].Col = color;
			vertexArray[3].Col = color;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;
			vertexArray[3].Mode = mode;
		}

		vertexCount += 4;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 t3, in Color color)
	{
		PushQuad();
		EnsureVertexCapacity(vertexCount + 4);

		unsafe
		{
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 4);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[3].Pos = Vector2.Transform(v3, Matrix);
			vertexArray[0].Tex = t0;
			vertexArray[1].Tex = t1;
			vertexArray[2].Tex = t2;
			vertexArray[3].Tex = t3;
			vertexArray[0].Col = color;
			vertexArray[1].Col = color;
			vertexArray[2].Col = color;
			vertexArray[3].Col = color;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;
			vertexArray[3].Mode = mode;

			if (currentBatch.FlipVerticalUV)
				FlipVerticalUVs(vertexPtr, vertexCount, 4);
		}

		vertexCount += 4;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Color c0, in Color c1, in Color c2, in Color c3)
	{
		PushQuad();
		EnsureVertexCapacity(vertexCount + 4);

		unsafe
		{
			var mode = new Color(0, 0, 255, 0);
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 4);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[3].Pos = Vector2.Transform(v3, Matrix);
			vertexArray[0].Col = c0;
			vertexArray[1].Col = c1;
			vertexArray[2].Col = c2;
			vertexArray[3].Col = c3;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;
			vertexArray[3].Mode = mode;
		}

		vertexCount += 4;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 t3, Color c0, Color c1, Color c2, Color c3)
	{
		PushQuad();
		EnsureVertexCapacity(vertexCount + 4);

		unsafe
		{
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 4);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[3].Pos = Vector2.Transform(v3, Matrix);
			vertexArray[0].Tex = t0;
			vertexArray[1].Tex = t1;
			vertexArray[2].Tex = t2;
			vertexArray[3].Tex = t3;
			vertexArray[0].Col = c0;
			vertexArray[1].Col = c1;
			vertexArray[2].Col = c2;
			vertexArray[3].Col = c3;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;
			vertexArray[3].Mode = mode;

			if (currentBatch.FlipVerticalUV)
				FlipVerticalUVs(vertexPtr, vertexCount, 4);
		}

		vertexCount += 4;
	}

	public void QuadLine(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d, float thickness, in Color color)
	{
		Line(a, b, thickness, color);
		Line(b, c, thickness, color);
		Line(c, d, thickness, color);
		Line(d, a, thickness, color);
	}

	[Obsolete("Use QuadLine instead")]
	public void QuadLines(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d, float thickness, in Color color)
		=> QuadLine(a, b, c, d, thickness, color);

	#endregion

	#region Triangle

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, Color color)
	{
		PushTriangle();
		EnsureVertexCapacity(vertexCount + 3);

		unsafe
		{
			var mode = new Color(0, 0, 255, 0);
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 3);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[0].Col = color;
			vertexArray[1].Col = color;
			vertexArray[2].Col = color;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;
		}

		vertexCount += 3;
	}

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 uv0, in Vector2 uv1, in Vector2 uv2, Color color)
	{
		PushTriangle();
		EnsureVertexCapacity(vertexCount + 3);

		unsafe
		{
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 3);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[0].Tex = uv0;
			vertexArray[1].Tex = uv1;
			vertexArray[2].Tex = uv2;
			vertexArray[0].Col = color;
			vertexArray[1].Col = color;
			vertexArray[2].Col = color;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;

			if (currentBatch.FlipVerticalUV)
				FlipVerticalUVs(vertexPtr, vertexCount, 3);
		}

		vertexCount += 3;
	}

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, Color c0, Color c1, Color c2)
	{
		PushTriangle();
		EnsureVertexCapacity(vertexCount + 3);

		unsafe
		{
			var mode = new Color(0, 0, 255, 0);
			var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 3);

			vertexArray[0].Pos = Vector2.Transform(v0, Matrix);
			vertexArray[1].Pos = Vector2.Transform(v1, Matrix);
			vertexArray[2].Pos = Vector2.Transform(v2, Matrix);
			vertexArray[0].Col = c0;
			vertexArray[1].Col = c1;
			vertexArray[2].Col = c2;
			vertexArray[0].Mode = mode;
			vertexArray[1].Mode = mode;
			vertexArray[2].Mode = mode;

			if (currentBatch.FlipVerticalUV)
				FlipVerticalUVs(vertexPtr, vertexCount, 3);
		}

		vertexCount += 3;
	}

	public void TriangleLine(in Vector2 v0, in Vector2 v1, in Vector2 v2, float thickness, in Color color)
	{
		Line(v0, v1, thickness, color);
		Line(v1, v2, thickness, color);
		Line(v2, v0, thickness, color);
	}

	#endregion

	#region Rect

	public void Rect(in Rect rect, Color color)
	{
		Quad(
			new Vector2(rect.X, rect.Y),
			new Vector2(rect.X + rect.Width, rect.Y),
			new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
			new Vector2(rect.X, rect.Y + rect.Height),
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
			new Vector2(x, y),
			new Vector2(x + width, y),
			new Vector2(x + width, y + height),
			new Vector2(x, y + height), color);
	}

	public void Rect(in Rect rect, Color c0, Color c1, Color c2, Color c3)
	{
		Quad(
			new Vector2(rect.X, rect.Y),
			new Vector2(rect.X + rect.Width, rect.Y),
			new Vector2(rect.X + rect.Width, rect.Y + rect.Height),
			new Vector2(rect.X, rect.Y + rect.Height),
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
			new Vector2(x, y),
			new Vector2(x + width, y),
			new Vector2(x + width, y + height),
			new Vector2(x, y + height),
			c0, c1, c2, c3);
	}
	
	public void RectLine(in Rect rect, float t, Color color)
	{
		if (t > 0)
		{
			var tx = Math.Min(t, rect.Width / 2f);
			var ty = Math.Min(t, rect.Height / 2f);

			Rect(rect.X, rect.Y, rect.Width, ty, color);
			Rect(rect.X, rect.Bottom - ty, rect.Width, ty, color);
			Rect(rect.X, rect.Y + ty, tx, rect.Height - ty * 2, color);
			Rect(rect.Right - tx, rect.Y + ty, tx, rect.Height - ty * 2, color);
		}
	}

	public void RectDashed(Rect rect, float thickness, in Color color, float dashLength, float dashOffset)
	{
		rect = rect.Inflate(-thickness / 2);
		LineDashed(rect.TopLeft, rect.TopRight, thickness, color, dashLength, dashOffset);
		LineDashed(rect.TopRight, rect.BottomRight, thickness, color, dashLength, dashOffset);
		LineDashed(rect.BottomRight, rect.BottomLeft, thickness, color, dashLength, dashOffset);
		LineDashed(rect.BottomLeft, rect.TopLeft, thickness, color, dashLength, dashOffset);
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

			// set tris
			unsafe
			{
				EnsureIndexCapacity(indexCount + 30);

				var indexArray = new Span<int>((int*)indexPtr + indexCount, 30);

				// top quad
				{
					indexArray[00] = vertexCount + 00; // r0b
					indexArray[01] = vertexCount + 03; // r1a
					indexArray[02] = vertexCount + 05; // r1d

					indexArray[03] = vertexCount + 00; // r0b
					indexArray[04] = vertexCount + 05; // r1d
					indexArray[05] = vertexCount + 01; // r0c
				}

				// left quad
				{
					indexArray[06] = vertexCount + 02; // r0d
					indexArray[07] = vertexCount + 01; // r0c
					indexArray[08] = vertexCount + 10; // r3b

					indexArray[09] = vertexCount + 02; // r0d
					indexArray[10] = vertexCount + 10; // r3b
					indexArray[11] = vertexCount + 09; // r3a
				}

				// right quad
				{
					indexArray[12] = vertexCount + 05; // r1d
					indexArray[13] = vertexCount + 04; // r1c
					indexArray[14] = vertexCount + 07; // r2b

					indexArray[15] = vertexCount + 05; // r1d
					indexArray[16] = vertexCount + 07; // r2b
					indexArray[17] = vertexCount + 06; // r2a
				}

				// bottom quad
				{
					indexArray[18] = vertexCount + 10; // r3b
					indexArray[19] = vertexCount + 06; // r2a
					indexArray[20] = vertexCount + 08; // r2d

					indexArray[21] = vertexCount + 10; // r3b
					indexArray[22] = vertexCount + 08; // r2d
					indexArray[23] = vertexCount + 11; // r3c
				}

				// center quad
				{
					indexArray[24] = vertexCount + 01; // r0c
					indexArray[25] = vertexCount + 05; // r1d
					indexArray[26] = vertexCount + 06; // r2a

					indexArray[27] = vertexCount + 01; // r0c
					indexArray[28] = vertexCount + 06; // r2a
					indexArray[29] = vertexCount + 10; // r3b
				}

				indexCount += 30;
				currentBatch.Elements += 10;
				dirty = true;
			}

			// set verts
			unsafe
			{
				EnsureVertexCapacity(vertexCount + 12);

				var vertexArray = new Span<Vertex>((Vertex*)vertexPtr + vertexCount, 12);

				var mode = new Color(0, 0, 255, 0);

				vertexArray[00].Pos = Vector2.Transform(r0_tr, Matrix); // 0
				vertexArray[01].Pos = Vector2.Transform(r0_br, Matrix); // 1
				vertexArray[02].Pos = Vector2.Transform(r0_bl, Matrix); // 2

				vertexArray[03].Pos = Vector2.Transform(r1_tl, Matrix); // 3
				vertexArray[04].Pos = Vector2.Transform(r1_br, Matrix); // 4
				vertexArray[05].Pos = Vector2.Transform(r1_bl, Matrix); // 5

				vertexArray[06].Pos = Vector2.Transform(r2_tl, Matrix); // 6
				vertexArray[07].Pos = Vector2.Transform(r2_tr, Matrix); // 7
				vertexArray[08].Pos = Vector2.Transform(r2_bl, Matrix); // 8

				vertexArray[09].Pos = Vector2.Transform(r3_tl, Matrix); // 9
				vertexArray[10].Pos = Vector2.Transform(r3_tr, Matrix); // 10
				vertexArray[11].Pos = Vector2.Transform(r3_br, Matrix); // 11

				for (int i = 0; i < vertexArray.Length; i++)
				{
					vertexArray[i].Col = color;
					vertexArray[i].Mode = mode;
				}

				vertexCount += 12;
			}

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
			var add = Calc.AngleDiff(startRadians, endRadians);
			var lastInner = Calc.AngleToVector(startRadians, radius - t);
			var lastOuter = Calc.AngleToVector(startRadians, radius);

			for (int i = 1; i <= steps; i++)
			{
				var nextInner = Calc.AngleToVector(startRadians + add * (i / (float)steps), radius - t);
				var nextOuter = Calc.AngleToVector(startRadians + add * (i / (float)steps), radius);

				Quad(center + lastInner, center + lastOuter, center + nextOuter, center + nextInner, color);

				lastInner = nextInner;
				lastOuter = nextOuter;
			}
		}
	}

	public void Circle(in Vector2 center, float radius, int steps, in Color color)
		=> Circle(center, radius, steps, color, color);

	public void Circle(in Vector2 center, float radius, int steps, in Color centerColor, in Color edgeColor)
	{
		var last = Calc.AngleToVector(0, radius);

		for (int i = 1; i <= steps; i++)
		{
			var next = Calc.AngleToVector((i / (float)steps) * Calc.TAU, radius);
			Triangle(center + last, center + next, center, edgeColor, edgeColor, centerColor);
			last = next;
		}
	}

	public void Circle(in Circle circle, int steps, in Color color)
		=> Circle(circle.Position, circle.Radius, steps, color, color);

	public void CircleLine(in Vector2 center, float radius, float thickness, int steps, in Color color)
	{
		var innerRadius = radius - thickness;
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

	public void CircleLine(in Circle circle, float thickness, int steps, in Color color)
		=> CircleLine(circle.Position, circle.Radius, thickness, steps, color);

	public void CircleDashed(in Vector2 center, float radius, float thickness, int steps, in Color color, float dashLength, float dashOffset)
	{
		var last = Calc.AngleToVector(0, radius);
		var segmentLength = (last - Calc.AngleToVector(Calc.TAU / steps, radius)).Length();

		for (int i = 1; i <= steps; i++)
		{
			var next = Calc.AngleToVector((i / (float)steps) * Calc.TAU, radius);
			LineDashed(center + last, center + next, thickness, color, dashLength, dashOffset);
			dashOffset += segmentLength;
			last = next;
		}
	}

	public void CircleDashed(in Circle circle, float thickness, int steps, in Color color, float dashLength, float dashOffset)
		=> CircleDashed(circle.Position, circle.Radius, thickness, steps, color, dashLength, dashOffset);

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
		in Vector2 uv0, in Vector2 uv1, in Vector2 uv2, in Vector2 uv3,
		Color col0, Color col1, Color col2, Color col3)
	{
		SetTexture(texture);
		Quad(pos0, pos1, pos2, pos3, uv0, uv1, uv2, uv3, col0, col1, col2, col3);
	}

	public void Image(Texture texture,
		in Vector2 pos0, in Vector2 pos1, in Vector2 pos2, in Vector2 pos3,
		in Vector2 uv0, in Vector2 uv1, in Vector2 uv2, in Vector2 uv3,
		Color color)
	{
		SetTexture(texture);
		Quad(pos0, pos1, pos2, pos3, uv0, uv1, uv2, uv3, color);
	}

	public void Image(Texture texture, Color color)
	{
		SetTexture(texture);
		Quad(
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
		SetTexture(texture);
		Quad(
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

		SetTexture(texture);
		Quad(
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

		SetTexture(texture);
		Quad(
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

		SetTexture(texture);
		Quad(
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
		SetTexture(subtex.Texture);
		Quad(
			subtex.DrawCoords0, subtex.DrawCoords1, subtex.DrawCoords2, subtex.DrawCoords3,
			subtex.TexCoords0, subtex.TexCoords1, subtex.TexCoords2, subtex.TexCoords3,
			color);
	}

	public void Image(in Subtexture subtex, in Vector2 position, Color color)
	{
		SetTexture(subtex.Texture);
		Quad(position + subtex.DrawCoords0, position + subtex.DrawCoords1, position + subtex.DrawCoords2, position + subtex.DrawCoords3,
			subtex.TexCoords0, subtex.TexCoords1, subtex.TexCoords2, subtex.TexCoords3,
			color);
	}

	public void Image(in Subtexture subtex, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		SetTexture(subtex.Texture);
		Quad(
			subtex.DrawCoords0, subtex.DrawCoords1, subtex.DrawCoords2, subtex.DrawCoords3,
			subtex.TexCoords0, subtex.TexCoords1, subtex.TexCoords2, subtex.TexCoords3,
			color);

		Matrix = was;
	}

	public void Image(in Subtexture subtex, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color c0, Color c1, Color c2, Color c3)
	{
		var was = Matrix;

		Matrix = Transform.CreateMatrix(position, origin, scale, rotation) * Matrix;

		SetTexture(subtex.Texture);
		Quad(
			subtex.DrawCoords0, subtex.DrawCoords1, subtex.DrawCoords2, subtex.DrawCoords3,
			subtex.TexCoords0, subtex.TexCoords1, subtex.TexCoords2, subtex.TexCoords3,
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

		SetTexture(subtex.Texture);
		Quad(
			new Vector2(px0, py0), new Vector2(px1, py0), new Vector2(px1, py1), new Vector2(px0, py1),
			new Vector2(tx0, ty0), new Vector2(tx1, ty0), new Vector2(tx1, ty1), new Vector2(tx0, ty1),
			color);

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

	#region SpriteFont

	public void Text(SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Color color)
	{
		Text(font, text, position, Vector2.Zero, color);
	}

	public void Text(SpriteFont font, ReadOnlySpan<char> text, Vector2 position, Vector2 justify, Color color)
	{
		// TODO:
		// I feel like the vertical alignment is slightly off, but not sure how.

		var at = position + new Vector2(0, font.Ascent);
		var last = 0;

		if (justify.X != 0)
			at.X -= justify.X * font.WidthOfLine(text);

		if (justify.Y != 0)
			at.Y -= justify.Y * font.HeightOf(text);

		at.X = Calc.Round(at.X);
		at.Y = Calc.Round(at.Y);

		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				at.X = position.X;
				if (justify.X != 0 && i < text.Length - 1)
					at.X -= justify.X * font.WidthOfLine(text[(i + 1)..]);
				at.Y += font.LineHeight;
				last = 0;
				continue;
			}

			if (font.TryGetCharacter(text, i, out var ch, out var step))
			{
				if (last != 0)
					at.X += font.GetKerning(last, ch.Codepoint);

				if (ch.Subtexture.Texture != null)
					Image(ch.Subtexture, at + ch.Offset, color);

				last = ch.Codepoint;
				at.X += ch.Advance;
				i += step - 1;
			}
		}
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void PushTriangle()
	{
		EnsureIndexCapacity(indexCount + 3);

		unsafe
		{
			var indexArray = new Span<int>((int*)indexPtr + indexCount, 3);

			indexArray[0] = vertexCount + 0;
			indexArray[1] = vertexCount + 1;
			indexArray[2] = vertexCount + 2;
		}

		indexCount += 3;
		currentBatch.Elements++;
		dirty = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void PushQuad()
	{
		EnsureIndexCapacity(indexCount + 6);

		unsafe
		{
			var indexArray = new Span<int>((int*)indexPtr + indexCount, 6);

			indexArray[0] = vertexCount + 0;
			indexArray[1] = vertexCount + 1;
			indexArray[2] = vertexCount + 2;
			indexArray[3] = vertexCount + 0;
			indexArray[4] = vertexCount + 2;
			indexArray[5] = vertexCount + 3;
		}

		indexCount += 6;
		currentBatch.Elements += 2;
		dirty = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void EnsureIndexCapacity(int index)
	{
		if (index >= indexCapacity)
		{
			if (indexCapacity == 0)
				indexCapacity = 32;

			while (index >= indexCapacity)
				indexCapacity *= 2;

			var newPtr = Marshal.AllocHGlobal(sizeof(int) * indexCapacity);

			if (indexCount > 0)
				Buffer.MemoryCopy((void*)indexPtr, (void*)newPtr, indexCapacity * sizeof(int), indexCount * sizeof(int));

			if (indexPtr != IntPtr.Zero)
				Marshal.FreeHGlobal(indexPtr);

			indexPtr = newPtr;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void EnsureVertexCapacity(int index)
	{
		if (index >= vertexCapacity)
		{
			if (vertexCapacity == 0)
				vertexCapacity = 32;

			while (index >= vertexCapacity)
				vertexCapacity *= 2;

			var newPtr = Marshal.AllocHGlobal(sizeof(Vertex) * vertexCapacity);

			if (vertexCount > 0)
				Buffer.MemoryCopy((void*)vertexPtr, (void*)newPtr, vertexCapacity * sizeof(Vertex), vertexCount * sizeof(Vertex));

			if (vertexPtr != IntPtr.Zero)
				Marshal.FreeHGlobal(vertexPtr);

			vertexPtr = newPtr;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void FlipVerticalUVs(IntPtr ptr, int start, int count)
	{
		Vertex* it = (Vertex*)ptr + start;
		Vertex* end = it + count;
		while (it < end)
		{
			it->Tex.Y = 1.0f - it->Tex.Y;
			it++;
		}
	}

	#endregion
}
