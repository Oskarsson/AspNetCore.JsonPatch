// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using AspNetCore.JsonPatch.Adapters;
using AspNetCore.JsonPatch.Exceptions;

namespace AspNetCore.JsonPatch.Operations
{
    /// <summary>
    ///     A type describing a typed patch operation.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    public class Operation<TModel> : Operation where TModel : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Operation" />
        /// </summary>
        /// <param name="op">The operation.</param>
        /// <param name="path">The path.</param>
        /// <param name="from">An optional from path.</param>
        /// <param name="value">An optional value.</param>
        public Operation(string op, string path, string? from, object? value = null)
            : base(op, path, from, value)
        {
        }

        /// <summary>
        ///     Applies the operation to the target object using the specified adapter.
        /// </summary>
        /// <param name="objectToApplyTo">The target object.</param>
        /// <param name="adapter">The <see cref="IObjectAdapter" />.</param>
        public void Apply(TModel objectToApplyTo, IObjectAdapter adapter)
        {
            if (objectToApplyTo == null)
                throw new ArgumentNullException(nameof(objectToApplyTo));

            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            switch (OperationType)
            {
                case OperationType.Add:
                    adapter.Add(this, objectToApplyTo, typeof(TModel));
                    break;
                case OperationType.Remove:
                    adapter.Remove(this, objectToApplyTo, typeof(TModel));
                    break;
                case OperationType.Replace:
                    adapter.Replace(this, objectToApplyTo, typeof(TModel));
                    break;
                case OperationType.Move:
                    adapter.Move(this, objectToApplyTo, typeof(TModel));
                    break;
                case OperationType.Copy:
                    adapter.Copy(this, objectToApplyTo, typeof(TModel));
                    break;
                case OperationType.Test when adapter is IObjectAdapterWithTest adapterWithTest:
                    adapterWithTest.Test(this, objectToApplyTo, typeof(TModel));
                    break;
                case OperationType.Test:
                    throw new JsonPatchException(new JsonPatchError(objectToApplyTo, this, "The test operation is not supported."));
                case OperationType.Invalid:
                    throw new JsonPatchException($"Invalid JsonPatch operation '{Op}'.");
            }
        }
    }
}
