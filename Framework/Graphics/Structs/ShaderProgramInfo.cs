namespace Foster.Framework;

/// <summary>
/// Reflection Data used to create a new Shader Program
/// </summary>
public readonly record struct ShaderProgramInfo(
	byte[] Code,
	int SamplerCount,
	ShaderUniform[] Uniforms,
	string EntryPoint = "main"
);