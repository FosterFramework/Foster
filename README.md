# Foster
Foster is cross-platform game framework made in C#.

_★ very work in progress! likely to have frequent, breaking changes! please use at your own risk! ★_

## what's here
 - **Framework**: The main Foster library used for creating a Window, handling Input, and Drawing.
 - **Platform**: A small C library used to handle native platform implementations, which in turn uses SDL2.

## dependencies
 - [dotnet 7.0](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) and [C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11)
 - [SDL2](https://github.com/libsdl-org/sdl) is the only external dependency, which is required by the Platform library. By default this is statically compiled.

## rendering
 - implemented in OpenGL for Linux/Mac/Windows and D3D11 for Windows.
 - separate shaders are required depending on which rendering API you're targetting.
 - when [SDL3 GPU](https://github.com/libsdl-org/SDL_shader_tools/blob/main/docs/README-SDL_gpu.md) is finished, replace the custom OpenGL and D3D11 rendering implementations with that. This will likely require breaking changes to the C# Shader implementation.

## inspiration
Taken a lot of inspiration from other Frameworks and APIs, namely [FNA](https://fna-xna.github.io/).

## notes
 - This is the second iteration of this library. The first can be found here.
 - 
