using System.Runtime.InteropServices;
using System.Text;

namespace Foster.Framework;

internal static partial class Platform
{
	public const string DLL = "FosterPlatform";

	[Flags]
	public enum FosterFlags
	{
		Fullscreen = 1 << 0,
		Vsync = 1 << 1,
		Resizable = 1 << 2,
		MouseVisible = 1 << 3,
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
			public float deltaX;
			public float deltaY;
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
		public nint elements;
		public int elementCount;
		public int stride;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterUniformInfo
	{
		public int index;
		public nint name;
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
	public struct FosterRect(int x, int y, int w, int h)
	{
		public int X = x, Y = y, W = w, H = h;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct FosterDrawCommand
	{
		public nint target;
		public nint mesh;
		public nint shader;
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
		public nint target;
		public FosterRect clip;
		public Color color;
		public float depth;
		public int stencil;
		public ClearMask mask;
	}

	public static unsafe string ParseUTF8(nint s)
	{
		if (s == 0)
			return string.Empty;

		byte* ptr = (byte*)s;
		while (*ptr != 0)
			ptr++;
		return Encoding.UTF8.GetString((byte*)s, (int)(ptr - (byte*)s));
	}

	public static unsafe nint ToUTF8(in string str)
	{
		var count = Encoding.UTF8.GetByteCount(str) + 1;
		var ptr = Marshal.AllocHGlobal(count);
		var span = new Span<byte>((byte*)ptr.ToPointer(), count);
		Encoding.UTF8.GetBytes(str, span);
		span[^1] = 0;
		return ptr;
	}

	public static void FreeUTF8(nint ptr)
	{
		Marshal.FreeHGlobal(ptr);
	}

	[UnmanagedCallersOnly]
	private static void HandleLog(nint msg, int type)
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
		FosterSetLogCallback(&HandleLog, 0);
	}

	[LibraryImport(DLL)]
	public static unsafe partial void FosterSetLogCallback(delegate* unmanaged<nint, int, void> logFn, int level);
	[LibraryImport(DLL)]
	public static partial void FosterStartup(FosterDesc desc);
	[LibraryImport(DLL)]
	public static partial void FosterBeginFrame();
	[LibraryImport(DLL)]
	public static partial byte FosterPollEvents(out FosterEvent fosterEvent);
	[LibraryImport(DLL)]
	public static partial void FosterEndFrame();
	[LibraryImport(DLL)]
	public static partial void FosterShutdown();
	[LibraryImport(DLL)]
	public static partial byte FosterIsRunning();
	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial void FosterSetTitle(string title);
	[LibraryImport(DLL)]
	public static partial void FosterSetSize(int width, int height);
	[LibraryImport(DLL)]
	public static partial void FosterGetSize(out int width, out int height);
	[LibraryImport(DLL)]
	public static partial void FosterGetSizeInPixels(out int width, out int height);
	[LibraryImport(DLL)]
	public static partial void FosterGetDisplaySize(out int width, out int height);
	[LibraryImport(DLL)]
	public static partial void FosterSetFlags(FosterFlags flags);
	[LibraryImport(DLL)]
	public static partial void FosterSetCentered();
	[LibraryImport(DLL)]
	public static partial nint FosterGetUserPath();
	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial void FosterSetClipboard(string ptr);
	[LibraryImport(DLL)]
	public static partial nint FosterGetClipboard();
	[LibraryImport(DLL)]
	public static partial byte FosterGetFocused();
	[LibraryImport(DLL)]
	public static unsafe partial nint FosterImageLoad(void* memory, int length, out int w, out int h);
	[LibraryImport(DLL)]
	public static partial void FosterImageFree(nint data);
	[LibraryImport(DLL)]
	public static unsafe partial byte FosterImageWrite(delegate* unmanaged<nint, nint, int, void> func, IntPtr context, ImageWriteFormat format, int w, int h, IntPtr data);
	[LibraryImport(DLL)]
	public static partial nint FosterFontInit(nint data, int length);
	[LibraryImport(DLL)]
	public static partial void FosterFontGetMetrics(nint font, out int ascent, out int descent, out int linegap);
	[LibraryImport(DLL)]
	public static partial int FosterFontGetGlyphIndex(nint font, int codepoint);
	[LibraryImport(DLL)]
	public static partial float FosterFontGetScale(nint font, float size);
	[LibraryImport(DLL)]
	public static partial float FosterFontGetKerning(nint font, int glyph1, int glyph2, float scale);
	[LibraryImport(DLL)]
	public static partial void FosterFontGetCharacter(nint font, int glyph, float scale, out int width, out int height, out float advance, out float offsetX, out float offsetY, out int visible);
	[LibraryImport(DLL)]
	public static partial void FosterFontGetPixels(nint font, nint dest, int glyph, int width, int height, float scale);
	[LibraryImport(DLL)]
	public static partial void FosterFontFree(nint font);
	[LibraryImport(DLL)]
	public static partial Renderers FosterGetRenderer();
	[LibraryImport(DLL)]
	public static partial nint FosterTextureCreate(int width, int height, TextureFormat format);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterTextureSetData(nint texture, void* data, int length);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterTextureGetData(nint texture, void* data, int length);
	[LibraryImport(DLL)]
	public static partial void FosterTextureDestroy(nint texture);
	[LibraryImport(DLL)]
	public static partial nint FosterTargetCreate(int width, int height, TextureFormat[] formats, int formatCount);
	[LibraryImport(DLL)]
	public static partial nint FosterTargetGetAttachment(nint target, int index);
	[LibraryImport(DLL)]
	public static partial void FosterTargetDestroy(nint target);
	[DllImport(DLL)]
	public static extern nint FosterShaderCreate(ref FosterShaderData data);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterShaderGetUniforms(IntPtr shader, FosterUniformInfo* output, out int count, int max);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterShaderSetUniform(IntPtr shader, int index, float* values);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterShaderSetTexture(IntPtr shader, int index, IntPtr* values);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterShaderSetSampler(IntPtr shader, int index, TextureSampler* values);
	[LibraryImport(DLL)]
	public static partial void FosterShaderDestroy(IntPtr shader);
	[LibraryImport(DLL)]
	public static partial nint FosterMeshCreate();
	[LibraryImport(DLL)]
	public static partial void FosterMeshSetVertexFormat(nint mesh, ref FosterVertexFormat format);
	[LibraryImport(DLL)]
	public static partial void FosterMeshSetVertexData(nint mesh, nint data, int dataSize, int dataDestOffset);
	[LibraryImport(DLL)]
	public static partial void FosterMeshSetIndexFormat(nint mesh, IndexFormat format);
	[LibraryImport(DLL)]
	public static partial void FosterMeshSetIndexData(nint mesh, nint data, int dataSize, int dataDestOffset);
	[LibraryImport(DLL)]
	public static partial void FosterMeshDestroy(nint mesh);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterDraw(FosterDrawCommand* command);
	[LibraryImport(DLL)]
	public static unsafe partial void FosterClear(FosterClearCommand* command);

	// Non-Foster Calls:

	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial int SDL_GameControllerAddMapping(string mappingString);

	// [LibraryImport(DLL)]
	// public static partial void emscripten_set_main_loop(IntPtr action, int fps, bool simulateInfiniteLoop);
}
