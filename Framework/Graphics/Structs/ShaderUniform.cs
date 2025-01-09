namespace Foster.Framework;

/// <summary>
/// Holds information on an individual Shader Uniform
/// </summary>
public readonly record struct ShaderUniform(
	string Name,
	UniformType Type,
	int ArrayElements = 1
);