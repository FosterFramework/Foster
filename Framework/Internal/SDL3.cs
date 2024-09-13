using System.Runtime.InteropServices;

using SDL_DisplayID = System.UInt32;
using SDL_WindowID = System.UInt32;
using SDL_PixelFormat = System.UInt32;
using SDL_KeyboardID = System.UInt32;
using SDL_Keycode = System.UInt32;
using SDL_Keymod = System.UInt16;
using SDL_MouseID = System.UInt32;
using SDL_MouseButtonFlags = System.UInt32;
using SDL_JoystickID = System.UInt32;
using System.Runtime.InteropServices.Marshalling;

namespace Foster.Framework;

/// <summary>
/// These are light bindings around SDL3, and only what we need specifically
/// for Foster to function.
/// </summary>
internal static partial class SDL3
{
	public const string DLL = "SDL3";

	// SDL_version.h

	[LibraryImport(DLL)]
	public static partial int SDL_GetVersion();

	// SDL_init.h

	[Flags]
	public enum SDL_InitFlags : UInt32
	{
		TIMER      = 0x00000001u,
		AUDIO      = 0x00000010u,
		VIDEO      = 0x00000020u,
		JOYSTICK   = 0x00000200u,
		HAPTIC     = 0x00001000u,
		GAMEPAD    = 0x00002000u,
		EVENTS     = 0x00004000u,
		SENSOR     = 0x00008000u,
		CAMERA     = 0x00010000u,
	}

	[LibraryImport(DLL)] [return:MarshalAs(UnmanagedType.Bool)]
	public static partial bool SDL_Init(SDL_InitFlags flags);

	[LibraryImport(DLL)]
	public static partial void SDL_Quit();

	// SDL_error.h

	[LibraryImport(DLL)]
	public static partial nint SDL_GetError();

	// SDL_video.h

	[Flags]
	public enum SDL_WindowFlags : UInt64
	{
		FULLSCREEN            = 0x0000000000000001,
		OPENGL                = 0x0000000000000002,
		OCCLUDED              = 0x0000000000000004,
		HIDDEN                = 0x0000000000000008,
		BORDERLESS            = 0x0000000000000010,
		RESIZABLE             = 0x0000000000000020,
		MINIMIZED             = 0x0000000000000040,
		MAXIMIZED             = 0x0000000000000080,
		MOUSE_GRABBED         = 0x0000000000000100,
		INPUT_FOCUS           = 0x0000000000000200,
		MOUSE_FOCUS           = 0x0000000000000400,
		EXTERNAL              = 0x0000000000000800,
		MODAL                 = 0x0000000000001000,
		HIGH_PIXEL_DENSITY    = 0x0000000000002000,
		MOUSE_CAPTURE         = 0x0000000000004000,
		ALWAYS_ON_TOP         = 0x0000000000008000,
		UTILITY               = 0x0000000000020000,
		TOOLTIP               = 0x0000000000040000,
		POPUP_MENU            = 0x0000000000080000,
		KEYBOARD_GRABBED      = 0x0000000000100000,
		VULKAN                = 0x0000000010000000,
		METAL                 = 0x0000000020000000,
		TRANSPARENT           = 0x0000000040000000,
		NOT_FOCUSABLE         = 0x0000000080000000,
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_DisplayMode
	{
		public SDL_DisplayID displayID;
		public SDL_PixelFormat format;
		public int w;
		public int h;
		public float pixel_density;
		public float refresh_rate;
		public int refresh_rate_numerator;
		public int refresh_rate_denominator;
		public nint @internal;
	}

	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial nint SDL_CreateWindow(string title, int w, int h, SDL_WindowFlags flags);

	[LibraryImport(DLL)]
	public static partial void SDL_DestroyWindow(nint window);

	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial void SDL_SetWindowTitle(nint window, string title);

	[LibraryImport(DLL)]
	public static partial SDL_WindowFlags SDL_GetWindowFlags(nint window);

	[LibraryImport(DLL)]
	public static partial int SDL_GetWindowSize(nint window, out int w, out int h);

	[LibraryImport(DLL)]
	public static partial int SDL_SetWindowSize(nint window, int w, int h);

	[LibraryImport(DLL)]
	public static partial int SDL_GetWindowSizeInPixels(nint window, out int w, out int h);

	[LibraryImport(DLL)]
	public static partial SDL_DisplayID SDL_GetDisplayForWindow(nint window);

