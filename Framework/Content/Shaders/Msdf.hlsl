cbuffer VertexUniformBlock : register(b0, space1)
{
    float4x4 Matrix;
};

cbuffer FragmentUniformBlock : register(b0, space3)
{
    float DistanceRange;
};

Texture2D Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);

struct VsInput
{
    float2 Position : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 Color : TEXCOORD2;
    float4 Type : TEXCOORD4;
};

struct VsOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

VsOutput vertex_main(VsInput input)
{
    VsOutput output;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    output.Position = mul(Matrix, float4(input.Position, 0.0, 1.0));
    return output;
}

float median(float x, float y, float z)
{
	return max(min(x, y), min(max(x, y), z));
}

float4 fragment_main(VsOutput input) : SV_Target0
{
    // get msdf rgb
	float3 msd = Texture.Sample(Sampler, input.TexCoord).rgb;

    // get texture size
	float2 textureSize;
	Texture.GetDimensions(textureSize.x, textureSize.y);
	float2 size = (1.0f / fwidth(input.TexCoord));

    // calculate alpha based on distance
    float  dist  = median(msd.r, msd.g, msd.b);
	float2 unit  = DistanceRange / textureSize;
    float  value = max(0.5 * dot(unit, size), 1.0) * (dist - 0.5);
    float  alpha = clamp(value + 0.5f, 0, 1);
    return input.Color * alpha;
}
