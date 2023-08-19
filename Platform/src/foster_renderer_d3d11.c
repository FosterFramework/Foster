#if 0 && defined(FOSTER_D3D11_ENABLED)

// TODO:
// Note the D3D11 Implementation is still a work-in-progress

#include "blah_renderer.h"
#include "blah_internal.h"
#include "blah_platform.h"
#include <blah_common.h>
#include <cstdio>
#include <cstring>
#include <cstddef>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>

// shorthand to our internal state
#define RENDERER ((Renderer_D3D11*)Internal::app_renderer())

namespace Blah
{
	const char* d3d11_batch_shader = ""
		"cbuffer constants : register(b0)\n"
		"{\n"
		"	row_major float4x4 u_matrix;\n"
		"}\n"

		"struct vs_in\n"
		"{\n"
		"	float2 position : POS;\n"
		"	float2 texcoord : TEX;\n"
		"	float4 color : COL;\n"
		"	float4 mask : MASK;\n"
		"};\n"

		"struct vs_out\n"
		"{\n"
		"	float4 position : SV_POSITION;\n"
		"	float2 texcoord : TEX;\n"
		"	float4 color : COL;\n"
		"	float4 mask : MASK;\n"
		"};\n"

		"Texture2D    u_texture : register(t0);\n"
		"SamplerState u_texture_sampler : register(s0);\n"

		"vs_out vs_main(vs_in input)\n"
		"{\n"
		"	vs_out output;\n"

		"	output.position = mul(float4(input.position, 0.0f, 1.0f), u_matrix);\n"
		"	output.texcoord = input.texcoord;\n"
		"	output.color = input.color;\n"
		"	output.mask = input.mask;\n"

		"	return output;\n"
		"}\n"

		"float4 ps_main(vs_out input) : SV_TARGET\n"
		"{\n"
		"	float4 color = u_texture.Sample(u_texture_sampler, input.texcoord);\n"
		"	return\n"
		"		input.mask.x * color * input.color + \n"
		"		input.mask.y * color.a * input.color + \n"
		"		input.mask.z * input.color;\n"
		"}\n";

	const ShaderData d3d11_batch_shader_data = {
		d3d11_batch_shader,
		d3d11_batch_shader,
		{
			{ "POS", 0 },
			{ "TEX", 0 },
			{ "COL", 0 },
			{ "MASK", 0 },
		}
	};

	class D3D11_Shader;

	class Renderer_D3D11 : public Renderer
	{
	public:
		// main resources
		ID3D11Device* device = nullptr;
		ID3D11DeviceContext* context = nullptr;
		IDXGISwapChain* swap_chain = nullptr;
		ID3D11RenderTargetView* backbuffer_view = nullptr;
		ID3D11DepthStencilView* backbuffer_depth_view = nullptr;

		// last backbuffer size
		Point drawable_size;
		Point last_window_size;

		struct StoredInputLayout
		{
			u32 shader_hash;
			VertexFormat format;
			ID3D11InputLayout* layout;
		};

		struct StoredBlendState
		{
			BlendMode blend;
			ID3D11BlendState* state;
		};

		struct StoredRasterizer
		{
			Cull cull;
			bool has_scissor;
			ID3D11RasterizerState* state;
		};

		struct StoredSampler
		{
			TextureSampler sampler;
			ID3D11SamplerState* state;
		};

		struct StoredDepthStencil
		{
			Compare depth;
			ID3D11DepthStencilState* state;
		};

		Vector<StoredInputLayout> layout_cache;
		Vector<StoredBlendState> blend_cache;
		Vector<StoredRasterizer> rasterizer_cache;
		Vector<StoredSampler> sampler_cache;
		Vector<StoredDepthStencil> depthstencil_cache;

		bool init() override;
		void shutdown() override;
		void update() override;
		void before_render() override;
		void after_render() override;
		bool get_draw_size(int* w, int* h) override;
		void render(const DrawCall& pass) override;
		void clear_backbuffer(Color color, float depth, u8 stencil, ClearMask mask) override;
		TextureRef create_texture(int width, int height, TextureFormat format) override;
		TargetRef create_target(int width, int height, const TextureFormat* attachments, int attachment_count) override;
		ShaderRef create_shader(const ShaderData* data) override;
		MeshRef create_mesh() override;

		ID3D11InputLayout* get_layout(D3D11_Shader* shader, const VertexFormat& format);
		ID3D11BlendState* get_blend(const BlendMode& blend);
		ID3D11RasterizerState* get_rasterizer(const DrawCall& pass);
		ID3D11SamplerState* get_sampler(const TextureSampler& sampler);
		ID3D11DepthStencilState* get_depthstencil(const DrawCall& pass);
	};

	// Utility Methods
	D3D11_BLEND_OP blend_op(BlendOp op);
	D3D11_BLEND blend_factor(BlendFactor factor);
	bool reflect_uniforms(Vector<UniformInfo>& append_uniforms_to, Vector<ID3D11Buffer*>& append_buffers_to, ID3DBlob* shader, ShaderType shader_type);
	void apply_uniforms(D3D11_Shader* shader, const MaterialRef& material, ShaderType type);

	class D3D11_Texture : public Texture
	{
	private:
		int m_width;
		int m_height;
		TextureFormat m_format;
		DXGI_FORMAT m_dxgi_format;
		bool m_is_framebuffer;
		int m_size;

	public:
		ID3D11Texture2D* texture = nullptr;
		ID3D11Texture2D* staging = nullptr;
		ID3D11ShaderResourceView* view = nullptr;

		D3D11_Texture(int width, int height, TextureFormat format, bool is_framebuffer)
		{
			m_width = width;
			m_height = height;
			m_format = format;
			m_is_framebuffer = is_framebuffer;
			m_size = 0;

			D3D11_TEXTURE2D_DESC desc = { 0 };
			desc.Width = width;
			desc.Height = height;
			desc.MipLevels = 1;
			desc.ArraySize = 1;
			desc.SampleDesc.Count = 1;
			desc.SampleDesc.Quality = 0;
			desc.Usage = D3D11_USAGE_DEFAULT;
			desc.CPUAccessFlags = 0;
			desc.MiscFlags = 0;

			bool is_depth_stencil = false;

			switch (format)
			{
			case TextureFormat::R:
				desc.Format = DXGI_FORMAT_R8_UNORM;
				m_size = width * height;
				break;
			case TextureFormat::RG:
				desc.Format = DXGI_FORMAT_R8G8_UNORM;
				m_size = width * height * 2;
				break;
			case TextureFormat::RGBA:
				desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
				m_size = width * height * 4;
				break;
			case TextureFormat::DepthStencil:
				desc.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
				m_size = width * height * 4;
				is_depth_stencil = true;
				break;
			case TextureFormat::None:
			case TextureFormat::Count:
				break;
			}

			if (!is_depth_stencil)
				desc.BindFlags |= D3D11_BIND_SHADER_RESOURCE;
			else
				desc.BindFlags |= D3D11_BIND_DEPTH_STENCIL;

			if (is_framebuffer && !is_depth_stencil)
				desc.BindFlags |= D3D11_BIND_RENDER_TARGET;

			m_dxgi_format = desc.Format;

			auto hr = RENDERER->device->CreateTexture2D(&desc, NULL, &texture);
			if (!SUCCEEDED(hr))
			{
				if (texture)
					texture->Release();
				texture = nullptr;
				return;
			}

			if (!is_depth_stencil)
			{
				hr = RENDERER->device->CreateShaderResourceView(texture, NULL, &view);
				if (!SUCCEEDED(hr))
				{
					texture->Release();
					texture = nullptr;
				}
			}
		}

