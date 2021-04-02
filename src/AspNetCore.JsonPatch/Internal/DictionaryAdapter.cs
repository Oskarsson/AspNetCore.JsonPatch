// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AspNetCore.JsonPatch.Adapters;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal class DictionaryAdapter<TKey, TValue> : IAdapter where TKey : notnull
    {
        /// <inheritdoc />
        public virtual bool TryAdd(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            var key = segment == null ? null : jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;
            var dictionary = (IDictionary<TKey, TValue?>) target;

            // As per JsonPatch spec, if a key already exists, adding should replace the existing value
            if (!TryConvertKey(key, jsonSerializerOptions, out var convertedKey, out errorMessage))
                return false;

            if (!TryConvertValue(value, jsonSerializerOptions, out var convertedValue, out errorMessage))
                return false;

            dictionary[convertedKey] = convertedValue;
            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryGet(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? value, out string? errorMessage)
        {
            var key = segment == null ? null : jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;
            var dictionary = (IDictionary<TKey, TValue>) target;

            if (!TryConvertKey(key, jsonSerializerOptions, out var convertedKey, out errorMessage))
            {
                value = null;
                return false;
            }

            if (!dictionary.ContainsKey(convertedKey))
            {
                value = null;
                errorMessage = $"The target location specified by path segment '{segment}' was not found.";
                return false;
            }

            value = dictionary[convertedKey];
            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryRemove(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out string? errorMessage)
        {
            var key = segment == null ? null : jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;
            var dictionary = (IDictionary<TKey, TValue>) target;

            if (!TryConvertKey(key, jsonSerializerOptions, out var convertedKey, out errorMessage)) return false;

            // As per JsonPatch spec, the target location must exist for remove to be successful
            if (!dictionary.ContainsKey(convertedKey))
            {
                errorMessage = $"The target location specified by path segment '{segment}' was not found.";
                return false;
            }

            dictionary.Remove(convertedKey);

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryReplace(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            var key = segment == null ? null : jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;
            var dictionary = (IDictionary<TKey, TValue?>) target;

            if (!TryConvertKey(key, jsonSerializerOptions, out var convertedKey, out errorMessage))
                return false;

            // As per JsonPatch spec, the target location must exist for remove to be successful
            if (!dictionary.ContainsKey(convertedKey))
            {
                errorMessage = $"The target location specified by path segment '{segment}' was not found.";
                return false;
            }

            if (!TryConvertValue(value, jsonSerializerOptions, out var convertedValue, out errorMessage))
                return false;

            dictionary[convertedKey] = convertedValue;

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryTest(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            var key = segment == null ? null : jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;
            var dictionary = (IDictionary<TKey, TValue>) target;

            if (!TryConvertKey(key, jsonSerializerOptions, out var convertedKey, out errorMessage))
                return false;

            // As per JsonPatch spec, the target location must exist for test to be successful
            if (!dictionary.ContainsKey(convertedKey))
            {
                errorMessage = $"The target location specified by path segment '{segment}' was not found.";
                return false;
            }

            if (!TryConvertValue(value, jsonSerializerOptions, out var convertedValue, out errorMessage))
                return false;

            var currentValue = dictionary[convertedKey];

            // The target segment does not have an assigned value to compare the test value with
            if (currentValue == null || string.IsNullOrEmpty(currentValue.ToString()))
            {
                errorMessage = $"The value at '{segment}' cannot be null or empty to perform the test operation.";
                return false;
            }

            if (!string.Equals(JsonSerializer.Serialize(currentValue, jsonSerializerOptions), JsonSerializer.Serialize(convertedValue, jsonSerializerOptions)))
            {
                errorMessage = $"The current value '{currentValue}' at path '{segment}' is not equal to the test value '{value}'.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryTraverse(object? target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? nextTarget, out string? errorMessage)
        {
            var key = segment == null ? null : jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;
            if (target is not IDictionary<TKey, TValue> dictionary)
            {
                nextTarget = null;
                errorMessage = null;
                return false;
            }

            if (!TryConvertKey(key, jsonSerializerOptions, out var convertedKey, out errorMessage))
            {
                nextTarget = null;
                return false;
            }

            if (dictionary.ContainsKey(convertedKey))
            {
                nextTarget = dictionary[convertedKey];
                errorMessage = null;
                return true;
            }

            nextTarget = null;
            errorMessage = null;
            return false;
        }

        /// <summary>
        ///     Tries to convert a key.
        /// </summary>
        /// <param name="key">The key to convert.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="convertedKey">The converted key.</param>
        /// <param name="errorMessage">An optional error message if the key could not be converted; otherwise, null.</param>
        /// <returns>True if the key was converted; otherwise, false.</returns>
        protected virtual bool TryConvertKey(string? key, JsonSerializerOptions jsonSerializerOptions, [MaybeNullWhen(false)] out TKey convertedKey, out string? errorMessage)
        {
            var conversionResult = ConversionResultProvider.ConvertTo(key, typeof(TKey), jsonSerializerOptions);
            if (conversionResult.CanBeConverted && conversionResult.ConvertedInstance != null)
            {
                errorMessage = null;
                convertedKey = (TKey) conversionResult.ConvertedInstance;
                return true;
            }

            errorMessage = $"The provided path segment '{key}' cannot be converted to the target type.";
            convertedKey = default;
            return false;
        }

        /// <summary>
        ///     Tries to convert a value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="convertedValue">The converted value.</param>
        /// <param name="errorMessage">An optional error message if the key could not be converted; otherwise, null.</param>
        /// <returns>True if the value was converted; otherwise, false.</returns>
        protected virtual bool TryConvertValue(object? value, JsonSerializerOptions jsonSerializerOptions, [MaybeNull] out TValue convertedValue, out string? errorMessage)
        {
            var conversionResult = ConversionResultProvider.ConvertTo(value, typeof(TValue), jsonSerializerOptions);
            if (conversionResult.CanBeConverted)
            {
                errorMessage = null;
                convertedValue = conversionResult.ConvertedInstance != null ? (TValue) conversionResult.ConvertedInstance : default;
                return true;
            }

            errorMessage = $"The value '{value}' is invalid for target location.";
            convertedValue = default;
            return false;
        }
    }
}
