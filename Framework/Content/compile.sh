compile() {
	local input=$1
	local stage=$2
	local outdir=$3
	local filename="$(basename -- "$input")"
	local filename="${filename%.*}.$stage"
	echo "Compiling '$1' $stage stage ..."
	shadercross "$input" -e "${stage}_main" -t $stage -s HLSL -o "$outdir/$filename.spv"
	shadercross "$outdir/$filename.spv" -e "${stage}_main" -t $stage -s SPIRV -o "$outdir/$filename.msl"
	shadercross "$outdir/$filename.spv" -e "${stage}_main" -t $stage -s SPIRV -o "$outdir/$filename.dxil"
}

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

for file in $SCRIPT_DIR/*.hlsl; do
	compile "$file" vertex $SCRIPT_DIR/Compiled
	compile "$file" fragment $SCRIPT_DIR/Compiled
done