		~D3D11_Texture()
		{
			if (texture)
				texture->Release();
			texture = nullptr;
			if (staging)
				staging->Release();
			staging = nullptr;
			if (view)
				view->Release();
			view = nullptr;
		}

		int width() const override
		{
			return m_width;
		}

		int height() const override
		{
			return m_height;
		}

		TextureFormat format() const override
		{
			return m_format;
		}

		void set_data(const u8* data) override
		{
			// bounds
			D3D11_BOX box;
			box.left = 0;
			box.right = m_width;
			box.top = 0;
			box.bottom = m_height;
			box.front = 0;
			box.back = 1;

			// set data
			RENDERER->context->UpdateSubresource(
				texture,
				0,
				&box,
				data,
				m_size / m_height,
				0);
		}

		void get_data(u8* data) override
		{
			HRESULT hr;

			// create staging texture
			if (!staging)
			{
				D3D11_TEXTURE2D_DESC desc;
				desc.Width = m_width;
				desc.Height = m_height;
				desc.MipLevels = 1;
				desc.ArraySize = 1;
				desc.Format = m_dxgi_format;
				desc.SampleDesc.Count = 1;
				desc.SampleDesc.Quality = 0;
				desc.Usage = D3D11_USAGE_STAGING;
				desc.BindFlags = 0;
				desc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
				desc.MiscFlags = 0;

				hr = RENDERER->device->CreateTexture2D(&desc, NULL, &staging);
				if (!SUCCEEDED(hr))
				{
					BLAH_ASSERT(false, "Failed to create staging texture to get data");
					return;
				}
			}

			D3D11_BOX box;
			box.left = 0;
			box.right = m_width;
			box.bottom = m_height;
			box.top = 0;
			box.front = 0;
			box.back = 1;

			// copy data to staging texture
			RENDERER->context->CopySubresourceRegion(
				staging, 0,
				0, 0, 0,
				texture, 0,
				&box);

			// get data
			D3D11_MAPPED_SUBRESOURCE map;
			hr = RENDERER->context->Map(staging, 0, D3D11_MAP_READ, 0, &map);

			if (!SUCCEEDED(hr))
			{
				BLAH_ASSERT(false, "Failed to get texture data");
				return;
			}

			int bytes_per_pixel = m_size / (m_width * m_height);
			int bytes_per_row = m_width * bytes_per_pixel;
			for (int y = 0; y < m_height; y++)
				memcpy(data + y * bytes_per_row, (unsigned char*)map.pData + map.RowPitch * y, bytes_per_row);

			RENDERER->context->Unmap(staging, 0);
		}

		bool is_framebuffer() const override
		{
			return m_is_framebuffer;
		}

	};

	class D3D11_Target : public Target
	{
	private:
		Attachments m_attachments;

	public:
		StackVector<ID3D11RenderTargetView*, Attachments::capacity - 1> color_views;
		ID3D11DepthStencilView* depth_view = nullptr;

		D3D11_Target(int width, int height, const TextureFormat* attachments, int attachment_count)
		{
			for (int i = 0; i < attachment_count; i++)
			{
				auto tex = new D3D11_Texture(width, height, attachments[i], true);

				m_attachments.push_back(TextureRef(tex));

				if (attachments[i] == TextureFormat::DepthStencil)
				{
					RENDERER->device->CreateDepthStencilView(tex->texture, nullptr, &depth_view);
				}
				else
				{
					ID3D11RenderTargetView* view = nullptr;
					RENDERER->device->CreateRenderTargetView(tex->texture, nullptr, &view);
					color_views.push_back(view);
				}
			}
		}

		~D3D11_Target()
		{
			for (auto& it : color_views)
				it->Release();
			color_views.clear();

			if (depth_view)
				depth_view->Release();
			depth_view = nullptr;
		}

		Attachments& textures() override
		{
			return m_attachments;
		}

		const Attachments& textures() const override
		{
			return m_attachments;
		}

		void clear(Color color, float depth, u8 stencil, ClearMask mask) override
		{
			float col[4] = { color.r / 255.0f, color.g / 255.0f, color.b / 255.0f, color.a / 255.0f };

			if (((int)mask & (int)ClearMask::Color) == (int)ClearMask::Color)
			{
				for (int i = 0; i < color_views.size(); i++)
					RENDERER->context->ClearRenderTargetView(color_views[i], col);
			}

			if (depth_view)
			{
				UINT flags = 0;
				if (((int)mask & (int)ClearMask::Depth) == (int)ClearMask::Depth)
					flags |= D3D11_CLEAR_DEPTH;
				if (((int)mask & (int)ClearMask::Stencil) == (int)ClearMask::Stencil)
					flags |= D3D11_CLEAR_STENCIL;

				if (flags != 0)
					RENDERER->context->ClearDepthStencilView(depth_view, flags, depth, stencil);
			}
		}
	};

	class D3D11_Shader : public Shader
	{
	public:
		ID3D11VertexShader* vertex = nullptr;
		ID3D11PixelShader* fragment = nullptr;
		ID3DBlob* vertex_blob = nullptr;
		ID3DBlob* fragment_blob = nullptr;
		Vector<ID3D11Buffer*> vertex_uniform_buffers;
		Vector<ID3D11Buffer*> fragment_uniform_buffers;
		Vector<Vector<float>> vertex_uniform_values;
		Vector<Vector<float>> fragment_uniform_values;
		StackVector<ShaderData::HLSL_Attribute, 16> attributes;
		Vector<UniformInfo> uniform_list;
		u32 hash = 0;
		bool valid = false;

