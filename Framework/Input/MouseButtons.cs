namespace Foster.Framework;

/// <summary>
/// Mouse Buttons
/// </summary>
public enum MouseButtons
{
	None = 0,
	Left = 1,
	Middle = 2,
	Right = 3
}

public static class MouseButtonsExt
{
	public static IEnumerable<MouseButtons> All
	{
		get
		{
			yield return MouseButtons.Left;
			yield return MouseButtons.Middle;
			yield return MouseButtons.Right;
		}
	}
}
