// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AspNetCore.JsonPatch.Adapters;
using CSharpBinder = Microsoft.CSharp.RuntimeBinder;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class DynamicObjectAdapter : IAdapter
    {
        /// <inheritdoc />
        public virtual bool TryAdd(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            if (!TrySetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, value, out errorMessage))
                return false;

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryGet(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? value, out string? errorMessage)
        {
            if (!TryGetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, out value, out errorMessage))
            {
                value = null;
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryRemove(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out string? errorMessage)
        {
            if (!TryGetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, out var property, out errorMessage))
                return false;

            // Setting the value to "null" will use the default value in case of value types, and
            // null in case of reference types
            object? value = null;
            if (property.GetType().GetTypeInfo().IsValueType
                && Nullable.GetUnderlyingType(property.GetType()) == null)
                value = Activator.CreateInstance(property.GetType());

            if (!TrySetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, value, out errorMessage))
                return false;

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryReplace(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            if (!TryGetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, out var property, out errorMessage))
                return false;

            if (!TryConvertValue(value, property.GetType(), jsonSerializerOptions, out var convertedValue))
            {
                errorMessage = $"The value '{value}' is invalid for target location.";
                return false;
            }

            if (!TryRemove(target, targetType, segment, jsonSerializerOptions, out errorMessage))
                return false;

            if (!TrySetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, convertedValue, out errorMessage))
                return false;

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryTest(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            if (!TryGetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, out var property, out errorMessage))
                return false;

            if (!TryConvertValue(value, property.GetType(), jsonSerializerOptions, out var convertedValue))
            {
                errorMessage = $"The value '{value}' is invalid for target location.";
                return false;
            }

            if (!string.Equals(JsonSerializer.Serialize(property, jsonSerializerOptions), JsonSerializer.Serialize(convertedValue, jsonSerializerOptions)))
            {
                errorMessage = $"The current value '{property}' at path '{segment}' is not equal to the test value '{value}'.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryTraverse(object? target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? nextTarget, out string? errorMessage)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!TryGetDynamicObjectProperty(target, targetType, segment, jsonSerializerOptions, out var property, out errorMessage))
            {
                nextTarget = null;
                return false;
            }

            nextTarget = property;
            errorMessage = null;
            return true;
        }

        /// <summary>
        ///     Tries to retrieve a value from a dynamic property.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The name of the property.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="value">The value if the dynamic property was found; otherwise, null.</param>
        /// <param name="errorMessage">An optional error message if the object property could not be found; otherwise, null.</param>
        /// <returns>True if the dynamic property was found; otherwise, false.</returns>
        protected virtual bool TryGetDynamicObjectProperty(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, [MaybeNullWhen(false)] out object value, out string? errorMessage)
        {
            if (segment == null)
                throw new ArgumentNullException(segment);

            var propertyName = jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;

            var binder = CSharpBinder.Binder.GetMember(CSharpBinder.CSharpBinderFlags.None, propertyName, targetType, new List<CSharpBinder.CSharpArgumentInfo>
            {
                CSharpBinder.CSharpArgumentInfo.Create(CSharpBinder.CSharpArgumentInfoFlags.None, null)
            });

            var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);

            try
            {
                value = callsite.Target(callsite, target);
                errorMessage = null;
                return true;
            }
            catch (CSharpBinder.RuntimeBinderException)
            {
                value = null;
                errorMessage = $"The target location specified by path segment '{segment}' was not found.";
                return false;
            }
        }

        /// <summary>
        ///     Tries to set a value to a dynamic property.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The name of the property.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="errorMessage">An optional error message if the value could not be set; otherwise, false.</param>
        /// <returns>True if the value was successfully set; otherwise, false.</returns>
        protected virtual bool TrySetDynamicObjectProperty(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            if (segment == null)
                throw new ArgumentNullException(segment);

            var propertyName = jsonSerializerOptions.DictionaryKeyPolicy?.ConvertName(segment) ?? segment;

            var binder = CSharpBinder.Binder.SetMember(CSharpBinder.CSharpBinderFlags.None, propertyName, targetType, new List<CSharpBinder.CSharpArgumentInfo>
            {
                CSharpBinder.CSharpArgumentInfo.Create(CSharpBinder.CSharpArgumentInfoFlags.None, null),
                CSharpBinder.CSharpArgumentInfo.Create(CSharpBinder.CSharpArgumentInfoFlags.None, null)
            });

            var callsite = CallSite<Func<CallSite, object, object?, object>>.Create(binder);

            try
            {
                callsite.Target(callsite, target, value);
                errorMessage = null;
                return true;
            }
            catch (CSharpBinder.RuntimeBinderException)
            {
                errorMessage = $"The target location specified by path segment '{segment}' was not found.";
                return false;
            }
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