		D3D11_Shader(const ShaderData* data)
		{
			UINT flags = D3DCOMPILE_ENABLE_STRICTNESS | D3DCOMPILE_DEBUG;
			ID3DBlob* error_blob = nullptr;
			HRESULT hr;

			// compile vertex shader
			{
				hr = D3DCompile(
					data->vertex.cstr(),
					data->vertex.length(),
					nullptr,
					nullptr,
					nullptr,
					"vs_main",
					"vs_5_0",
					flags,
					0,
					&vertex_blob,
					&error_blob);

				if (FAILED(hr))
				{
					Log::error("%s", (char*)error_blob->GetBufferPointer());
					error_blob->Release();
					return;
				}
			}

			// compile fragment shader
			{
				hr = D3DCompile(
					data->fragment.cstr(),
					data->fragment.length(),
					nullptr,
					nullptr,
					nullptr,
					"ps_main",
					"ps_5_0",
					flags,
					0,
					&fragment_blob,
					&error_blob);

				if (FAILED(hr))
				{
					Log::error("%s", (char*)error_blob->GetBufferPointer());
					error_blob->Release();
					return;
				}
			}

			// create vertex shader
			{
				hr = RENDERER->device->CreateVertexShader(
					vertex_blob->GetBufferPointer(),
					vertex_blob->GetBufferSize(),
					NULL,
					&vertex);

				if (!SUCCEEDED(hr))
					return;
			}

			// create fragment shader
			{
				hr = RENDERER->device->CreatePixelShader(
					fragment_blob->GetBufferPointer(),
					fragment_blob->GetBufferSize(),
					NULL,
					&fragment);

				if (!SUCCEEDED(hr))
					return;
			}

			// get uniforms
			reflect_uniforms(uniform_list, vertex_uniform_buffers, vertex_blob, ShaderType::Vertex);
			reflect_uniforms(uniform_list, fragment_uniform_buffers, fragment_blob, ShaderType::Fragment);

			// combine uniforms that were in both
			for (int i = 0; i < uniform_list.size(); i++)
			{
				for (int j = i + 1; j < uniform_list.size(); j++)
				{
					if (strcmp(uniform_list[i].name, uniform_list[j].name) == 0)
					{
						if (uniform_list[i].type == uniform_list[j].type)
						{
							uniform_list[i].shader = (ShaderType)((int)uniform_list[i].shader | (int)uniform_list[j].shader);
							uniform_list.erase(j);
							j--;
						}
					}
				}
			}

			// create CPU uniform buffers, so we don't need to create them during rendering
			vertex_uniform_values.expand(vertex_uniform_buffers.size());
			fragment_uniform_values.expand(fragment_uniform_buffers.size());

			// copy HLSL attributes
			attributes = data->hlsl_attributes;

			// store hash
			hash = 5381;
			for (auto& it : attributes)
			{
				for (size_t i = 0, n = strlen(it.semantic_name); i < n; i++)
					hash = ((hash << 5) + hash) + it.semantic_name[i];
				hash = (it.semantic_index << 5) + hash;
			}

			// Shader is ready for use!
			valid = true;
		}

		~D3D11_Shader() override
		{
			if (vertex)
				vertex->Release();
			vertex = nullptr;

			if (vertex_blob)
				vertex_blob->Release();
			vertex_blob = nullptr;

			if (fragment)
				fragment->Release();
			fragment = nullptr;

			if (fragment_blob)
				fragment_blob->Release();
			fragment_blob = nullptr;

			for (auto& it : vertex_uniform_buffers)
				it->Release();
			vertex_uniform_buffers.clear();
			for (auto& it : fragment_uniform_buffers)
				it->Release();
			fragment_uniform_buffers.clear();
		}

		Vector<UniformInfo>& uniforms() override
		{
			return uniform_list;
		}

		const Vector<UniformInfo>& uniforms() const override
		{
			return uniform_list;
		}
	};

	class D3D11_Mesh : public Mesh
	{
	private:
		i64 m_vertex_count = 0;
		i64 m_vertex_capacity = 0;
		i64 m_index_count = 0;
		i64 m_index_capacity = 0;

	public:
		ID3D11Buffer* vertex_buffer = nullptr;
		VertexFormat vertex_format;
		ID3D11Buffer* index_buffer = nullptr;
		IndexFormat index_format = IndexFormat::UInt16;
		int index_stride = 0;

		D3D11_Mesh()
		{

		}

		~D3D11_Mesh()
		{
			if (vertex_buffer)
				vertex_buffer->Release();
			vertex_buffer = nullptr;
			if (index_buffer)
				index_buffer->Release();
			index_buffer = nullptr;
		}

