#version 450

struct type_UniformBlock
{
    mat4 Matrix;
};

uniform type_UniformBlock UniformBlock;

layout(location = 0) in vec2 in_var_TEXCOORD0;
layout(location = 1) in vec2 in_var_TEXCOORD1;
layout(location = 2) in vec4 in_var_TEXCOORD2;
layout(location = 0) out vec2 out_var_TEXCOORD0;
layout(location = 1) out vec4 out_var_TEXCOORD1;

mat4 spvWorkaroundRowMajor(mat4 wrap) { return wrap; }

void main()
{
    out_var_TEXCOORD0 = in_var_TEXCOORD1;
    out_var_TEXCOORD1 = in_var_TEXCOORD2;
    gl_Position = vec4(in_var_TEXCOORD0, 0.0, 1.0) * spvWorkaroundRowMajor(UniformBlock.Matrix);
}

