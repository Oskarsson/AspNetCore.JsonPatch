// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
    /// <typeparam name="TModel"></typeparam>
    [JsonConverter(typeof(JsonPatchDocumentJsonConverterFactory))]
    public class JsonPatchDocument<TModel> : IJsonPatchDocument where TModel : class
    {
        /// <summary>
        ///     The collection operations in the document.
        /// </summary>
        public List<Operation<TModel>> Operations { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonPatchDocument{TModel}"/> class.
        /// </summary>
        public JsonPatchDocument()
        {
            Operations = new List<Operation<TModel>>();
            JsonSerializerOptions = new JsonSerializerOptions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPatchDocument{TModel}"/> class.
        /// </summary>
        /// <param name="operations">The collection of <see cref="Operation"/> items.</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
        internal JsonPatchDocument(List<Operation<TModel>> operations, JsonSerializerOptions jsonSerializerOptions)
        {
            Operations = operations ?? throw new ArgumentNullException(nameof(operations));
            JsonSerializerOptions = jsonSerializerOptions;
        }

        /// <summary>
        ///     The <see cref="JsonSerializerOptions"/>.
        /// </summary>
        internal JsonSerializerOptions JsonSerializerOptions { get; }

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
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, TProp>> path, TProp value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("add", GetPath(path, null), null, value));
            return this;
        }

        /// <summary>
        ///     Add value to list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <param name="position">position</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value, int position)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("add", GetPath(path, position.ToString()), null, value));
            return this;
        }

        /// <summary>
        ///     Add value to the end of the list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Add<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("add", GetPath(path, "-"), null, value));
            return this;
        }

        /// <summary>
        ///     Remove value at target location.  Will result in, for example,
        ///     { "op": "remove", "path": "/a/b/c" }
        /// </summary>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, TProp>> path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("remove", GetPath(path, null), null));
            return this;
        }

        /// <summary>
        ///     Remove value from list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="position">position</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, IList<TProp>>> path, int position)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("remove", GetPath(path, position.ToString()), null));
            return this;
        }

        /// <summary>
        ///     Remove value from end of list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Remove<TProp>(Expression<Func<TModel, IList<TProp>>> path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("remove", GetPath(path, "-"), null));
            return this;
        }

        /// <summary>
        ///     Replace value.  Will result in, for example,
        ///     { "op": "replace", "path": "/a/b/c", "value": 42 }
        /// </summary>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, TProp>> path, TProp value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("replace", GetPath(path, null), null, value));
            return this;
        }

        /// <summary>
        ///     Replace value in a list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <param name="position">position</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value, int position)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("replace", GetPath(path, position.ToString()), null, value));
            return this;
        }

        /// <summary>
        ///     Replace value at end of a list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Replace<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("replace", GetPath(path, "-"), null, value));
            return this;
        }

        /// <summary>
        ///     Test value.  Will result in, for example,
        ///     { "op": "test", "path": "/a/b/c", "value": 42 }
        /// </summary>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Test<TProp>(Expression<Func<TModel, TProp>> path, TProp value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("test", GetPath(path, null), null, value));
            return this;
        }

        /// <summary>
        ///     Test value in a list at given position
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <param name="position">position</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Test<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value, int position)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("test", GetPath(path, position.ToString()), null, value));
            return this;
        }

        /// <summary>
        ///     Test value at end of a list
        /// </summary>
        /// <typeparam name="TProp">value type</typeparam>
        /// <param name="path">target location</param>
        /// <param name="value">value</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Test<TProp>(Expression<Func<TModel, IList<TProp>>> path, TProp value)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("test", GetPath(path, "-"), null, value));
            return this;
        }

        /// <summary>
        ///     Removes value at specified location and add it to the target location.  Will result in, for example:
        ///     { "op": "move", "from": "/a/b/c", "path": "/a/b/d" }
        /// </summary>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("move", GetPath(path, null), GetPath(from, null)));
            return this;
        }

        /// <summary>
        ///     Move from a position in a list to a new location
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("move", GetPath(path, null), GetPath(from, positionFrom.ToString())));
            return this;
        }

        /// <summary>
        ///     Move from a property to a location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path, int positionTo)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("move", GetPath(path, positionTo.ToString()), GetPath(from, null)));
            return this;
        }

        /// <summary>
        ///     Move from a position in a list to another location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position (source)</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position (target)</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path, int positionTo)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("move", GetPath(path, positionTo.ToString()), GetPath(from, positionFrom.ToString())));
            return this;
        }

        /// <summary>
        ///     Move from a position in a list to the end of another list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("move", GetPath(path, "-"), GetPath(from, positionFrom.ToString())));
            return this;
        }

        /// <summary>
        ///     Move to the end of a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Move<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("move", GetPath(path, "-"), GetPath(from, null)));
            return this;
        }

        /// <summary>
        ///     Copy the value at specified location to the target location.  Will result in, for example:
        ///     { "op": "copy", "from": "/a/b/c", "path": "/a/b/e" }
        /// </summary>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("copy", GetPath(path, null), GetPath(from, null)));
            return this;
        }

        /// <summary>
        ///     Copy from a position in a list to a new location
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, TProp>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("copy", GetPath(path, null), GetPath(from, positionFrom.ToString())));
            return this;
        }

        /// <summary>
        ///     Copy from a property to a location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path, int positionTo)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("copy", GetPath(path, positionTo.ToString()), GetPath(from, null)));
            return this;
        }

        /// <summary>
        ///     Copy from a position in a list to a new location in a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position (source)</param>
        /// <param name="path">target location</param>
        /// <param name="positionTo">position (target)</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path, int positionTo)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("copy", GetPath(path, positionTo.ToString()), GetPath(from, positionFrom.ToString())));
            return this;
        }

        /// <summary>
        ///     Copy from a position in a list to the end of another list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="positionFrom">position</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, IList<TProp>>> from, int positionFrom, Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("copy", GetPath(path, "-"), GetPath(from, positionFrom.ToString())));
            return this;
        }

        /// <summary>
        ///     Copy to the end of a list
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="from">source location</param>
        /// <param name="path">target location</param>
        /// <returns>The <see cref="JsonPatchDocument{TModel}" /> for chaining.</returns>
        public JsonPatchDocument<TModel> Copy<TProp>(Expression<Func<TModel, TProp>> from, Expression<Func<TModel, IList<TProp>>> path)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Operations.Add(new Operation<TModel>("copy", GetPath(path, "-"), GetPath(from, null)));
            return this;
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/>.</param>
        public void ApplyTo(TModel objectToApplyTo, JsonSerializerOptions? options = null)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            ApplyTo(objectToApplyTo, new ObjectAdapter(options ?? JsonSerializerOptions, null, new AdapterFactory()));
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="logErrorAction">Action to log errors</param>
        public void ApplyTo(TModel objectToApplyTo, Action<JsonPatchError> logErrorAction)
        {
            ApplyTo(objectToApplyTo, new ObjectAdapter(JsonSerializerOptions, logErrorAction, new AdapterFactory()), logErrorAction);
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/>.</param>
        /// <param name="logErrorAction">Action to log errors</param>
        public void ApplyTo(TModel objectToApplyTo, JsonSerializerOptions options, Action<JsonPatchError> logErrorAction)
        {
            ApplyTo(objectToApplyTo, new ObjectAdapter(options, logErrorAction, new AdapterFactory()), logErrorAction);
        }

        /// <summary>
        ///     Apply this JsonPatchDocument
        /// </summary>
        /// <param name="objectToApplyTo">Object to apply the JsonPatchDocument to</param>
        /// <param name="adapter">IObjectAdapter instance to use when applying</param>
        /// <param name="logErrorAction">Action to log errors</param>
        public void ApplyTo(TModel objectToApplyTo, IObjectAdapter adapter, Action<JsonPatchError>? logErrorAction)
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
        public void ApplyTo(TModel objectToApplyTo, IObjectAdapter adapter)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            // apply each operation in order
            foreach (var op in Operations)
                op.Apply(objectToApplyTo, adapter);
        }

        // Internal for testing
        internal static string GetPath<TProp>(Expression<Func<TModel, TProp>> expr, string? position)
        {
            var segments = GetPathSegments(expr.Body);
            var path = string.Join("/", segments);
            if (position != null)
            {
                path += "/" + position;
                if (segments.Count == 0) return path;
            }

            return "/" + path;
        }

        private static List<string> GetPathSegments(Expression? expr)
        {
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            var listOfSegments = new List<string>();
            switch (expr.NodeType)
            {
                case ExpressionType.ArrayIndex:
                    var binaryExpression = (BinaryExpression) expr;
                    listOfSegments.AddRange(GetPathSegments(binaryExpression.Left));
                    listOfSegments.Add(binaryExpression.Right.ToString());
                    return listOfSegments;

                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression) expr;
                    listOfSegments.AddRange(GetPathSegments(methodCallExpression.Object));
                    listOfSegments.Add(EvaluateExpression(methodCallExpression.Arguments[0]));
                    return listOfSegments;

                case ExpressionType.Convert:
                    listOfSegments.AddRange(GetPathSegments(((UnaryExpression) expr).Operand));
                    return listOfSegments;

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression) expr;
                    listOfSegments.AddRange(GetPathSegments(memberExpression.Expression));
                    // Get property name, respecting JsonProperty attribute
                    listOfSegments.Add(GetPropertyNameFromMemberExpression(memberExpression));
                    return listOfSegments;

                case ExpressionType.Parameter:
                    // Fits "x => x" (the whole document which is "" as JSON pointer)
                    return listOfSegments;

                default:
                    throw new InvalidOperationException($"The expression '{expr}' is not supported. Supported expressions include member access and indexer expressions.");
            }
        }

        private static string GetPropertyNameFromMemberExpression(MemberExpression memberExpression)
        {
            if (memberExpression.Expression?.Type != null)
                return memberExpression.Expression.Type.GetProperties()
                    .First(x => x.Name == memberExpression.Member.Name)
                    .GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? memberExpression.Member.Name;

            return memberExpression.Member.Name;
        }

        // Evaluates the value of the key or index which may be an int or a string,
        // or some other expression type.
        // The expression is converted to a delegate and the result of executing the delegate is returned as a string.
        private static string EvaluateExpression(Expression expression)
        {
            var converted = Expression.Convert(expression, typeof(object));
            var fakeParameter = Expression.Parameter(typeof(object), null);
            var lambda = Expression.Lambda<Func<object?, object>>(converted, fakeParameter);
            var func = lambda.Compile();

            return Convert.ToString(func(null), CultureInfo.InvariantCulture) ?? throw new InvalidOperationException();
        }
    }
}
