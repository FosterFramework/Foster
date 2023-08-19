namespace Foster.Framework;

public abstract class Module
{
	public virtual void Startup() { }
	public virtual void Shutdown() { }
	public virtual void Update() { }
	public virtual void Render() { }
}