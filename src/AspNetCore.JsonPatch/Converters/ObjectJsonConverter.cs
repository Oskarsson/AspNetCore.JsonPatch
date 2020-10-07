// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspNetCore.JsonPatch.Converters
{
    /// <summary>
    ///     Converts an <see cref="object" /> to or from JSON.
    /// </summary>
    public class ObjectJsonConverter : JsonConverter<object?>
    {
        /// <inheritdoc />
        public override object? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    if (reader.TryGetDouble(out var doubleValue))
                        return doubleValue;

                    if (reader.TryGetInt64(out var longValue))
                        return longValue;
                    break;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.Null:
                    return null;
            }

            using var document = JsonDocument.ParseValue(ref reader);

            return document.RootElement.Clone();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            throw new InvalidOperationException("Directly writing object not supported");
        }
    }
}
