using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Foster.Framework;

internal static class Platform
{
	public const string DLL = "FosterPlatform";

	[Flags]
	public enum FosterFlags
	{
		Fullscreen    = 1 << 0,
		Vsync         = 1 << 1,
		Resizable     = 1 << 2,
		MouseVisible  = 1 << 3,
	}

	public enum FosterEventType : int
	{
		None,
		ExitRequested,
		KeyboardInput,
		KeyboardKey,
		MouseButton,
		MouseMove,
		MouseWheel,
		ControllerConnect,
		ControllerDisconnect,
		ControllerButton,
		ControllerAxis
	}

	// [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	// public delegate void FosterLogFn(IntPtr msg, int type);
	
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void FosterWriteFn(IntPtr context, IntPtr data, int size);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterDesc
	{
		public IntPtr windowTitle;
		public IntPtr applicationName;
		public int width;
		public int height;
		public Renderers renderer;
		public FosterFlags flags;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct FosterEvent
	{
		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct KeyboardEvent
		{
			public FosterEventType EventType;
			public fixed byte Text[32];
			public int Key;
			public byte KeyPressed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MouseEvent
		{
			public FosterEventType EventType;
			public float X;
			public float Y;
			public int Button;
			public byte ButtonPressed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ControllerEvent
		{
			public FosterEventType EventType;
			public int Index;
			public IntPtr Name;
			public int ButtonCount;
			public int AxisCount;
			public byte IsGamepad;
			public GamepadTypes GamepadType;
			public ushort Vendor;
			public ushort Product;
			public ushort Version;
			public int Button;
			public byte ButtonPressed;
			public int Axis;
			public float AxisValue;
		}

		[FieldOffset(0)] public FosterEventType EventType;
		[FieldOffset(0)] public KeyboardEvent Keyboard;
		[FieldOffset(0)] public MouseEvent Mouse;
		[FieldOffset(0)] public ControllerEvent Controller;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterVertexElement
	{
		public int index;
		public VertexType type;
		public int normalized;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterVertexFormat
	{
		public IntPtr elements;
		public int elementCount;
		public int stride;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterUniformInfo
	{
		public int index;
		public IntPtr name;
		public UniformType type;
		public int arrayElements;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterShaderData
	{
		public string vertex;
		public string fragment;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterRect
	{
		public int x, y, w, h;

		public FosterRect(int x, int y, int w, int h) { this.x = x; this.y = y; this.w = w; this.h = h; }
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterDrawCommand
	{
		public IntPtr target;
		public IntPtr mesh;
		public IntPtr shader;
		public int hasViewport;
		public int hasScissor;
		public FosterRect viewport;
		public FosterRect scissor;
		public int indexStart;
		public int indexCount;
		public int instanceCount;
		public DepthCompare compare;
		public int depthMask;
		public CullMode cull;
		public BlendMode blend;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
	public struct FosterClearCommand
	{
		public IntPtr target;
		public FosterRect clip;
		public Color color;
		public float depth;
		public int stencil;
		public ClearMask mask;
	}

	public static unsafe string ParseUTF8(IntPtr s)
	{
		if (s == IntPtr.Zero)
			return string.Empty;

		byte* ptr = (byte*) s;
		while (*ptr != 0)
			ptr++;
		return Encoding.UTF8.GetString((byte*)s, (int)(ptr - (byte*)s));
	}

	public static unsafe IntPtr ToUTF8(in string str)
	{
		var count = Encoding.UTF8.GetByteCount(str) + 1;
		var ptr = Marshal.AllocHGlobal(count);
		var span = new Span<byte>((byte*)ptr.ToPointer(), count);
		Encoding.UTF8.GetBytes(str, span);
		span[^1] = 0;
		return ptr;
	}

	public static void FreeUTF8(IntPtr ptr)
	{
		Marshal.FreeHGlobal(ptr);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void HandleLog(IntPtr msg, int type)
	{
		switch (type)
		{
			case 0: Log.Info(msg); break;
			case 1: Log.Warning(msg); break;
			case 2: Log.Error(msg); break;
			default: Log.Info(msg); break;
		}
	}

	static unsafe Platform()
	{
		// This is done in this way so that the delegate is compatible with WASM/Emscripten,
		// which do not accept normal delegates (only unmanaged function pointer ones like this) 
		delegate* unmanaged[Cdecl]<IntPtr, int, void> fn = &HandleLog;
		FosterSetLogCallback((IntPtr)fn, 0);
	}

	[DllImport(DLL)]
	public static extern void FosterSetLogCallback(IntPtr logFn, int level);
	[DllImport(DLL)]
	public static extern void FosterStartup(FosterDesc desc);
	[DllImport(DLL)]
	public static extern void FosterBeginFrame();
	[DllImport(DLL)]
	public static extern byte FosterPollEvents(out FosterEvent fosterEvent);
	[DllImport(DLL)]
	public static extern void FosterEndFrame();
	[DllImport(DLL)]
	public static extern void FosterShutdown();
	[DllImport(DLL)]
	public static extern byte FosterIsRunning();
	[DllImport(DLL)]
	public static extern void FosterSetTitle(string title);
	[DllImport(DLL)]
	public static extern void FosterSetSize(int width, int height);
	[DllImport(DLL)]
	public static extern void FosterGetSize(out int width, out int height);
	[DllImport(DLL)]
	public static extern void FosterGetSizeInPixels(out int width, out int height);
	[DllImport(DLL)]
	public static extern void FosterGetDisplaySize(out int width, out int height);
	[DllImport(DLL)]
	public static extern void FosterSetFlags(FosterFlags flags);
	[DllImport(DLL)]
	public static extern void FosterSetCentered();
	[DllImport(DLL)]
	public static extern IntPtr FosterGetUserPath();
	[DllImport(DLL)]
	public static extern void FosterSetClipboard(string ptr);
	[DllImport(DLL)]
	public static extern IntPtr FosterGetClipboard();
	[DllImport(DLL)]
	public static extern byte FosterGetFocused();
	[DllImport(DLL)]
	public static extern IntPtr FosterImageLoad(IntPtr memory, int length, out int w, out int h);
	[DllImport(DLL)]
	public static extern void FosterImageFree(IntPtr data);
	[DllImport(DLL)]
	public static extern byte FosterImageWrite(FosterWriteFn func, IntPtr context, ImageWriteFormat format, int w, int h, IntPtr data);
	[DllImport(DLL)]
	public static extern IntPtr FosterFontInit(IntPtr data, int length);
	[DllImport(DLL)]
	public static extern void FosterFontGetMetrics(IntPtr font, out int ascent, out int descent, out int linegap);
	[DllImport(DLL)]
	public static extern int FosterFontGetGlyphIndex(IntPtr font, int codepoint);
	[DllImport(DLL)]
	public static extern float FosterFontGetScale(IntPtr font, float size);
	[DllImport(DLL)]
	public static extern float FosterFontGetKerning(IntPtr font, int glyph1, int glyph2, float scale);
	[DllImport(DLL)]
	public static extern void FosterFontGetCharacter(IntPtr font, int glyph, float scale, out int width, out int height, out float advance, out float offsetX, out float offsetY, out int visible);
	[DllImport(DLL)]
	public static extern void FosterFontGetPixels(IntPtr font, IntPtr dest, int glyph, int width, int height, float scale);
	[DllImport(DLL)]
	public static extern void FosterFontFree(IntPtr font);
	[DllImport(DLL)]
	public static extern Renderers FosterGetRenderer();
	[DllImport(DLL)]
	public static extern IntPtr FosterTextureCreate(int width, int height, TextureFormat format);
	[DllImport(DLL)]
	public static extern void FosterTextureSetData(IntPtr texture, IntPtr data, int length);
	[DllImport(DLL)]
	public static extern void FosterTextureGetData(IntPtr texture, IntPtr data, int length);
	[DllImport(DLL)]
	public static extern void FosterTextureDestroy(IntPtr texture);
	[DllImport(DLL)]
	public static extern IntPtr FosterTargetCreate(int width, int height, TextureFormat[] formats, int formatCount);
	[DllImport(DLL)]
	public static extern IntPtr FosterTargetGetAttachment(IntPtr target, int index);
	[DllImport(DLL)]
	public static extern void FosterTargetDestroy(IntPtr target);
	[DllImport(DLL)]
	public static extern IntPtr FosterShaderCreate(ref FosterShaderData data);
	[DllImport(DLL)]
	public static extern unsafe void FosterShaderGetUniforms(IntPtr shader, FosterUniformInfo* output, out int count, int max);
	[DllImport(DLL)]
	public static extern unsafe void FosterShaderSetUniform(IntPtr shader, int index, float* values);
	[DllImport(DLL)]
	public static extern unsafe void FosterShaderSetTexture(IntPtr shader, int index, IntPtr* values);
	[DllImport(DLL)]
	public static extern unsafe void FosterShaderSetSampler(IntPtr shader, int index, TextureSampler* values);
	[DllImport(DLL)]
	public static extern void FosterShaderDestroy(IntPtr shader);
	[DllImport(DLL)]
	public static extern IntPtr FosterMeshCreate();
	[DllImport(DLL)]
	public static extern void FosterMeshSetVertexFormat(IntPtr mesh, ref FosterVertexFormat format);
	[DllImport(DLL)]
	public static extern void FosterMeshSetVertexData(IntPtr mesh, IntPtr data, int dataSize, int dataDestOffset);
	[DllImport(DLL)]
	public static extern void FosterMeshSetIndexFormat(IntPtr mesh, IndexFormat format);
	[DllImport(DLL)]
	public static extern void FosterMeshSetIndexData(IntPtr mesh, IntPtr data, int dataSize, int dataDestOffset);
	[DllImport(DLL)]
	public static extern void FosterMeshDestroy(IntPtr mesh);
	[DllImport(DLL)]
	public static extern void FosterDraw(ref FosterDrawCommand command);
	[DllImport(DLL)]
	public static extern void FosterClear(ref FosterClearCommand command);

	// Non-Foster Calls:
	
	[DllImport(DLL, CharSet = CharSet.Ansi)]
	public static extern int SDL_GameControllerAddMapping(string mappingString);
	
	// [DllImport(DLL)]
	// public static extern void emscripten_set_main_loop(IntPtr action, int fps, bool simulateInfiniteLoop);
}
