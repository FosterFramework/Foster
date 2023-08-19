using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Foster.Framework;

public class Batcher
{
	private static readonly VertexFormat VertexFormat = VertexFormat.Create<Vertex>(
		new VertexFormat.Element(0, VertexType.Float2, false),
		new VertexFormat.Element(1, VertexType.Float2, false),
		new VertexFormat.Element(2, VertexType.UByte4, true),
		new VertexFormat.Element(3, VertexType.UByte4, true)
	);

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vertex : IVertex
	{
		public Vector2 Pos;
		public Vector2 Tex;
		public Color Col;
		public Color Mode;  // R = Multiply, G = Wash, B = Fill, A = Padding

		public Vertex(Vector2 position, Vector2 texcoord, Color color, Color mode)
		{
			Pos = position;
			Tex = texcoord;
			Col = color;
			Mode = mode;
		}

		public readonly VertexFormat Format => VertexFormat;
	}

	public static Shader? DefaultShader { get; private set; }

	public Matrix3x2 MatrixStack = Matrix3x2.Identity;
	public RectInt? Scissor => currentBatch.Scissor;

	public string TextureUniformName = "u_texture";
	public string SamplerUniformName = "u_texture_sampler";
	public string MatrixUniformName = "u_matrix";
	
	public int TriangleCount => indexCount / 3;
	public int VertexCount => vertexCount;
	public int IndexCount => indexCount;
	public int BatchCount => batches.Count + (currentBatch.Elements > 0 ? 1 : 0);

	private readonly Stack<Matrix3x2> matrixStack = new();
	private readonly Stack<RectInt?> scissorStack = new();
	private readonly Stack<BlendMode> blendStack = new();
	private readonly Stack<TextureSampler> samplerStack = new();
	private readonly Stack<Shader?> effectStack = new();
	private readonly Stack<int> layerStack = new();
	private readonly Stack<Color> modeStack = new();
	private readonly List<Batch> batches = new();
	private readonly Mesh mesh = new();
	private Batch currentBatch;
	private int currentBatchInsert;
	private Color mode = new(255, 0, 0, 0);
	private bool dirty;
	private Vertex[] vertexArray = new Vertex[64];
	private int vertexCount;
	private int[] indexArray = new int[64];
	private int indexCount;

	private struct Batch
	{
		public int Layer;
		public Shader? Shader;
		public BlendMode Blend;
		public Texture? Texture;
		public RectInt? Scissor;
		public TextureSampler Sampler;
		public int Offset;
		public int Elements;

		public Batch(Shader? effect, BlendMode blend, Texture? texture, TextureSampler sampler, int offset, int elements)
		{
			Layer = 0;
			Shader = effect;
			Blend = blend;
			Texture = texture;
			Sampler = sampler;
			Scissor = null;
			Offset = offset;
			Elements = elements;
		}
	}

	public Batcher()
	{
		DefaultShader ??= new Shader(ShaderDefaults.Batcher[Renderers.OpenGL]);
		Clear();
	}

	public void Clear()
	{
		vertexCount = 0;
		indexCount = 0;
		currentBatchInsert = 0;
		currentBatch = new Batch(null, BlendMode.Premultiply, null, new(), 0, 0);
		mode = new Color(255, 0, 0, 0);
		batches.Clear();
		matrixStack.Clear();
		scissorStack.Clear();
		blendStack.Clear();
		effectStack.Clear();
		layerStack.Clear();
		samplerStack.Clear();
		modeStack.Clear();
		MatrixStack = Matrix3x2.Identity;
	}

	#region Rendering

	public void Render(Target? target = null)
	{
		Matrix4x4 matrix = target != null
			? Matrix4x4.CreateOrthographicOffCenter(0, target.Width, target.Height, 0, 0, float.MaxValue)
			: Matrix4x4.CreateOrthographicOffCenter(0, App.WidthInPixels, App.HeightInPixels, 0, 0, float.MaxValue);
		Render(target, matrix);
	}