		void index_data(IndexFormat format, const void* indices, i64 count) override
		{
			m_index_count = count;

			if (index_format != format || !index_buffer || m_index_count > m_index_capacity)
			{
				index_stride = 0;
				index_format = format;
				m_index_capacity = max(m_index_capacity, m_index_count);

				switch (format)
				{
				case IndexFormat::UInt16: index_stride = sizeof(i16); break;
				case IndexFormat::UInt32: index_stride = sizeof(i32); break;
				}

				if (m_index_capacity > 0 && indices)
				{
					// release existing buffer
					if (index_buffer)
						index_buffer->Release();
					index_buffer = nullptr;

					// buffer description
					D3D11_BUFFER_DESC desc = { 0 };
					desc.ByteWidth = (UINT)(index_stride * m_index_capacity);
					desc.Usage = D3D11_USAGE_DYNAMIC;
					desc.BindFlags = D3D11_BIND_INDEX_BUFFER;
					desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;

					// buffer data
					D3D11_SUBRESOURCE_DATA data = { 0 };
					data.pSysMem = indices;

					// create
					auto hr = RENDERER->device->CreateBuffer(&desc, &data, &index_buffer);
					BLAH_ASSERT(SUCCEEDED(hr), "Failed to update Index Data");
				}
			}
			else if (indices)
			{
				D3D11_MAPPED_SUBRESOURCE map;

				auto hr = RENDERER->context->Map(index_buffer, 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
				BLAH_ASSERT(SUCCEEDED(hr), "Failed to update Index Data");

				if (SUCCEEDED(hr))
				{
					memcpy(map.pData, indices, index_stride * count);
					RENDERER->context->Unmap(index_buffer, 0);
				}
			}
		}

		void vertex_data(const VertexFormat& format, const void* vertices, i64 count) override
		{
			m_vertex_count = count;

			// recreate buffer if we've changed
			if (vertex_format.stride != format.stride || !vertex_buffer || m_vertex_count > m_vertex_capacity)
			{
				m_vertex_capacity = max(m_vertex_capacity, m_vertex_count);
				vertex_format = format;

				// discard old buffer
				if (vertex_buffer)
					vertex_buffer->Release();
				vertex_buffer = nullptr;

				if (m_vertex_capacity > 0 && vertices)
				{
					// buffer description
					D3D11_BUFFER_DESC desc = { 0 };
					desc.ByteWidth = (UINT)(format.stride * m_vertex_capacity);
					desc.Usage = D3D11_USAGE_DYNAMIC;
					desc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
					desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;

					// buffer data
					D3D11_SUBRESOURCE_DATA data = { 0 };
					data.pSysMem = vertices;

					// create
					auto hr = RENDERER->device->CreateBuffer(&desc, &data, &vertex_buffer);
					BLAH_ASSERT(SUCCEEDED(hr), "Failed to update Vertex Data");
				}
			}
			// otherwise just update it
			else if (vertices)
			{
				D3D11_MAPPED_SUBRESOURCE map;
				auto hr = RENDERER->context->Map(vertex_buffer, 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
				BLAH_ASSERT(SUCCEEDED(hr), "Failed to update Vertex Data");

				if (SUCCEEDED(hr))
				{
					memcpy(map.pData, vertices, vertex_format.stride * count);
					RENDERER->context->Unmap(vertex_buffer, 0);
				}
			}
		}

		void instance_data(const VertexFormat& format, const void* instances, i64 count) override
		{

		}

		i64 index_count() const override
		{
			return m_index_count;
		}

		i64 vertex_count() const override
		{
			return m_vertex_count;
		}

		i64 instance_count() const override
		{
			return 0;
		}
	};

	bool Renderer_D3D11::init()
	{
		last_window_size = App::get_size();

		// Define Swap Chain
		DXGI_SWAP_CHAIN_DESC desc = {};
		desc.BufferDesc.RefreshRate.Numerator = 0;
		desc.BufferDesc.RefreshRate.Denominator = 1;
		desc.BufferDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
		desc.SampleDesc.Count = 1;
		desc.SampleDesc.Quality = 0;
		desc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		desc.BufferCount = 1;
		desc.OutputWindow = (HWND)Platform::d3d11_get_hwnd();
		desc.Windowed = true;

		// Creation Flags
		UINT flags = D3D11_CREATE_DEVICE_SINGLETHREADED;
#if defined(DEBUG) || defined(_DEBUG)
		flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif

		// Create D3D device & context & swap chain
		D3D_FEATURE_LEVEL feature_level;
		HRESULT hr = D3D11CreateDeviceAndSwapChain(
			NULL,
			D3D_DRIVER_TYPE_HARDWARE,
			NULL,
			flags,
			NULL,
			0,
			D3D11_SDK_VERSION,
			&desc,
			&swap_chain,
			&device,
			&feature_level,
			&context);

		// Exit out if it's not OK
		if (!SUCCEEDED(hr) || !swap_chain || !device || !context)
			return false;

		// Get the backbuffer
		ID3D11Texture2D* frame_buffer = nullptr;
		swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&frame_buffer);
		if (frame_buffer)
		{
			D3D11_TEXTURE2D_DESC desc;
			frame_buffer->GetDesc(&desc);
			drawable_size = Point(desc.Width, desc.Height);

			device->CreateRenderTargetView(frame_buffer, nullptr, &backbuffer_view);
			frame_buffer->Release();
		}

		// TODO:
		// create a depth backbuffer

		// Store Features
		info.type = RendererType::D3D11;
		info.instancing = true;
		info.max_texture_size = D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION;
		info.origin_bottom_left = false;

		// Print Driver Info
		{
			IDXGIDevice* dxgi_device;
			IDXGIAdapter* dxgi_adapter;
			DXGI_ADAPTER_DESC adapter_desc;

			hr = device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device);

			if (SUCCEEDED(hr))
			{
				dxgi_device->GetAdapter(&dxgi_adapter);
				dxgi_adapter->GetDesc(&adapter_desc);

				Log::info("D3D11 %ls", adapter_desc.Description);

				dxgi_device->Release();
				dxgi_adapter->Release();
			}
			else
			{
				Log::info("D3D11");
			}
		}

		// create default sprite batch shader
		default_batcher_shader = Shader::create(d3d11_batch_shader_data);

		return true;
	}

	void Renderer_D3D11::shutdown()
	{
		// release cached objects
		for (auto& it : blend_cache)
			it.state->Release();
		for (auto& it : depthstencil_cache)
			it.state->Release();
		for (auto& it : layout_cache)
			it.layout->Release();
		for (auto& it : rasterizer_cache)
			it.state->Release();
		for (auto& it : sampler_cache)
			it.state->Release();

		// release main devices
		if (backbuffer_view)
			backbuffer_view->Release();
		if (backbuffer_depth_view)
			backbuffer_depth_view->Release();
		swap_chain->Release();
		context->ClearState();
		context->Flush();
		context->Release();
		device->Release();
	}

	void Renderer_D3D11::update()
	{

	}

