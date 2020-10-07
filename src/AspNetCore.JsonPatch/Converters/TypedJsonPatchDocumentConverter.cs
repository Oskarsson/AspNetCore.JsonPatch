// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Converters
{
    internal class TypedJsonPatchDocumentConverter<TModel> : JsonConverter<JsonPatchDocument<TModel>?> where TModel : class
    {
        public override JsonPatchDocument<TModel>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException("The current TokenType must be of type StartArray.");

                var clone = new JsonSerializerOptions(options);
                clone.Converters.Add(new ObjectJsonConverter());

                var operations = JsonSerializer.Deserialize<List<Operation<TModel>>>(ref reader, clone) ?? new List<Operation<TModel>>();

                return new JsonPatchDocument<TModel>(operations, options);
            }
            catch (Exception ex)
            {
                throw new JsonException("The JSON patch document was malformed and could not be parsed.", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, JsonPatchDocument<TModel>? value, JsonSerializerOptions options)
        {
            if (value is not IJsonPatchDocument patchDocument)
                return;

            var operations = patchDocument.GetOperations();

            JsonSerializer.Serialize(writer, operations, options);
        }
    }
}
