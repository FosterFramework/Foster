namespace Foster.Framework;

/// <summary>
/// Combination of a Texture and Sampler used during <see cref="DrawCommand"/> and <see cref="ComputeCommand"/> 
/// </summary>
public readonly record struct BoundSampler(Texture? Texture, TextureSampler Sampler);