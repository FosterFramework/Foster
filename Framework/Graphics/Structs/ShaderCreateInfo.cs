namespace Foster.Framework;

/// <summary>
/// Code and reflection data used to create a new Shader Program
/// </summary>
/// <param name="Code">
/// The Shader Code for the given program.<br/>
/// <br/>
/// The Provided <see cref="Code"/> must match the <see cref="GraphicsDriver"/>
/// in use, which can be checked with <see cref="GraphicsDevice.Driver"/>.<br/>
/// <br/>
/// Shaders must match SDL_GPU Shader resource binding rules:
/// https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks
/// </param>
/// <param name="Stage">The stage this shader is built for</param>
/// <param name="SamplerCount">The number of Samplers</param>
/// <param name="UniformBufferCount">The number of Uniform Buffers</param>
/// <param name="StorageBufferCount">The number of Storage Buffers</param>
/// <param name="EntryPoint">The Shader's Entry Point</param>
public readonly record struct ShaderCreateInfo(
	ShaderStage Stage,
	byte[] Code,
	int SamplerCount = 0,
	int UniformBufferCount = 0,
	int StorageBufferCount = 0,
	string EntryPoint = "main"
);