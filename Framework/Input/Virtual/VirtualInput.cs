namespace Foster.Framework;

/// <summary>
/// A virtual input
/// </summary>
public abstract class VirtualInput : IDisposable
{
	/// <summary>
	/// The Input Manager we belong to
	/// </summary>
	public readonly Input Input;

	/// <summary>
	/// Name of the virtual Input
	/// </summary>
	public readonly string Name;

	/// <summary>
	/// Which Controller Index we are subscribed to
	/// </summary>
	public abstract int ControllerIndex { get; set; }

	public bool IsDisposed { get; private set; }

	internal VirtualInput(Input input, string name, int controllerIndex)
	{
		Input = input;
		Name = name;
		ControllerIndex = controllerIndex;
		Input.AddVirtualInput(this);
	}

	/// <summary>
	/// Updates the Virtual Input values
	/// </summary>
	internal abstract void Update(in Time time);

	/// <summary>
	/// Disposes of the Virtual Input.
	/// Once disposed, no values will be updated.
	/// </summary>
	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);

		if (!IsDisposed)
			Input.RemoveVirtualInput(this);

		IsDisposed = true;
	}
}
