The Platform library is a small C99 utility library that implements native methods required to run an application, as well as compiling and dynamically linking SDL3.

The repo already contains many prebuilt binaries, but you can also build it yourself using CMake:
```sh
mkdir build
cd build
cmake ../
make
```
If built successfully, the library should appear in `libs/{YourPlatform}`, which is then used & copied from `Framework/Foster.Framework.csproj`.
