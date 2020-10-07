// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch.Exceptions
{
    /// <summary>
    ///     A type describing a JsonPatch exception.
    /// </summary>
    public class JsonPatchException : Exception
    {
        /// <summary>
        ///     The failed operation.
        /// </summary>
        public Operation? FailedOperation { get; }

        /// <summary>
        ///     The affected object.
        /// </summary>
        public object? AffectedObject { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonPatchException" /> class.
        /// </summary>
        /// <param name="jsonPatchError">The <see cref="JsonPatchError" />.</param>
        /// <param name="innerException">The inner exception.</param>
        public JsonPatchException(JsonPatchError jsonPatchError, Exception? innerException = null)
            : base(jsonPatchError.ErrorMessage, innerException)
        {
            FailedOperation = jsonPatchError.Operation;
            AffectedObject = jsonPatchError.AffectedObject;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonPatchException" /> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public JsonPatchException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
