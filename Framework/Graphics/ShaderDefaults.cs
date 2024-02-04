namespace Foster.Framework;

internal static class ShaderDefaults
{
	public static Dictionary<Renderers, ShaderCreateInfo> Batcher = new()
	{
		[Renderers.OpenGL] = new()
		{
			VertexShader =
				@"#version 330
				uniform mat4 u_matrix;
				layout(location=0) in vec2 a_position;
				layout(location=1) in vec2 a_tex;
				layout(location=2) in vec4 a_color;
				layout(location=3) in vec4 a_type;
				out vec2 v_tex;
				out vec4 v_col;
				out vec4 v_type;
				void main(void)
				{
					gl_Position = u_matrix * vec4(a_position.xy, 0, 1);
					v_tex = a_tex;
					v_col = a_color;
					v_type = a_type;
				}",
			FragmentShader =
				@"#version 330
				uniform sampler2D u_texture;
				in vec2 v_tex;
				in vec4 v_col;
				in vec4 v_type;
				out vec4 o_color;
				void main(void)
				{
					vec4 color = texture(u_texture, v_tex);
					o_color = 
						v_type.x * color * v_col + 
						v_type.y * color.a * v_col + 
						v_type.z * v_col;
				}"
		}
	};
}
