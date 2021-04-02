// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal class ConversionResult
    {
        /// <summary>
        ///     True if the instance could be converted; otherwise, false.
        /// </summary>
        public bool CanBeConverted { get; }

        /// <summary>
        ///     The converted instance of the value.
        /// </summary>
        public object? ConvertedInstance { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConversionResult" /> class.
        /// </summary>
        /// <param name="canBeConverted">True if the instance could be converted; otherwise, false.</param>
        /// <param name="convertedInstance">The converted instance of the value.</param>
        public ConversionResult(bool canBeConverted, object? convertedInstance)
        {
            CanBeConverted = canBeConverted;
            ConvertedInstance = convertedInstance;
        }
    }
}
