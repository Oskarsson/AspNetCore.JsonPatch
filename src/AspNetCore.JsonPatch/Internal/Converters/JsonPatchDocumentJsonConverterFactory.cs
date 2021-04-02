// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspNetCore.JsonPatch.Converters
{
    internal class JsonPatchDocumentJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }

        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert.IsGenericType)
            {
                var modelType = typeToConvert.GenericTypeArguments[0];
                var converterType = typeof(JsonPatchDocumentJsonConverter<>).MakeGenericType(modelType);
                return Activator.CreateInstance(converterType) as JsonConverter;
            }

            return new JsonPatchDocumentJsonConverter();
        }
    }
}
