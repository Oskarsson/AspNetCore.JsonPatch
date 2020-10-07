// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspNetCore.JsonPatch.Converters
{
    internal class TypedJsonPatchDocumentConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
                return false;

            var type = typeToConvert;
            if (!type.IsGenericTypeDefinition)
                type = type.GetGenericTypeDefinition();

            return type == typeof(JsonPatchDocument<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var keyType = typeToConvert.GenericTypeArguments[0];
            var converterType = typeof(TypedJsonPatchDocumentConverter<>).MakeGenericType(keyType);
            return (JsonConverter) (Activator.CreateInstance(converterType) ?? throw new InvalidOperationException());
        }
    }
}
