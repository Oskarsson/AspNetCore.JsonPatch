// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace AspNetCore.Mvc.JsonPatch
{
    /// <summary>
    ///     A <see cref="TextInputFormatter"/> for JSON Patch content that uses <see cref="JsonSerializer"/>.
    /// </summary>
    public class JsonPatchInputFormatter : TextInputFormatter, IInputFormatterExceptionPolicy
    {
        private static readonly MediaTypeHeaderValue ApplicationJsonPatch = MediaTypeHeaderValue.Parse("application/json-patch+json").CopyAsReadOnly();

        private readonly ILogger<JsonPatchInputFormatter> _logger;

        /// <summary>
        ///     Gets the <see cref="JsonSerializerOptions" /> used to configure the <see cref="JsonSerializer" />.
        /// </summary>
        /// <remarks>A single instance of <see cref="SystemTextJsonInputFormatter" /> is used for all JSON formatting. Any changes to the options will affect all input formatting.</remarks>
        public JsonSerializerOptions SerializerOptions { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonPatchInputFormatter" /> class.
        /// </summary>
        /// <param name="options">The <see cref="JsonOptions" />.</param>
        /// <param name="logger">The logger used to write log messages.</param>
        public JsonPatchInputFormatter(JsonOptions options, ILogger<JsonPatchInputFormatter> logger)
        {
            SerializerOptions = options.JsonSerializerOptions;
            _logger = logger;

            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);

            SupportedMediaTypes.Add(ApplicationJsonPatch);
        }

        /// <inheritdoc />
        public InputFormatterExceptionPolicy ExceptionPolicy => GetType() == typeof(JsonPatchInputFormatter)
            ? InputFormatterExceptionPolicy.MalformedInputExceptions
            : InputFormatterExceptionPolicy.AllExceptions;

        /// <inheritdoc />
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            var httpContext = context.HttpContext;
            var (inputStream, usesTranscodingStream) = GetInputStream(httpContext, encoding);

            try
            {
                var model = await JsonSerializer.DeserializeAsync(inputStream, context.ModelType, SerializerOptions);

                if (model == null && !context.TreatEmptyInputAsDefaultValue)
                    return InputFormatterResult.NoValue();

                Log.JsonInputSuccess(_logger, context.ModelType);

                if (model is IJsonPatchDocument patchDocument)
                    patchDocument.JsonSerializerOptions = SerializerOptions;

                return InputFormatterResult.Success(model!);
            }
            catch (JsonException ex)
            {
                var path = ex.Path ?? string.Empty;

                var formatterException = new InputFormatterException(ex.Message, ex);

                context.ModelState.TryAddModelError(path, formatterException, context.Metadata);

                Log.JsonInputException(_logger, ex);

                return InputFormatterResult.Failure();
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                // The code in System.Text.Json never throws these exceptions. However a custom converter could produce these errors for instance when
                // parsing a value. These error messages are considered safe to report to users using ModelState.

                context.ModelState.TryAddModelError(string.Empty, ex, context.Metadata);
                Log.JsonInputException(_logger, ex);

                return InputFormatterResult.Failure();
            }
            finally
            {
                if (usesTranscodingStream)
                    await inputStream.DisposeAsync();
            }
        }

        /// <inheritdoc />
        public override bool CanRead(InputFormatterContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var modelTypeInfo = context.ModelType.GetTypeInfo();
            if (!typeof(IJsonPatchDocument).GetTypeInfo().IsAssignableFrom(modelTypeInfo) || !modelTypeInfo.IsGenericType)
                return false;

            return base.CanRead(context);
        }

        private static (Stream inputStream, bool usesTranscodingStream) GetInputStream(HttpContext httpContext, Encoding encoding)
        {
            if (encoding.CodePage == Encoding.UTF8.CodePage)
                return (httpContext.Request.Body, false);

            var inputStream = Encoding.CreateTranscodingStream(httpContext.Request.Body, encoding, Encoding.UTF8, true);
            return (inputStream, true);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string?, Exception?> _jsonInputFormatterException;
            private static readonly Action<ILogger, string?, Exception?> _jsonInputSuccess;

            static Log()
            {
                _jsonInputFormatterException = LoggerMessage.Define<string?>(LogLevel.Debug, new EventId(1, "JsonPatchInputFormatterException"), "JSON input formatter threw an exception: {Message}");
                _jsonInputSuccess = LoggerMessage.Define<string?>(LogLevel.Debug, new EventId(2, "JsonPatchInputFormatterException"), "JSON input formatter succeeded, deserializing to type '{TypeName}'");
            }

            public static void JsonInputException(ILogger logger, Exception exception)
            {
                _jsonInputFormatterException(logger, exception.Message, exception);
            }

            public static void JsonInputSuccess(ILogger logger, Type modelType)
            {
                _jsonInputSuccess(logger, modelType.FullName, null);
            }
        }
    }
}
