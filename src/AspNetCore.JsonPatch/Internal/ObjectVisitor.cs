// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AspNetCore.JsonPatch.Adapters;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    internal class ObjectVisitor
    {
        private readonly IAdapterFactory _adapterFactory;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly ParsedPath _path;

        /// <summary>
        ///     Initializes a new instance of <see cref="ObjectVisitor" />.
        /// </summary>
        /// <param name="path">The path of the JsonPatch operation</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        public ObjectVisitor(ParsedPath path, JsonSerializerOptions jsonSerializerOptions)
            : this(path, jsonSerializerOptions, new AdapterFactory())
        {
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="ObjectVisitor" />.
        /// </summary>
        /// <param name="path">The path of the JsonPatch operation</param>
        /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions" />.</param>
        /// <param name="adapterFactory">The <see cref="IAdapterFactory" /> to use when creating adapters.</param>
        public ObjectVisitor(ParsedPath path, JsonSerializerOptions jsonSerializerOptions, IAdapterFactory adapterFactory)
        {
            _path = path;
            _jsonSerializerOptions = jsonSerializerOptions ?? throw new ArgumentNullException(nameof(jsonSerializerOptions));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        }

        /// <summary>
        ///     Tries to visit the object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="targetType">The type of the target object.</param>
        /// <param name="adapter">The <see cref="IAdapter"/>.</param>
        /// <param name="errorMessage">An optional error message if the object could not be visited; otherwise, false.</param>
        /// <returns>True if the object could be visited; otherwise, false.</returns>
        public bool TryVisit([MaybeNullWhen(false)] [AllowNull] ref object target, ref Type targetType, [MaybeNullWhen(false)] out IAdapter adapter, out string? errorMessage)
        {
            if (target == null)
            {
                adapter = null;
                errorMessage = null;
                return false;
            }

            adapter = SelectAdapter(target, targetType);

            // Traverse until the penultimate segment to get the target object and adapter
            for (var i = 0; i < _path.Segments.Count - 1; i++)
            {
                if (!adapter.TryTraverse(target, targetType, _path.Segments[i], _jsonSerializerOptions, out var next, out errorMessage))
                {
                    adapter = null;
                    return false;
                }

                // If we hit a null on an interior segment then we need to stop traversing.
                if (next == null)
                {
                    adapter = null;
                    return false;
                }

                target = next;
                targetType = next.GetType();
                adapter = SelectAdapter(target, targetType);
            }

            errorMessage = null;
            return true;
        }

        private IAdapter SelectAdapter(object targetObject, Type targetType)
        {
            return _adapterFactory.Create(targetObject, targetType);
        }
    }
}
