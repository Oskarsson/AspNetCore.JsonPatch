// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch
{
    /// <summary>
    ///     Captures error message and the related entity and the operation that caused it.
    /// </summary>
    public class JsonPatchError
    {
        /// <summary>
        ///     Gets the object that is affected by the error.
        /// </summary>
        public object? AffectedObject { get; }

        /// <summary>
        ///     Gets the <see cref="Operation" /> that caused the error.
        /// </summary>
        public Operation? Operation { get; }

        /// <summary>
        ///     Gets the error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        ///     Initializes a new instance of <see cref="JsonPatchError" />.
        /// </summary>
        /// <param name="affectedObject">The object that is affected by the error.</param>
        /// <param name="operation">The <see cref="Operation" /> that caused the error.</param>
        /// <param name="errorMessage">The error message.</param>
        public JsonPatchError(object? affectedObject, Operation? operation, string errorMessage)
        {
            AffectedObject = affectedObject;
            Operation = operation;
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        }
    }
}
