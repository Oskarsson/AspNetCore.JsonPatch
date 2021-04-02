using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Converters
{
    internal abstract class BaseOperationJsonConverter<T> : JsonConverter<T> where T : Operation
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            string? op = null;
            string? path = null;
            string? from = null;
            object? value = null;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                var propertyName = reader.GetString();

                if (!reader.Read())
                    throw new JsonException();

                switch (propertyName)
                {
                    case "op":
                        op = reader.GetString();
                        break;
                    case "path":
                        path = reader.GetString();
                        break;
                    case "from":
                        from = reader.GetString();
                        break;
                    case "value":
                        value = ReadObject(ref reader);
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(op))
                throw new JsonException("Op cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(path))
                throw new JsonException("Path cannot be null or empty.");

            return CreateInstance(op, path, from, value);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            switch (value.OperationType)
            {
                case OperationType.Add:
                    writer.WriteString("op", "add");
                    writer.WritePropertyName("value");
                    JsonSerializer.Serialize(writer, value.Value, options);
                    break;
                case OperationType.Remove:
                    writer.WriteString("op", "remove");
                    break;
                case OperationType.Replace:
                    writer.WriteString("op", "replace");
                    writer.WritePropertyName("value");
                    JsonSerializer.Serialize(writer, value.Value, options);
                    break;
                case OperationType.Move:
                    writer.WriteString("op", "move");
                    writer.WriteString("from", value.From);
                    break;
                case OperationType.Copy:
                    writer.WriteString("op", "copy");
                    writer.WriteString("from", value.From);
                    break;
                case OperationType.Test:
                    writer.WriteString("op", "test");
                    writer.WritePropertyName("value");
                    JsonSerializer.Serialize(writer, value.Value, options);
                    break;
                default: throw new JsonException($"Operation Type {value.OperationType} is not supported.");
            }

            writer.WriteString("path", value.Path);

            writer.WriteEndObject();
        }

        protected abstract T CreateInstance(string op, string path, string? from, object? value);

        private static object? ReadObject(ref Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt64(out var value) => value,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String when reader.TryGetDateTime(out var value) => value,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Null => null,
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
            };
        }
    }
}
