using System.Numerics;
using static SDL3.SDL;

namespace Foster.Framework;

internal sealed class InputProviderSDL(App app) : InputProvider
{
	public readonly App App = app;
	private Vector2 lastMouse;

	private readonly List<(uint ID, nint Ptr)> openJoysticks = [];
	private readonly List<(uint ID, nint Ptr)> openGamepads = [];

	public override string GetClipboard()
	{
		return SDL_GetClipboardText();
	}

	public override void SetClipboard(string text)
	{
		SDL_SetClipboardText(text);
	}

	public override void Rumble(ControllerID id, float lowIntensity, float highIntensity, float duration)
	{
		var highFrequency = (ushort)(Calc.Clamp(highIntensity, 0, 1) * 0xFFFF);
		var lowFrequency = (ushort)(Calc.Clamp(lowIntensity, 0, 1) * 0xFFFF);
		var durationms = (uint)TimeSpan.FromSeconds(duration).TotalMilliseconds;

		if (Input.GetController(id)?.IsGamepad ?? false)
		{
			var ptr = SDL_GetGamepadFromID(id.Value);
			if (ptr != nint.Zero)
				SDL_RumbleGamepad(ptr, lowFrequency, highFrequency, durationms);

		}
		else
		{
			var ptr = SDL_GetJoystickFromID(id.Value);
			if (ptr != nint.Zero)
				SDL_RumbleJoystick(ptr, lowFrequency, highFrequency, durationms);
		}
	}

	public override void Update(in Time time)
	{
		// get window properties
		var windowSize = new Point2(App.Window.Width, App.Window.Height);
		var windowSizeInPx = new Point2(App.Window.WidthInPixels, App.Window.HeightInPixels);
		var windowPos = new Point2();
		SDL_GetWindowPosition(App.Window.Handle, out windowPos.X, out windowPos.Y);

		// use global mouse position so we can get it as it moves outside the window
		var mouse = new Vector2();
		SDL_GetGlobalMouseState(out mouse.X, out mouse.Y);
		mouse -= windowPos;

		// scale it to the pixel coords
		mouse = mouse / windowSize * windowSizeInPx;
		var delta = mouse - lastMouse;

		// get mouse delta if we're in relative mouse mode
		if (SDL_GetWindowRelativeMouseMode(App.Window.Handle))
		{
			SDL_GetRelativeMouseState(out float dx, out float dy);
			delta = new Vector2(dx, dy) / windowSize * windowSizeInPx;
		}

		// add new event if moved
		if (lastMouse.X != mouse.X || lastMouse.Y != mouse.Y || delta.X != 0 || delta.Y != 0)
		{
			lastMouse = mouse;
			MouseMove(mouse, delta, time.Elapsed);
		}

		base.Update(time);
	}

