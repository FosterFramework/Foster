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
			MouseButton((int)Platform.GetMouseFromSDL(ev.button.button), true, App.Time.Elapsed);
			break;
		case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
			MouseButton((int)Platform.GetMouseFromSDL(ev.button.button), false, App.Time.Elapsed);
			break;
		case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
			MouseWheel(new(ev.wheel.x, ev.wheel.y));
			break;

		// keyboard
		case SDL_EventType.SDL_EVENT_KEY_DOWN:
			if (!ev.key.repeat)
				Key((int)Platform.GetKeyFromSDL(ev.key.scancode), true, App.Time.Elapsed);
			break;
		case SDL_EventType.SDL_EVENT_KEY_UP:
			if (!ev.key.repeat)
				Key((int)Platform.GetKeyFromSDL(ev.key.scancode), false, App.Time.Elapsed);
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
					button: (int)Platform.GetButtonFromSDL((SDL_GamepadButton)ev.gbutton.button),
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
					axis: (int)Platform.GetAxisFromSDL((SDL_GamepadAxis)ev.gaxis.axis),
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
}
