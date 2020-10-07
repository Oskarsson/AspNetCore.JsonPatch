// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace AspNetCore.JsonPatch.Operations
{
    /// <summary>
    ///     Describes the possible operations.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        ///     Adds a new item.
        /// </summary>
        Add,

        /// <summary>
        ///     Removes an item.
        /// </summary>
        Remove,

        /// <summary>
        ///     Replaces an existing item with a new item.
        /// </summary>
        Replace,

        /// <summary>
        ///     Moves an item from one location to another.
        /// </summary>
        Move,

        /// <summary>
        ///     Copies an item from one location to another.
        /// </summary>
        Copy,

        /// <summary>
        ///     Tests an item for a value.
        /// </summary>
        Test,

        /// <summary>
        ///     Invalid Operation.
        /// </summary>
        Invalid
    }
}
