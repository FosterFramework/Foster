using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework.JsonConverters;

/// <summary>
/// A Vector2 JsonConverter
/// </summary>
public class Vector2Converter()
	: FloatVectorJsonConverter<Vector2>([["X"], ["Y"]]);

/// <summary>
/// A Vector3 JsonConverter
/// </summary>
public class Vector3Converter()
	: FloatVectorJsonConverter<Vector3>([["X"], ["Y"], ["Z"]]);

/// <summary>
/// A Vector4 JsonConverter
/// </summary>
public class Vector4Converter()
	: FloatVectorJsonConverter<Vector4>([["X"], ["Y"], ["Z"], ["W"]]);

/// <summary>
/// A Matrix3x2 JsonConverter
/// </summary>
public class Matrix3x2Converter : JsonConverter<Matrix3x2>
{
	public override Matrix3x2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
			return default;

		var index = 0;
		Span<float> values = stackalloc float[6];

		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			if (index >= values.Length || reader.TokenType != JsonTokenType.Number)
			{
				reader.Skip();
				continue;
			}

			values[index++] = reader.GetSingle();
		}

		return new(values[0], values[1], values[2], values[3], values[4], values[5]);
	}

	public override void Write(Utf8JsonWriter writer, Matrix3x2 value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"[{value.M11}, {value.M12}, {value.M21}, {value.M22}, {value.M31}, {value.M32}]");
}

/// <summary>
/// JsonConverter for serializing structs of float components.<br/>
/// This is generally unsafe and should only be used on types that are a list of floats, like a Vector
/// </summary>
public abstract class FloatVectorJsonConverter<T>(string[][] Components) : VectorJsonConverter<T, float>(
	Components,
	static (w, v) => w.WriteNumberValue(v),
	static (ref r) => r.GetSingle(),
	static (s, out v) => float.TryParse(s, out v)
) where T : unmanaged;

/// <summary>
/// JsonConverter for serializing structs of int components.<br/>
/// This is generally unsafe and should only be used on types that are a list of ints, like a Point
/// </summary>
public abstract class IntVectorJsonConverter<T>(string[][] Components) : VectorJsonConverter<T, int>(
	Components,
	static (w, v) => w.WriteNumberValue(v),
	static (ref r) => r.GetInt32(),
	static (s, out v) => int.TryParse(s, out v)
) where T : unmanaged;

/// <summary>
/// JsonConverter for serializing structs of a single component type.<br/>
/// This is generally unsafe and should only be used on types that are a list of a single component, like a Vector
/// </summary>
public abstract unsafe class VectorJsonConverter<T, TComponent>(
	string[][] Components, 
	VectorJsonConverter<T, TComponent>.WriteComponentFn WriteComponent, 
	VectorJsonConverter<T, TComponent>.ReadComponentFn ReadComponent,
	VectorJsonConverter<T, TComponent>.TryParseFn TryParseComponent) : JsonConverter<T>
	where T : unmanaged
	where TComponent : unmanaged
{
	public delegate void WriteComponentFn(Utf8JsonWriter writer, TComponent element);
	public delegate TComponent ReadComponentFn(ref Utf8JsonReader reader);
	public delegate bool TryParseFn(string key, out TComponent value);

	private static readonly int ComponentCount = sizeof(T) / sizeof(TComponent);

	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
	{
		var values = MemoryMarshal.Cast<T, TComponent>(new Span<T>(ref value));
		writer.WritePropertyName(string.Join(',', [..values]));
	}

	public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var comps = reader.GetString()?.Split(',');
		if (comps == null || comps.Length < ComponentCount)
			return default;
		
		Span<TComponent> values = stackalloc TComponent[ComponentCount];
		for (int i = 0; i < values.Length; i ++)
		{
			if (TryParseComponent(comps[i], out var it))
				values[i] = it;
		}
		return MemoryMarshal.Cast<TComponent, T>(values)[0];
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Span<TComponent> values = stackalloc TComponent[ComponentCount];

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName)
			{
				reader.Skip();
				continue;
			}

			var component = reader.GetString();
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
			{
				reader.Skip();
				continue;
			}

			for (int i = 0; i < Components.Length; i ++)
			for (int j = 0; j < Components[i].Length; j ++)
				if (Components[i][j].Equals(component, StringComparison.OrdinalIgnoreCase))
				{
					values[i] = ReadComponent(ref reader);
					goto NEXT;
				}

			reader.Skip();
		NEXT:;
		}
		
		return MemoryMarshal.Cast<TComponent, T>(values)[0];
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		var values = MemoryMarshal.Cast<T, TComponent>([value]);
		writer.WriteStartObject();
		for (int i = 0; i < values.Length; i ++)
		{
			writer.WritePropertyName(Components[i][0]);
			WriteComponent(writer, values[i]);
		}
		writer.WriteEndObject();
	}
}