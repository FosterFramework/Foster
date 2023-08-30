using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace Foster.Framework;

public class Batcher
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

	private readonly ShaderState defaultShaderState = new();
	private readonly Stack<Matrix3x2> matrixStack = new();
	private readonly Stack<RectInt?> scissorStack = new();
	private readonly Stack<BlendMode> blendStack = new();
	private readonly Stack<TextureSampler> samplerStack = new();
	private readonly Stack<ShaderState> effectStack = new();
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

	private readonly struct ShaderState
	{
		public readonly Shader Shader;
		public readonly Shader.Uniform MatrixUniform;
		public readonly Shader.Uniform TextureUniform;
		public readonly Shader.Uniform SamplerUniform;

		public ShaderState(Shader shader, string matrixUniformName, string textureUniformName, string samplerUniformName)
		{
			Shader = shader;
			MatrixUniform = shader[matrixUniformName];
			TextureUniform = shader[textureUniformName];
			SamplerUniform = shader[samplerUniformName];
		}
	}

	private struct Batch
	{
		public int Layer;
		public ShaderState ShaderState;
		public BlendMode Blend;
		public Texture? Texture;
		public RectInt? Scissor;
		public TextureSampler Sampler;
		public int Offset;
		public int Elements;

		public Batch(ShaderState shaderState, BlendMode blend, Texture? texture, TextureSampler sampler, int offset, int elements)
		{
			Layer = 0;
			ShaderState = shaderState;
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
		DefaultShader ??= new Shader(ShaderDefaults.Batcher[App.Renderer]);
		defaultShaderState = new(DefaultShader, "u_matrix", "u_texture", "u_texture_sampler");
		Clear();
	}

	/// <summary>
	/// Clears the Batcher.
	/// </summary>
	public void Clear()
	{
		vertexCount = 0;
		indexCount = 0;
		currentBatchInsert = 0;
		currentBatch = new Batch(defaultShaderState, BlendMode.Premultiply, null, new(), 0, 0);
		mode = new Color(255, 0, 0, 0);
		batches.Clear();
		matrixStack.Clear();
		scissorStack.Clear();
		blendStack.Clear();
		effectStack.Clear();
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
		Matrix4x4 matrix = target != null
			? Matrix4x4.CreateOrthographicOffCenter(0, target.Width, target.Height, 0, 0, float.MaxValue)
			: Matrix4x4.CreateOrthographicOffCenter(0, App.WidthInPixels, App.HeightInPixels, 0, 0, float.MaxValue);
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
		Debug.Assert(target == null || !target.IsDisposed, "Target is disposed");

		if (batches.Count <= 0 && currentBatch.Elements <= 0)
			return;
		
		// upload our data if we've been modified since the last time we rendered
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
		batch.ShaderState.MatrixUniform.Set(matrix);
		batch.ShaderState.TextureUniform.Set(texture);
		batch.ShaderState.SamplerUniform.Set(batch.Sampler);

		DrawCommand command = new(target, mesh, batch.ShaderState.Shader)
		{
			Viewport = viewport,
			Scissor = trimmed,
			BlendMode = batch.Blend,
			MeshIndexStart = batch.Offset * 3,
			MeshIndexCount = batch.Elements * 3
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
	
	private void SetShader(ShaderState shaderState)
	{
		if (currentBatch.Elements == 0)
		{
			currentBatch.ShaderState = shaderState;
		}
		else if (
			currentBatch.ShaderState.Shader != shaderState.Shader ||
			currentBatch.ShaderState.MatrixUniform != shaderState.MatrixUniform ||
			currentBatch.ShaderState.TextureUniform != shaderState.TextureUniform ||
			currentBatch.ShaderState.SamplerUniform != shaderState.SamplerUniform)
		{
			batches.Insert(currentBatchInsert, currentBatch);

			currentBatch.ShaderState = shaderState;
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
	/// Pushes a Shader to draw with
	/// </summary>
	public void PushShader(Shader shader)
	{
		effectStack.Push(currentBatch.ShaderState);
		SetShader(new(shader, "u_matrix", "u_texture", "u_texture_sampler"));
	}

	/// <summary>
	/// Pushes a Shader to draw with
	/// </summary>
	public void PushShader(Shader shader, string matrixUniform, string textureUniform, string samplerUniform)
	{
		effectStack.Push(currentBatch.ShaderState);
		SetShader(new(shader, matrixUniform, textureUniform, samplerUniform));
	}

	/// <summary>
	/// Pops the current Shader
	/// </summary>
	public void PopShader()
	{
		SetShader(effectStack.Pop());
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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, Matrix);
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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, Matrix);
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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, Matrix);
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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
		vertexArray[vertexCount + 3].Pos = Vector2.Transform(v3, Matrix);

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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
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
		vertexArray[vertexCount + 0].Pos = Vector2.Transform(v0, Matrix);
		vertexArray[vertexCount + 1].Pos = Vector2.Transform(v1, Matrix);
		vertexArray[vertexCount + 2].Pos = Vector2.Transform(v2, Matrix);
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

				vertexArray[vertexCount + 00].Pos = Vector2.Transform(r0_tr, Matrix); // 0
				vertexArray[vertexCount + 01].Pos = Vector2.Transform(r0_br, Matrix); // 1
				vertexArray[vertexCount + 02].Pos = Vector2.Transform(r0_bl, Matrix); // 2

				vertexArray[vertexCount + 03].Pos = Vector2.Transform(r1_tl, Matrix); // 3
				vertexArray[vertexCount + 04].Pos = Vector2.Transform(r1_br, Matrix); // 4
				vertexArray[vertexCount + 05].Pos = Vector2.Transform(r1_bl, Matrix); // 5

				vertexArray[vertexCount + 06].Pos = Vector2.Transform(r2_tl, Matrix); // 6
				vertexArray[vertexCount + 07].Pos = Vector2.Transform(r2_tr, Matrix); // 7
				vertexArray[vertexCount + 08].Pos = Vector2.Transform(r2_bl, Matrix); // 8

				vertexArray[vertexCount + 09].Pos = Vector2.Transform(r3_tl, Matrix); // 9
				vertexArray[vertexCount + 10].Pos = Vector2.Transform(r3_tr, Matrix); // 10
				vertexArray[vertexCount + 11].Pos = Vector2.Transform(r3_br, Matrix); // 11

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
