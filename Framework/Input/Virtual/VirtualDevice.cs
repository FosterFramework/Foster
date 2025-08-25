using System.Collections.ObjectModel;

namespace Foster.Framework;

/// <summary>
/// A virtual device is a collection of <see cref="VirtualInput"/>'s that can be
/// managed together and easily swapped to different Controllers.
/// <br/><br/>
/// Note that VirtualInput's created within this Device will automatically have
/// their <see cref="VirtualInput.ControllerIndex"/> assigned, and should no longer
/// be manually set.
/// </summary>
public class VirtualDevice : VirtualInput, IDisposable
{
	/// <summary>
	/// How the Controller Index should be updated
	/// </summary>
	public enum IndexModes
	{
		/// <summary>
		/// Controller Index should be manually assigned
		/// </summary>
		Manual,

		/// <summary>
		/// Automatically switch the Controller Index to the most recently used controller
		/// </summary>
		AutomaticLatest
	}

	private int controllerIndex;
	private readonly List<VirtualInput> inputs = [];

	public override int ControllerIndex
	{
		get => controllerIndex;
		set
		{
			if (IndexMode == IndexModes.AutomaticLatest)
				throw new Exception("ControllerIndex is automatically being updated; switch to IndexModes.Manual");
			UpdateControllerIndex(value);
		}
	}

	/// <summary>
	/// How the Device should select a Controller Index
	/// </summary>
	public IndexModes IndexMode = IndexModes.Manual;

	/// <summary>
	/// Inputs registered to this device
	/// </summary>
	public readonly ReadOnlyCollection<VirtualInput> Inputs;

	public VirtualDevice(Input input, string name, int controllerIndex = 0)
		: base(input, name, controllerIndex)
	{
		Inputs = new(inputs);
	}

	/// <summary>
	/// Adds a Virtual Action to this Device
	/// </summary>
	public VirtualAction AddAction(string name, ActionBindingSet set, float buffer = 0)
	{
		if (IsDisposed)
			throw new Exception("Virtual Device is disposed");

		var action = new VirtualAction(Input, name, set, ControllerIndex, buffer);
		inputs.Add(action);
		return action;
	}

	/// <summary>
	/// Adds a Virtual Action to this Device
	/// </summary>
	public VirtualAction AddAction(string name, float buffer = 0)
	{
		if (IsDisposed)
			throw new Exception("Virtual Device is disposed");

		var action = new VirtualAction(Input, name, ControllerIndex, buffer);
		inputs.Add(action);
		return action;
	}

	/// <summary>
	/// Adds a Virtual Axis to this Device
	/// </summary>
	public VirtualAxis AddAxis(string name, AxisBindingSet set)
	{
		if (IsDisposed)
			throw new Exception("Virtual Device is disposed");

		var axis = new VirtualAxis(Input, name, set, ControllerIndex);
		inputs.Add(axis);
		return axis;
	}

	/// <summary>
	/// Adds a Virtual Axis to this Device
	/// </summary>
	public VirtualAxis AddAxis(string name)
	{
		if (IsDisposed)
			throw new Exception("Virtual Device is disposed");

		var axis = new VirtualAxis(Input, name, ControllerIndex);
		inputs.Add(axis);
		return axis;
	}

	/// <summary>
	/// Adds a Virtual Stick to this Device
	/// </summary>
	public VirtualStick AddStick(string name, StickBindingSet set)
	{
		if (IsDisposed)
			throw new Exception("Virtual Device is disposed");

		var stick = new VirtualStick(Input, name, set, ControllerIndex);
		inputs.Add(stick);
		return stick;
	}

	/// <summary>
	/// Adds a Virtual Stick to this Device
	/// </summary>
	public VirtualStick AddStick(string name)
	{
		if (IsDisposed)
			throw new Exception("Virtual Device is disposed");

		var stick = new VirtualStick(Input, name, ControllerIndex);
		inputs.Add(stick);
		return stick;
	}

	internal override void Update(in Time time)
	{
		if (IndexMode == IndexModes.AutomaticLatest)
		{
			var latest = 0;
			for (int i = 1; i < Input.Controllers.Length; i ++)
				if (Input.Controllers[i].IsGamepad && 
					Input.Controllers[i].InputTimestamp > Input.Controllers[latest].InputTimestamp)
					latest = i;
			UpdateControllerIndex(latest);
		}
	}

	/// <summary>
	/// called when the Controller Index is modified
	/// </summary>
	protected virtual void ControllerIndexChanged() {}

	public override void Dispose()
	{
		inputs.Clear();
		base.Dispose();
	}

	private void UpdateControllerIndex(int value)
	{
		if (controllerIndex == value)
			return;

		controllerIndex = value;
		foreach (var it in inputs)
			it.ControllerIndex = value;
		ControllerIndexChanged();
	}
}