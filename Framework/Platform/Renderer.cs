using System.Diagnostics;

using static Foster.Framework.SDL3;

namespace Foster.Framework;

internal static partial class Renderer
{
	private struct NextRenderPass
	{
		public StackList4<nint> ColorTargets;
		public nint DepthStencilTarget;
		public Color? ClearColor;
	}

	private static nint renderCmdBuf;
	private static nint uploadCmdBuf;
	private static nint swapchainTexture;
	private static nint renderPass;
	private static nint copyPass;
	private static NextRenderPass nextRenderPass;

	public static void Startup()
	{
		AcquireCommandBuffers();
	}

	public static void Shutdown()
	{
		Flush(true);
	}

	private static unsafe void AcquireCommandBuffers()
	{
		uint w, h;
		renderCmdBuf = SDL_AcquireGPUCommandBuffer(Platform.Device);
		uploadCmdBuf = SDL_AcquireGPUCommandBuffer(Platform.Device);
		swapchainTexture = SDL_AcquireGPUSwapchainTexture(renderCmdBuf, Platform.Window, &w, &h);
	}

	private static unsafe void Flush(bool wait)
	{
		CopyPassEnd();
		RenderPassEnsureClear();
		RenderPassEnd();

		if (wait)
		{
			var renderFence = SDL_SubmitGPUCommandBufferAndAcquireFence(renderCmdBuf);
			var uploadFence = SDL_SubmitGPUCommandBufferAndAcquireFence(uploadCmdBuf);
			var fences = stackalloc nint[] { renderFence, uploadFence };
			SDL_WaitForGPUFences(Platform.Device, 1, fences, 2);
			SDL_ReleaseGPUFence(Platform.Device, renderFence);
			SDL_ReleaseGPUFence(Platform.Device, uploadFence);
		}
		else
		{
			SDL_SubmitGPUCommandBuffer(renderCmdBuf);
			SDL_SubmitGPUCommandBuffer(uploadCmdBuf);
		}
	}

	public static void Present()
	{
		Flush(false);
		AcquireCommandBuffers();
	}

	public static void BindBackbuffer()
	{
		BindTargets([swapchainTexture], nint.Zero);
	}

	public static void BindTargets(in ReadOnlySpan<Texture> colorTargets, Texture? depthStencilTarget)
	{
		StackList4<nint> colorTargetPtrs = new();
		foreach (var it in colorTargets)
			colorTargetPtrs.Add(it.resource);
		BindTargets(colorTargetPtrs, depthStencilTarget?.resource ?? nint.Zero);
	}

	private static void BindTargets(in StackList4<nint> colorTargets, nint depthStencilTarget)
	{
		// check if the assignment is actually different
		bool changing = 
			colorTargets.Count != nextRenderPass.ColorTargets.Count ||
			depthStencilTarget != nextRenderPass.DepthStencilTarget;

		if (!changing)
		{
			for (int i = 0; i < colorTargets.Count; i++)
			{
				if (colorTargets[i] != nextRenderPass.ColorTargets[i])
				{
					changing = true;
					break;
				}
			}
		}
		
		if (!changing)
			return;

		// make sure previously bound targets were cleared if asked
		RenderPassEnsureClear();

		// set next pass targets
		nextRenderPass.ColorTargets = colorTargets;
		nextRenderPass.DepthStencilTarget = depthStencilTarget;
	}

	public static void Clear(Color color)
	{
		nextRenderPass.ClearColor = color;
	}

	private static unsafe void CopyPassBegin()
	{
		Debug.Assert(copyPass == nint.Zero);
		copyPass = SDL_BeginGPUCopyPass(uploadCmdBuf);
	}

	private static void CopyPassEnd()
	{
		if (copyPass != nint.Zero)
			SDL_EndGPUCopyPass(copyPass);
		copyPass = nint.Zero;
	}

	private static unsafe void RenderPassBegin()
	{
		Debug.Assert(renderPass == nint.Zero);

		var colorAttachments = 
			stackalloc SDL_GPUColorAttachmentInfo[nextRenderPass.ColorTargets.Count];

		for (int i = 0; i < nextRenderPass.ColorTargets.Count; i ++)
		{
			colorAttachments[i] = new()
			{
				texture = nextRenderPass.ColorTargets[i],
				clearColor = GetColor(nextRenderPass.ClearColor ?? Color.Transparent),
				loadOp = nextRenderPass.ClearColor.HasValue ? 
					SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR : 
					SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
				storeOp = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE
			};
		}

		renderPass = SDL_BeginGPURenderPass(
			renderCmdBuf,
			colorAttachments,
			(uint)nextRenderPass.ColorTargets.Count,
			null
		);

		nextRenderPass = new();
	}

	private static void RenderPassEnsureClear()
	{
		if (nextRenderPass.ClearColor.HasValue)
			RenderPassBegin();
	}

	private static void RenderPassEnd()
	{
		if (renderPass != nint.Zero)
			SDL_EndGPURenderPass(renderPass);
		renderPass = nint.Zero;
	}

	private static SDL_FColor GetColor(Color color)
	{
		var vec4 = color.ToVector4();
		return new() { r = vec4.X, g = vec4.Y, b = vec4.Z, a = vec4.W, };
	}
}