	public void Render(Target? target, Matrix4x4 matrix, RectInt? viewport = null, RectInt? scissor = null)
	{
		Debug.Assert(target == null || !target.IsDisposed, "Target is disposed");

		if (batches.Count <= 0 && currentBatch.Elements <= 0)
			return;
		
		if (dirty)
		{
			mesh.SetIndices<int>(indexArray.AsSpan(0, indexCount));
			mesh.SetVertices<Vertex>(vertexArray.AsSpan(0, vertexCount));
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
		var shader = batch.Shader ?? DefaultShader ?? throw new Exception("No Default Shader");
		shader[TextureUniformName].Set(texture);
		shader[SamplerUniformName].Set(batch.Sampler);
		shader[MatrixUniformName].Set(matrix);

		DrawCommand command = new(target, mesh, shader)
		{
			Viewport = viewport,
			Scissor = scissor,
			BlendMode = batch.Blend,
			MeshIndexStart = batch.Offset * 3,
			MeshIndexCount = batch.Elements * 3
		};
		command.Submit();
	}

	#endregion

	#region Modify State

	public void SetTexture(Texture? texture)
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
	
	private void SetShader(Shader? effect)
	{
		if (currentBatch.Elements == 0)
		{
			currentBatch.Shader = effect;
		}
		else if (currentBatch.Shader != effect)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.Shader = effect;
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

	public void PushLayer(int delta)
	{
		layerStack.Push(currentBatch.Layer);
		SetLayer(currentBatch.Layer + delta);
	}

	public void PopLayer()
	{
		SetLayer(layerStack.Pop());
	}

	public void PushShader(Shader effect)
	{
		effectStack.Push(currentBatch.Shader);
		SetShader(effect);
	}

	public void PopShader()
	{
		SetShader(effectStack.Pop());
	}

	public void PushSampler(TextureSampler state)
	{
		samplerStack.Push(currentBatch.Sampler);
		SetSampler(state);
	}

	public void PopSampler()
	{
		SetSampler(samplerStack.Pop());
	}

	public void PushBlend(BlendMode blend)
	{
		blendStack.Push(currentBatch.Blend);
		SetBlend(blend);
	}

	public void PopBlend()
	{
		SetBlend(blendStack.Pop());
	}

	public void PushScissor(RectInt? scissor)
	{
		scissorStack.Push(currentBatch.Scissor);
		SetScissor(scissor);
	}

	public void PopScissor()
	{
		SetScissor(scissorStack.Pop());
	}

	public Matrix3x2 PushMatrix(in Vector2 position, in Vector2 scale, in Vector2 origin, float rotation, bool relative = true)
	{
		return PushMatrix(Transform.CreateMatrix(position, origin, scale, rotation), relative);
	}

	public Matrix3x2 PushMatrix(Transform transform, bool relative = true)
	{
		return PushMatrix(transform.Matrix, relative);
	}

	public Matrix3x2 PushMatrix(in Vector2 position, bool relative = true)
	{
		return PushMatrix(Matrix3x2.CreateTranslation(position.X, position.Y), relative);
	}

	public Matrix3x2 PushMatrix(in Matrix3x2 matrix, bool relative = true)
	{
		matrixStack.Push(MatrixStack);

		if (relative)
		{
			MatrixStack = matrix * MatrixStack;
		}
		else
		{
			MatrixStack = matrix;
		}

		return MatrixStack;
	}

	public Matrix3x2 PopMatrix()
	{
		MatrixStack = matrixStack.Pop();
		return MatrixStack;
	}

	public void PushModeNormal()
	{
		modeStack.Push(mode);
		mode = new Color(255, 0, 0, 0);
	}

	public void PushModeWash()
	{
		modeStack.Push(mode);
		mode = new Color(0, 255, 0, 0);
	}

	public void PushModeFill()
	{
		modeStack.Push(mode);
		mode = new Color(0, 0, 255, 0);
	}

	public void PushMode(Color value)
	{
		modeStack.Push(mode);
		mode = value;
	}

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

	public void QuadLines(in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d, float thickness, in Color color)
	{
		Line(a, b, thickness, color);
		Line(b, c, thickness, color);
		Line(c, d, thickness, color);
		Line(d, a, thickness, color);
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
		ExpandvertexArray(vertexCount + 4);

		unchecked
		{
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, MatrixStack);
		vertexArray[vertexCount + 0].Col = color;
		vertexArray[vertexCount + 1].Col = color;
		vertexArray[vertexCount + 2].Col = color;
		vertexArray[vertexCount + 3].Col = color;
		vertexArray[vertexCount + 0].Mode = new(0, 0, 255, 0);
		vertexArray[vertexCount + 1].Mode = new(0, 0, 255, 0);
		vertexArray[vertexCount + 2].Mode = new(0, 0, 255, 0);
		vertexArray[vertexCount + 3].Mode = new(0, 0, 255, 0);
		}

		vertexCount += 4;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 t3, in Color color)
	{
		PushQuad();
		ExpandvertexArray(vertexCount + 4);
		unchecked
		{
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, MatrixStack);
		vertexArray[vertexCount + 0].Tex = t0;
		vertexArray[vertexCount + 1].Tex = t1;
		vertexArray[vertexCount + 2].Tex = t2;
		vertexArray[vertexCount + 3].Tex = t3;
		vertexArray[vertexCount + 0].Col = color;
		vertexArray[vertexCount + 1].Col = color;
		vertexArray[vertexCount + 2].Col = color;
		vertexArray[vertexCount + 3].Col = color;
		vertexArray[vertexCount + 0].Mode = mode;
		vertexArray[vertexCount + 1].Mode = mode;
		vertexArray[vertexCount + 2].Mode = mode;
		vertexArray[vertexCount + 3].Mode = mode;
		}

		vertexCount += 4;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Color c0, in Color c1, in Color c2, in Color c3)
	{
		PushQuad();
		ExpandvertexArray(vertexCount + 4);

		unchecked
		{
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, MatrixStack);
		vertexArray[vertexCount + 0].Col = c0;
		vertexArray[vertexCount + 1].Col = c1;
		vertexArray[vertexCount + 2].Col = c2;
		vertexArray[vertexCount + 3].Col = c3;
		vertexArray[vertexCount + 0].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 1].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 2].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 3].Mode = new Color(0, 0, 255, 0);
		}

		vertexCount += 4;
	}

	public void Quad(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 v3, in Vector2 t0, in Vector2 t1, in Vector2 t2, in Vector2 t3, Color c0, Color c1, Color c2, Color c3)
	{
		PushQuad();
		ExpandvertexArray(vertexCount + 4);

		unchecked
		{

		// POS
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, MatrixStack);

		// TEX
		vertexArray[vertexCount + 0].Tex = t0;
		vertexArray[vertexCount + 1].Tex = t1;
		vertexArray[vertexCount + 2].Tex = t2;
		vertexArray[vertexCount + 3].Tex = t3;

		// COL
		vertexArray[vertexCount + 0].Col = c0;
		vertexArray[vertexCount + 1].Col = c1;
		vertexArray[vertexCount + 2].Col = c2;
		vertexArray[vertexCount + 3].Col = c3;

		// MULT
		vertexArray[vertexCount + 0].Mode = mode;
		vertexArray[vertexCount + 1].Mode = mode;
		vertexArray[vertexCount + 2].Mode = mode;
		vertexArray[vertexCount + 3].Mode = mode;

		}

		vertexCount += 4;
	}

	#endregion

	#region Triangle

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, Color color)
	{
		PushTriangle();
		ExpandvertexArray(vertexCount + 3);

		unchecked
		{
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 0].Col = color;
		vertexArray[vertexCount + 1].Col = color;
		vertexArray[vertexCount + 2].Col = color;
		vertexArray[vertexCount + 0].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 1].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 2].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 3].Mode = new Color(0, 0, 255, 0);
		}

		vertexCount += 3;
	}

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 uv0, in Vector2 uv1, in Vector2 uv2, Color color)
	{
		PushTriangle();
		ExpandvertexArray(vertexCount + 3);

		unchecked
		{
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 0].Tex = uv0;
		vertexArray[vertexCount + 1].Tex = uv1;
		vertexArray[vertexCount + 2].Tex = uv2;
		vertexArray[vertexCount + 0].Col = color;
		vertexArray[vertexCount + 1].Col = color;
		vertexArray[vertexCount + 2].Col = color;
		vertexArray[vertexCount + 0].Mode = mode;
		vertexArray[vertexCount + 1].Mode = mode;
		vertexArray[vertexCount + 2].Mode = mode;
		vertexArray[vertexCount + 3].Mode = mode;
		}

		vertexCount += 3;
	}

	public void Triangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, Color c0, Color c1, Color c2)
	{
		PushTriangle();
		ExpandvertexArray(vertexCount + 3);

		unchecked
		{
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, MatrixStack);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, MatrixStack);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, MatrixStack);
		vertexArray[vertexCount + 0].Col = c0;
		vertexArray[vertexCount + 1].Col = c1;
		vertexArray[vertexCount + 2].Col = c2;
		vertexArray[vertexCount + 0].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 1].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 2].Mode = new Color(0, 0, 255, 0);
		vertexArray[vertexCount + 3].Mode = new Color(0, 0, 255, 0);
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

	#endregion

	#region Rounded Rect

	public void RoundedRect(float x, float y, float width, float height, float r0, float r1, float r2, float r3, Color color)
	{
		RoundedRect(new Rect(x, y, width, height), r0, r1, r2, r3, color);
	}

	public void RoundedRect(float x, float y, float width, float height, float radius, Color color)
	{
		RoundedRect(new Rect(x, y, width, height), radius, radius, radius, radius, color);
	}

	public void RoundedRect(in Rect rect, float radius, Color color)
	{
		RoundedRect(rect, radius, radius, radius, radius, color);
	}

	public void RoundedRect(in Rect rect, float r0, float r1, float r2, float r3, Color color)
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

			unchecked
			{

			// set tris
			{
				while (indexCount + 30 >= indexArray.Length)
					Array.Resize(ref indexArray, indexArray.Length * 2);

				// top quad
				{
					indexArray[indexCount + 00] = vertexCount + 00; // r0b
					indexArray[indexCount + 01] = vertexCount + 03; // r1a
					indexArray[indexCount + 02] = vertexCount + 05; // r1d

					indexArray[indexCount + 03] = vertexCount + 00; // r0b
					indexArray[indexCount + 04] = vertexCount + 05; // r1d
					indexArray[indexCount + 05] = vertexCount + 01; // r0c
				}

				// left quad
				{
					indexArray[indexCount + 06] = vertexCount + 02; // r0d
					indexArray[indexCount + 07] = vertexCount + 01; // r0c
					indexArray[indexCount + 08] = vertexCount + 10; // r3b

					indexArray[indexCount + 09] = vertexCount + 02; // r0d
					indexArray[indexCount + 10] = vertexCount + 10; // r3b
					indexArray[indexCount + 11] = vertexCount + 09; // r3a
				}

				// right quad
				{
					indexArray[indexCount + 12] = vertexCount + 05; // r1d
					indexArray[indexCount + 13] = vertexCount + 04; // r1c
					indexArray[indexCount + 14] = vertexCount + 07; // r2b

					indexArray[indexCount + 15] = vertexCount + 05; // r1d
					indexArray[indexCount + 16] = vertexCount + 07; // r2b
					indexArray[indexCount + 17] = vertexCount + 06; // r2a
				}

				// bottom quad
				{
					indexArray[indexCount + 18] = vertexCount + 10; // r3b
					indexArray[indexCount + 19] = vertexCount + 06; // r2a
					indexArray[indexCount + 20] = vertexCount + 08; // r2d

					indexArray[indexCount + 21] = vertexCount + 10; // r3b
					indexArray[indexCount + 22] = vertexCount + 08; // r2d
					indexArray[indexCount + 23] = vertexCount + 11; // r3c
				}

				// center quad
				{
					indexArray[indexCount + 24] = vertexCount + 01; // r0c
					indexArray[indexCount + 25] = vertexCount + 05; // r1d
					indexArray[indexCount + 26] = vertexCount + 06; // r2a

					indexArray[indexCount + 27] = vertexCount + 01; // r0c
					indexArray[indexCount + 28] = vertexCount + 06; // r2a
					indexArray[indexCount + 29] = vertexCount + 10; // r3b
				}

				indexCount += 30;
				currentBatch.Elements += 10;
				dirty = true;
			}

			// set verts
			{
				ExpandvertexArray(vertexCount + 12);

				Array.Fill(vertexArray, new Vertex(Vector2.Zero, Vector2.Zero, color, new Color(0, 0, 255, 0)), vertexCount, 12);

				vertexArray[vertexCount + 00].Pos = Vector2.Transform(r0_tr, MatrixStack); // 0
				vertexArray[vertexCount + 01].Pos = Vector2.Transform(r0_br, MatrixStack); // 1
				vertexArray[vertexCount + 02].Pos = Vector2.Transform(r0_bl, MatrixStack); // 2

				vertexArray[vertexCount + 03].Pos = Vector2.Transform(r1_tl, MatrixStack); // 3
				vertexArray[vertexCount + 04].Pos = Vector2.Transform(r1_br, MatrixStack); // 4
				vertexArray[vertexCount + 05].Pos = Vector2.Transform(r1_bl, MatrixStack); // 5

				vertexArray[vertexCount + 06].Pos = Vector2.Transform(r2_tl, MatrixStack); // 6
				vertexArray[vertexCount + 07].Pos = Vector2.Transform(r2_tr, MatrixStack); // 7
				vertexArray[vertexCount + 08].Pos = Vector2.Transform(r2_bl, MatrixStack); // 8

				vertexArray[vertexCount + 09].Pos = Vector2.Transform(r3_tl, MatrixStack); // 9
				vertexArray[vertexCount + 10].Pos = Vector2.Transform(r3_tr, MatrixStack); // 10
				vertexArray[vertexCount + 11].Pos = Vector2.Transform(r3_br, MatrixStack); // 11

				vertexCount += 12;
			}

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

	#endregion

	#region Dashed Rect

	public void RectDashed(Rect rect, float thickness, in Color color, float dashLength, float dashOffset)
	{
		rect = rect.Inflate(-thickness / 2);
		LineDashed(rect.TopLeft, rect.TopRight, thickness, color, dashLength, dashOffset);
		LineDashed(rect.TopRight, rect.BottomRight, thickness, color, dashLength, dashOffset);
		LineDashed(rect.BottomRight, rect.BottomLeft, thickness, color, dashLength, dashOffset);
		LineDashed(rect.BottomLeft, rect.TopLeft, thickness, color, dashLength, dashOffset);
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
		var last = Calc.AngleToVector(0, radius);

		for (int i = 1; i <= steps; i++)
		{
			var next = Calc.AngleToVector((i / (float)steps) * Calc.TAU, radius);
			Line(center + last, center + next, thickness, color);
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

	#endregion

	#region Rect Line

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
		var was = MatrixStack;

		MatrixStack = Transform.CreateMatrix(position, origin, scale, rotation) * MatrixStack;

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

		MatrixStack = was;
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
		var was = MatrixStack;

		MatrixStack = Transform.CreateMatrix(position, origin, scale, rotation) * MatrixStack;

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

		MatrixStack = was;
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
		var was = MatrixStack;

		MatrixStack = Transform.CreateMatrix(position, origin, scale, rotation) * MatrixStack;

		SetTexture(subtex.Texture);
		Quad(
			subtex.DrawCoords0, subtex.DrawCoords1, subtex.DrawCoords2, subtex.DrawCoords3,
			subtex.TexCoords0, subtex.TexCoords1, subtex.TexCoords2, subtex.TexCoords3,
			color);

		MatrixStack = was;
	}

	public void Image(in Subtexture subtex, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color c0, Color c1, Color c2, Color c3)
	{
		var was = MatrixStack;

		MatrixStack = Transform.CreateMatrix(position, origin, scale, rotation) * MatrixStack;

		SetTexture(subtex.Texture);
		Quad(
			subtex.DrawCoords0, subtex.DrawCoords1, subtex.DrawCoords2, subtex.DrawCoords3,
			subtex.TexCoords0, subtex.TexCoords1, subtex.TexCoords2, subtex.TexCoords3,
			c0, c1, c2, c3);

		MatrixStack = was;
	}

	public void Image(in Subtexture subtex, in Rect clip, in Vector2 position, in Vector2 origin, in Vector2 scale, float rotation, Color color)
	{
		var (source, frame) = subtex.GetClip(clip);
		var tex = subtex.Texture;
		var was = MatrixStack;

		MatrixStack = Transform.CreateMatrix(position, origin, scale, rotation) * MatrixStack;

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

		MatrixStack = was;
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

	#region Nine-Slice & Three-Slice

	public void ThreeSlice(in Subtexture texture, in Rect bounds, float inset, Color color)
	{
		ThreeSlice(texture, bounds, inset, 1.0f, color);
	}

	public void ThreeSlice(in Subtexture texture, Rect rect, float inset, float textureScale, Color color)
	{
		rect = rect.Inflate(inset);
		rect.Width /= textureScale;
		rect.Height /= textureScale;

		PushMatrix(rect.Position, Vector2.One * textureScale, Vector2.Zero, 0);

		// split into 3 subtextures
		var cell = new Vector2(texture.Frame.Width / 3, texture.Frame.Height);
		var a = texture.GetClipSubtexture(new Rect(0, 0, cell.X, cell.Y));
		var b = texture.GetClipSubtexture(new Rect(cell.X, 0, cell.X, cell.Y));
		var c = texture.GetClipSubtexture(new Rect(cell.X * 2, 0, cell.X, cell.Y));

		// figure out scale based on vertical size
		var scale = rect.Height / c.Height;
		var fill = rect.Width - cell.X * 2 * scale;

		// draw images
		Image(a, new Vector2(0, 0), Vector2.Zero, Vector2.One * scale, 0, color);
		Image(b, new Vector2(0 + cell.X * scale, 0), Vector2.Zero, new Vector2(fill / cell.X, scale), 0, color);
		Image(c, new Vector2(0 + rect.Width - cell.X * scale, 0), Vector2.Zero, Vector2.One * scale, 0, color);

		PopMatrix();
	}

	public void NineSlice(in Subtexture texture, in Rect bounds, float inset, Color color, bool stretch = false)
	{
		NineSlice(texture, bounds, inset, 1.0f, color, stretch);
	}

	public void NineSlice(in Subtexture texture, Rect rect, float inset, float textureScale, Color color, bool stretch = false)
	{
		static void draw_edge(Batcher batch, in Subtexture tex, in Rect area, Color color, bool stretch)
		{
			var size = new Vector2(tex.Width, tex.Height);

			if (tex.Texture == null || size.X <= 0 || size.Y <= 0)
				return;

			// single, stretched
			if (stretch)
			{
				batch.Image(tex, new Vector2(area.X, area.Y), Vector2.Zero, new Vector2(area.Width / size.X, area.Height / size.Y), 0, color);
			}
			// centered, tiled
			else
			{
				int columns = (int)MathF.Ceiling(area.Width / size.X);
				int rows = (int)MathF.Ceiling(area.Height / size.Y);

				// keeping grid an odd number forces center-alignment
				if (columns % 2 == 0) columns++;
				if (rows % 2 == 0) rows++;

				// get area
				float left = area.CenterX - columns * 0.5f * size.X;
				float right = area.CenterX + columns * 0.5f * size.X;
				float top = area.CenterY - rows * 0.5f * size.Y;
				float bottom = area.CenterY + rows * 0.5f * size.Y;

				for (float x = left; x < right; x += size.X)
				{
					for (float y = top; y < bottom; y += size.Y)
					{
						var cell = new Rect(x, y, size.X, size.Y);

						// crop to visible area
						var visible = cell.OverlapRect(area);
						if (visible.Width <= 0 || visible.Height <= 0)
							continue;

						// draw segment
						var offset = visible.TopLeft - cell.TopLeft;
						var crop = new Rect(offset.X, offset.Y, visible.Width, visible.Height);
						batch.Image(tex, crop, new Vector2(x, y) + offset, Vector2.Zero, Vector2.One, 0, color);
					}
				}
			}
		};

		rect = rect.Inflate(inset);

		var trim = new Vector2(texture.Frame.Width / 3, texture.Frame.Height / 3);
		var sw = texture.Width;
		var sh = texture.Height;
		var width = (rect.Width / textureScale);
		var height = (rect.Height / textureScale);

		// crop 9-slice into the rectangle we're drawing in ...
		if (trim.X > width / 2)
			trim.X = (width / 2);
		if (trim.Y > height / 2)
			trim.Y = (height / 2);

		// get 9-slice textures
		StackList16<Subtexture> cells = new();
		cells.Resize(9);
		cells[0] = texture.GetClipSubtexture(new Rect(0, 0, trim.X, trim.Y));
		cells[1] = texture.GetClipSubtexture(new Rect((sw - trim.X) / 2, 0, trim.X, trim.Y));
		cells[2] = texture.GetClipSubtexture(new Rect(sw - trim.X, 0, trim.X, trim.Y));
		cells[3] = texture.GetClipSubtexture(new Rect(0, (sh - trim.Y) / 2, trim.X, trim.Y));
		cells[4] = texture.GetClipSubtexture(new Rect((sw - trim.X) / 2, (sh - trim.Y) / 2, trim.X, trim.Y));
		cells[5] = texture.GetClipSubtexture(new Rect(sw - trim.X, (sh - trim.Y) / 2, trim.X, trim.Y));
		cells[6] = texture.GetClipSubtexture(new Rect(0, sh - trim.Y, trim.X, trim.Y));
		cells[7] = texture.GetClipSubtexture(new Rect((sw - trim.X) / 2, sh - trim.Y, trim.X, trim.Y));
		cells[8] = texture.GetClipSubtexture(new Rect(sw - trim.X, sh - trim.Y, trim.X, trim.Y));

		// figure out filled space
		float fill_x = width - trim.X * 2;
		float fill_y = height - trim.Y * 2;

		PushMatrix(rect.TopLeft, Vector2.One * textureScale, Vector2.Zero, 0.0f);
		{
			// top left
			Image(cells[0], new Vector2(0, 0), Vector2.Zero, Vector2.One, 0, color);

			// top-center
			if (fill_x > 0)
				draw_edge(this, cells[1], new Rect(trim.X, 0, fill_x, trim.Y), color, stretch);

			// top right
			Image(cells[2], new Vector2(width - trim.X, 0), Vector2.Zero, Vector2.One, 0, color);

			if (fill_y > 0)
			{
				// left
				draw_edge(this, cells[3], new Rect(0, trim.Y, trim.X, fill_y), color, stretch);

				// center
				if (fill_x > 0)
					draw_edge(this, cells[4], new Rect(trim.X, trim.Y, fill_x, fill_y), color, stretch);

				// right
				draw_edge(this, cells[5], new Rect(width - trim.X, trim.Y, trim.X, fill_y), color, stretch);
			}

			// bottom-left
			Image(cells[6], new Vector2(0, height - trim.Y), Vector2.Zero, Vector2.One, 0, color);

			// bottom-center
			if (fill_x > 0)
				draw_edge(this, cells[7], new Rect(trim.X, height - trim.Y, fill_x, trim.Y), color, stretch);

			// bottom-right
			Image(cells[8], new Vector2(width - trim.X, height - trim.Y), Vector2.Zero, Vector2.One, 0, color);
		}
		PopMatrix();
	}

	#endregion

	#region Copy Arrays

	/// <summary>
	/// Copies the contents of a Vertex and Index array to this Batcher
	/// </summary>
	public void CopyArray(ReadOnlySpan<Vertex> vertexBuffer, ReadOnlySpan<int> indexBuffer)
	{
		// copy vertexArray over
		ExpandvertexArray(vertexCount + vertexBuffer.Length);
		vertexBuffer.CopyTo(vertexArray.AsSpan().Slice(vertexCount));

		// copy indexArray over
		while (indexCount + indexBuffer.Length >= indexArray.Length)
			Array.Resize(ref indexArray, indexArray.Length * 2);

		unchecked
		{
			for (int i = 0, n = indexCount; i < indexBuffer.Length; i++, n++)
				indexArray[n] = vertexCount + indexBuffer[i];
		}

		// increment
		vertexCount += vertexBuffer.Length;
		indexCount += indexBuffer.Length;
		currentBatch.Elements += (vertexBuffer.Length / 3);
		dirty = true;
	}

	#endregion

	#region Misc.

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
		while (indexCount + 3 >= indexArray.Length)
			Array.Resize(ref indexArray, indexArray.Length * 2);

		unchecked
		{
		indexArray[indexCount + 0] = vertexCount + 0;
		indexArray[indexCount + 1] = vertexCount + 1;
		indexArray[indexCount + 2] = vertexCount + 2;
		}

		indexCount += 3;
		currentBatch.Elements++;
		dirty = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void PushQuad()
	{
		int index = indexCount;
		int vert = vertexCount;

		while (index + 6 >= indexArray.Length)
			Array.Resize(ref indexArray, indexArray.Length * 2);

		unchecked
		{
		indexArray[index + 0] = vert + 0;
		indexArray[index + 1] = vert + 1;
		indexArray[index + 2] = vert + 2;
		indexArray[index + 3] = vert + 0;
		indexArray[index + 4] = vert + 2;
		indexArray[index + 5] = vert + 3;
		}

		indexCount += 6;
		currentBatch.Elements += 2;
		dirty = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ExpandvertexArray(int index)
	{
		while (index >= vertexArray.Length)
		{
			Array.Resize(ref vertexArray, vertexArray.Length * 2);
		}
	}

	#endregion
}