	[LibraryImport(DLL)]
	public static partial float SDL_GetDisplayContentScale(SDL_DisplayID displayID);

	[LibraryImport(DLL)]
	public static unsafe partial SDL_DisplayMode* SDL_GetCurrentDisplayMode(SDL_DisplayID displayID);

	[LibraryImport(DLL)]
	public static unsafe partial int SDL_SetWindowFullscreenMode(nint window, SDL_DisplayMode* mode);

	[LibraryImport(DLL)]
	public static partial int SDL_SetWindowFullscreen(nint window, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);

	[LibraryImport(DLL)]
	public static partial int SDL_SetWindowResizable(nint window, [MarshalAs(UnmanagedType.Bool)] bool resizable);

	[LibraryImport(DLL)]
	public static partial int SDL_SetWindowBordered(nint window, [MarshalAs(UnmanagedType.Bool)] bool bordered);

	[LibraryImport(DLL)]
	public static partial int SDL_ShowWindow(nint window);

	// SDL_filesystem.h

	[LibraryImport(DLL)]
	public static unsafe partial nint SDL_GetPrefPath(nint org, nint app);

	// SDL_keyboard.h

	[LibraryImport(DLL)]
	public static unsafe partial int SDL_StartTextInput(nint window);

	[LibraryImport(DLL)]
	public static unsafe partial int SDL_StopTextInput(nint window);

	// SDL_rect.h

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_Rect
	{
		public int x, y, w, h;
	}

	// SDL_pixels.h

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_FColor
	{
		public float r, g, b, a;
	}

	// SDL_surface.h

	public enum SDL_FlipMode : int
	{
		SDL_FLIP_NONE,
		SDL_FLIP_HORIZONTAL,
		SDL_FLIP_VERTICAL
	}

	// SDL_mouse.h

	public enum SDL_MouseWheelDirection : int
	{
		NORMAL,
		FLIPPED
	}

	[LibraryImport(DLL)]
	public static partial int SDL_ShowCursor();

