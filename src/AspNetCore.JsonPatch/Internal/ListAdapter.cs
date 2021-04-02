// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AspNetCore.JsonPatch.Adapters;
using Microsoft.Extensions.Internal;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal class ListAdapter : IAdapter
    {
        /// <inheritdoc />
        public virtual bool TryAdd(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            var list = (IList) target;

            if (!TryGetListTypeArgument(list, out var typeArgument, out errorMessage))
                return false;

            if (!TryGetPositionInfo(list, segment, OperationType.Add, out var positionInfo, out errorMessage))
                return false;

            if (!TryConvertValue(value, typeArgument, segment, jsonSerializerOptions, out var convertedValue, out errorMessage))
                return false;

            if (positionInfo.Type == PositionType.EndOfList)
                list.Add(convertedValue);
            else
                list.Insert(positionInfo.Index, convertedValue);

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryGet(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? value, out string? errorMessage)
        {
            var list = (IList) target;

            if (!TryGetListTypeArgument(list, out _, out errorMessage))
            {
                value = null;
                return false;
            }

            if (!TryGetPositionInfo(list, segment, OperationType.Get, out var positionInfo, out errorMessage))
            {
                value = null;
                return false;
            }

            value = positionInfo.Type == PositionType.EndOfList
                ? list[^1]
                : list[positionInfo.Index];

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryRemove(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out string? errorMessage)
        {
            var list = (IList) target;

            if (!TryGetListTypeArgument(list, out _, out errorMessage))
                return false;

            if (!TryGetPositionInfo(list, segment, OperationType.Remove, out var positionInfo, out errorMessage))
                return false;

            if (positionInfo.Type == PositionType.EndOfList)
                list.RemoveAt(list.Count - 1);
            else
                list.RemoveAt(positionInfo.Index);

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryReplace(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            var list = (IList) target;

            if (!TryGetListTypeArgument(list, out var typeArgument, out errorMessage))
                return false;

            if (!TryGetPositionInfo(list, segment, OperationType.Replace, out var positionInfo, out errorMessage))
                return false;

            if (!TryConvertValue(value, typeArgument, segment, jsonSerializerOptions, out var convertedValue, out errorMessage))
                return false;

            if (positionInfo.Type == PositionType.EndOfList)
                list[^1] = convertedValue;
            else
                list[positionInfo.Index] = convertedValue;

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryTest(object target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, object? value, out string? errorMessage)
        {
            var list = (IList) target;

            if (!TryGetListTypeArgument(list, out var typeArgument, out errorMessage))
                return false;

            if (!TryGetPositionInfo(list, segment, OperationType.Replace, out var positionInfo, out errorMessage))
                return false;

            if (!TryConvertValue(value, typeArgument, segment, jsonSerializerOptions, out var convertedValue, out errorMessage))
                return false;

            var currentValue = list[positionInfo.Index];
            if (!string.Equals(JsonSerializer.Serialize(currentValue, jsonSerializerOptions), JsonSerializer.Serialize(convertedValue, jsonSerializerOptions)))
            {
                errorMessage = $"The current value '{currentValue}' at position '{positionInfo.Index}' is not equal to the test value '{value}'.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public virtual bool TryTraverse(object? target, Type targetType, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? nextTarget, out string? errorMessage)
        {
            if (target is not IList list)
            {
                nextTarget = null;
                errorMessage = null;
                return false;
            }

            if (!int.TryParse(segment, out var index))
            {
                nextTarget = null;
                errorMessage = $"The path segment '{segment}' is invalid for an array index.";
                return false;
            }

            if (index < 0 || index >= list.Count)
            {
                nextTarget = null;
                errorMessage = $"The index value provided by path segment '{segment}' is out of bounds of the array size.";
                return false;
            }

            nextTarget = list[index];
            errorMessage = null;
            return true;
        }

        /// <summary>
        ///     Tries to convert a value to the item type of the collection.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="listTypeArgument">The item type of the collection.</param>
        /// <param name="segment">The current segment.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="convertedValue">The converted value; or null if the value could not be converted.</param>
        /// <param name="errorMessage">An optional error message if the key could not be converted; otherwise, null.</param>
        /// <returns>True if the value was converted; otherwise, false.</returns>
        protected virtual bool TryConvertValue(object? value, Type listTypeArgument, string? segment, JsonSerializerOptions jsonSerializerOptions, out object? convertedValue, out string? errorMessage)
        {
            var conversionResult = ConversionResultProvider.ConvertTo(value, listTypeArgument, jsonSerializerOptions);
            if (!conversionResult.CanBeConverted)
            {
                convertedValue = null;
                errorMessage = $"The value '{value}' is invalid for target location.";
                return false;
            }

            convertedValue = conversionResult.ConvertedInstance;
            errorMessage = null;
            return true;
        }

        /// <summary>
        ///     Tries to retrieve the item type of the list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="listTypeArgument">The item type of the list if successfully retrieved; otherwise, null.</param>
        /// <param name="errorMessage">An optional error message if the item type could not be retrieved; otherwise, null.</param>
        /// <returns>True if the item type could be retrieved; otherwise, false.</returns>
        protected virtual bool TryGetListTypeArgument(IList list, [MaybeNullWhen(false)] out Type listTypeArgument, out string? errorMessage)
        {
            // Arrays are not supported as they have fixed size and operations like Add, Insert do not make sense
            var listType = list.GetType();
            if (listType.IsArray)
            {
                errorMessage = $"The type '{listType.FullName}' which is an array is not supported for json patch operations as it has a fixed size.";
                listTypeArgument = null;
                return false;
            }

            var genericList = ClosedGenericMatcher.ExtractGenericInterface(listType, typeof(IList<>));
            if (genericList == null)
            {
                errorMessage = $"The type '{listType.FullName}' which is a non generic list is not supported for json patch operations. Only generic list types are supported.";
                listTypeArgument = null;
                return false;
            }

            listTypeArgument = genericList.GenericTypeArguments[0];
            errorMessage = null;
            return true;
        }

        /// <summary>
        ///     Tries to retrieve the <see cref="PositionInfo"/> based on the segment.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="segment">The segment.</param>
        /// <param name="operationType">The operation type.</param>
        /// <param name="positionInfo">The <see cref="PositionInfo"/> if successfully retrieved; otherwise, null.</param>
        /// <param name="errorMessage">An optional error message if the <see cref="PositionInfo"/> could not be retrieved; otherwise, null.</param>
        /// <returns>True if the <see cref="PositionInfo"/> could be retrieved; otherwise, false.</returns>
        protected virtual bool TryGetPositionInfo(IList list, string? segment, OperationType operationType, out PositionInfo positionInfo, out string? errorMessage)
        {
            if (segment == "-")
            {
                positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                errorMessage = null;
                return true;
            }

            if (int.TryParse(segment, out var position))
            {
                if (position >= 0 && position < list.Count)
                {
                    positionInfo = new PositionInfo(PositionType.Index, position);
                    errorMessage = null;
                    return true;
                }
                // As per JSON Patch spec, for Add operation the index value representing the number of elements is valid,
                // where as for other operations like Remove, Replace, Move and Copy the target index MUST exist.

                if (position == list.Count && operationType == OperationType.Add)
                {
                    positionInfo = new PositionInfo(PositionType.EndOfList, -1);
                    errorMessage = null;
                    return true;
                }

                positionInfo = new PositionInfo(PositionType.OutOfBounds, position);
                errorMessage = $"The index value provided by path segment '{segment}' is out of bounds of the array size.";
                return false;
            }

            positionInfo = new PositionInfo(PositionType.Invalid, -1);
            errorMessage = $"The path segment '{segment}' is invalid for an array index.";
            return false;
        }

        /// <summary>
        ///     This API supports infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected readonly struct PositionInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PositionInfo"/> class.
            /// </summary>
            /// <param name="type">The kind of position.</param>
            /// <param name="index">The index in the collection.</param>
            public PositionInfo(PositionType type, int index)
            {
                Type = type;
                Index = index;
            }

            /// <summary>
            ///     The kind of position.
            /// </summary>
            public PositionType Type { get; }

            /// <summary>
            ///     The index in the collection.
            /// </summary>
            public int Index { get; }
        }

        /// <summary>
        ///     This API supports infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected enum PositionType
        {
            /// <summary>
            /// valid index
            /// </summary>
            Index,

            /// <summary>
            /// '-'
            /// </summary>
            EndOfList,

            /// <summary>
            ///     Ex: not an integer
            /// </summary>
            Invalid,

            /// <summary>
            ///     Index out of bounds
            /// </summary>
            OutOfBounds
        }

        /// <summary>
        ///     This API supports infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected enum OperationType
        {
            /// <summary>
            ///     Add item to the list.
            /// </summary>
            Add,

            /// <summary>
            ///     Remove an item from the list.
            /// </summary>
            Remove,

            /// <summary>
            ///     Get an item from the list.
            /// </summary>
            Get,

            /// <summary>
            ///     Replace an item in the list.
            /// </summary>
            Replace
        }
    }
}
