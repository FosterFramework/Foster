namespace Foster.Framework;

/// <summary>
/// Data Required to create a new Shader
/// </summary>
public readonly record struct ShaderCreateInfo(
	ShaderProgramInfo Vertex, 
	ShaderProgramInfo Fragment
);