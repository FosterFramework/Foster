#version 450

layout (location = 0) in vec2 TexCoord;
layout (location = 1) in vec4 Color;
layout (location = 2) in vec4 Type;

layout (location = 0) out vec4 FragColor;

layout (binding = 0) uniform sampler2D Sampler;

void main(void)
{
	vec4 color = texture(Sampler, TexCoord);
	FragColor = 
		Type.x * color * Color + 
		Type.y * color.a * Color + 
		Type.z * Color;
}