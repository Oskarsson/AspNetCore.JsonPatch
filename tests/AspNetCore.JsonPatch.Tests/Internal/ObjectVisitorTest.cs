// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using AspNetCore.JsonPatch.Internal;
using Xunit;

namespace AspNetCore.JsonPatch.Tests.Internal
{
    public class ObjectVisitorTest
    {
        public static IEnumerable<object[]> ReturnsListAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] {model, "/States/-", model.States};
                yield return new object[] {model.States, "/-", model.States};

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] {nestedModel, "/Customers/0/States/-", nestedModel.Customers[0].States};
                yield return new object[] {nestedModel, "/Customers/0/States/0", nestedModel.Customers[0].States};
                yield return new object[] {nestedModel.Customers, "/0/States/-", nestedModel.Customers[0].States};
                yield return new object[] {nestedModel.Customers[0], "/States/-", nestedModel.Customers[0].States};
            }
        }

        public static IEnumerable<object[]> ReturnsDictionaryAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] {model, "/CountriesAndRegions/USA", model.CountriesAndRegions};
                yield return new object[] {model.CountriesAndRegions, "/USA", model.CountriesAndRegions};

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] {nestedModel, "/Customers/0/CountriesAndRegions/USA", nestedModel.Customers[0].CountriesAndRegions};
                yield return new object[] {nestedModel.Customers, "/0/CountriesAndRegions/USA", nestedModel.Customers[0].CountriesAndRegions};
                yield return new object[] {nestedModel.Customers[0], "/CountriesAndRegions/USA", nestedModel.Customers[0].CountriesAndRegions};
            }
        }

        public static IEnumerable<object[]> ReturnsExpandoAdapterData
        {
            get
            {
                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] {nestedModel, "/Customers/0/Items/Name", nestedModel.Customers[0].Items};
                yield return new object[] {nestedModel.Customers, "/0/Items/Name", nestedModel.Customers[0].Items};
                yield return new object[] {nestedModel.Customers[0], "/Items/Name", nestedModel.Customers[0].Items};
            }
        }

        public static IEnumerable<object[]> ReturnsPocoAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] {model, "/Name", model};

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] {nestedModel, "/Customers/0/Name", nestedModel.Customers[0]};
                yield return new object[] {nestedModel.Customers, "/0/Name", nestedModel.Customers[0]};
                yield return new object[] {nestedModel.Customers[0], "/Name", nestedModel.Customers[0]};
            }
        }

        [Theory]
        [MemberData(nameof(ReturnsListAdapterData))]
        public void Visit_ValidPathToArray_ReturnsListAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var targetType = targetObject.GetType();
            var visitor = new ObjectVisitor(new ParsedPath(path), new JsonSerializerOptions());

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out var adapter, out var message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.IsType<ListAdapter>(adapter);
        }

        [Theory]
        [MemberData(nameof(ReturnsDictionaryAdapterData))]
        public void Visit_ValidPathToDictionary_ReturnsDictionaryAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var targetType = targetObject.GetType();
            var visitor = new ObjectVisitor(new ParsedPath(path), new JsonSerializerOptions());

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out var adapter, out var message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.Equal(typeof(DictionaryAdapter<string, string>), adapter.GetType());
        }

        [Theory]
        [MemberData(nameof(ReturnsExpandoAdapterData))]
        public void Visit_ValidPathToExpandoObject_ReturnsExpandoAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var targetType = targetObject.GetType();
            var visitor = new ObjectVisitor(new ParsedPath(path), new JsonSerializerOptions());

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out var adapter, out var message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.Same(typeof(DictionaryAdapter<string, object>), adapter.GetType());
        }

        [Theory]
        [MemberData(nameof(ReturnsPocoAdapterData))]
        public void Visit_ValidPath_ReturnsExpandoAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var targetType = targetObject.GetType();
            var visitor = new ObjectVisitor(new ParsedPath(path), new JsonSerializerOptions());

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out var adapter, out var message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.IsType<PocoAdapter>(adapter);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public void Visit_InvalidIndexToArray_Fails(string position)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath($"/Customers/{position}/States/-"), new JsonSerializerOptions());
            var automobileDepartment = new Class1Nested();
            object targetObject = automobileDepartment;
            var targetType = typeof(Class1Nested);

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out _, out var message);

            // Assert
            Assert.False(visitStatus);
            Assert.Equal($"The index value provided by path segment '{position}' is out of bounds of the array size.", message);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("foo")]
        public void Visit_InvalidIndexFormatToArray_Fails(string position)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath($"/Customers/{position}/States/-"), new JsonSerializerOptions());
            var automobileDepartment = new Class1Nested();
            object targetObject = automobileDepartment;
            var targetType = targetObject.GetType();

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out _, out var message);

            // Assert
            Assert.False(visitStatus);
            Assert.Equal($"The path segment '{position}' is invalid for an array index.", message);
        }

        [Fact]
        public void Visit_DoesNotValidate_FinalPathSegment()
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath("/NonExisting"), new JsonSerializerOptions());
            var model = new Class1();
            object targetObject = model;
            var targetType = targetObject.GetType();

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, ref targetType, out var adapter, out var message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.IsType<PocoAdapter>(adapter);
        }

        [Fact]
        public void Visit_NullInteriorTarget_ReturnsFalse()
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath("/States/0"), new JsonSerializerOptions());
            object target = new Class1 {States = null};
            var targetType = target.GetType();

            // Act
            var visitStatus = visitor.TryVisit(ref target, ref targetType, out var adapter, out var message);

            // Assert
            Assert.False(visitStatus);
            Assert.Null(adapter);
            Assert.Null(message);
        }

        [Fact]
        public void Visit_NullTarget_ReturnsNullAdapter()
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath("test"), new JsonSerializerOptions());
            object target = null;
            Type targetType = null;

            // Act

            var visitStatus = visitor.TryVisit(ref target, ref targetType, out var adapter, out var message);

            // Assert
            Assert.False(visitStatus);
            Assert.Null(adapter);
            Assert.Null(message);
        }

        private class Class1
        {
            public readonly IDictionary<string, string> CountriesAndRegions = new Dictionary<string, string>();
            public string Name { get; set; }
            public IList<string> States { get; set; } = new List<string>();
            public dynamic Items { get; } = new ExpandoObject();
        }

        private class Class1Nested
        {
            public List<Class1> Customers { get; } = new List<Class1>();
        }
    }
}
