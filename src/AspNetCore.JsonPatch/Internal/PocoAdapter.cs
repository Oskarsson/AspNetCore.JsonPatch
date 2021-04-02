// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Adapters;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal class PocoAdapter : IAdapter
    {
        /// <inheritdoc />
        public virtual bool TryAdd(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            return TryReplace(target, targetType, segment, jsonSerializerOptions, value, out errorMessage);
        }

        /// <inheritdoc />
        public virtual bool TryGet(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? value, out string? errorMessage)
        {
            if (TryGetPropertyInfo(targetType, segment, out var propertyInfo))
            {
                if (!propertyInfo.CanRead)
                {
                    errorMessage = $"The property at '{segment}' could not be read.";
                    value = null;
                    return false;
                }

                value = propertyInfo.GetValue(target);
                errorMessage = null;
                return true;
            }

            if (TryGetFieldInfo(targetType, segment, out var fieldInfo))
            {
                if (fieldInfo.IsInitOnly)
                {
                    errorMessage = $"The field at '{segment}' could not be read.";
                    value = null;
                    return false;
                }

                value = fieldInfo.GetValue(target);
                errorMessage = null;
                return true;
            }

            errorMessage = $"The target location specified by path segment '{segment}' was not found.";
            value = null;
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryRemove(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out string? errorMessage)
        {
            if (TryGetPropertyInfo(targetType, segment, out var propertyInfo))
            {
                if (!propertyInfo.CanWrite)
                {
                    errorMessage = $"The property at path '{segment}' could not be updated.";
                    return false;
                }

                // Setting the value to "null" will use the default value in case of value types, and
                // null in case of reference types
                object? value = null;
                if (propertyInfo.PropertyType.IsValueType && Nullable.GetUnderlyingType(propertyInfo.PropertyType) == null)
                    value = Activator.CreateInstance(propertyInfo.PropertyType);

                propertyInfo.SetValue(target, value);

                errorMessage = null;
                return true;
            }

            if (TryGetFieldInfo(targetType, segment, out var fieldInfo))
            {
                if (fieldInfo.IsInitOnly)
                {
                    errorMessage = $"The field at path '{segment}' could not be updated.";
                    return false;
                }

                // Setting the value to "null" will use the default value in case of value types, and
                // null in case of reference types
                object? value = null;
                if (fieldInfo.FieldType.IsValueType && Nullable.GetUnderlyingType(fieldInfo.FieldType) == null)
                    value = Activator.CreateInstance(fieldInfo.FieldType);

                fieldInfo.SetValue(target, value);

                errorMessage = null;
                return true;
            }

            errorMessage = $"The target location specified by path segment '{segment}' was not found.";
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryReplace(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            if (TryGetPropertyInfo(targetType, segment, out var propertyInfo))
            {
                if (!propertyInfo.CanWrite)
                {
                    errorMessage = $"The property at path '{segment}' could not be updated.";
                    return false;
                }

                if (!TryConvertValue(value, propertyInfo.PropertyType, jsonSerializerOptions, out var convertedValue))
                {
                    errorMessage = $"The value '{value}' is invalid for target location.";
                    return false;
                }

                propertyInfo.SetValue(target, convertedValue);
                errorMessage = null;
                return true;
            }

            if (TryGetFieldInfo(targetType, segment, out var fieldInfo))
            {
                if (fieldInfo.IsInitOnly)
                {
                    errorMessage = $"The field at path '{segment}' could not be updated.";
                    return false;
                }

                if (!TryConvertValue(value, fieldInfo.FieldType, jsonSerializerOptions, out var convertedValue))
                {
                    errorMessage = $"The value '{value}' is invalid for target location.";
                    return false;
                }

                fieldInfo.SetValue(target, convertedValue);
                errorMessage = null;
                return true;
            }

            errorMessage = $"The target location specified by path segment '{segment}' was not found.";
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryTest(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            if (TryGetPropertyInfo(targetType, segment, out var propertyInfo))
            {
                if (!propertyInfo.CanRead)
                {
                    errorMessage = $"The property at '{segment}' could not be read.";
                    return false;
                }

                if (!TryConvertValue(value, propertyInfo.PropertyType, jsonSerializerOptions, out var convertedValue))
                {
                    errorMessage = $"The value '{value}' is invalid for target location.";
                    return false;
                }

                var currentValue = propertyInfo.GetValue(target);
                if (!string.Equals(JsonSerializer.Serialize(currentValue, jsonSerializerOptions), JsonSerializer.Serialize(convertedValue, jsonSerializerOptions)))
                {
                    errorMessage = $"The current value '{currentValue}' at path '{segment}' is not equal to the test value '{value}'.";
                    return false;
                }

                errorMessage = null;
                return true;
            }

            if (TryGetFieldInfo(targetType, segment, out var fieldInfo))
            {
                if (!TryConvertValue(value, fieldInfo.FieldType, jsonSerializerOptions, out var convertedValue))
                {
                    errorMessage = $"The value '{value}' is invalid for target location.";
                    return false;
                }

                var currentValue = fieldInfo.GetValue(target);
                if (!string.Equals(JsonSerializer.Serialize(currentValue, jsonSerializerOptions), JsonSerializer.Serialize(convertedValue, jsonSerializerOptions)))
                {
                    errorMessage = $"The current value '{currentValue}' at path '{segment}' is not equal to the test value '{value}'.";
                    return false;
                }

                errorMessage = null;
                return true;
            }

            errorMessage = $"The target location specified by path segment '{segment}' was not found.";
            return false;
        }

        /// <inheritdoc />
        public virtual bool TryTraverse(object? target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? value, out string? errorMessage)
        {
            if (target == null)
            {
                value = null;
                errorMessage = null;
                return false;
            }

            if (TryGetPropertyInfo(targetType, segment, out var propertyInfo))
            {
                value = propertyInfo.GetValue(target);
                errorMessage = null;
                return true;
            }

            if (TryGetFieldInfo(targetType, segment, out var fieldInfo))
            {
                value = fieldInfo.GetValue(target);
                errorMessage = null;
                return true;
            }

            value = null;
            errorMessage = $"The target location specified by path segment '{segment}' was not found.";
            return false;
        }

        /// <summary>
        ///     Tries to retrieve the <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyInfo">The <see cref="PropertyInfo"/> if found; otherwise, null.</param>
        /// <returns>True if a <see cref="PropertyInfo"/> was found; otherwise, false.</returns>
        protected virtual bool TryGetPropertyInfo(Type targetType, string? propertyName, [MaybeNullWhen(false)] out PropertyInfo propertyInfo)
        {
            propertyInfo = targetType.GetProperties().FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase) || string.Equals(x.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            return propertyInfo != null;
        }

        /// <summary>
        ///     Tries to retrieve the <see cref="FieldInfo"/>.
        /// </summary>
        /// <param name="targetType">The target type.</param>
        /// <param name="propertyName">The name of the field.</param>
        /// <param name="fieldInfo">The <see cref="FieldInfo"/> if found; otherwise, null.</param>
        /// <returns>True if a <see cref="FieldInfo"/> was found; otherwise, false.</returns>
        protected virtual bool TryGetFieldInfo(Type targetType, string? propertyName, [MaybeNullWhen(false)] out FieldInfo fieldInfo)
        {
            fieldInfo = targetType.GetFields().FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.OrdinalIgnoreCase) || string.Equals(x.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            return fieldInfo != null;
        }

        /// <summary>
        ///     Tries to convert a value to the specified type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="convertedValue">The converted value.</param>
        /// <returns>True if the value was converted; otherwise, false.</returns>
        protected virtual bool TryConvertValue(object? value, Type targetType, JsonSerializerOptions jsonSerializerOptions, out object? convertedValue)
        {
            var conversionResult = ConversionResultProvider.ConvertTo(value, targetType, jsonSerializerOptions);
            if (!conversionResult.CanBeConverted)
            {
                convertedValue = null;
                return false;
            }

            convertedValue = conversionResult.ConvertedInstance;
            return true;
        }
    }
}
