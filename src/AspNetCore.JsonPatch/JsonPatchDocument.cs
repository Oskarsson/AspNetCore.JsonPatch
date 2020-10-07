// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Adapters;
using AspNetCore.JsonPatch.Converters;
using AspNetCore.JsonPatch.Exceptions;
using AspNetCore.JsonPatch.Internal;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch
{
    /// <summary>
    ///     A type describing a JsonPatch document.
    /// </summary>
    [JsonConverter(typeof(JsonPatchDocumentConverter))]
    public class JsonPatchDocument : IJsonPatchDocument
    {
        /// <summary>
        ///     The collection operations in the document.
        /// </summary>
        public List<Operation> Operations { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonPatchDocument"/> class.
        /// </summary>
        public JsonPatchDocument()
        {
            Operations = new List<Operation>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonPatchDocument"/> class.
        /// </summary>
        /// <param name="operations">The collection of operations.</param>
        public JsonPatchDocument(List<Operation> operations)
        {
            Operations = operations ?? throw new ArgumentNullException(nameof(operations));
        }

        /// <summary>
        ///     The <see cref="IJsonPatchDocument.JsonSerializerOptions"/>.
        /// </summary>
        [JsonIgnore]
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();

        /// <summary>
        ///     Retrieves the operations in the patch document.
        /// </summary>
        /// <returns>The collection of operations.</returns>
        IList<Operation> IJsonPatchDocument.GetOperations()
        {
            return Operations.Select(op => new Operation(op.Op, op.Path, op.From, op.Value)).ToList();
        }

        /// <summary>
        ///     Add operation.  Will result in, for example,
        ///     { "op": "add", "path": "/a/b/c", "value": [ "foo", "bar" ] }
        /// </summary>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument" /> for chaining.</returns>
        public JsonPatchDocument Add(string path, object value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation("add", PathHelpers.ValidateAndNormalizePath(path), null, value));
            return this;
        }

        /// <summary>
        ///     Remove value at target location.  Will result in, for example,
        ///     { "op": "remove", "path": "/a/b/c" }
        /// </summary>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument" /> for chaining.</returns>
        public JsonPatchDocument Remove(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation("remove", PathHelpers.ValidateAndNormalizePath(path), null));
            return this;
        }

        /// <summary>
        ///     Replace value.  Will result in, for example,
        ///     { "op": "replace", "path": "/a/b/c", "value": 42 }
        /// </summary>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument" /> for chaining.</returns>
        public JsonPatchDocument Replace(string path, object value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation("replace", PathHelpers.ValidateAndNormalizePath(path), null, value));
            return this;
        }

        /// <summary>
        ///     Test value.  Will result in, for example,
        ///     { "op": "test", "path": "/a/b/c", "value": 42 }
        /// </summary>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument" /> for chaining.</returns>
        public JsonPatchDocument Test(string path, object value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation("test", PathHelpers.ValidateAndNormalizePath(path), null, value));
            return this;
        }

        /// <summary>
        ///     Removes value at specified location and add it to the target location.  Will result in, for example:
        ///     { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
        /// </summary>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument" /> for chaining.</returns>
        public JsonPatchDocument Move(string from, string path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation("move", PathHelpers.ValidateAndNormalizePath(path), PathHelpers.ValidateAndNormalizePath(from)));
            return this;
        }

        /// <summary>
        ///     Copy the value at specified location to the target location.  Will result in, for example:
        ///     { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
        /// </summary>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument" /> for chaining.</returns>
        public JsonPatchDocument Copy(string from, string path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation("copy", PathHelpers.ValidateAndNormalizePath(path), PathHelpers.ValidateAndNormalizePath(from)));
            return this;
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        public void ApplyTo(object objectToApplyTo)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            ApplyTo(objectToApplyTo, new ObjectAdapter(JsonSerializerOptions, null, new AdapterFactory()));
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="logErrorAction">Action to log errors</param>
        public void ApplyTo(object objectToApplyTo, Action<JsonPatchError> logErrorAction)
        {
            ApplyTo(objectToApplyTo, new ObjectAdapter(JsonSerializerOptions, logErrorAction, new AdapterFactory()), logErrorAction);
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="adapter">IObjectAdapter instance to use when applying</param>
        /// <param name="logErrorAction">Action to log errors</param>
        public void ApplyTo(object objectToApplyTo, IObjectAdapter adapter, Action<JsonPatchError>? logErrorAction)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            foreach (var op in Operations)
            {
                try
                {
                    op.Apply(objectToApplyTo, adapter);
                }
                catch (JsonPatchException jsonPatchException)
                {
                    var errorReporter = logErrorAction ?? ErrorReporter.Default;
                    errorReporter(new JsonPatchError(objectToApplyTo, op, jsonPatchException.Message));

                    // As per JSON Patch spec if an operation results in error, further operations should not be executed.
                    break;
                }
            }
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="adapter">IObjectAdapter instance to use when applying</param>
        public void ApplyTo(object objectToApplyTo, IObjectAdapter adapter)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            // apply each operation in order
            foreach (var op in Operations)
                op.Apply(objectToApplyTo, adapter);
        }
    }
}
