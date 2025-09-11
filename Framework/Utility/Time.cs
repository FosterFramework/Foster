
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
	/// The Time, in seconds, since the previous Update
	/// </summary>
	public readonly float Delta = (float)(Elapsed - Previous).TotalSeconds;

	/// <summary>
	/// The Time, in a TimeSpan format, since the previous Update
	/// </summary>
	public readonly TimeSpan DeltaTimeSpan => Elapsed - Previous;

	/// <summary>
	/// Total time in Seconds (shorthand to Elapsed.TotalSeconds)
	/// </summary>
	public double Seconds => Elapsed.TotalSeconds;

	/// <summary>
	/// Advances <see cref="Elapsed"/> by the given delta value, increments <see cref="Frame"/> and assigns <see cref="Delta"/>.<br/>
	/// This does not advance <see cref="RenderFrame"/>.
	/// </summary>
	/// <returns>The new Time struct</returns>
	public Time Advance(TimeSpan delta) => new(
		Elapsed + delta,
		Elapsed,
		Frame + 1,
		RenderFrame
	);

	/// <summary>
	/// Advances the Render Frame
	/// </summary>
	/// <returns>The new Time struct</returns>
	public Time AdvanceRenderFrame()
		=> this with { RenderFrame = RenderFrame + 1 };

	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public bool OnInterval(double interval, double offset = 0.0)
		=> OnInterval(Elapsed.TotalSeconds, Delta, interval, offset);

	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(in Time time, double interval, double offset)
		=> OnInterval(time.Elapsed.TotalSeconds, time.Delta, interval, offset);

	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(double time, double delta, double interval, double offset)
		=> Math.Floor((time - offset - delta) / interval) < Math.Floor((time - offset) / interval);

	/// <summary>
	/// Returns true when the elapsed time is between the given interval. Ex: an interval of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	public bool BetweenInterval(double interval, double offset = 0.0)
		=> BetweenInterval(Elapsed.TotalSeconds, interval, offset);

	/// <summary>
	/// Returns true when the elapsed time is between the given interval. Ex: an interval of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	public static bool BetweenInterval(double time, double interval, double offset)
		=> (time - offset) % (interval * 2) >= interval;

	/// <summary>
	/// Sine-wave a value between `from` and `to` with a period of `duration`.
	/// You can use `offsetPercent` to offset the sine wave.
	/// </summary>
	/// <param name="from">Sine wave from</param>
	/// <param name="to">Sine wave to</param>
	/// <param name="duration">Duration, in seconds, of the period of the SineWave</param>
	/// <param name="offsetPercent">Offset time by a percentage of the Duration</param>
	public float SineWave(float from, float to, float duration, float offsetPercent = 0)
	{
		var dur = TimeSpan.FromSeconds(duration);
		var input = (Elapsed + dur * offsetPercent).Modulo(dur).TotalSeconds / duration;
		return Calc.ClampedMap((float)Math.Sin(input * MathF.Tau), -1, 1, from, to);
	}
}
