namespace Foster.Framework;

/// <summary>
/// Code and reflection data used to create a new Shader Program
/// </summary>
/// <param name="Code">
/// The Shader Code for the given program.<br/>
/// <br/>
/// The Provided <see cref="Code"/> must match the <see cref="GraphicsDriver"/>
/// in use, which can be checked with <see cref="Renderer.Driver"/>.<br/>
/// <br/>
/// Shaders must match SDL_GPU Shader resource binding rules:
/// https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks
/// </param>
/// <param name="SamplerCount">The number of Samplers</param>
/// <param name="Uniforms">A list of Uniforms used by the Shader</param>
/// <param name="EntryPoint">The Shader's Entry Point</param>
public readonly record struct ShaderProgramInfo(
	byte[] Code,
	int SamplerCount,
	ShaderUniform[] Uniforms,
	string EntryPoint = "main"
);