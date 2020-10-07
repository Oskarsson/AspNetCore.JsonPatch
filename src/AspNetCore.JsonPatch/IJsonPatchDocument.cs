// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json;
using AspNetCore.JsonPatch.Operations;

namespace AspNetCore.JsonPatch
{
    /// <summary>
    ///     Describes a json patch document.
    /// </summary>
    public interface IJsonPatchDocument
    {
        /// <summary>
        ///     The <see cref="JsonSerializerOptions"/>.
        /// </summary>
        JsonSerializerOptions JsonSerializerOptions { get; set; }

        /// <summary>
        ///     Retrieves the operations in the patch document.
        /// </summary>
        /// <returns>The collection of operations.</returns>
        IList<Operation> GetOperations();
    }
}
