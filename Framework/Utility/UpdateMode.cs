namespace Foster.Framework;

/// <summary>
/// Stores information about the current Update Mode
/// </summary>
/// <param name="Mode">The type of Update to perform</param>
/// <param name="FixedTargetTime">The target time per frame for a Fixed Update.</param>
/// <param name="FixedMaxTime">The maximum amount of time a Fixed Update is allowed to take before the Application starts dropping frames.</param>
/// <param name="FixedWaitEnabled">
/// 	This will force the main thread to wait until another Fixed update is ready.
/// 	This uses less CPU but means that your render loop can not run any faster than your fixed update rate.
/// </param>
public readonly record struct UpdateMode(
	UpdateMode.Modes Mode,
	TimeSpan FixedTargetTime,
	TimeSpan FixedMaxTime,
	bool FixedWaitEnabled
)
{
	/// <summary>
	/// How the Application will run its update loop
	/// </summary>
	public enum Modes
	{
		/// <summary>
		/// The Application will update on a fixed time
		/// </summary>
		Fixed,
		
		/// <summary>
		/// How the Application will update as fast as it can
		/// </summary>
		Unlocked,
	}

	/// <summary>
	/// The Update loop will run at a fixed rate.
	/// </summary>
	/// <param name="targetTimePerFrame">The target time per frame</param>
	/// <param name="maxTimePerFrame">The maximum time allowed per frame before the Application starts dropping updates to avoid a spiral of death.</param>
	/// <param name="waitForNextUpdate">The thread will wait for the next fixed update. This will also lock your render to the update rate, but will use less CPU.</param>
	public static UpdateMode FixedStep(
		TimeSpan targetTimePerFrame,
		TimeSpan? maxTimePerFrame = null,
		bool waitForNextUpdate = true)
	{
		return new(
			Modes.Fixed,
			targetTimePerFrame,
			maxTimePerFrame ?? (targetTimePerFrame * 5),
			waitForNextUpdate
		);
	}

	/// <summary>
	/// The Update loop will run at a fixed rate.
	/// </summary>
	/// <param name="fps">The target frames per second</param>
	/// <param name="waitForNextUpdate">The thread will wait for the next fixed update. This will also lock your render to the update rate, but will use less CPU.</param>
	public static UpdateMode FixedStep(int fps, bool waitForNextUpdate = true)
		=> FixedStep(TimeSpan.FromSeconds(1.0f / fps), null, waitForNextUpdate);

	/// <summary>
	/// The Update loop will run as fast as it can.
	/// This means there will always be one Update per Render.
	/// </summary>
	public static UpdateMode UnlockedStep()
	{
		return new(
			Modes.Unlocked,
			TimeSpan.Zero,
			TimeSpan.Zero,
			false
		);
	}
}