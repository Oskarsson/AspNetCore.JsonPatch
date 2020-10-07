// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using AspNetCore.JsonPatch.Internal;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Adapters
{
    /// <inheritdoc />
    public class ObjectAdapter : IObjectAdapterWithTest
    {
        /// <summary>
        ///     Gets or sets the <see cref="IAdapterFactory" />
        /// </summary>
        public IAdapterFactory AdapterFactory { get; }

        /// <summary>
        ///     Action for logging <see cref="JsonPatchError" />.
        /// </summary>
        public Action<JsonPatchError>? LogErrorAction { get; }

        /// <summary>
        ///     The <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; }

        private Action<JsonPatchError> ErrorReporter => LogErrorAction ?? Internal.ErrorReporter.Default;

        /// <summary>
        ///     Initializes a new instance of <see cref="ObjectAdapter" />.
        /// </summary>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="logErrorAction">The <see cref="Action" /> for logging <see cref="JsonPatchError" />.</param>
        public ObjectAdapter(JsonSerializerOptions jsonSerializerOptions, Action<JsonPatchError> logErrorAction) :
            this(jsonSerializerOptions, logErrorAction, new AdapterFactory())
        {
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="ObjectAdapter" />.
        /// </summary>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="logErrorAction">The <see cref="Action" /> for logging <see cref="JsonPatchError" />.</param>
        /// <param name="adapterFactory">The <see cref="IAdapterFactory" /> to use when creating adapters.</param>
        public ObjectAdapter(JsonSerializerOptions jsonSerializerOptions, Action<JsonPatchError>? logErrorAction, IAdapterFactory adapterFactory)
        {
            JsonSerializerOptions = jsonSerializerOptions;
            LogErrorAction = logErrorAction;
            AdapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        }

        /// <inheritdoc />
        public void Add(Operation operation, object objectToApplyTo, Type objectType)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            Add(operation.Path, operation.Value, objectToApplyTo, objectType, operation);
        }

        /// <inheritdoc />
        public void Move(Operation operation, object objectToApplyTo, Type objectType)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            // Get value at 'from' location and add that value to the 'path' location
            if (TryGetValue(operation.From, objectToApplyTo, objectType, operation, out var propertyValue))
            {
                // remove that value
                Remove(operation.From, objectToApplyTo, objectType, operation);

                // add that value to the path location
                Add(operation.Path,
                    propertyValue,
                    objectToApplyTo,
                    objectType,
                    operation);
            }
        }

        /// <inheritdoc />
        public void Remove(Operation operation, object objectToApplyTo, Type objectType)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            if (objectToApplyTo == null) throw new ArgumentNullException(nameof(objectToApplyTo));

            Remove(operation.Path, objectToApplyTo, objectType, operation);
        }

        /// <inheritdoc />
        public void Replace(Operation operation, object objectToApplyTo, Type objectType)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            var parsedPath = new ParsedPath(operation.Path);
            var visitor = new ObjectVisitor(parsedPath, JsonSerializerOptions, AdapterFactory);

            var target = objectToApplyTo;
            var targetType = objectType;
            if (!visitor.TryVisit(ref target, ref targetType, out var adapter, out var errorMessage))
            {
                var error = CreatePathNotFoundError(objectToApplyTo, operation.Path, operation, errorMessage);
                ErrorReporter(error);
                return;
            }

            if (!adapter.TryReplace(target, targetType, parsedPath.LastSegment, JsonSerializerOptions, operation.Value, out errorMessage))
            {
                var error = CreateOperationFailedError(objectToApplyTo, operation.Path, operation, errorMessage);
                ErrorReporter(error);
            }
        }

        /// <inheritdoc />
        public void Copy(Operation operation, object objectToApplyTo, Type objectType)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            // Get value at 'from' location and add that value to the 'path' location
            if (TryGetValue(operation.From, objectToApplyTo, objectType, operation, out var propertyValue))
            {
                // Create deep copy
                var copyResult = ConversionResultProvider.CopyTo(propertyValue, propertyValue?.GetType(), JsonSerializerOptions);
                if (copyResult.CanBeConverted)
                {
                    Add(operation.Path, copyResult.ConvertedInstance, objectToApplyTo, objectType, operation);
                }
                else
                {
                    var error = CreateOperationFailedError(objectToApplyTo, operation.Path, operation, $"The property at '{operation.From}' could not be copied.");
                    ErrorReporter(error);
                }
            }
        }

        /// <inheritdoc />
        public void Test(Operation operation, object objectToApplyTo, Type objectType)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            if (objectToApplyTo == null) throw new ArgumentNullException(nameof(objectToApplyTo));

            var parsedPath = new ParsedPath(operation.Path);
            var visitor = new ObjectVisitor(parsedPath, JsonSerializerOptions, AdapterFactory);

            var target = objectToApplyTo;
            var targetType = objectType;

            if (!visitor.TryVisit(ref target, ref targetType, out var adapter, out var errorMessage))
            {
                var error = CreatePathNotFoundError(objectToApplyTo, operation.Path, operation, errorMessage);
                ErrorReporter(error);
                return;
            }

            if (!adapter.TryTest(target, targetType, parsedPath.LastSegment, JsonSerializerOptions, operation.Value, out errorMessage))
            {
                var error = CreateOperationFailedError(objectToApplyTo, operation.Path, operation, errorMessage);
                ErrorReporter(error);
            }
        }

        /// <summary>
        ///     Add is used by various operations (eg: add, copy, ...), yet through different operations;
        ///     This method allows code reuse yet reporting the correct operation on error
        /// </summary>
        private void Add(string path, object? value, object objectToApplyTo, Type objectType, Operation operation)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var parsedPath = new ParsedPath(path);
            var visitor = new ObjectVisitor(parsedPath, JsonSerializerOptions, AdapterFactory);

            var target = objectToApplyTo;
            var targetType = objectType;
            if (!visitor.TryVisit(ref target, ref targetType, out var adapter, out var errorMessage))
            {
                var error = CreatePathNotFoundError(objectToApplyTo, path, operation, errorMessage);
                ErrorReporter(error);
                return;
            }

            if (!adapter.TryAdd(target, targetType, parsedPath.LastSegment, JsonSerializerOptions, value, out errorMessage))
            {
                var error = CreateOperationFailedError(objectToApplyTo, path, operation, errorMessage);
                ErrorReporter(error);
            }
        }

        /// <summary>
        ///     Remove is used by various operations (eg: remove, move, ...), yet through different operations;
        ///     This method allows code reuse yet reporting the correct operation on error.  The return value
        ///     contains the type of the item that has been removed (and a bool possibly signifying an error)
        ///     This can be used by other methods, like replace, to ensure that we can pass in the correctly
        ///     typed value to whatever method follows.
        /// </summary>
        private void Remove(string? path, object objectToApplyTo, Type typeOfObjectToApplyTo, Operation operationToReport)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var parsedPath = new ParsedPath(path);
            var visitor = new ObjectVisitor(parsedPath, JsonSerializerOptions, AdapterFactory);

            var target = objectToApplyTo;
            var targetType = typeOfObjectToApplyTo;
            if (!visitor.TryVisit(ref target, ref targetType, out var adapter, out var errorMessage))
            {
                var error = CreatePathNotFoundError(objectToApplyTo, path, operationToReport, errorMessage);
                ErrorReporter(error);
                return;
            }

            if (!adapter.TryRemove(target, targetType, parsedPath.LastSegment, JsonSerializerOptions, out errorMessage))
            {
                var error = CreateOperationFailedError(objectToApplyTo, path, operationToReport, errorMessage);
                ErrorReporter(error);
            }
        }

        private bool TryGetValue(string? fromLocation, object objectToGetValueFrom, Type typeOfObjectToGetValueFrom, Operation operation, out object? propertyValue)
        {
            if (fromLocation == null)
                throw new ArgumentNullException(nameof(fromLocation));

            if (objectToGetValueFrom == null)
                throw new ArgumentNullException(nameof(objectToGetValueFrom));

            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            propertyValue = null;

            var parsedPath = new ParsedPath(fromLocation);
            var visitor = new ObjectVisitor(parsedPath, JsonSerializerOptions, AdapterFactory);

            var target = objectToGetValueFrom;
            var targetType = typeOfObjectToGetValueFrom;
            if (!visitor.TryVisit(ref target, ref targetType, out var adapter, out var errorMessage))
            {
                var error = CreatePathNotFoundError(objectToGetValueFrom, fromLocation, operation, errorMessage);
                ErrorReporter(error);
                return false;
            }

            if (!adapter.TryGet(target, targetType, parsedPath.LastSegment, JsonSerializerOptions, out propertyValue, out errorMessage))
            {
                var error = CreateOperationFailedError(objectToGetValueFrom, fromLocation, operation, errorMessage);
                ErrorReporter(error);
                return false;
            }

            return true;
        }

        private static JsonPatchError CreateOperationFailedError(object target, string? path, Operation operation, string? errorMessage)
        {
            return new JsonPatchError(target, operation, errorMessage ?? $"The '{operation.Op}' operation at path '{path}' could not be performed.");
        }

        private static JsonPatchError CreatePathNotFoundError(object target, string? path, Operation operation, string? errorMessage)
        {
            return new JsonPatchError(target, operation, errorMessage ?? $"For operation '{operation.Op}', the target location specified by path '{path}' was not found.");
        }
    }
}
