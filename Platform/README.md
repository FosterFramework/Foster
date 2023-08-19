The Platform library is a small C library that implements the native methods required to run an application, a small rendering API, as well as various utility methods that are easier to implement in C than in C# (such as image loading).

The repo already contains prebuilt binaries for Windows/Mac/Linux, but you can also build it yourself using CMake:
```sh
mkdir build
cd build
cmake ../
make
```
If built successfully, the library should appear in `libs/{yourPlatform}`, which is then used & copied from `Foster.csproj`.