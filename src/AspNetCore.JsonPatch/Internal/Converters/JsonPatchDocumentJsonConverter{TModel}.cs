// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Converters
{
    internal class JsonPatchDocumentJsonConverter<TModel> : JsonConverter<JsonPatchDocument<TModel>> where TModel : class
    {
        /// <inheritdoc />
        public override JsonPatchDocument<TModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                var operations = JsonSerializer.Deserialize<List<Operation<TModel>>>(ref reader, options);
                return new JsonPatchDocument<TModel>(operations!, options);
            }
            catch (Exception exception)
            {
                throw new JsonException("The JSON patch document was malformed and could not be parsed.", exception);
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, JsonPatchDocument<TModel> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.Operations, options);
        }
    }
}
