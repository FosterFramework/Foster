namespace Foster.Framework;

public static class TimeSpanExt
{
	/// <summary>
	/// Performs a modulo operation on the timespan in seconds, returning the result
	/// </summary>
	public static TimeSpan Modulo(this TimeSpan timespan, float modInSeconds)
		=> TimeSpan.FromTicks(timespan.Ticks % TimeSpan.FromSeconds(modInSeconds).Ticks);

	/// <summary>
	/// Performs a modulo operation on the timespan, returning the result
	/// </summary>
	public static TimeSpan Modulo(this TimeSpan timespan, TimeSpan mod)
		=> TimeSpan.FromTicks(timespan.Ticks % mod.Ticks);
}