	void Renderer_D3D11::before_render()
	{
		HRESULT hr;

		auto next_window_size = App::get_size();
		if (last_window_size != next_window_size)
		{
			last_window_size = next_window_size;

			// release old buffer
			if (backbuffer_view)
				backbuffer_view->Release();

			// perform resize
			hr = swap_chain->ResizeBuffers(0, 0, 0, DXGI_FORMAT_B8G8R8A8_UNORM, 0);
			BLAH_ASSERT(SUCCEEDED(hr), "Failed to update Backbuffer on Resize");

			// get the new buffer
			ID3D11Texture2D* frame_buffer = nullptr;
			hr = swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&frame_buffer);
			if (SUCCEEDED(hr) && frame_buffer)
			{
				// get backbuffer drawable size
				D3D11_TEXTURE2D_DESC desc;
				frame_buffer->GetDesc(&desc);
				drawable_size = Point(desc.Width, desc.Height);

				// create view
				hr = device->CreateRenderTargetView(frame_buffer, nullptr, &backbuffer_view);
				BLAH_ASSERT(SUCCEEDED(hr), "Failed to update Backbuffer on Resize");
				frame_buffer->Release();
			}
		}
	}

	bool Renderer_D3D11::get_draw_size(int* w, int* h)
	{
		*w = drawable_size.x;
		*h = drawable_size.y;
		return true;
	}

	void Renderer_D3D11::after_render()
	{
		auto vsync = App::get_flag(Flags::VSync);
		auto hr = swap_chain->Present(vsync ? 1 : 0, 0);
		BLAH_ASSERT(SUCCEEDED(hr), "Failed to Present swap chain");
	}

	TextureRef Renderer_D3D11::create_texture(int width, int height, TextureFormat format)
	{
		auto result = new D3D11_Texture(width, height, format, false);

		if (result->texture)
			return TextureRef(result);

		delete result;
		return TextureRef();
	}

	TargetRef Renderer_D3D11::create_target(int width, int height, const TextureFormat* attachments, int attachment_count)
	{
		return TargetRef(new D3D11_Target(width, height, attachments, attachment_count));
	}

	ShaderRef Renderer_D3D11::create_shader(const ShaderData* data)
	{
		auto result = new D3D11_Shader(data);
		if (result->valid)
			return ShaderRef(result);

		delete result;
		return ShaderRef();
	}

	MeshRef Renderer_D3D11::create_mesh()
	{
		return MeshRef(new D3D11_Mesh());
	}

	void Renderer_D3D11::render(const DrawCall& pass)
	{
		auto ctx = context;
		auto mesh = (D3D11_Mesh*)pass.mesh.get();
		auto shader = (D3D11_Shader*)(pass.material->shader().get());

		// OM
		{
			// Set the Target
			if (pass.target == App::backbuffer() || !pass.target)
			{
				ctx->OMSetRenderTargets(1, &backbuffer_view, backbuffer_depth_view);
			}
			else
			{
				auto target = (D3D11_Target*)(pass.target.get());
				ctx->OMSetRenderTargets(target->color_views.size(), target->color_views.begin(), target->depth_view);
			}

			// Depth
			{
				auto depthstencil = get_depthstencil(pass);
				if (depthstencil)
					ctx->OMSetDepthStencilState(depthstencil, 0);
			}

			// Blend Mode
			{
				auto blend = get_blend(pass.blend);
				if (blend)
				{
					auto color = Color::from_rgba(pass.blend.rgba);
					float factor[4]{ color.r / 255.0f, color.g / 255.0f, color.b / 255.0f, color.a / 255.0f };
					auto mask = 0xffffffff;
					ctx->OMSetBlendState(blend, factor, mask);
				}
				else
				{
					// if we failed to create a blend mode for some reason
					ctx->OMSetBlendState(nullptr, nullptr, 0);
				}
			}
		}

		// IA
		{
			// We draw triangles
			ctx->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

			// Assign Layout
			auto layout = get_layout(shader, mesh->vertex_format);
			ctx->IASetInputLayout(layout);

			// Assign Vertex Buffer
			{
				UINT stride = mesh->vertex_format.stride;
				UINT offset = 0;

				ctx->IASetVertexBuffers(
					0,
					1,
					&mesh->vertex_buffer,
					&stride,
					&offset);
			}

			// Assign Index Buffer
			{
				DXGI_FORMAT format = DXGI_FORMAT_R16_UINT;

				switch (mesh->index_format)
				{
				case IndexFormat::UInt16: format = DXGI_FORMAT_R16_UINT; break;
				case IndexFormat::UInt32: format = DXGI_FORMAT_R32_UINT; break;
				}

				ctx->IASetIndexBuffer(mesh->index_buffer, format, 0);
			}
		}

		// VS
		{
			apply_uniforms(shader, pass.material, ShaderType::Vertex);
			ctx->VSSetShader(shader->vertex, nullptr, 0);
			ctx->VSSetConstantBuffers(0, shader->vertex_uniform_buffers.size(), shader->vertex_uniform_buffers.begin());
		}

		// PS
		{
			apply_uniforms(shader, pass.material, ShaderType::Fragment);
			ctx->PSSetShader(shader->fragment, nullptr, 0);
			ctx->PSSetConstantBuffers(0, shader->fragment_uniform_buffers.size(), shader->fragment_uniform_buffers.begin());

			// Fragment Shader Textures
			auto& textures = pass.material->textures();
			for (int i = 0; i < textures.size(); i++)
			{
				if (textures[i])
				{
					// Assign the Texture
					auto view = ((D3D11_Texture*)textures[i].get())->view;
					ctx->PSSetShaderResources(i, 1, &view);
				}
			}

			// Fragment Shader Samplers
			auto& samplers = pass.material->samplers();
			for (int i = 0; i < samplers.size(); i++)
			{
				auto sampler = get_sampler(samplers[i]);
				if (sampler)
					ctx->PSSetSamplers(i, 1, &sampler);
			}
		}

		// RS
		{
			// Set the Viewport
			{
				D3D11_VIEWPORT viewport = {
					pass.viewport.x, pass.viewport.y,
					pass.viewport.w, pass.viewport.h,
					0.0f, 1.0f };

				ctx->RSSetViewports(1, &viewport);
			}

			// Scissor Rect
			if (pass.has_scissor)
			{
				D3D11_RECT scissor = {
					(int)pass.scissor.x, (int)pass.scissor.y,
					(int)pass.scissor.x + (int)pass.scissor.w, (int)pass.scissor.y + (int)pass.scissor.h };

				ctx->RSSetScissorRects(1, &scissor);
			}
			else
			{
				ctx->RSSetScissorRects(0, nullptr);
			}

			// Rasterizer
			{
				auto rasterizer = get_rasterizer(pass);
				if (rasterizer)
					ctx->RSSetState(rasterizer);
			}
		}

		// Draw
		{
			if (mesh->instance_count() <= 0)
			{
				ctx->DrawIndexed(
					static_cast<UINT>(pass.index_count),
					static_cast<UINT>(pass.index_start), 0);
			}
			else
			{
				// TODO:
				// Draw Instanced data
				BLAH_ASSERT(false, "Instanced Drawing not implemented yet");
			}
		}

		// UnBind Shader Resources
		{
			auto& textures = pass.material->textures();
			ID3D11ShaderResourceView* view = nullptr;
			for (int i = 0; i < textures.size(); i++)
				ctx->PSSetShaderResources(i, 1, &view);
		}
	}

	void Renderer_D3D11::clear_backbuffer(Color color, float depth, u8 stencil, ClearMask mask)
	{
		if (((int)mask & (int)ClearMask::Color) == (int)ClearMask::Color)
		{
			float clear[4] = { color.r / 255.0f, color.g / 255.0f, color.b / 255.0f, color.a / 255.0f };
			context->ClearRenderTargetView(backbuffer_view, clear);
		}

		if (backbuffer_depth_view)
		{
			UINT flags = 0;
			if (((int)mask & (int)ClearMask::Depth) == (int)ClearMask::Depth)
				flags |= D3D11_CLEAR_DEPTH;
			if (((int)mask & (int)ClearMask::Stencil) == (int)ClearMask::Stencil)
				flags |= D3D11_CLEAR_STENCIL;

			if (flags != 0)
				context->ClearDepthStencilView(backbuffer_depth_view, flags, depth, stencil);
		}
	}

	// Utility Methods

	D3D11_BLEND_OP blend_op(BlendOp op)
	{
		switch (op)
		{
		case BlendOp::Add: return D3D11_BLEND_OP_ADD;
		case BlendOp::Subtract: return D3D11_BLEND_OP_SUBTRACT;
		case BlendOp::ReverseSubtract: return D3D11_BLEND_OP_REV_SUBTRACT;
		case BlendOp::Min: return D3D11_BLEND_OP_MIN;
		case BlendOp::Max: return D3D11_BLEND_OP_MAX;
		}

		return D3D11_BLEND_OP_ADD;
	}

	D3D11_BLEND blend_factor(BlendFactor factor)
	{
		switch (factor)
		{
		case BlendFactor::Zero: return D3D11_BLEND_ZERO;
		case BlendFactor::One: return D3D11_BLEND_ONE;
		case BlendFactor::SrcColor: return D3D11_BLEND_SRC_COLOR;
		case BlendFactor::OneMinusSrcColor: return D3D11_BLEND_INV_SRC_COLOR;
		case BlendFactor::DstColor: return D3D11_BLEND_DEST_COLOR;
		case BlendFactor::OneMinusDstColor: return D3D11_BLEND_INV_DEST_COLOR;
		case BlendFactor::SrcAlpha: return D3D11_BLEND_SRC_ALPHA;
		case BlendFactor::OneMinusSrcAlpha: return D3D11_BLEND_INV_SRC_ALPHA;
		case BlendFactor::DstAlpha: return D3D11_BLEND_DEST_ALPHA;
		case BlendFactor::OneMinusDstAlpha: return D3D11_BLEND_INV_DEST_ALPHA;
		case BlendFactor::ConstantColor: return D3D11_BLEND_BLEND_FACTOR;
		case BlendFactor::OneMinusConstantColor: return D3D11_BLEND_INV_BLEND_FACTOR;
		case BlendFactor::ConstantAlpha: return D3D11_BLEND_BLEND_FACTOR;
		case BlendFactor::OneMinusConstantAlpha: return D3D11_BLEND_INV_BLEND_FACTOR;
		case BlendFactor::SrcAlphaSaturate: return D3D11_BLEND_SRC_ALPHA_SAT;
		case BlendFactor::Src1Color: return D3D11_BLEND_SRC1_COLOR;
		case BlendFactor::OneMinusSrc1Color: return D3D11_BLEND_INV_SRC1_COLOR;
		case BlendFactor::Src1Alpha: return D3D11_BLEND_SRC1_ALPHA;
		case BlendFactor::OneMinusSrc1Alpha: return D3D11_BLEND_INV_SRC1_ALPHA;
		}

		return D3D11_BLEND_ZERO;
	}

	bool reflect_uniforms(Vector<UniformInfo>& append_uniforms_to, Vector<ID3D11Buffer*>& append_buffers_to, ID3DBlob* shader, ShaderType shader_type)
	{
		ID3D11ShaderReflection* reflector = nullptr;
		D3DReflect(shader->GetBufferPointer(), shader->GetBufferSize(), IID_ID3D11ShaderReflection, (void**)&reflector);

		D3D11_SHADER_DESC shader_desc;
		reflector->GetDesc(&shader_desc);

		for (UINT i = 0; i < shader_desc.BoundResources; i++)
		{
			D3D11_SHADER_INPUT_BIND_DESC desc;
			reflector->GetResourceBindingDesc(i, &desc);

			if (desc.Type == D3D_SIT_TEXTURE && desc.Dimension == D3D_SRV_DIMENSION_TEXTURE2D)
			{
				auto uniform = append_uniforms_to.expand();
				uniform->name = desc.Name;
				uniform->shader = shader_type;
				uniform->register_index = desc.BindPoint;
				uniform->buffer_index = 0;
				uniform->array_length = max(1, desc.BindCount);
				uniform->type = UniformType::Texture2D;
			}
			else if (desc.Type == D3D_SIT_SAMPLER)
			{
				auto uniform = append_uniforms_to.expand();
				uniform->name = desc.Name;
				uniform->shader = shader_type;
				uniform->register_index = desc.BindPoint;
				uniform->buffer_index = 0;
				uniform->array_length = max(1, desc.BindCount);
				uniform->type = UniformType::Sampler2D;
			}
		}

		for (UINT i = 0; i < shader_desc.ConstantBuffers; i++)
		{
			D3D11_SHADER_BUFFER_DESC desc;

			auto cb = reflector->GetConstantBufferByIndex(i);
			cb->GetDesc(&desc);

			// create the constant buffer for assigning data later
			{
				D3D11_BUFFER_DESC buffer_desc = {};
				buffer_desc.ByteWidth = desc.Size;
				buffer_desc.Usage = D3D11_USAGE_DYNAMIC;
				buffer_desc.BindFlags = D3D11_BIND_CONSTANT_BUFFER;
				buffer_desc.CPUAccessFlags = D3D11_CPU_ACCESS_WRITE;

				ID3D11Buffer* buffer;
				RENDERER->device->CreateBuffer(&buffer_desc, nullptr, &buffer);
				append_buffers_to.push_back(buffer);
			}

			// get the uniforms
			for (UINT j = 0; j < desc.Variables; j++)
			{
				D3D11_SHADER_VARIABLE_DESC var_desc;
				D3D11_SHADER_TYPE_DESC type_desc;

				auto var = cb->GetVariableByIndex(j);
				var->GetDesc(&var_desc);

				auto type = var->GetType();
				type->GetDesc(&type_desc);

				auto uniform = append_uniforms_to.expand();
				uniform->name = var_desc.Name;
				uniform->shader = shader_type;
				uniform->register_index = 0;
				uniform->buffer_index = i;
				uniform->array_length = max(1, type_desc.Elements);
				uniform->type = UniformType::None;

				if (type_desc.Type == D3D_SVT_FLOAT)
				{
					if (type_desc.Rows == 1)
					{
						if (type_desc.Columns == 1)
							uniform->type = UniformType::Float;
						else if (type_desc.Columns == 2)
							uniform->type = UniformType::Float2;
						else if (type_desc.Columns == 3)
							uniform->type = UniformType::Float3;
						else if (type_desc.Columns == 4)
							uniform->type = UniformType::Float4;
					}
					else if (type_desc.Rows == 2 && type_desc.Columns == 3)
					{
						uniform->type = UniformType::Mat3x2;
					}
					else if (type_desc.Rows == 4 && type_desc.Columns == 4)
					{
						uniform->type = UniformType::Mat4x4;
					}
				}
			}
		}

		return true;
	}

	void apply_uniforms(D3D11_Shader* shader, const MaterialRef& material, ShaderType type)
	{
		auto& buffers = (type == ShaderType::Vertex ? shader->vertex_uniform_buffers : shader->fragment_uniform_buffers);
		auto& values = (type == ShaderType::Vertex ? shader->vertex_uniform_values : shader->fragment_uniform_values);

		for (int i = 0; i < buffers.size(); i++)
		{
			// clear previous values
			values[i].clear();

			// build block
			const float* data = material->data();
			for (auto& it : shader->uniforms())
			{
				if (it.type == UniformType::None ||
					it.type == UniformType::Texture2D ||
					it.type == UniformType::Sampler2D)
					continue;

				int size = 0;
				switch (it.type)
				{
				case UniformType::None: break;
				case UniformType::Texture2D: break;
				case UniformType::Sampler2D: break;
				case UniformType::Float: size = 1; break;
				case UniformType::Float2: size = 2; break;
				case UniformType::Float3: size = 3; break;
				case UniformType::Float4: size = 4; break;
				case UniformType::Mat3x2: size = 6; break;
				case UniformType::Mat4x4: size = 16; break;
				}

				int length = size * it.array_length;

				if (it.buffer_index == i && ((int)it.shader & (int)type) != 0)
				{
					// HLSL uniforms can not pass 16-byte (4-float) boundaries, therefore potentially add padding to the buffer so we align to 16 bytes
					// For example, if we have filled in 6 floats, our remaining space will be 2. If the next value is larger than 2 floats, we will
					// need to move to the next 16-byte (4-float) boundary (in this case 2 floats to begin at 8 floats).
					// see: https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules
					int remaining = 4 - values[i].size() % 4;
					if (remaining != 4 && remaining + length > 4)
						values[i].expand(remaining);
					
					auto start = values[i].expand(length);
					memcpy(start, data, sizeof(float) * length);
				}

				data += length;
			}

			// apply block
			if (buffers[i])
			{
				D3D11_MAPPED_SUBRESOURCE map;
				RENDERER->context->Map(buffers[i], 0, D3D11_MAP_WRITE_DISCARD, 0, &map);
				memcpy(map.pData, values[i].begin(), values[i].size() * sizeof(float));
				RENDERER->context->Unmap(buffers[i], 0);
			}
		}
	}

	ID3D11InputLayout* Renderer_D3D11::get_layout(D3D11_Shader* shader, const VertexFormat& format)
	{
		// find existing
		for (auto& it : layout_cache)
		{
			if (it.shader_hash == shader->hash && it.format.stride == format.stride && it.format.attributes.size() == format.attributes.size())
			{
				bool same_format = true;
				for (int n = 0; same_format && n < format.attributes.size(); n++)
					if (it.format.attributes[n].index != format.attributes[n].index ||
						it.format.attributes[n].type != format.attributes[n].type ||
						it.format.attributes[n].normalized != format.attributes[n].normalized)
						same_format = false;

				if (same_format)
					return it.layout;
			}
		}

		// create a new one
		Vector<D3D11_INPUT_ELEMENT_DESC> desc;
		for (int i = 0; i < shader->attributes.size(); i++)
		{
			auto it = desc.expand();
			it->SemanticName = shader->attributes[i].semantic_name;
			it->SemanticIndex = shader->attributes[i].semantic_index;

			if (!format.attributes[i].normalized)
			{
				switch (format.attributes[i].type)
				{
				case VertexType::None: break;
				case VertexType::Float: it->Format = DXGI_FORMAT_R32_FLOAT;  break;
				case VertexType::Float2: it->Format = DXGI_FORMAT_R32G32_FLOAT; break;
				case VertexType::Float3: it->Format = DXGI_FORMAT_R32G32B32_FLOAT; break;
				case VertexType::Float4: it->Format = DXGI_FORMAT_R32G32B32A32_FLOAT; break;
				case VertexType::Byte4: it->Format = DXGI_FORMAT_R8G8B8A8_SINT; break;
				case VertexType::UByte4: it->Format = DXGI_FORMAT_R8G8B8A8_UINT; break;
				case VertexType::Short2: it->Format = DXGI_FORMAT_R16G16_SINT; break;
				case VertexType::UShort2: it->Format = DXGI_FORMAT_R16G16_UINT; break;
				case VertexType::Short4: it->Format = DXGI_FORMAT_R16G16B16A16_SINT; break;
				case VertexType::UShort4: it->Format = DXGI_FORMAT_R16G16B16A16_UINT; break;
				}
			}
			else
			{
				switch (format.attributes[i].type)
				{
				case VertexType::None: break;
				case VertexType::Float: it->Format = DXGI_FORMAT_R32_FLOAT;  break;
				case VertexType::Float2: it->Format = DXGI_FORMAT_R32G32_FLOAT; break;
				case VertexType::Float3: it->Format = DXGI_FORMAT_R32G32B32_FLOAT; break;
				case VertexType::Float4: it->Format = DXGI_FORMAT_R32G32B32A32_FLOAT; break;
				case VertexType::Byte4: it->Format = DXGI_FORMAT_R8G8B8A8_SNORM; break;
				case VertexType::UByte4: it->Format = DXGI_FORMAT_R8G8B8A8_UNORM; break;
				case VertexType::Short2: it->Format = DXGI_FORMAT_R16G16_SNORM; break;
				case VertexType::UShort2: it->Format = DXGI_FORMAT_R16G16_UNORM; break;
				case VertexType::Short4: it->Format = DXGI_FORMAT_R16G16B16A16_SNORM; break;
				case VertexType::UShort4: it->Format = DXGI_FORMAT_R16G16B16A16_UNORM; break;
				}
			}

			it->InputSlot = 0;
			it->AlignedByteOffset = (i == 0 ? 0 : D3D11_APPEND_ALIGNED_ELEMENT);
			it->InputSlotClass = D3D11_INPUT_PER_VERTEX_DATA;
			it->InstanceDataStepRate = 0;
		}

		ID3D11InputLayout* layout = nullptr;

		auto hr = device->CreateInputLayout(
			desc.begin(),
			desc.size(),
			shader->vertex_blob->GetBufferPointer(),
			shader->vertex_blob->GetBufferSize(),
			&layout);

		if (SUCCEEDED(hr))
		{
			auto entry = layout_cache.expand();
			entry->shader_hash = shader->hash;
			entry->format = format;
			entry->layout = layout;
			return layout;
		}

		return nullptr;
	}

	ID3D11BlendState* Renderer_D3D11::get_blend(const BlendMode& blend)
	{
		for (auto& it : blend_cache)
			if (it.blend == blend)
				return it.state;

		D3D11_BLEND_DESC desc = { 0 };
		desc.AlphaToCoverageEnable = 0;
		desc.IndependentBlendEnable = 0;

		desc.RenderTarget[0].BlendEnable = !(
			blend.color_src == BlendFactor::One && blend.color_dst == BlendFactor::Zero &&
			blend.alpha_src == BlendFactor::One && blend.alpha_dst == BlendFactor::Zero
			);

		desc.RenderTarget[0].RenderTargetWriteMask = 0;
		if (((int)blend.mask & (int)BlendMask::Red) == (int)BlendMask::Red)
			desc.RenderTarget[0].RenderTargetWriteMask |= D3D11_COLOR_WRITE_ENABLE_RED;
		if (((int)blend.mask & (int)BlendMask::Green) == (int)BlendMask::Green)
			desc.RenderTarget[0].RenderTargetWriteMask |= D3D11_COLOR_WRITE_ENABLE_GREEN;
		if (((int)blend.mask & (int)BlendMask::Blue) == (int)BlendMask::Blue)
			desc.RenderTarget[0].RenderTargetWriteMask |= D3D11_COLOR_WRITE_ENABLE_BLUE;
		if (((int)blend.mask & (int)BlendMask::Alpha) == (int)BlendMask::Alpha)
			desc.RenderTarget[0].RenderTargetWriteMask |= D3D11_COLOR_WRITE_ENABLE_ALPHA;

		if (desc.RenderTarget[0].BlendEnable)
		{
			desc.RenderTarget[0].BlendOp = blend_op(blend.color_op);
			desc.RenderTarget[0].SrcBlend = blend_factor(blend.color_src);
			desc.RenderTarget[0].DestBlend = blend_factor(blend.color_dst);

			desc.RenderTarget[0].BlendOpAlpha = blend_op(blend.alpha_op);
			desc.RenderTarget[0].SrcBlendAlpha = blend_factor(blend.alpha_src);
			desc.RenderTarget[0].DestBlendAlpha = blend_factor(blend.alpha_dst);
		}

		for (int i = 1; i < 8; i++)
			desc.RenderTarget[i] = desc.RenderTarget[0];

		ID3D11BlendState* blend_state = nullptr;
		auto hr = RENDERER->device->CreateBlendState(&desc, &blend_state);

		if (SUCCEEDED(hr))
		{
			auto entry = blend_cache.expand();
			entry->blend = blend;
			entry->state = blend_state;
			return blend_state;
		}

		return nullptr;
	}

	ID3D11SamplerState* Renderer_D3D11::get_sampler(const TextureSampler& sampler)
	{
		for (auto& it : sampler_cache)
			if (it.sampler == sampler)
				return it.state;

		D3D11_SAMPLER_DESC desc = {};
		desc.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
		desc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
		desc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
		desc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
		desc.ComparisonFunc = D3D11_COMPARISON_NEVER;

		switch (sampler.filter)
		{
		case TextureFilter::None: break;
		case TextureFilter::Nearest: desc.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT; break;
		case TextureFilter::Linear: desc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR; break;
		}

		switch (sampler.wrap_x)
		{
		case TextureWrap::None: break;
		case TextureWrap::Clamp: desc.AddressU = D3D11_TEXTURE_ADDRESS_CLAMP; break;
		case TextureWrap::Repeat: desc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP; break;
		}

		switch (sampler.wrap_y)
		{
		case TextureWrap::None: break;
		case TextureWrap::Clamp: desc.AddressV = D3D11_TEXTURE_ADDRESS_CLAMP; break;
		case TextureWrap::Repeat: desc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP; break;
		}

		ID3D11SamplerState* result;
		auto hr = RENDERER->device->CreateSamplerState(&desc, &result);

		if (SUCCEEDED(hr))
		{
			auto entry = sampler_cache.expand();
			entry->sampler = sampler;
			entry->state = result;
			return result;
		}

		return nullptr;
	}

	ID3D11RasterizerState* Renderer_D3D11::get_rasterizer(const DrawCall& pass)
	{
		for (auto& it : rasterizer_cache)
			if (it.cull == pass.cull && it.has_scissor == pass.has_scissor)
				return it.state;

		D3D11_RASTERIZER_DESC desc = {};
		desc.FillMode = D3D11_FILL_SOLID;
		desc.CullMode = D3D11_CULL_NONE;

		switch (pass.cull)
		{
		case Cull::None: desc.CullMode = D3D11_CULL_NONE; break;
		case Cull::Front: desc.CullMode = D3D11_CULL_FRONT; break;
		case Cull::Back: desc.CullMode = D3D11_CULL_BACK; break;
		}

		desc.FrontCounterClockwise = true;
		desc.DepthBias = 0;
		desc.DepthBiasClamp = 0;
		desc.SlopeScaledDepthBias = 0;
		desc.DepthClipEnable = false;
		desc.ScissorEnable = pass.has_scissor;
		desc.MultisampleEnable = false;
		desc.AntialiasedLineEnable = false;

		ID3D11RasterizerState* result;
		auto hr = RENDERER->device->CreateRasterizerState(&desc, &result);

		if (SUCCEEDED(hr))
		{
			auto entry = rasterizer_cache.expand();
			entry->cull = pass.cull;
			entry->has_scissor = pass.has_scissor;
			entry->state = result;
			return result;
		}

		return nullptr;
	}

	ID3D11DepthStencilState* Renderer_D3D11::get_depthstencil(const DrawCall& pass)
	{
		for (auto& it : depthstencil_cache)
			if (it.depth == pass.depth)
				return it.state;

		D3D11_DEPTH_STENCIL_DESC desc = {};
		desc.DepthEnable = pass.depth != Compare::None;
		desc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
		desc.DepthFunc = D3D11_COMPARISON_NEVER;

		switch (pass.depth)
		{
		case Compare::None: desc.DepthFunc = D3D11_COMPARISON_NEVER; break;
		case Compare::Always: desc.DepthFunc = D3D11_COMPARISON_ALWAYS; break;
		case Compare::Never: desc.DepthFunc = D3D11_COMPARISON_NEVER; break;
		case Compare::Less: desc.DepthFunc = D3D11_COMPARISON_LESS; break;
		case Compare::Equal: desc.DepthFunc = D3D11_COMPARISON_EQUAL; break;
		case Compare::LessOrEqual: desc.DepthFunc = D3D11_COMPARISON_LESS_EQUAL; break;
		case Compare::Greater: desc.DepthFunc = D3D11_COMPARISON_GREATER; break;
		case Compare::NotEqual: desc.DepthFunc = D3D11_COMPARISON_NOT_EQUAL; break;
		case Compare::GreaterOrEqual: desc.DepthFunc = D3D11_COMPARISON_GREATER_EQUAL; break;
		}

		ID3D11DepthStencilState* result;
		auto hr = RENDERER->device->CreateDepthStencilState(&desc, &result);

		if (SUCCEEDED(hr))
		{
			auto entry = depthstencil_cache.expand();
			entry->depth = pass.depth;
			entry->state = result;
			return result;
		}

		return nullptr;
	}
}

Blah::Renderer* Blah::Renderer::try_make_d3d11()
{
	return new Blah::Renderer_D3D11();
}

#else // FOSTER_D3D11_ENABLED

#include "foster_renderer.h"

bool FosterGetDevice_D3D11(FosterRenderDevice* device)
{
	device->renderer = FOSTER_RENDERER_D3D11;
	return false;
}

#endif