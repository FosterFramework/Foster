using System.Runtime.InteropServices;
using System.Text;

using static Foster.Framework.SDL3;

namespace Foster.Framework;

internal static partial class Platform
{
	public const string DLL = "FosterPlatform";

	/// <summary>
	/// Converts a utf8 null-terminating string into a C# string
	/// </summary>
	public static unsafe string ParseUTF8(nint s)
	{
		if (s == 0)
			return string.Empty;

		byte* ptr = (byte*)s;
		while (*ptr != 0)
			ptr++;
		return Encoding.UTF8.GetString((byte*)s, (int)(ptr - (byte*)s));
	}

	/// <summary>
	/// Converts and allocates a null-terminating utf8 string from a C# string
	/// </summary>
	public static unsafe nint ToUTF8(in string str)
	{
		var count = Encoding.UTF8.GetByteCount(str) + 1;
		var ptr = Marshal.AllocHGlobal(count);
		var span = new Span<byte>((byte*)ptr.ToPointer(), count);
		Encoding.UTF8.GetBytes(str, span);
		span[^1] = 0;
		return ptr;
	}

	/// <summary>
	/// Frees a UTF8 string that was allocated from <seealso cref="ToUTF8"/>
	/// </summary>
	public static void FreeUTF8(nint ptr) => Marshal.FreeHGlobal(ptr);

	/// <summary>
	/// Wrapper around SDL_GetError() to return a C# string
	/// </summary>
	public static string GetSDLError() => ParseUTF8(SDL_GetError());

	[LibraryImport(DLL, EntryPoint = "FosterImageLoad")]
	public static unsafe partial nint ImageLoad(void* memory, int length, out int w, out int h);
	
	[LibraryImport(DLL, EntryPoint = "FosterImageFree")]
	public static partial void ImageFree(nint data);
	
	[LibraryImport(DLL, EntryPoint = "FosterImageWrite")]
	public static unsafe partial byte ImageWrite(delegate* unmanaged<nint, nint, int, void> func, IntPtr context, ImageWriteFormat format, int w, int h, IntPtr data);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontInit")]
	public static partial nint FontInit(nint data, int length);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontGetMetrics")]
	public static partial void FontGetMetrics(nint font, out int ascent, out int descent, out int linegap);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontGetGlyphIndex")]
	public static partial int FontGetGlyphIndex(nint font, int codepoint);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontGetScale")]
	public static partial float FontGetScale(nint font, float size);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontGetKerning")]
	public static partial float FontGetKerning(nint font, int glyph1, int glyph2, float scale);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontGetCharacter")]
	public static partial void FontGetCharacter(nint font, int glyph, float scale, out int width, out int height, out float advance, out float offsetX, out float offsetY, out int visible);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontGetPixels")]
	public static partial void FontGetPixels(nint font, nint dest, int glyph, int width, int height, float scale);
	
	[LibraryImport(DLL, EntryPoint = "FosterFontFree")]
	public static partial void FontFree(nint font);

	[UnmanagedCallersOnly]
	public static void HandleLog(nint userdata, int category, SDL_LogPriority priority, nint message)
	{
		switch (priority)
		{
			case SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE:
			case SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG:
			case SDL_LogPriority.SDL_LOG_PRIORITY_INFO:
				Log.Info(message);
				break;
			case SDL_LogPriority.SDL_LOG_PRIORITY_WARN:
				Log.Warning(message);
				break;
			case SDL_LogPriority.SDL_LOG_PRIORITY_ERROR:
			case SDL_LogPriority.SDL_LOG_PRIORITY_CRITICAL:
				Log.Error(message);
				break;
		}
	}
	
