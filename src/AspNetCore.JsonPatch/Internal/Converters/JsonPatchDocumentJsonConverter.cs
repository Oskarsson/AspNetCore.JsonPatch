// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Converters
{
    /// <summary>
    ///     Converts an <see cref="JsonPatchDocument{TModel}" /> to or from JSON.
    /// </summary>
    internal class JsonPatchDocumentJsonConverter : JsonConverter<JsonPatchDocument>
    {
        /// <inheritdoc />
        public override JsonPatchDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                var operations = JsonSerializer.Deserialize<List<Operation>>(ref reader, options) ??
                                 new List<Operation>();

                return new JsonPatchDocument(operations, options);
            }
            catch (Exception ex)
            {
                throw new JsonException("The JSON patch document was malformed and could not be parsed.", ex);
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, JsonPatchDocument value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Operations, options);
        }
    }
}
