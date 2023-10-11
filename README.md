<p align="center">
<img width="480" src="Foster.png" alt="Foster logo">
</p>

# Foster
Foster is small cross-platform 2D game framework in C#.

_★ very work in progress! likely to have frequent, breaking changes! please use at your own risk! ★_

To use the framework either 
 - add a refence to the [NuGet package](https://www.nuget.org/packages/FosterFramework), 
 - or clone this repository and add a reference to `Foster/Framework/Foster.Framework.csproj`.

There is a [Samples](https://github.com/NoelFB/Foster-Samples) repo which contains various demos and examples which can help you get started.

Check out [Discussons](https://github.com/NoelFB/Foster/discussions) or [Discord](https://discord.gg/K7tdFuP3Bg) to get involved.

### Dependencies
 - [dotnet 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) and [C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11)
 - [SDL2](https://github.com/libsdl-org/sdl) is the only external dependency, which is required by the [Platform library](https://github.com/NoelFB/Foster/tree/main/Platform). By default this is statically compiled.

### Platform Library
 - The [Platform library](https://github.com/NoelFB/Foster/tree/main/Platform) is a C library that implements native methods required to run the application.
 - By default it is currently built for 64-bit Linux, MacOS, and Windows through [Github Actions](https://github.com/NoelFB/Foster/actions/workflows/build-libs.yml).
 - To add support for more platforms, you need to build the [Platform library](https://github.com/NoelFB/Foster/tree/main/Platform) and then include it in [Foster.Framework.csproj](https://github.com/NoelFB/Foster/blob/main/Framework/Foster.Framework.csproj#L27)

### Rendering
 - Implemented in OpenGL for Linux/Mac/Windows and D3D11 for Windows.
 - Separate Shaders are required depending on which rendering API you're targetting.
 - Planning to replace the rendering implementation with [SDL3 GPU when it is complete](https://github.com/NoelFB/Foster2023/issues/1).

### Notes
 - Taken a lot of inspiration from other Frameworks and APIs, namely [FNA](https://fna-xna.github.io/).
 - This is the second iteration of this library. The first [can be found here](https://github.com/noelfb/fosterold).
 - Contributions are welcome! However, anything that adds external dependencies or complicates the build process will not be accepted.
