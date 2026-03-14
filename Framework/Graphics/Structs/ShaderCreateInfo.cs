namespace Foster.Framework;

/// <summary>
/// Code and reflection data used to create a new Render Shader.
/// <br/>
/// The Provided <see cref="Code"/> must match the <see cref="GraphicsDriver"/>
/// in use, which can be checked with <see cref="GraphicsDevice.Driver"/>.<br/>
/// <br/>
/// Render Shaders must match SDL_GPU Shader resource binding rules:
/// https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks
/// </summary>
/// <param name="Code">The Shader Code for the given program.</param>
/// <param name="Stage">The stage this shader is built for</param>
/// <param name="SamplerCount">The number of Samplers</param>
/// <param name="UniformBufferCount">The number of Uniform Buffers</param>
/// <param name="StorageBufferCount">The number of Storage Buffers</param>
/// <param name="EntryPoint">The Shader's Entry Point</param>
[Obsolete("Use Shader Constructor Instead")]
public readonly record struct ShaderCreateInfo(
	ShaderStage Stage,
	byte[] Code,
	int SamplerCount = 0,
	int UniformBufferCount = 0,
	int StorageBufferCount = 0,
	string EntryPoint = "main"
);