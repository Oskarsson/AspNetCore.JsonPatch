// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AspNetCore.Mvc.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Sets up JSON formatter options for <see cref="MvcOptions" />.
    /// </summary>
    internal class JsonPatchMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly JsonOptions _jsonOptions;
        private readonly ILoggerFactory _loggerFactory;

        public JsonPatchMvcOptionsSetup(ILoggerFactory loggerFactory, IOptions<JsonOptions> jsonOptions)
        {
            if (jsonOptions == null)
                throw new ArgumentNullException(nameof(jsonOptions));

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _jsonOptions = jsonOptions.Value;
        }

        public void Configure(MvcOptions options)
        {
            var logger = _loggerFactory.CreateLogger<JsonPatchInputFormatter>();
            var formatter = new JsonPatchInputFormatter(_jsonOptions, logger);

            var index = IndexOfType<IInputFormatter, JsonPatchInputFormatter>(options.InputFormatters);
            if (index < 0)
                options.InputFormatters.Add(formatter);
            else
                options.InputFormatters.Insert(index, formatter);
        }

        private static int IndexOfType<T1, T2>(IReadOnlyList<T1> items)
        {
            var formatterType = typeof(T2);
            for (var i = items.Count - 1; i >= 0; i--)
            {
                var formatter = items[i];
                if (formatter?.GetType() == formatterType)
                    return i;
            }

            return -1;
        }
    }
}
