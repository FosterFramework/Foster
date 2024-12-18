<p align="center">
<img width="480" src="Foster.png" alt="Foster logo">
</p>

# Foster
Foster is a small cross-platform 2D game framework in C#.

_★ very work in progress! likely to have frequent, breaking changes! please use at your own risk! ★_

To use the framework either 
 - add a refence to the [NuGet package](https://www.nuget.org/packages/FosterFramework), 
 - or clone this repository and add a reference to `Foster/Framework/Foster.Framework.csproj`.

There is a [Samples](https://github.com/FosterFramework/Samples) repo which contains various demos and examples that can help you get started.

Check out [Discussons](https://github.com/FosterFramework/Foster/discussions) or [Discord](https://discord.gg/K7tdFuP3Bg) to get involved.

### Dependencies
 - [dotnet 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) and [C# 13](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)
 - [SDL3](https://github.com/libsdl-org/sdl) is the only external dependency, which is built and dynamically included by the Platform library.

### Platform Library
 - The [Platform library](https://github.com/FosterFramework/Foster/tree/main/Platform) is a C library that implements native methods required to run the application.
 - It is automatically built for 64-bit Linux, MacOS, and Windows through [Github Actions](https://github.com/FosterFramework/Foster/actions/workflows/build-libs.yml).
 - To add support for other platforms you must build and include it in the [csproj](https://github.com/FosterFramework/Foster/blob/main/Framework/Foster.Framework.csproj#L27)

### Rendering
 - Rendering is implemented in both [SDL_GPU](https://wiki.libsdl.org/SDL3/CategoryGPU) and OpenGL.
 - Shaders provided to SDL_GPU must follow the [SDL_GPU shader resource requirements](https://wiki.libsdl.org/SDL3/SDL_CreateGPUShader#remarks).
 - You can specify which Renderer to use when you run your Application.
 - You must provide shaders for the resulting Renderer (ex. GLSL shaders for OpenGL, SPIR-V for Vulkan, etc). There are built-in shaders for generic 2D rendering so this only matters if you want to write custom shaders.

### Notes
 - Taken a lot of inspiration from other Frameworks and APIs, namely [FNA](https://fna-xna.github.io/).
 - This is the second iteration of this library. The first [can be found here](https://github.com/NoelFB/fosterold).
 - Contributions are welcome! However, anything that adds external dependencies or complicates the build process will not be accepted.
