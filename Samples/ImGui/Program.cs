using Foster.Framework;
using System.Numerics;
using ImGuiNET;

namespace FosterImGui;

class Program
{
	public static void Main()
	{
		App.Register<Editor>();
		App.Run("Hello Dear ImGui", 1920, 1080);
	}
}

class Editor : Module
{
	public override void Startup()
	{
		Renderer.Startup();
	}

	public override void Shutdown()
	{
		Renderer.Shutdown();
	}

	public override void Update()
	{
		Renderer.BeginLayout();

		ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Appearing);
		if (ImGui.Begin("Hello Foster x Dear ImGui"))
		{
			ImGui.Text("Some Foster Sprite Batching:");
			Renderer.BeginBatch(out var batch, out var bounds);

			batch.CheckeredPattern(bounds, 16, 16, Color.DarkGray, Color.Gray);
			batch.Circle(bounds.Center, 32, 16, Color.Red);

			Renderer.EndBatch();
			ImGui.End();
		}

		ImGui.ShowDemoWindow();

		Renderer.EndLayout();
	}

	public override void Render()
	{
		Graphics.Clear(Color.Black);
		Renderer.Render();
	}
}