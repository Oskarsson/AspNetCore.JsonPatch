// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using AspNetCore.Mvc.JsonPatch;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extension methods for adding JsonPatch to <see cref="MvcCoreBuilder" />.
    /// </summary>
    public static class JsonMvcCoreBuilderExtensions
    {
        /// <summary>
        ///     Configures JsonPatch specific features such as input and output formatters.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" />.</param>
        /// <returns>The <see cref="IMvcCoreBuilder" />.</returns>
        public static IMvcCoreBuilder AddJsonPatch(this IMvcCoreBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor
                .Transient<IApiDescriptionProvider, JsonPatchOperationsArrayProvider>());

            return builder;
        }
    }
}