	public static Buttons GetButtonFromSDL(SDL_GamepadButton button) => button switch
	{
		SDL_GamepadButton.INVALID => Buttons.None,
		SDL_GamepadButton.SOUTH => Buttons.South,
		SDL_GamepadButton.EAST => Buttons.East,
		SDL_GamepadButton.WEST => Buttons.West,
		SDL_GamepadButton.NORTH => Buttons.North,
		SDL_GamepadButton.BACK => Buttons.Back,
		SDL_GamepadButton.GUIDE => Buttons.Select,
		SDL_GamepadButton.START => Buttons.Start,
		SDL_GamepadButton.LEFT_STICK => Buttons.LeftStick,
		SDL_GamepadButton.RIGHT_STICK => Buttons.RightStick,
		SDL_GamepadButton.LEFT_SHOULDER => Buttons.LeftShoulder,
		SDL_GamepadButton.RIGHT_SHOULDER => Buttons.RightShoulder,
		SDL_GamepadButton.DPAD_UP => Buttons.Up,
		SDL_GamepadButton.DPAD_DOWN => Buttons.Down,
		SDL_GamepadButton.DPAD_LEFT => Buttons.Left,
		SDL_GamepadButton.DPAD_RIGHT => Buttons.Right,
		_ => Buttons.None,
	};

	public static MouseButtons GetMouseFromSDL(int button) => button switch
	{
		1 => MouseButtons.Left,
		2 => MouseButtons.Middle,
		3 => MouseButtons.Right,
		_ => MouseButtons.None,
	};

	public static Axes GetAxisFromSDL(SDK_GamepadAxis axis) => axis switch
	{
		SDK_GamepadAxis.INVALID => Axes.None,
		SDK_GamepadAxis.LEFTX => Axes.LeftX,
		SDK_GamepadAxis.LEFTY => Axes.LeftY,
		SDK_GamepadAxis.RIGHTX => Axes.RightX,
		SDK_GamepadAxis.RIGHTY => Axes.RightY,
		SDK_GamepadAxis.LEFT_TRIGGER => Axes.LeftTrigger,
		SDK_GamepadAxis.RIGHT_TRIGGER => Axes.RightTrigger,
		_ => Axes.None,
	};

