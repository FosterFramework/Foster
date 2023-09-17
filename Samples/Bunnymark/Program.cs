using Foster.Framework;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Bunnymark;

class Program
{
    public static void Main()
    {
        App.Register<Game>();
        App.Run("Bunnymark", 1280, 720);
    }
}

class Game : Module
{
    private const int MaxBunnies = 1_000_000;
    private const int AddRemoveAmount = 5_000;
    private const int DrawBatchSize = 32768;

    private Bunny[] bunnies = new Bunny[MaxBunnies];
    private int bunniesCount = 0;
    private Rng rng = new(1337);
    private Mesh mesh = new();
    private Vertex[] vertexArray = new Vertex[DrawBatchSize * 4];
    private Texture texture = null!;
    private Shader shader = null!;
    private Batcher batcher = new();
    private SpriteFont font = null!;
    private FrameCounter frameCounter = new();

    public override void Startup()
    {
        App.VSync = true;
        Time.FixedStep = false;
        App.Resizable = false;

        using var image = new Image(@"Assets\wabbit_alpha.png");
        image.Premultiply();
        texture = new Texture(image);

        font = new SpriteFont(@"Assets\monogram.ttf", 32);

        shader = new Shader(ShaderDefinitions[Graphics.Renderer]);

        // We only need to initialize indices once, since we're only drawing quads
        var indexArray = new int[DrawBatchSize * 6];
        var vertexCount = 0;

        for (int i = 0; i < indexArray.Length; i += 6)
        {
            indexArray[i + 0] = vertexCount + 0;
            indexArray[i + 1] = vertexCount + 1;
            indexArray[i + 2] = vertexCount + 2;
            indexArray[i + 3] = vertexCount + 0;
            indexArray[i + 4] = vertexCount + 2;
            indexArray[i + 5] = vertexCount + 3;
            vertexCount += 4;
        }

        mesh.SetIndices<int>(indexArray);

        // Texture coordinates will not change, so we can initialize those
        for (int i = 0; i < DrawBatchSize * 4; i += 4)
        {
            vertexArray[i].Tex = new(0, 0);
            vertexArray[i + 1].Tex = new(1, 0);
            vertexArray[i + 2].Tex = new(1, 1);
            vertexArray[i + 3].Tex = new(0, 1);
        }
    }

    public override void Update()
    {
        // Spawn bunnies
        if (Input.Mouse.LeftDown)
        {
            for (int i = 0; i < AddRemoveAmount; i++)
            {
                if (bunniesCount < MaxBunnies)
                {
                    bunnies[bunniesCount].Position = Input.Mouse.Position;
                    bunnies[bunniesCount].Speed.X = rng.Float(-250, 250) / 60.0f;
                    bunnies[bunniesCount].Speed.Y = rng.Float(-250, 250) / 60.0f;
                    bunnies[bunniesCount].Color = new Color(
                                rng.U8(50, 240),
                                rng.U8(80, 240),
                                rng.U8(100, 240),
                                255
                            );
                    bunniesCount++;
                }
            }

        }
        // Remove bunnies
        else if (Input.Mouse.RightDown)
        {
            bunniesCount = Math.Max(0, bunniesCount - AddRemoveAmount);
        }

        // Update bunnies
        Vector2 halfSize = ((Vector2)texture.Size) / 2f;
        Vector2 screenSize = new Vector2(App.WidthInPixels, App.HeightInPixels);

        for (int i = 0; i < bunniesCount; i++)
        {
            bunnies[i].Position += bunnies[i].Speed;

            if (((bunnies[i].Position.X + halfSize.X) > screenSize.X) ||
                ((bunnies[i].Position.X + halfSize.X) < 0))
            {
                bunnies[i].Speed.X *= -1;
            }

            if (((bunnies[i].Position.Y + halfSize.Y) > screenSize.Y) ||
                ((bunnies[i].Position.Y + halfSize.Y - 40) < 0))
            {
                bunnies[i].Speed.Y *= -1;
            }
        }
    }

    public override void Render()
    {
        frameCounter.Update();

        Graphics.Clear(Color.White);

        batcher.Clear();
        batcher.Text(font, $"{bunniesCount} Bunnies : {frameCounter.FPS} FPS", new(8, -2), Color.Black);
        batcher.Render();

        // Batching/batch size is important: too low = excessive draw calls, too high = slower gpu copies
        for (int i = 0; i < bunniesCount; i += DrawBatchSize)
        {
            var count = Math.Min(bunniesCount - i, DrawBatchSize);
            if (Input.Keyboard.Down(Keys.Space))
            {
                RenderBunnyBatchCustom(i, count);
            }
            else
            {
                RenderBunnyBatchFoster(i, count);
            }
        }
    }

