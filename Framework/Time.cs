namespace Foster.Framework;

public static class Time
{
	/// <summary>
	/// Time in Seconds since our last Update.
	/// </summary>
	public static float Delta;

	/// <summary>
	/// An Accumulation of the Delta Time, incremented each Update.
	/// </summary>
	public static TimeSpan Duration;

	/// <summary>
	/// Current frame index
	/// </summary>
	public static ulong Frame = 0;

	/// <summary>
	/// Requests the current time since the Application was started.
	/// </summary>
	public static TimeSpan Now => App.Now();

	public static bool FixedStep = true;

	public static TimeSpan FixedStepTarget = TimeSpan.FromSeconds(1.0f / 60.0f);

	public static TimeSpan FixedStepMaxElapsedTime = TimeSpan.FromSeconds(5.0f / 60.0f);


	public static void Advance(TimeSpan delta)
	{
		Delta = (float)delta.TotalSeconds;
		Duration += delta;
	}
	
	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(double time, double delta, double interval, double offset)
	{
		return Math.Floor((time - offset - delta) / interval) < Math.Floor((time - offset) / interval);
	}

	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(double delta, double interval, double offset)
	{
		return OnInterval(Duration.TotalSeconds, delta, interval, offset);
	}

	/// <summary>
	/// Returns true when the elapsed time passes a given interval based on the delta time
	/// </summary>
	public static bool OnInterval(double interval, double offset = 0.0)
	{
		return OnInterval(Duration.TotalSeconds, Delta, interval, offset);
	}

	/// <summary>
	/// Returns true when the elapsed time is between the given interval. Ex: an interval of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	public static bool BetweenInterval(double time, double interval, double offset)
	{
		return (time - offset) % (interval * 2) >= interval;
	}

	/// <summary>
	/// Returns true when the elapsed time is between the given interval. Ex: an interval of 0.1 will be false for 0.1 seconds, then true for 0.1 seconds, and then repeat.
	/// </summary>
	public static bool BetweenInterval(double interval, double offset = 0.0)
	{
		return BetweenInterval(Duration.TotalSeconds, interval, offset);
	}

	/// <summary>
	/// Sine-wave a value between `from` and `to` with a period of `duration`.
	/// You can use `offsetPercent` to offset the sine wave.
	/// </summary>
	public static float SineWave(float from, float to, float duration, float offsetPercent)
	{
		float total = (float)Duration.TotalSeconds;
		float range = (to - from) * 0.5f;
		return from + range + MathF.Sin(((total + duration * offsetPercent) / duration) * MathF.Tau) * range;
	}
}