	[LibraryImport(DLL)]
	public static partial int SDL_HideCursor();

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.Bool)]  
	public static partial bool SDL_CursorVisible();

	[LibraryImport(DLL)]
	public static partial SDL_MouseButtonFlags SDL_GetMouseState(out float x, out float y);

	[LibraryImport(DLL)]
	public static partial SDL_MouseButtonFlags SDL_GetRelativeMouseState(out float x, out float y);
	

	// SDL_gamepad.h

	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial int SDL_AddGamepadMapping(string mapping);

	// SDL_clipboard.h

	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)]
	public static partial int SDL_SetClipboardText(string text);

	[LibraryImport(DLL)]
	public static partial nint SDL_GetClipboardText();

	// SDL_log.h

	public enum SDL_LogPriority : uint
	{
		SDL_LOG_PRIORITY_VERBOSE = 1,
		SDL_LOG_PRIORITY_DEBUG,
		SDL_LOG_PRIORITY_INFO,
		SDL_LOG_PRIORITY_WARN,
		SDL_LOG_PRIORITY_ERROR,
		SDL_LOG_PRIORITY_CRITICAL,
		SDL_NUM_LOG_PRIORITIES
	}

	[LibraryImport(DLL)]
	public static unsafe partial void SDL_SetLogOutputFunction(delegate* unmanaged<nint, int, SDL_LogPriority, nint, void> callback, nint userdata);

	// SDL_hints.h

	public static class Hints
	{
		public static readonly string SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS = "SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS";
	}

	[LibraryImport(DLL, StringMarshalling = StringMarshalling.Utf8)][return:MarshalAs(UnmanagedType.Bool)] 
	public static partial bool SDL_SetHint(string name, string value);

	// SDL_events.h

	public enum SDL_EventType : UInt32
	{
		FIRST = 0,
		QUIT = 0x100,

		TERMINATING,
		LOW_MEMORY,
		WILL_ENTER_BACKGROUND,
		DID_ENTER_BACKGROUND,
		WILL_ENTER_FOREGROUND,
		DID_ENTER_FOREGROUND,
		LOCALE_CHANGED,
		SYSTEM_THEME_CHANGED, 

		DISPLAY_ORIENTATION = 0x151,
		DISPLAY_ADDED,
		DISPLAY_REMOVED,
		DISPLAY_MOVED,
		DISPLAY_DESKTOP_MODE_CHANGED,
		DISPLAY_CURRENT_MODE_CHANGED,
		DISPLAY_CONTENT_SCALE_CHANGED,
		DISPLAY_FIRST = DISPLAY_ORIENTATION,
		DISPLAY_LAST = DISPLAY_CONTENT_SCALE_CHANGED,

		WINDOW_SHOWN = 0x202,
		WINDOW_HIDDEN,
		WINDOW_EXPOSED,
		WINDOW_MOVED,
		WINDOW_RESIZED,
		WINDOW_PIXEL_SIZE_CHANGED,
		WINDOW_METAL_VIEW_RESIZED,
		WINDOW_MINIMIZED,
		WINDOW_MAXIMIZED,
		WINDOW_RESTORED,
		WINDOW_MOUSE_ENTER,
		WINDOW_MOUSE_LEAVE,
		WINDOW_FOCUS_GAINED,
		WINDOW_FOCUS_LOST,
		WINDOW_CLOSE_REQUESTED,
		WINDOW_HIT_TEST,
		WINDOW_ICCPROF_CHANGED,
		WINDOW_DISPLAY_CHANGED,
		WINDOW_DISPLAY_SCALE_CHANGED,
		WINDOW_SAFE_AREA_CHANGED,
		WINDOW_OCCLUDED,
		WINDOW_ENTER_FULLSCREEN,
		WINDOW_LEAVE_FULLSCREEN,
		WINDOW_DESTROYED,
		WINDOW_PEN_ENTER,
		WINDOW_PEN_LEAVE,
		WINDOW_HDR_STATE_CHANGED,
		WINDOW_FIRST = WINDOW_SHOWN,
		WINDOW_LAST = WINDOW_HDR_STATE_CHANGED,

		KEY_DOWN = 0x300,
		KEY_UP,
		TEXT_EDITING,
		TEXT_INPUT,
		KEYMAP_CHANGED,
		KEYBOARD_ADDED,
		KEYBOARD_REMOVED,
		TEXT_EDITING_CANDIDATES,

		MOUSE_MOTION = 0x400,
		MOUSE_BUTTON_DOWN,
		MOUSE_BUTTON_UP,
		MOUSE_WHEEL,
		MOUSE_ADDED,
		MOUSE_REMOVED,

		JOYSTICK_AXIS_MOTION = 0x600,
		JOYSTICK_BALL_MOTION,
		JOYSTICK_HAT_MOTION,
		JOYSTICK_BUTTON_DOWN,
		JOYSTICK_BUTTON_UP,
		JOYSTICK_ADDED,
		JOYSTICK_REMOVED,
		JOYSTICK_BATTERY_UPDATED,
		JOYSTICK_UPDATE_COMPLETE,

		GAMEPAD_AXIS_MOTION = 0x650,
		GAMEPAD_BUTTON_DOWN,
		GAMEPAD_BUTTON_UP,
		GAMEPAD_ADDED,
		GAMEPAD_REMOVED,
		GAMEPAD_REMAPPED,
		GAMEPAD_TOUCHPAD_DOWN,
		GAMEPAD_TOUCHPAD_MOTION,
		GAMEPAD_TOUCHPAD_UP,
		GAMEPAD_SENSOR_UPDATE,
		GAMEPAD_UPDATE_COMPLETE,
		GAMEPAD_STEAM_HANDLE_UPDATED,

		FINGER_DOWN = 0x700,
		FINGER_UP,
		FINGER_MOTION,

		CLIPBOARD_UPDATE = 0x900,

		DROP_FILE = 0x1000,
		DROP_TEXT,
		DROP_BEGIN,
		DROP_COMPLETE,
		DROP_POSITION,

		AUDIO_DEVICE_ADDED = 0x1100,
		AUDIO_DEVICE_REMOVED,
		AUDIO_DEVICE_FORMAT_CHANGED,

		SENSOR_UPDATE = 0x1200,

		PEN_DOWN = 0x1300,
		PEN_UP,
		PEN_MOTION,
		PEN_BUTTON_DOWN,
		PEN_BUTTON_UP,

		CAMERA_DEVICE_ADDED = 0x1400,
		CAMERA_DEVICE_REMOVED,
		CAMERA_DEVICE_APPROVED,
		CAMERA_DEVICE_DENIED,

		RENDER_TARGETS_RESET = 0x2000,
		RENDER_DEVICE_RESET,

		POLL_SENTINEL = 0x7F00,
		USER = 0x8000,
		LAST = 0xFFFF,
		ENUM_PADDING = 0x7FFFFFFF
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_WindowEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public Int32 data1;
		public Int32 data2;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_KeyboardEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public SDL_KeyboardID which;
		public SDL_Scancode scancode;
		public SDL_Keycode key;
		public SDL_Keymod mod;
		public UInt16 raw;
		public byte state;
		public byte repeat;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_TextEditingEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public nint text;
		public Int32 start;
		public Int32 length;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_TextInputEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public nint text;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_MouseDeviceEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_MouseID which;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_MouseMotionEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public SDL_MouseID which;
		public SDL_MouseButtonFlags state;
		public float x;
		public float y;
		public float xrel;
		public float yrel;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_MouseButtonEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public SDL_MouseID which;
		public byte button;
		public byte state;
		public byte clicks;
		public byte padding;
		public float x;
		public float y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_MouseWheelEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_WindowID windowID;
		public SDL_MouseID which;
		public float x;
		public float y;
		public SDL_MouseWheelDirection direction;
		public float mouse_x;
		public float mouse_y;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_JoyAxisEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public byte axis;
		public byte padding1;
		public byte padding2;
		public byte padding3;
		public Int16 value;
		public UInt16 padding4;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_JoyBallEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public byte ball;
		public byte padding1;
		public byte padding2;
		public byte padding3;
		public Int16 xrel;
		public Int16 yrel;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_JoyHatEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public byte hat;
		public byte value;
		public byte padding1;
		public byte padding2;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_JoyButtonEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public byte button;
		public byte state;
		public byte padding1;
		public byte padding2;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_JoyDeviceEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GamepadAxisEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public byte axis;
		public byte padding1;
		public byte padding2;
		public byte padding3;
		public Int16 value;
		public UInt16 padding4;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GamepadButtonEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public byte button;
		public byte state;
		public byte padding1;
		public byte padding2;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GamepadDeviceEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_GamepadTouchpadEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public Int32 touchpad;
		public Int32 finger;
		public float x;
		public float y;
		public float pressure;
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct SDL_GamepadSensorEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
		public SDL_JoystickID which;
		public Int32 sensor;
		public fixed float data[3];
		public UInt64 sensor_timestamp;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SDL_QuitEvent
	{
		public SDL_EventType type;
		public UInt32 reserved;
		public UInt64 timestamp;
	}

	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct SDL_Event
	{
		[FieldOffset(0)] public SDL_EventType type;
		[FieldOffset(0)] public SDL_WindowEvent window;
		[FieldOffset(0)] public SDL_KeyboardEvent key;
		[FieldOffset(0)] public SDL_TextEditingEvent edit;
		[FieldOffset(0)] public SDL_TextInputEvent text;
		[FieldOffset(0)] public SDL_MouseDeviceEvent mdevice;
		[FieldOffset(0)] public SDL_MouseMotionEvent motion;
		[FieldOffset(0)] public SDL_MouseButtonEvent button;
		[FieldOffset(0)] public SDL_MouseWheelEvent wheel;
		[FieldOffset(0)] public SDL_JoyDeviceEvent jdevice;
		[FieldOffset(0)] public SDL_JoyAxisEvent jaxis;
		[FieldOffset(0)] public SDL_JoyBallEvent jball;
		[FieldOffset(0)] public SDL_JoyHatEvent jhat;
		[FieldOffset(0)] public SDL_JoyButtonEvent jbutton;
		[FieldOffset(0)] public SDL_GamepadDeviceEvent gdevice;
		[FieldOffset(0)] public SDL_GamepadAxisEvent gaxis;
		[FieldOffset(0)] public SDL_GamepadButtonEvent gbutton;
		[FieldOffset(0)] public SDL_GamepadTouchpadEvent gtouchpad;
		[FieldOffset(0)] public SDL_GamepadSensorEvent gsensor;
		[FieldOffset(0)] public SDL_QuitEvent quit;
		[FieldOffset(0)] public fixed byte padding[128];
	}

	[LibraryImport(DLL)][return:MarshalAs(UnmanagedType.Bool)] 
	public static unsafe partial bool SDL_PollEvent(SDL_Event* eventPtr);
}
