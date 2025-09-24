namespace Foster.Framework;

public static class TimeSpanExt
{
	/// <summary>
	/// Performs a modulo operation on the timespan in seconds, returning the result
	/// </summary>
	public static TimeSpan Modulo(this TimeSpan timespan, double modInSeconds)
		=> TimeSpan.FromTicks(timespan.Ticks % TimeSpan.FromSeconds(modInSeconds).Ticks);

	/// <summary>
	/// Performs a modulo operation on the timespan, returning the result
	/// </summary>
	public static TimeSpan Modulo(this TimeSpan timespan, TimeSpan mod)
		=> TimeSpan.FromTicks(timespan.Ticks % mod.Ticks);

	/// <summary>
	/// Return a value that moves from 0 to 1 over the duration, then snaps back to zero and repeats
	/// </summary>
	public static float Lerp(this TimeSpan timespan, double durationInSeconds)
		=> (float)(timespan.Modulo(durationInSeconds) / TimeSpan.FromSeconds(durationInSeconds));

	/// <summary>
	/// Return a value that moves from 0 to 1 and back to 0 over the duration, then repeats
	/// </summary>
	/// <param name="timespan">The timespan to operate on</param>
	/// <param name="durationInSeconds">Time in seconds for the entire sequence 0 -> 1 -> 0 to play out</param>
	/// <param name="increasing">True while the value is moving up from 0 to 1, false while moving down from 1 to 0</param>
	public static float Yoyo(this TimeSpan timespan, double durationInSeconds, out bool increasing)
	{
		var t = timespan.Modulo(durationInSeconds) / TimeSpan.FromSeconds(durationInSeconds);
		t *= 2;
		if (t > 1)
		{
			increasing = false;
			return (float)(1 - (t - 1));
		}
		else
		{
			increasing = true;
			return (float)t;
		}
	}

	/// <summary>
	/// Return a value that moves from 0 to 1 and back to 0 over the duration, then repeats
	/// </summary>
	/// <param name="timespan">The timespan to operate on</param>
	/// <param name="durationInSeconds">Time in seconds for the entire sequence 0 -> 1 -> 0 to play out</param>
	public static float Yoyo(this TimeSpan timespan, double durationInSeconds)
		=> timespan.Yoyo(durationInSeconds, out _);

	/// <summary>
	/// Returns a <see cref="MathF.Sin"/> value using TimeSpan by gracefully
	/// wrapping the input value by <see cref="MathF.Tau"/> at the given <paramref name="rate"/>
	/// </summary>
	public static float Sin(this TimeSpan timespan, double rate = 1, double offset = 0)
		=> MathF.Sin((float)(timespan * rate + TimeSpan.FromSeconds(offset)).Modulo(MathF.Tau).TotalSeconds);

	/// <summary>
	/// Returns a <see cref="MathF.Cos"/> value using TimeSpan by gracefully
	/// wrapping the input value by <see cref="MathF.Tau"/> at the given <paramref name="rate"/>
	/// </summary>
	public static float Cos(this TimeSpan timespan, double rate = 1, double offset = 0)
		=> MathF.Cos((float)(timespan * rate + TimeSpan.FromSeconds(offset)).Modulo(MathF.Tau).TotalSeconds);
}