	public static Keys GetKeyFromSDL(SDL_Scancode scancode) => scancode switch
	{
		SDL_Scancode.SDL_SCANCODE_UNKNOWN => Keys.Unknown,
		SDL_Scancode.SDL_SCANCODE_A => Keys.A,
		SDL_Scancode.SDL_SCANCODE_B => Keys.B,
		SDL_Scancode.SDL_SCANCODE_C => Keys.C,
		SDL_Scancode.SDL_SCANCODE_D => Keys.D,
		SDL_Scancode.SDL_SCANCODE_E => Keys.E,
		SDL_Scancode.SDL_SCANCODE_F => Keys.F,
		SDL_Scancode.SDL_SCANCODE_G => Keys.G,
		SDL_Scancode.SDL_SCANCODE_H => Keys.H,
		SDL_Scancode.SDL_SCANCODE_I => Keys.I,
		SDL_Scancode.SDL_SCANCODE_J => Keys.J,
		SDL_Scancode.SDL_SCANCODE_K => Keys.K,
		SDL_Scancode.SDL_SCANCODE_L => Keys.L,
		SDL_Scancode.SDL_SCANCODE_M => Keys.M,
		SDL_Scancode.SDL_SCANCODE_N => Keys.N,
		SDL_Scancode.SDL_SCANCODE_O => Keys.O,
		SDL_Scancode.SDL_SCANCODE_P => Keys.P,
		SDL_Scancode.SDL_SCANCODE_Q => Keys.Q,
		SDL_Scancode.SDL_SCANCODE_R => Keys.R,
		SDL_Scancode.SDL_SCANCODE_S => Keys.S,
		SDL_Scancode.SDL_SCANCODE_T => Keys.T,
		SDL_Scancode.SDL_SCANCODE_U => Keys.U,
		SDL_Scancode.SDL_SCANCODE_V => Keys.V,
		SDL_Scancode.SDL_SCANCODE_W => Keys.W,
		SDL_Scancode.SDL_SCANCODE_X => Keys.X,
		SDL_Scancode.SDL_SCANCODE_Y => Keys.Y,
		SDL_Scancode.SDL_SCANCODE_Z => Keys.Z,
		SDL_Scancode.SDL_SCANCODE_1 => Keys.D1,
		SDL_Scancode.SDL_SCANCODE_2 => Keys.D2,
		SDL_Scancode.SDL_SCANCODE_3 => Keys.D3,
		SDL_Scancode.SDL_SCANCODE_4 => Keys.D4,
		SDL_Scancode.SDL_SCANCODE_5 => Keys.D5,
		SDL_Scancode.SDL_SCANCODE_6 => Keys.D6,
		SDL_Scancode.SDL_SCANCODE_7 => Keys.D7,
		SDL_Scancode.SDL_SCANCODE_8 => Keys.D8,
		SDL_Scancode.SDL_SCANCODE_9 => Keys.D9,
		SDL_Scancode.SDL_SCANCODE_0 => Keys.D0,
		SDL_Scancode.SDL_SCANCODE_RETURN => Keys.Enter,
		SDL_Scancode.SDL_SCANCODE_ESCAPE => Keys.Escape,
		SDL_Scancode.SDL_SCANCODE_BACKSPACE => Keys.Backslash,
		SDL_Scancode.SDL_SCANCODE_TAB => Keys.Tab,
		SDL_Scancode.SDL_SCANCODE_SPACE => Keys.Space,
		SDL_Scancode.SDL_SCANCODE_MINUS => Keys.Minus,
		SDL_Scancode.SDL_SCANCODE_EQUALS => Keys.Equals,
		SDL_Scancode.SDL_SCANCODE_LEFTBRACKET => Keys.LeftBracket,
		SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET => Keys.RightBracket,
		SDL_Scancode.SDL_SCANCODE_BACKSLASH => Keys.Backslash,
		SDL_Scancode.SDL_SCANCODE_SEMICOLON => Keys.Semicolon,
		SDL_Scancode.SDL_SCANCODE_APOSTROPHE => Keys.Apostrophe,
		SDL_Scancode.SDL_SCANCODE_GRAVE => Keys.Tilde,
		SDL_Scancode.SDL_SCANCODE_COMMA => Keys.Comma,
		SDL_Scancode.SDL_SCANCODE_PERIOD => Keys.Period,
		SDL_Scancode.SDL_SCANCODE_SLASH => Keys.Slash,
		SDL_Scancode.SDL_SCANCODE_CAPSLOCK => Keys.Capslock,
		SDL_Scancode.SDL_SCANCODE_F1 => Keys.F1,
		SDL_Scancode.SDL_SCANCODE_F2 => Keys.F2,
		SDL_Scancode.SDL_SCANCODE_F3 => Keys.F3,
		SDL_Scancode.SDL_SCANCODE_F4 => Keys.F4,
		SDL_Scancode.SDL_SCANCODE_F5 => Keys.F5,
		SDL_Scancode.SDL_SCANCODE_F6 => Keys.F6,
		SDL_Scancode.SDL_SCANCODE_F7 => Keys.F7,
		SDL_Scancode.SDL_SCANCODE_F8 => Keys.F8,
		SDL_Scancode.SDL_SCANCODE_F9 => Keys.F9,
		SDL_Scancode.SDL_SCANCODE_F10 => Keys.F10,
		SDL_Scancode.SDL_SCANCODE_F11 => Keys.F11,
		SDL_Scancode.SDL_SCANCODE_F12 => Keys.F12,
		SDL_Scancode.SDL_SCANCODE_PRINTSCREEN => Keys.PrintScreen,
		SDL_Scancode.SDL_SCANCODE_SCROLLLOCK => Keys.ScrollLock,
		SDL_Scancode.SDL_SCANCODE_PAUSE => Keys.Pause,
		SDL_Scancode.SDL_SCANCODE_INSERT => Keys.Insert,
		SDL_Scancode.SDL_SCANCODE_HOME => Keys.Home,
		SDL_Scancode.SDL_SCANCODE_PAGEUP => Keys.PageUp,
		SDL_Scancode.SDL_SCANCODE_DELETE => Keys.Delete,
		SDL_Scancode.SDL_SCANCODE_END => Keys.End,
		SDL_Scancode.SDL_SCANCODE_PAGEDOWN => Keys.PageDown,
		SDL_Scancode.SDL_SCANCODE_RIGHT => Keys.Right,
		SDL_Scancode.SDL_SCANCODE_LEFT => Keys.Left,
		SDL_Scancode.SDL_SCANCODE_DOWN => Keys.Down,
		SDL_Scancode.SDL_SCANCODE_UP => Keys.Up,
		SDL_Scancode.SDL_SCANCODE_KP_DIVIDE => Keys.KeypadDivide,
		SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY => Keys.KeypadMultiply,
		SDL_Scancode.SDL_SCANCODE_KP_MINUS => Keys.KeypadMinus,
		SDL_Scancode.SDL_SCANCODE_KP_PLUS => Keys.KeypadPlus,
		SDL_Scancode.SDL_SCANCODE_KP_ENTER => Keys.KeypadEnter,
		SDL_Scancode.SDL_SCANCODE_KP_1 => Keys.Keypad1,
		SDL_Scancode.SDL_SCANCODE_KP_2 => Keys.Keypad2,
		SDL_Scancode.SDL_SCANCODE_KP_3 => Keys.Keypad3,
		SDL_Scancode.SDL_SCANCODE_KP_4 => Keys.Keypad4,
		SDL_Scancode.SDL_SCANCODE_KP_5 => Keys.Keypad5,
		SDL_Scancode.SDL_SCANCODE_KP_6 => Keys.Keypad6,
		SDL_Scancode.SDL_SCANCODE_KP_7 => Keys.Keypad7,
		SDL_Scancode.SDL_SCANCODE_KP_8 => Keys.Keypad8,
		SDL_Scancode.SDL_SCANCODE_KP_9 => Keys.Keypad9,
		SDL_Scancode.SDL_SCANCODE_KP_0 => Keys.Keypad0,
		SDL_Scancode.SDL_SCANCODE_APPLICATION => Keys.Application,
		SDL_Scancode.SDL_SCANCODE_KP_EQUALS => Keys.KeypadEquals,
		SDL_Scancode.SDL_SCANCODE_F13 => Keys.F13,
		SDL_Scancode.SDL_SCANCODE_F14 => Keys.F14,
		SDL_Scancode.SDL_SCANCODE_F15 => Keys.F15,
		SDL_Scancode.SDL_SCANCODE_F16 => Keys.F16,
		SDL_Scancode.SDL_SCANCODE_F17 => Keys.F17,
		SDL_Scancode.SDL_SCANCODE_F18 => Keys.F18,
		SDL_Scancode.SDL_SCANCODE_F19 => Keys.F19,
		SDL_Scancode.SDL_SCANCODE_F20 => Keys.F20,
		SDL_Scancode.SDL_SCANCODE_F21 => Keys.F21,
		SDL_Scancode.SDL_SCANCODE_F22 => Keys.F22,
		SDL_Scancode.SDL_SCANCODE_F23 => Keys.F23,
		SDL_Scancode.SDL_SCANCODE_F24 => Keys.F24,
		SDL_Scancode.SDL_SCANCODE_EXECUTE => Keys.Execute,
		SDL_Scancode.SDL_SCANCODE_HELP => Keys.Help,
		SDL_Scancode.SDL_SCANCODE_MENU => Keys.Menu,
		SDL_Scancode.SDL_SCANCODE_SELECT => Keys.Select,
		SDL_Scancode.SDL_SCANCODE_STOP => Keys.Stop,
		SDL_Scancode.SDL_SCANCODE_UNDO => Keys.Undo,
		SDL_Scancode.SDL_SCANCODE_CUT => Keys.Cut,
		SDL_Scancode.SDL_SCANCODE_COPY => Keys.Copy,
		SDL_Scancode.SDL_SCANCODE_PASTE => Keys.Paste,
		SDL_Scancode.SDL_SCANCODE_FIND => Keys.Find,
		SDL_Scancode.SDL_SCANCODE_MUTE => Keys.Mute,
		SDL_Scancode.SDL_SCANCODE_VOLUMEUP => Keys.VolumeUp,
		SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN => Keys.VolumeDown,
		SDL_Scancode.SDL_SCANCODE_KP_COMMA => Keys.KeypadComma,
		SDL_Scancode.SDL_SCANCODE_ALTERASE => Keys.AltErase,
		SDL_Scancode.SDL_SCANCODE_SYSREQ => Keys.SysReq,
		SDL_Scancode.SDL_SCANCODE_CANCEL => Keys.Cancel,
		SDL_Scancode.SDL_SCANCODE_CLEAR => Keys.Clear,
		SDL_Scancode.SDL_SCANCODE_PRIOR => Keys.Prior,
		SDL_Scancode.SDL_SCANCODE_RETURN2 => Keys.Enter2,
		SDL_Scancode.SDL_SCANCODE_SEPARATOR => Keys.Separator,
		SDL_Scancode.SDL_SCANCODE_OUT => Keys.Out,
		SDL_Scancode.SDL_SCANCODE_OPER => Keys.Oper,
		SDL_Scancode.SDL_SCANCODE_CLEARAGAIN => Keys.ClearAgain,
		SDL_Scancode.SDL_SCANCODE_KP_00 => Keys.Keypad00,
		SDL_Scancode.SDL_SCANCODE_KP_000 => Keys.Keypad000,
		SDL_Scancode.SDL_SCANCODE_KP_LEFTPAREN => Keys.KeypadLeftParen,
		SDL_Scancode.SDL_SCANCODE_KP_RIGHTPAREN => Keys.KeypadRightParen,
		SDL_Scancode.SDL_SCANCODE_KP_LEFTBRACE => Keys.KeypadLeftBrace,
		SDL_Scancode.SDL_SCANCODE_KP_RIGHTBRACE => Keys.KeypadRightBrace,
		SDL_Scancode.SDL_SCANCODE_KP_TAB => Keys.KeypadTab,
		SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE => Keys.KeypadBackspace,
		SDL_Scancode.SDL_SCANCODE_KP_A => Keys.KeypadA,
		SDL_Scancode.SDL_SCANCODE_KP_B => Keys.KeypadB,
		SDL_Scancode.SDL_SCANCODE_KP_C => Keys.KeypadC,
		SDL_Scancode.SDL_SCANCODE_KP_D => Keys.KeypadD,
		SDL_Scancode.SDL_SCANCODE_KP_E => Keys.KeypadE,
		SDL_Scancode.SDL_SCANCODE_KP_F => Keys.KeypadF,
		SDL_Scancode.SDL_SCANCODE_KP_XOR => Keys.KeypadXor,
		SDL_Scancode.SDL_SCANCODE_KP_POWER => Keys.KeypadPower,
		SDL_Scancode.SDL_SCANCODE_KP_PERCENT => Keys.KeypadPercent,
		SDL_Scancode.SDL_SCANCODE_KP_LESS => Keys.KeypadLess,
		SDL_Scancode.SDL_SCANCODE_KP_GREATER => Keys.KeypadGreater,
		SDL_Scancode.SDL_SCANCODE_KP_AMPERSAND => Keys.KeypadAmpersand,
		SDL_Scancode.SDL_SCANCODE_KP_COLON => Keys.KeypadColon,
		SDL_Scancode.SDL_SCANCODE_KP_HASH => Keys.KeypadHash,
		SDL_Scancode.SDL_SCANCODE_KP_SPACE => Keys.KeypadSpace,
		SDL_Scancode.SDL_SCANCODE_KP_CLEAR => Keys.KeypadClear,
		SDL_Scancode.SDL_SCANCODE_LCTRL => Keys.LeftControl,
		SDL_Scancode.SDL_SCANCODE_LSHIFT => Keys.LeftShift,
		SDL_Scancode.SDL_SCANCODE_LALT => Keys.LeftAlt,
		SDL_Scancode.SDL_SCANCODE_LGUI => Keys.LeftOS,
		SDL_Scancode.SDL_SCANCODE_RCTRL => Keys.RightControl,
		SDL_Scancode.SDL_SCANCODE_RSHIFT => Keys.RightShift,
		SDL_Scancode.SDL_SCANCODE_RALT => Keys.RightAlt,
		SDL_Scancode.SDL_SCANCODE_RGUI => Keys.RightOS,
		_ => Keys.Unknown,
	};
}
