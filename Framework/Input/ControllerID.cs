namespace Foster.Framework;

/// <summary>
/// Unique ID per Controller.
/// Every time a Controller is connected/disconnected, it is given a new ID.
/// </summary>
public readonly record struct ControllerID(uint Value);