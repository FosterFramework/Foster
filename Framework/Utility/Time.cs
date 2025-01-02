
namespace Foster.Framework;

/// <summary>
/// Application Time state management
/// </summary>
/// <param name="Elapsed">Accumulation of Delta Time since the Application Started</param>
/// <param name="Previous">The Previous Elapsed Time value</param>
/// <param name="Frame">The total number of update frames since the Application Started</param>
/// <param name="RenderFrame">The total number of render frames since the Application Started</param>
public readonly record struct Time(
	TimeSpan Elapsed,
	TimeSpan Previous,
	ulong Frame,
	ulong RenderFrame
)
{
	/// <summary>
	/// Time, in seconds, since the previous Update
	/// </summary>
	public readonly float Delta = (float)(Elapsed - Previous).TotalSeconds;

	/// <summary>
	/// Advances <see cref="Elapsed"/> by the given delta value, increments <see cref="Frame"/> and assigns <see cref="Delta"/>.<br/>
	/// This does not advance <see cref="RenderFrame"/>. 
	/// </summary>
	/// <returns>The new Time struct</returns>
	public readonly Time Advance(TimeSpan delta)
	{
		return new Time(
			Elapsed + delta,
			Elapsed,
			Frame + 1,
			RenderFrame	
		);
	}

	/// <summary>
	/// Advances the Render Frame
	/// </summary>
	/// <returns>The new Time struct</returns>
	public readonly Time AdvanceRenderFrame()
	{
		return this with { RenderFrame = RenderFrame + 1 };
	}

	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public bool OnInterval(double interval, double offset = 0.0)
	{
		return OnInterval(Elapsed.TotalSeconds, Delta, interval, offset);
	}
	
	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(in Time time, double interval, double offset)
		=> OnInterval(time.Elapsed.TotalSeconds, time.Delta, interval, offset);
	
	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(double time, double delta, double interval, double offset)
	{
		return Math.Floor((time - offset - delta) / interval) < Math.Floor((time - offset) / interval);
	}

	/// <summary>
	/// Returns true when the elapsed time is between the given interval. Ex: an interval of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	public bool BetweenInterval(double interval, double offset = 0.0)
	{
		return BetweenInterval(Elapsed.TotalSeconds, interval, offset);
	}

	/// <summary>
	/// Returns true when the elapsed time is between the given interval. Ex: an interval of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	public static bool BetweenInterval(double time, double interval, double offset)
	{
		return (time - offset) % (interval * 2) >= interval;
	}

	/// <summary>
	/// Sine-wave a value between `from` and `to` with a period of `duration`.
	/// You can use `offsetPercent` to offset the sine wave.
	/// </summary>
	public float SineWave(float from, float to, float duration, float offsetPercent)
	{
		float total = (float)Elapsed.TotalSeconds;
		float range = (to - from) * 0.5f;
		return from + range + MathF.Sin(((total + duration * offsetPercent) / duration) * MathF.Tau) * range;
	}
}