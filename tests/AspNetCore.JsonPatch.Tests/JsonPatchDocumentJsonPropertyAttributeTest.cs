﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text.Json.Serialization;
using Xunit;

namespace AspNetCore.JsonPatch.Tests
{
    public class JsonPatchDocumentJsonPropertyAttributeTest
    {
        [Fact]
        public void Add_RespectsJsonPropertyAttribute()
        {
            // Arrange
            var patchDocument = new JsonPatchDocument<JsonPropertyObject>();

            // Act
            patchDocument.Add(p => p.Name, "John");

            // Assert
            var pathToCheck = patchDocument.Operations.First().Path;
            Assert.Equal("/AnotherName", pathToCheck);
        }

        [Fact]
        public void Add_RespectsJsonPropertyAttribute_WithDotWhitespaceAndBackslashInName()
        {
            // Arrange
            var obj = new JsonPropertyObjectWithStrangeNames();
            var patchDocument = new JsonPatchDocument();

            // Act
            patchDocument.Add("/First Name.", "John");
            patchDocument.Add("Last\\Name", "Doe");
            patchDocument.ApplyTo(obj);

            // Assert
            Assert.Equal("John", obj.FirstName);
            Assert.Equal("Doe", obj.LastName);
        }

        [Fact]
        public void Move_FallsbackToPropertyName_WhenJsonPropertyAttributeName_IsEmpty()
        {
            // Arrange
            var patchDocument = new JsonPatchDocument<JsonPropertyWithNoPropertyName>();

            // Act
            patchDocument.Move(m => m.StringProperty, m => m.StringProperty2);

            // Assert
            var fromPath = patchDocument.Operations.First().From;
            Assert.Equal("/StringProperty", fromPath);
            var toPath = patchDocument.Operations.First().Path;
            Assert.Equal("/StringProperty2", toPath);
        }

        private class JsonPropertyObject
        {
            [JsonPropertyName("AnotherName")]
            public string Name { get; set; }
        }

        private class JsonPropertyObjectWithStrangeNames
        {
            [JsonPropertyName("First Name.")]
            public string FirstName { get; set; }

            [JsonPropertyName("Last\\Name")]
            public string LastName { get; set; }
        }

        private class JsonPropertyWithNoPropertyName
        {
            public string StringProperty { get; set; }

            public string[] ArrayProperty { get; set; }

            public string StringProperty2 { get; set; }

            public string SSN { get; set; }
        }
    }
}