	public unsafe void OnEvent(SDL_Event ev)
	{
		switch ((SDL_EventType)ev.type)
		{
		// mouse
		case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
			MouseButton((int)GetMouseFromSDL(ev.button.button), true, App.Time.Elapsed);
			break;
		case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
			MouseButton((int)GetMouseFromSDL(ev.button.button), false, App.Time.Elapsed);
			break;
		case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
			MouseWheel(new(ev.wheel.x, ev.wheel.y));
			break;

		// keyboard
		case SDL_EventType.SDL_EVENT_KEY_DOWN:
			if (!ev.key.repeat)
				Key((int)GetKeyFromSDL(ev.key.scancode), true, App.Time.Elapsed);
			break;
		case SDL_EventType.SDL_EVENT_KEY_UP:
			if (!ev.key.repeat)
				Key((int)GetKeyFromSDL(ev.key.scancode), false, App.Time.Elapsed);
			break;

		case SDL_EventType.SDL_EVENT_TEXT_INPUT:
			Text(new nint(ev.text.text), App.Window);
			break;

		// joystick
		case SDL_EventType.SDL_EVENT_JOYSTICK_ADDED:
			{
				var id = ev.jdevice.which;
				if (SDL_IsGamepad(id))
					break;

				var ptr = SDL_OpenJoystick(id);
				openJoysticks.Add((id, ptr));

				ConnectController(
					id: new(id),
					name: SDL_GetJoystickName(ptr),
					buttonCount: SDL_GetNumJoystickButtons(ptr),
					axisCount: SDL_GetNumJoystickAxes(ptr),
					isGamepad: false,
					type: GamepadTypes.Unknown,
					vendor: SDL_GetJoystickVendor(ptr),
					product: SDL_GetJoystickProduct(ptr),
					version: SDL_GetJoystickProductVersion(ptr)
				);
				break;
			}
		case SDL_EventType.SDL_EVENT_JOYSTICK_REMOVED:
			{
				var id = ev.jdevice.which;
				if (SDL_IsGamepad(id))
					break;

				for (int i = 0; i < openJoysticks.Count; i ++)
					if (openJoysticks[i].ID == id)
					{
						SDL_CloseJoystick(openJoysticks[i].Ptr);
						openJoysticks.RemoveAt(i);
					}

				DisconnectController(new(id));
				break;
			}
		case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN:
		case SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_UP:
			{
				var id = ev.jbutton.which;
				if (SDL_IsGamepad(id))
					break;

				ControllerButton(
					id: new(id),
					button: ev.jbutton.button,
					pressed: ev.type == (uint)SDL_EventType.SDL_EVENT_JOYSTICK_BUTTON_DOWN,
					time: App.Time.Elapsed);

				break;
			}
		case SDL_EventType.SDL_EVENT_JOYSTICK_AXIS_MOTION:
			{
				var id = ev.jaxis.which;
				if (SDL_IsGamepad(id))
					break;

				float value = ev.jaxis.value >= 0
					? ev.jaxis.value / 32767.0f
					: ev.jaxis.value / 32768.0f;

				ControllerAxis(
					id: new(id),
					axis: ev.jaxis.axis,
					value: value,
					time: App.Time.Elapsed);

				break;
			}

		// gamepad
		case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
			{
				var id = ev.gdevice.which;
				var ptr = SDL_OpenGamepad(id);
				openGamepads.Add((id, ptr));

				ConnectController(
					id: new(id),
					name: SDL_GetGamepadName(ptr),
					buttonCount: 15,
					axisCount: 6,
					isGamepad: true,
					type: (GamepadTypes)SDL_GetGamepadType(ptr),
					vendor: SDL_GetGamepadVendor(ptr),
					product: SDL_GetGamepadProduct(ptr),
					version: SDL_GetGamepadProductVersion(ptr)
				);
				break;
			}
		case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
			{
				var id = ev.gdevice.which;
				for (int i = 0; i < openGamepads.Count; i ++)
					if (openGamepads[i].ID == id)
					{
						SDL_CloseGamepad(openGamepads[i].Ptr);
						openGamepads.RemoveAt(i);
					}

				DisconnectController(new(id));
				break;
			}
		case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
		case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
			{
				var id = ev.gbutton.which;
				ControllerButton(
					id: new(id),
					button: (int)GetButtonFromSDL((SDL_GamepadButton)ev.gbutton.button),
					pressed: ev.type == (uint)SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN,
					time: App.Time.Elapsed);

				break;
			}
		case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
			{
				var id = ev.gbutton.which;
				float value = ev.gaxis.value >= 0
					? ev.gaxis.value / 32767.0f
					: ev.gaxis.value / 32768.0f;

				ControllerAxis(
					id: new(id),
					axis: (int)GetAxisFromSDL((SDL_GamepadAxis)ev.gaxis.axis),
					value: value,
					time: App.Time.Elapsed);

				break;
			}
		}
	}

	public void CloseDevices()
    {
		foreach (var it in openJoysticks)
			SDL_CloseJoystick(it.Ptr);
		foreach (var it in openGamepads)
			SDL_CloseGamepad(it.Ptr);
		openJoysticks.Clear();
		openGamepads.Clear();
	}

	private static Buttons GetButtonFromSDL(SDL_GamepadButton button) => button switch
	{
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID => Buttons.None,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => Buttons.South,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => Buttons.East,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => Buttons.West,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => Buttons.North,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => Buttons.Back,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE => Buttons.Guide,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START => Buttons.Start,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK => Buttons.LeftStick,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK => Buttons.RightStick,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => Buttons.LeftShoulder,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => Buttons.RightShoulder,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => Buttons.Up,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => Buttons.Down,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => Buttons.Left,
		SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => Buttons.Right,
		_ => Buttons.None,
	};

	private static MouseButtons GetMouseFromSDL(int button) => button switch
	{
		1 => MouseButtons.Left,
		2 => MouseButtons.Middle,
		3 => MouseButtons.Right,
		_ => MouseButtons.None,
	};

	private static Axes GetAxisFromSDL(SDL_GamepadAxis axis) => axis switch
	{
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_INVALID => Axes.None,
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => Axes.LeftX,
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => Axes.LeftY,
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX => Axes.RightX,
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY => Axes.RightY,
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER => Axes.LeftTrigger,
		SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER => Axes.RightTrigger,
		_ => Axes.None,
	};

	private static Keys GetKeyFromSDL(SDL_Scancode scancode) => scancode switch
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
		SDL_Scancode.SDL_SCANCODE_BACKSPACE => Keys.Backspace,
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
