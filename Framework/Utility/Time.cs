
using System.Runtime.CompilerServices;

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
	public TimeSpan DeltaTimeSpan => Elapsed - Previous;

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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Time AdvanceRenderFrame()
		=> this with { RenderFrame = RenderFrame + 1 };

	/// <summary>
	/// Returns true when the <see cref="Time"/> passes a given <paramref name="interval"/>. Ex: with an <paramref name="interval"/> of 0.1, this will be true for one frame every 0.1 seconds
	/// </summary>
	/// <param name="interval">Interval to check whether we've crossed</param>
	/// <param name="offset">Offset to the interval (so we can, in effect, start partway through an interval)</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool OnInterval(double interval, double offset = 0.0)
		=> Calc.OnInterval(Elapsed.TotalSeconds, Delta, interval, offset);

	/// <summary>
	/// Returns true when the <see cref="Time"/> is between the given <paramref name="interval"/>. Ex: an <paramref name="interval"/> of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	/// <param name="interval">Interval to check whether we're between</param>
	/// <param name="offset">Offset to the interval (so we can, in effect, start partway through an interval)</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool BetweenInterval(double interval, double offset = 0)
		=> Calc.BetweenInterval(Elapsed.TotalSeconds, interval, offset);

	/// <summary>
	/// Returns true when the <see cref="Time"/> is between the given <paramref name="falseInterval"/> and <paramref name="trueInterval"/>. Ex: with a <paramref name="falseInterval"/> of 0.1 and <paramref name="trueInterval"/> of 0.2, this will be false for 0.1 seconds, then true for 0.2 seconds, and then repeat.
	/// </summary>
	/// <param name="falseInterval">Time to be false for</param>
	/// <param name="trueInterval">Time to be true for</param>
	/// <param name="offset">Offset to the interval (so we can, in effect, start partway through an interval)</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool BetweenInterval(double falseInterval, double trueInterval, double offset)
		=> Calc.BetweenInterval(Elapsed.TotalSeconds, falseInterval, trueInterval, offset);

	/// <summary>
	/// Sine-wave a value between `from` and `to` with a period of `duration`.
	/// You can use `offsetPercent` to offset the sine wave.
	/// </summary>
	/// <param name="from">Sine wave from</param>
	/// <param name="to">Sine wave to</param>
	/// <param name="duration">Duration, in seconds, of the period of the SineWave</param>
	/// <param name="offsetPercent">Offset time by a percentage of the Duration</param>
	public float SineWave(float from, float to, double duration, float offsetPercent = 0)
	{
		var dur = TimeSpan.FromSeconds(duration);
		var input = (Elapsed + dur * offsetPercent).Modulo(dur).TotalSeconds / duration;
		return Calc.ClampedMap((float)Math.Sin(input * MathF.Tau), -1, 1, from, to);
	}

	/// <summary>
	/// Get this <see cref="Time"/> struct with our delta time multiplied by a scalar value.
	/// This can be useful for features such as slowing down or speeding up time.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Time MultiplyDelta(double multiplier)
		=> new(Previous + DeltaTimeSpan * multiplier, Previous, Frame, RenderFrame);
}
