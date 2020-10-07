// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public interface IAdapter
    {
        /// <summary>
        ///     Tries to find the property with the name matching the segment.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The current segment of the path.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="nextTarget">The instance of the property if it's a complex type.</param>
        /// <param name="errorMessage">An optional error message if the system cannot traverse the target instance.</param>
        /// <returns>True if the a property matching the segment is found; otherwise, false.</returns>
        bool TryTraverse(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? nextTarget, out string? errorMessage);

        /// <summary>
        ///     Tries to add a value to the property found using the segment on the target instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The type of the target instance.</param>
        /// <param name="segment">The current segment of the path.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="errorMessage">An optional error message if the system failed to add the value.</param>
        /// <returns>True if the value was successfully added; otherwise, false.</returns>
        bool TryAdd(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage);

        /// <summary>
        ///     Tries to remove a value from the property found using the segment on the target instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The current segment of the path.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="errorMessage">An optional error message if the system failed to remove a value.</param>
        /// <returns>True if a value was successfully removed; otherwise, false.</returns>
        bool TryRemove(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out string? errorMessage);

        /// <summary>
        ///     Tries to retrieve a value from the property found using the segment on the target instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The current segment of the path.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="value">The value if found; otherwise, null.</param>
        /// <param name="errorMessage">An optional error message if the system failed to retrieve a value.</param>
        /// <returns>True if a value was found; otherwise, false.</returns>
        bool TryGet(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? value, out string? errorMessage);

        /// <summary>
        ///     Tries to replace a value from the property found using the segment on the target instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The current segment of the path.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="value">The value to replace with.</param>
        /// <param name="errorMessage">An optional error message if the system failed to replace the value.</param>
        /// <returns>True if the value was replaced; otherwise, false.</returns>
        bool TryReplace(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage);

        /// <summary>
        ///     Tries to test a value from the property found using the segment on the target instance with another value.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="segment">The current segment of the path.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="value">The value to compare with.</param>
        /// <param name="errorMessage">An optional error message if the system failed to compare the values.</param>
        /// <returns>True if the values are equal; otherwise, false.</returns>
        bool TryTest(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage);
    }
}
