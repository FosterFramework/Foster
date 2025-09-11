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

	/// <summary>
	/// Returns a <see cref="MathF.Sin"/> value using TimeSpan by gracefully
	/// wrapping the input value by <see cref="MathF.Tau"/> at the given <paramref name="rate"/>
	/// </summary>
	public static float Sin(this TimeSpan timespan, float rate = 1, float offset = 0)
		=> MathF.Sin((float)(timespan * rate + TimeSpan.FromSeconds(offset)).Modulo(MathF.Tau).TotalSeconds);

	/// <summary>
	/// Returns a <see cref="MathF.Cos"/> value using TimeSpan by gracefully
	/// wrapping the input value by <see cref="MathF.Tau"/> at the given <paramref name="rate"/>
	/// </summary>
	public static float Cos(this TimeSpan timespan, float rate = 1, float offset = 0)
		=> MathF.Cos((float)(timespan * rate + TimeSpan.FromSeconds(offset)).Modulo(MathF.Tau).TotalSeconds);
}