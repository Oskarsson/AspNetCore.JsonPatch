// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Text.Json;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal static class ConversionResultProvider
    {
        /// <summary>
        ///     Converts a value to a different type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="typeToConvertTo">The type to convert to.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <returns>The <see cref="ConversionResult"/>.</returns>
        public static ConversionResult ConvertTo(object? value, Type typeToConvertTo, JsonSerializerOptions jsonSerializerOptions)
        {
            if (value == null)
                return new ConversionResult(IsNullableType(typeToConvertTo), null);

            if (typeToConvertTo.IsInstanceOfType(value))
                // No need to convert
                return new ConversionResult(true, value);

            try
            {
                var deserialized = Convert.ChangeType(value, typeToConvertTo);
                return new ConversionResult(true, deserialized);
            }
            catch
            {
                try
                {
                    var serialized = JsonSerializer.Serialize(value, jsonSerializerOptions);

                    var deserialized = JsonSerializer.Deserialize(serialized, typeToConvertTo, jsonSerializerOptions);
                    return new ConversionResult(true, deserialized);
                }
                catch
                {
                    return new ConversionResult(false, null);
                }
            }
        }

        /// <summary>
        ///     Converts a value to the target type and copy the value.
        /// </summary>
        /// <param name="value">The value to convert and copy.</param>
        /// <param name="typeToConvertTo">The type to convert to.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <returns>The <see cref="ConversionResult"/>.</returns>
        public static ConversionResult CopyTo(object? value, Type? typeToConvertTo, JsonSerializerOptions jsonSerializerOptions)
        {
            if (value == null)
                return new ConversionResult(true, null);

            if (typeToConvertTo == null)
                return new ConversionResult(false, null);

            if (typeToConvertTo.IsInstanceOfType(value))
                // Keep original type
                typeToConvertTo = value.GetType();

            try
            {
                var deserialized = JsonSerializer.Deserialize(JsonSerializer.Serialize(value, jsonSerializerOptions), typeToConvertTo, jsonSerializerOptions);
                return new ConversionResult(true, deserialized);
            }
            catch
            {
                //try
                //{
                //    var deserialized = Convert.ChangeType(value, typeToConvertTo);
                //    return new ConversionResult(true, deserialized);
                //}
                //catch
                //{
                    return new ConversionResult(false, null);
                //}
            }
        }

        private static bool IsNullableType(Type type)
        {
            if (type.IsValueType)
                // value types are only nullable if they are Nullable<T>
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

            // reference types are always nullable
            return true;
        }
    }
}
