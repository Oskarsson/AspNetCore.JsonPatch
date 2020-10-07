// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    ///     Defines the operations used for loading an <see cref="IAdapter" /> based on the current object and
    ///     ContractResolver.
    /// </summary>
    public interface IAdapterFactory
    {
        /// <summary>
        ///     Creates an <see cref="IAdapter" /> for the current object
        /// </summary>
        /// <param name="target">The target object</param>
        /// <param name="targetType">The type of the target object.</param>
        /// <returns>The needed <see cref="IAdapter" /></returns>
        IAdapter Create(object target, Type targetType);
    }
}
