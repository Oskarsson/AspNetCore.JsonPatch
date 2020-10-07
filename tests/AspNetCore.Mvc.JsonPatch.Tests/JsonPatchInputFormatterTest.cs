// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.JsonPatch;
using AspNetCore.Mvc.JsonPatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class JsonPatchInputFormatterTest
    {
        private static readonly JsonOptions JsonOptions = new JsonOptions();

        [Fact]
        public async Task Constructor_BuffersRequestBody_ByDefault()
        {
            // Arrange
            var formatter = new JsonPatchInputFormatter(JsonOptions, GetLogger());

            var content = "[{\"op\":\"add\",\"path\":\"Customer/Name\",\"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IHttpResponseFeature>(new TestResponseFeature());
            httpContext.Request.Body = new NonSeekableReadStream(contentBytes, false);
            httpContext.Request.ContentType = "application/json";

            var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
            Assert.Equal("add", patchDocument.Operations[0].Op);
            Assert.Equal("Customer/Name", patchDocument.Operations[0].Path);
            Assert.Equal("John", patchDocument.Operations[0].Value);
        }


        [Fact]
        public async Task JsonPatchInputFormatter_ReadsOneOperation_Successfully()
        {
            // Arrange
            var formatter = new JsonPatchInputFormatter(JsonOptions, GetLogger());

            var content = "[{\"op\":\"add\",\"path\":\"Customer/Name\",\"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = CreateHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
            Assert.Equal("add", patchDocument.Operations[0].Op);
            Assert.Equal("Customer/Name", patchDocument.Operations[0].Path);
            Assert.Equal("John", patchDocument.Operations[0].Value);
        }

        [Fact]
        public async Task JsonPatchInputFormatter_ReadsMultipleOperations_Successfully()
        {
            // Arrange
            var formatter = new JsonPatchInputFormatter(JsonOptions, GetLogger());

            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"},{\"op\": \"remove\", \"path\" : \"Customer/Name\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = CreateHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.False(result.HasError);
            var patchDocument = Assert.IsType<JsonPatchDocument<Customer>>(result.Model);
            Assert.Equal("add", patchDocument.Operations[0].Op);
            Assert.Equal("Customer/Name", patchDocument.Operations[0].Path);
            Assert.Equal("John", patchDocument.Operations[0].Value);
            Assert.Equal("remove", patchDocument.Operations[1].Op);
            Assert.Equal("Customer/Name", patchDocument.Operations[1].Path);
        }

        [Theory]
        [InlineData("application/json-patch+json", true)]
        [InlineData("application/json", false)]
        [InlineData("application/*", false)]
        [InlineData("*/*", false)]
        public void CanRead_ReturnsTrueOnlyForJsonPatchContentType(string requestContentType, bool expectedCanRead)
        {
            // Arrange
            var formatter = new JsonPatchInputFormatter(JsonOptions, GetLogger());

            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = CreateHttpContext(contentBytes, requestContentType);

            var formatterContext = CreateInputFormatterContext(typeof(JsonPatchDocument<Customer>), httpContext);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.Equal(expectedCanRead, result);
        }

        [Theory]
        [InlineData(typeof(Customer))]
        [InlineData(typeof(IJsonPatchDocument))]
        public void CanRead_ReturnsFalse_NonJsonPatchContentType(Type modelType)
        {
            // Arrange
            var formatter = new JsonPatchInputFormatter(JsonOptions, GetLogger());

            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = CreateHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(modelType, httpContext);

            // Act
            var result = formatter.CanRead(formatterContext);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task JsonPatchInputFormatter_ReturnsModelStateErrors_InvalidModelType()
        {
            // Arrange
            var exceptionMessage = $"The JSON value could not be converted to {typeof(Customer).FullName}. Path: $ | LineNumber: 0 | BytePositionInLine: 1.";

            // This test relies on 2.1 error message behavior
            var formatter = new JsonPatchInputFormatter(JsonOptions, GetLogger());

            var content = "[{\"op\": \"add\", \"path\" : \"Customer/Name\", \"value\":\"John\"}]";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var httpContext = CreateHttpContext(contentBytes);

            var formatterContext = CreateInputFormatterContext(typeof(Customer), httpContext);

            // Act
            var result = await formatter.ReadAsync(formatterContext);

            // Assert
            Assert.True(result.HasError);
            Assert.Contains(exceptionMessage, formatterContext.ModelState["$"].Errors[0].ErrorMessage);
        }

        private static ILogger<JsonPatchInputFormatter> GetLogger()
        {
            return NullLogger<JsonPatchInputFormatter>.Instance;
        }

        private static InputFormatterContext CreateInputFormatterContext(Type modelType, HttpContext httpContext)
        {
            var provider = new EmptyModelMetadataProvider();
            var metadata = provider.GetMetadataForType(modelType);

            return new InputFormatterContext(httpContext, string.Empty, new ModelStateDictionary(), metadata, new TestHttpRequestStreamReaderFactory().CreateReader);
        }

        private static HttpContext CreateHttpContext(byte[] contentBytes, string contentType = "application/json-patch+json")
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
            request.SetupGet(f => f.ContentType).Returns(contentType);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private class Customer
        {
            public string Name { get; set; }
        }

        private class TestResponseFeature : HttpResponseFeature
        {
            public override void OnCompleted(Func<object, Task> callback, object state)
            {
                // do not do anything
            }
        }
    }
}
