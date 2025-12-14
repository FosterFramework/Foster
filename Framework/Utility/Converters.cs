using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Foster.Framework.JsonConverters;

public class Point2Converter : JsonConverter<Point2>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Point2 value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y}");

	public override Point2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Point2 value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetInt32(); break;
			case "y" or "Y": value.Y = reader.GetInt32(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Point2 value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y}}}");
}

public class Vector2Converter : JsonConverter<Vector2>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Vector2 value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y}");

	public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Vector2 value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetSingle(); break;
			case "y" or "Y": value.Y = reader.GetSingle(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y}}}");
}

public class Point3Converter : JsonConverter<Point3>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Point3 value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y},{value.Z}");

	public override Point3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Point3 value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetInt32(); break;
			case "y" or "Y": value.Y = reader.GetInt32(); break;
			case "z" or "Z": value.Z = reader.GetInt32(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Point3 value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y},\"Z\":{value.Z}}}");
}

public class Vector3Converter : JsonConverter<Vector3>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Vector3 value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y},{value.Z}");

	public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Vector3 value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetSingle(); break;
			case "y" or "Y": value.Y = reader.GetSingle(); break;
			case "z" or "Z": value.Z = reader.GetSingle(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y},\"Z\":{value.Z}}}");
}

public class Vector4Converter : JsonConverter<Vector4>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Vector4 value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y},{value.Z},{value.W}");

	public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Vector4 value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetSingle(); break;
			case "y" or "Y": value.Y = reader.GetSingle(); break;
			case "z" or "Z": value.Z = reader.GetSingle(); break;
			case "w" or "W": value.W = reader.GetSingle(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y},\"W\":{value.Z},\"H\":{value.W}}}");
}

public class CircleConverter : JsonConverter<Circle>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Circle value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.Position.X},{value.Position.Y},{value.Radius}");

	public override Circle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Circle value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.Position.X = reader.GetSingle(); break;
			case "y" or "Y": value.Position.Y = reader.GetSingle(); break;
			case "r" or "R" or "radius" or "Radius": value.Radius = reader.GetSingle(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Circle value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.Position.X},\"Y\":{value.Position.Y},\"Z\":{value.Radius}}}");
}

public class RectConverter : JsonConverter<Rect>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] Rect value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y},{value.Width},{value.Height}");

	public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		Rect value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetSingle(); break;
			case "y" or "Y": value.Y = reader.GetSingle(); break;
			case "w" or "W" or "width" or "Width": value.Width = reader.GetSingle(); break;
			case "h" or "H" or "height" or "Height": value.Height = reader.GetSingle(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y},\"W\":{value.Width},\"H\":{value.Height}}}");
}

public class RectIntConverter : JsonConverter<RectInt>
{
	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] RectInt value, JsonSerializerOptions options)
		=> writer.WritePropertyName($"{value.X},{value.Y},{value.Width},{value.Height}");

	public override RectInt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			return default;

		RectInt value = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() is not {} component)
				continue;
			if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
				continue;

			switch (component)
			{
			case "x" or "X": value.X = reader.GetInt32(); break;
			case "y" or "Y": value.Y = reader.GetInt32(); break;
			case "w" or "W" or "width" or "Width": value.Width = reader.GetInt32(); break;
			case "h" or "H" or "height" or "Height": value.Height = reader.GetInt32(); break;
			}
		}
		
		return value;
	}

	public override void Write(Utf8JsonWriter writer, RectInt value, JsonSerializerOptions options)
		=> writer.WriteRawValue($"{{\"X\":{value.X},\"Y\":{value.Y},\"W\":{value.Width},\"H\":{value.Height}}}");
}

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
