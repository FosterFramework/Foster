The Platform library is a small C99 library that implements the native methods required to run an application. This includes a small rendering API, image and font loading, and window & input management.

The repo already contains prebuilt binaries for 64-bit Windows/Linux/macOS, but you can also build it yourself using CMake:
```sh
mkdir build
cd build
cmake ../
make
```
If built successfully, the library should appear in `libs/{yourPlatform}`, which is then used & copied from `Framework/Foster.Framework.csproj`.
