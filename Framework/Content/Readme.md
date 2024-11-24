### Cross-Compiling Shaders
Shaders are generated using [SDL's shadercross](https://github.com/libsdl-org/SDL_shadercross) tool to generate SPIR-V/DXIL/MSL shaders, and [SPIRV-cross](https://github.com/KhronosGroup/SPIRV-Cross) to generate GLSL shaders.

An example using a bash script to compile the shaders here, using the tools above.
```bash
compile() {
	local input=$1
	local stage=$2
	local outdir=$3
	local filename="$(basename -- "$input")"
	local filename="${filename%.*}.$stage"
	echo "Compiling '$1' $stage stage ..."
	./shadercross "$input" -e "${stage}_main" -t $stage -s HLSL -o "$outdir/$filename.spv"
	./shadercross "$outdir/$filename.spv" -e "${stage}_main" -t $stage -s SPIRV -o "$outdir/$filename.msl"
	./shadercross "$outdir/$filename.spv" -e "${stage}_main" -t $stage -s SPIRV -o "$outdir/$filename.dxil"
	./spirv-cross "$outdir/$filename.spv" --output "$outdir/$filename.glsl" --version 450 --glsl-emit-ubo-as-plain-uniforms --glsl-force-flattened-io-blocks
}

for file in $1/*.hlsl; do
	compile "$file" vertex $1/Compiled
	compile "$file" fragment $1/Compiled
done
```
For example:
```bash
bash ./compile.sh "Foster/Framework/Content"
```