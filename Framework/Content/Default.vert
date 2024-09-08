#version 450

layout (location = 0) in vec2 inPosition;
layout (location = 1) in vec2 inTexCoord;
layout (location = 2) in vec4 inColor;
layout (location = 3) in vec4 inType;

layout (location = 0) out vec2 outTexCoord;
layout (location = 1) out vec4 outColor;
layout (location = 2) out vec4 outType;

layout (binding = 0) uniform UniformBlock
{
	mat4 Matrix;
};

void main(void)
{
	outTexCoord = inTexCoord;
	outColor = inColor;
	outType = inType;
	gl_Position = Matrix * vec4(inPosition.xy, 0, 1);
}