    /// <summary>
    /// Plain Foster.
    /// So simple, so fast.
    /// </summary>
    private void RenderBunnyBatchFoster(int from, int count)
    {
        batcher.Clear();
        for (int i = 0; i < count; i++)
        {
            var bunny = bunnies[i + from];
            batcher.Image(texture, bunny.Position, bunny.Color);
        }
        batcher.Render();
    }

    /// <summary>
    /// A tailor made solution for shoving bunnies into a gpu.
    /// Goes down a rabbit hole for a few extra frames:
    /// - Smaller vertex format
    /// - Simplified shader logic
    /// - Initialize indices only on startup
    /// - Initialize vertex texture coords on startup
    /// - One time shader uniform set per frame
    /// - A lot of inlining (same result could be achieved with AggressiveInlining)
    /// </summary>
    private void RenderBunnyBatchCustom(int from, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var bunny = bunnies[i + from];
            var v = i * 4;
            vertexArray[v].Col = bunny.Color;
            vertexArray[v + 1].Col = bunny.Color;
            vertexArray[v + 2].Col = bunny.Color;
            vertexArray[v + 3].Col = bunny.Color;
            vertexArray[v].Pos = bunny.Position;
            vertexArray[v + 1].Pos = bunny.Position + new Vector2(texture.Width, 0);
            vertexArray[v + 2].Pos = bunny.Position + new Vector2(texture.Width, texture.Height);
            vertexArray[v + 3].Pos = bunny.Position + new Vector2(0, texture.Height);
        }

        mesh.SetVertices<Vertex>(vertexArray.AsSpan(0, count * 4));

        if (from == 0)
        {
            var matrix = Matrix4x4.CreateOrthographicOffCenter(0, App.WidthInPixels, App.HeightInPixels, 0, 0, float.MaxValue);
            shader["u_matrix"].Set(matrix);
            shader["u_texture"].Set(texture);
            shader["u_texture_sampler"].Set(new TextureSampler());
        }

        DrawCommand command = new(null, mesh, shader)
        {
            BlendMode = BlendMode.Premultiply,
            MeshIndexStart = 0,
            MeshIndexCount = count * 6
        };

        command.Submit();
    }

    public struct Bunny
    {
        public Vector2 Position;
        public Vector2 Speed;
        public Color Color;
    }

    private static readonly VertexFormat VertexFormat = VertexFormat.Create<Vertex>(
        new VertexFormat.Element(0, VertexType.Float2, false),
        new VertexFormat.Element(1, VertexType.Float2, false),
        new VertexFormat.Element(2, VertexType.UByte4, true)
    );

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex : IVertex
    {
        public Vector2 Pos;
        public Vector2 Tex;
        public Color Col;

        public readonly VertexFormat Format => VertexFormat;
    }

    private static Dictionary<Renderers, ShaderCreateInfo> ShaderDefinitions = new()
    {
        [Renderers.OpenGL] = new()
        {
            VertexShader =
                "#version 330\n" +
                "uniform mat4 u_matrix;\n" +
                "layout(location=0) in vec2 a_position;\n" +
                "layout(location=1) in vec2 a_tex;\n" +
                "layout(location=2) in vec4 a_color;\n" +
                "out vec2 v_tex;\n" +
                "out vec4 v_col;\n" +
                "void main(void)\n" +
                "{\n" +
                "	gl_Position = u_matrix * vec4(a_position.xy, 0, 1);\n" +
                "	v_tex = a_tex;\n" +
                "	v_col = a_color;\n" +
                "}",
            FragmentShader =
                "#version 330\n" +
                "uniform sampler2D u_texture;\n" +
                "in vec2 v_tex;\n" +
                "in vec4 v_col;\n" +
                "out vec4 o_color;\n" +
                "void main(void)\n" +
                "{\n" +
                "	vec4 color = texture(u_texture, v_tex);\n" +
                "	o_color = color * v_col;\n" +
                "}"
        }
    };
}

/// <summary>
/// Simple utility to count frames in last second
/// </summary>
public class FrameCounter
{
    public int FPS = 0;
    public int Frames = 0;
    public Stopwatch sw = Stopwatch.StartNew();

    public void Update()
    {
        Frames++;
        var elapsed = sw.Elapsed.TotalSeconds;
        if (elapsed > 1)
        {
            sw.Restart();
            FPS = Frames;
            Frames = 0;
        }
    }
}