<p align="center">
<img width="480" src="Foster.png" alt="Foster logo">
</p>

# Foster
Foster is cross-platform game framework made in C#.

_★ very work in progress! likely to have frequent, breaking changes! please use at your own risk! ★_

## what's here
 - **Framework**: The main Foster library used for creating a Window, handling Input, and Drawing.
 - **Platform**: A small C library used to handle native platform implementations, which in turn uses SDL2.

## dependencies
 - [dotnet 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) and [C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11)
 - [SDL2](https://github.com/libsdl-org/sdl) is the only external dependency, which is required by the [Platform library](https://github.com/NoelFB/Foster/tree/main/Platform). By default this is statically compiled.

## platform library
 - The Platform library is a simple C library that implements native methods required to run the application.
 - By defauly it is currently built for 64-bit Linux and Windows
 - To add support for more platforms, you need to build the [Platform library](https://github.com/NoelFB/Foster/tree/main/Platform) and then include it in [Foster.Framework.csproj](https://github.com/NoelFB/Foster/blob/main/Framework/Foster.Framework.csproj#L27)

## rendering
 - implemented in OpenGL for Linux/Mac/Windows and D3D11 for Windows.
 - separate shaders are required depending on which rendering API you're targetting.
 - planning to replace the rendering implementation with [SDL3 GPU when it is complete](https://github.com/NoelFB/Foster2023/issues/1).

## notes
 - Taken a lot of inspiration from other Frameworks and APIs, namely [FNA](https://fna-xna.github.io/).
 - This is the second iteration of this library. The first [can be found here](https://github.com/noelfb/fosterold).
