// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AspNetCore.JsonPatch.Tests.TestObjectModels;
using Xunit;

namespace AspNetCore.JsonPatch.Tests
{
    public class JsonPatchDocumentGetPathTest
    {
        [Fact]
        public void ExpressionType_MemberAccess()
        {
            // Act
            var path = JsonPatchDocument<SimpleObjectWithNestedObject>.GetPath(p => p.SimpleObject.IntegerList, "-");

            // Assert
            Assert.Equal("/SimpleObject/IntegerList/-", path);
        }

        [Fact]
        public void ExpressionType_ArrayIndex()
        {
            // Act
            var path = JsonPatchDocument<int[]>.GetPath(p => p[3], null);

            // Assert
            Assert.Equal("/3", path);
        }

        [Fact]
        public void ExpressionType_Call()
        {
            // Act
            var path = JsonPatchDocument<Dictionary<string, int>>.GetPath(p => p["key"], "3");

            // Assert
            Assert.Equal("/key/3", path);
        }

        [Fact]
        public void ExpressionType_Parameter_NullPosition()
        {
            // Act
            var path = JsonPatchDocument<SimpleObject>.GetPath(p => p, null);

            // Assert
            Assert.Equal("/", path);
        }

        [Fact]
        public void ExpressionType_Parameter_WithPosition()
        {
            // Act
            var path = JsonPatchDocument<SimpleObject>.GetPath(p => p, "-");

            // Assert
            Assert.Equal("/-", path);
        }

        [Fact]
        public void ExpressionType_Convert()
        {
            // Act
            var path = JsonPatchDocument<NestedObjectWithDerivedClass>.GetPath(p => (BaseClass) p.DerivedObject, null);

            // Assert
            Assert.Equal("/DerivedObject", path);
        }

        [Fact]
        public void ExpressionType_NotSupported()
        {
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => { JsonPatchDocument<SimpleObject>.GetPath(p => p.IntegerValue >= 4, null); });

            // Assert
            Assert.Equal("The expression '(p.IntegerValue >= 4)' is not supported. Supported expressions include member access and indexer expressions.", exception.Message);
        }
    }

    internal class DerivedClass : BaseClass
    {
    }

    internal class NestedObjectWithDerivedClass
    {
        public DerivedClass DerivedObject { get; set; }
    }

    internal class BaseClass
    {
    }
}
