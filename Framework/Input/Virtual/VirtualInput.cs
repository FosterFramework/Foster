namespace Foster.Framework;

/// <summary>
/// A virtual input
/// </summary>
public abstract class VirtualInput : IDisposable
{
	public readonly Input Input;
	public readonly string Name;

	public bool IsDisposed { get; private set; }

	internal VirtualInput(Input input, string name)
	{
		Input = input;
		Name = name;
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
	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (!IsDisposed)
			Input.RemoveVirtualInput(this);

		IsDisposed = true;
	}
}
