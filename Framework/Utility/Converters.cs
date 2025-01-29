using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework.JsonConverters;

public class Vector2 : JsonConverter<System.Numerics.Vector2>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] System.Numerics.Vector2 value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y}");

	public override System.Numerics.Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		System.Numerics.Vector2 value = new();
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					break;

				if (reader.TokenType == JsonTokenType.PropertyName)
				{
					var component = reader.GetString();
					if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
						continue;

					switch (component)
					{
					case "x": value.X = reader.GetSingle(); break;
					case "X": value.X = reader.GetSingle(); break;
					case "y": value.Y = reader.GetSingle(); break;
					case "Y": value.Y = reader.GetSingle(); break;
					}
				}
			}
		}
		return value;
	}

	public override void Write(Utf8JsonWriter writer, System.Numerics.Vector2 value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("X", value.X);
		writer.WriteNumber("Y", value.Y);
		writer.WriteEndObject();
	}
}