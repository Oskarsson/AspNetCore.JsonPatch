// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json.Serialization;
using AspNetCore.JsonPatch.Adapters;
using AspNetCore.JsonPatch.Converters;

namespace AspNetCore.JsonPatch.Operations
{
    /// <summary>
    ///     A type describing an untyped patch operation.
    /// </summary>
    [JsonConverter(typeof(OperationJsonConverter))]
    public class Operation
    {
        private string _op = null!;

        /// <summary>
        ///     The operation type.
        /// </summary>
        [JsonIgnore]
        public OperationType OperationType { get; private set; }

        /// <summary>
        ///     The target path.
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }

        /// <summary>
        ///     The operation type as a string.
        /// </summary>
        [JsonPropertyName("op")]
        public string Op
        {
            get => _op;
            set
            {
                if (!Enum.TryParse<OperationType>(value, true, out var result))
                    result = OperationType.Invalid;

                OperationType = result;
                _op = value;
            }
        }

        /// <summary>
        ///     An optional path where to retrieve the value from.
        /// </summary>
        [JsonPropertyName("from")]
        public string? From { get; set; }

        /// <summary>
        ///     An optional value.
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Operation" />
        /// </summary>
        /// <param name="op">The operation.</param>
        /// <param name="path">The path.</param>
        /// <param name="from">An optional from path.</param>
        /// <param name="value">An optional value.</param>
        public Operation(string op, string path, string? from, object? value = null)
        {
            Op = op ?? throw new ArgumentNullException(nameof(op));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            From = from;
            Value = value;
        }

        /// <summary>
        ///     Applies the operation to the target object using the specified adapter.
        /// </summary>
        /// <param name="objectToApplyTo">The target object.</param>
        /// <param name="adapter">The <see cref="IObjectAdapter" />.</param>
        public void Apply(object objectToApplyTo, IObjectAdapter adapter)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            switch (OperationType)
            {
                case OperationType.Add:
                    adapter.Add(this, objectToApplyTo, objectToApplyTo.GetType());
                    break;
                case OperationType.Remove:
                    adapter.Remove(this, objectToApplyTo, objectToApplyTo.GetType());
                    break;
                case OperationType.Replace:
                    adapter.Replace(this, objectToApplyTo, objectToApplyTo.GetType());
                    break;
                case OperationType.Move:
                    adapter.Move(this, objectToApplyTo, objectToApplyTo.GetType());
                    break;
                case OperationType.Copy:
                    adapter.Copy(this, objectToApplyTo, objectToApplyTo.GetType());
                    break;
                case OperationType.Test when adapter is IObjectAdapterWithTest adapterWithTest:
                    adapterWithTest.Test(this, objectToApplyTo, objectToApplyTo.GetType());
                    break;
                case OperationType.Test:
                    throw new NotSupportedException("The test operation is not supported.");
            }
        }
    }
}
