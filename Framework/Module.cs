namespace Foster.Framework;

public abstract class Module
{
	/// <summary>
	/// Called when the Application is starting up, or when the 
	/// Module was Registered if the Application is already running.
	/// </summary>
	public virtual void Startup() { }

	/// <summary>
	/// Called then the Application is shutting down
	/// </summary>
	public virtual void Shutdown() { }

	/// <summary>
	/// Called once per frame when the Application updates
	/// </summary>
	public virtual void Update() { }

	/// <summary>
	/// Called whenever the Application renders
	/// </summary>
	public virtual void Render() { }
}