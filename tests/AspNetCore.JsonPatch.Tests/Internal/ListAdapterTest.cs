﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using AspNetCore.JsonPatch.Internal;
using AspNetCore.JsonPatch.Tests.TestObjectModels;
using Xunit;

namespace AspNetCore.JsonPatch.Tests.Internal
{
    public class ListAdapterTest
    {
        public static TheoryData<List<int>, List<int>> AppendAtEndOfListData =>
            new TheoryData<List<int>, List<int>>
            {
                {
                    new List<int>(),
                    new List<int> {20}
                },
                {
                    new List<int> {5, 10},
                    new List<int> {5, 10, 20}
                }
            };

        public static TheoryData<IList, object, string, IList> AddingDifferentComplexTypeWorksData =>
            new TheoryData<IList, object, string, IList>
            {
                {
                    new List<string>(),
                    "a",
                    "-",
                    new List<string> {"a"}
                },
                {
                    new List<string> {"a", "b"},
                    "c",
                    "-",
                    new List<string> {"a", "b", "c"}
                },
                {
                    new List<string> {"a", "b"},
                    "c",
                    "0",
                    new List<string> {"c", "a", "b"}
                },
                {
                    new List<string> {"a", "b"},
                    "c",
                    "1",
                    new List<string> {"a", "c", "b"}
                }
            };

        public static TheoryData<IList, object, string, IList> AddingKeepsObjectReferenceData
        {
            get
            {
                var sDto1 = new SimpleObject();
                var sDto2 = new SimpleObject();
                var sDto3 = new SimpleObject();
                return new TheoryData<IList, object, string, IList>
                {
                    {
                        new List<SimpleObject>(),
                        sDto1,
                        "-",
                        new List<SimpleObject> {sDto1}
                    },
                    {
                        new List<SimpleObject> {sDto1, sDto2},
                        sDto3,
                        "-",
                        new List<SimpleObject> {sDto1, sDto2, sDto3}
                    },
                    {
                        new List<SimpleObject> {sDto1, sDto2},
                        sDto3,
                        "0",
                        new List<SimpleObject> {sDto3, sDto1, sDto2}
                    },
                    {
                        new List<SimpleObject> {sDto1, sDto2},
                        sDto3,
                        "1",
                        new List<SimpleObject> {sDto1, sDto3, sDto2}
                    }
                };
            }
        }

        public static TheoryData<string, List<int>> ReplacesValuesAtPositionData =>
            new TheoryData<string, List<int>>
            {
                {
                    "0",
                    new List<int> {30, 20}
                },
                {
                    "1",
                    new List<int> {10, 30}
                }
            };

        [Fact]
        public void Patch_OnArrayObject_Fails()
        {
            // Arrange
            var targetObject = new[] {20, 30};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(int[]), "0", options, "40", out var message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal($"The type '{targetObject.GetType().FullName}' which is an array is not supported for json patch operations as it has a fixed size.", message);
        }

        [Fact]
        public void Patch_OnNonGenericListObject_Fails()
        {
            // Arrange
            var targetObject = new ArrayList
            {
                20, 30
            };
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(ArrayList), "-", options, "40", out var message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal($"The type '{targetObject.GetType().FullName}' which is a non generic list is not supported for json patch operations. Only generic list types are supported.", message);
        }

        [Fact]
        public void Add_WithIndexSameAsNumberOfElements_Works()
        {
            // Arrange
            var targetObject = new List<string> {"James", "Mike"};
            var listAdapter = new ListAdapter();
            var position = targetObject.Count.ToString();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<string>), position, options, "Rob", out var message);

            // Assert
            Assert.Null(message);
            Assert.True(addStatus);
            Assert.Equal(3, targetObject.Count);
            Assert.Equal(new List<string> {"James", "Mike", "Rob"}, targetObject);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("-2")]
        [InlineData("3")]
        public void Add_WithOutOfBoundsIndex_Fails(string position)
        {
            // Arrange
            var targetObject = new List<string> {"James", "Mike"};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<string>), position, options, "40", out var message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal($"The index value provided by path segment '{position}' is out of bounds of the array size.", message);
        }

        [Theory]
        [InlineData("_")]
        [InlineData("blah")]
        public void Patch_WithInvalidPositionFormat_Fails(string position)
        {
            // Arrange
            var targetObject = new List<string> {"James", "Mike"};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<string>), position, options, "40", out var message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal($"The path segment '{position}' is invalid for an array index.", message);
        }

        [Theory]
        [MemberData(nameof(AppendAtEndOfListData))]
        public void Add_Appends_AtTheEnd(List<int> targetObject, List<int> expected)
        {
            // Arrange
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<int>), "-", options, "20", out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected.Count, targetObject.Count);
            Assert.Equal(expected, targetObject);
        }

        [Fact]
        public void Add_NullObject_ToReferenceTypeListWorks()
        {
            // Arrange
            var listAdapter = new ListAdapter();
            var targetObject = new List<string> {"James", "Mike"};
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<string>), "-", options, null, out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(3, targetObject.Count);
            Assert.Equal(new List<string> {"James", "Mike", null}, targetObject);
        }

        [Fact]
        public void Add_CompatibleTypeWorks()
        {
            // Arrange
            var sDto = new SimpleObject();
            var iDto = new InheritedObject();
            var targetObject = new List<SimpleObject> {sDto};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<SimpleObject>), "-", options, iDto, out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(2, targetObject.Count);
            Assert.Equal(new List<SimpleObject> {sDto, iDto}, targetObject);
        }

        [Fact]
        public void Add_NonCompatibleType_Fails()
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(List<int>), "-", options, "James", out var message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal("The value 'James' is invalid for target location.", message);
        }

        [Theory]
        [MemberData(nameof(AddingDifferentComplexTypeWorksData))]
        public void Add_DifferentComplexTypeWorks(IList targetObject, object value, string position, IList expected)
        {
            // Arrange
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(IList), position, options, value, out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected.Count, targetObject.Count);
            Assert.Equal(expected, targetObject);
        }

        [Theory]
        [MemberData(nameof(AddingKeepsObjectReferenceData))]
        public void Add_KeepsObjectReference(IList targetObject, object value, string position, IList expected)
        {
            // Arrange
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, typeof(IList), position, options, value, out var message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected.Count, targetObject.Count);
            Assert.Equal(expected, targetObject);
        }

        [Theory]
        [InlineData(new int[] { }, "0")]
        [InlineData(new[] {10, 20}, "-1")]
        [InlineData(new[] {10, 20}, "2")]
        public void Get_IndexOutOfBounds(int[] input, string position)
        {
            // Arrange
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var getStatus = listAdapter.TryGet(targetObject, typeof(List<int>), position, options, out _, out var message);

            // Assert
            Assert.False(getStatus);
            Assert.Equal($"The index value provided by path segment '{position}' is out of bounds of the array size.", message);
        }

        [Theory]
        [InlineData(new[] {10, 20}, "0", 10)]
        [InlineData(new[] {10, 20}, "1", 20)]
        [InlineData(new[] {10}, "0", 10)]
        public void Get(int[] input, string position, object expected)
        {
            // Arrange
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var getStatus = listAdapter.TryGet(targetObject, typeof(List<int>), position, options, out var value, out _);

            // Assert
            Assert.True(getStatus);
            Assert.Equal(expected, value);
            Assert.Equal(new List<int>(input), targetObject);
        }

        [Theory]
        [InlineData(new int[] { }, "0")]
        [InlineData(new[] {10, 20}, "-1")]
        [InlineData(new[] {10, 20}, "2")]
        public void Remove_IndexOutOfBounds(int[] input, string position)
        {
            // Arrange
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var removeStatus = listAdapter.TryRemove(targetObject, typeof(List<int>), position, options, out var message);

            // Assert
            Assert.False(removeStatus);
            Assert.Equal($"The index value provided by path segment '{position}' is out of bounds of the array size.", message);
        }

        [Theory]
        [InlineData(new[] {10, 20}, "0", new[] {20})]
        [InlineData(new[] {10, 20}, "1", new[] {10})]
        [InlineData(new[] {10}, "0", new int[] { })]
        public void Remove(int[] input, string position, int[] expected)
        {
            // Arrange
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var removeStatus = listAdapter.TryRemove(targetObject, typeof(List<int>), position, options, out _);

            // Assert
            Assert.True(removeStatus);
            Assert.Equal(new List<int>(expected), targetObject);
        }

        [Fact]
        public void Replace_NonCompatibleType_Fails()
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var replaceStatus = listAdapter.TryReplace(targetObject, typeof(List<int>), "-", options, "James", out var message);

            // Assert
            Assert.False(replaceStatus);
            Assert.Equal("The value 'James' is invalid for target location.", message);
        }

        [Fact]
        public void Replace_ReplacesValue_AtTheEnd()
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var replaceStatus = listAdapter.TryReplace(targetObject, typeof(List<int>), "-", options, "30", out var message);

            // Assert
            Assert.True(replaceStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(new List<int> {10, 30}, targetObject);
        }

        [Theory]
        [MemberData(nameof(ReplacesValuesAtPositionData))]
        public void Replace_ReplacesValue_AtGivenPosition(string position, List<int> expected)
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var replaceStatus = listAdapter.TryReplace(targetObject, typeof(List<int>), position, options, "30", out var message);

            // Assert
            Assert.True(replaceStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected, targetObject);
        }

        [Fact]
        public void Test_DoesNotThrowException_IfTestIsSuccessful()
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var options = new JsonSerializerOptions();

            // Act
            var testStatus = listAdapter.TryTest(targetObject, typeof(List<int>), "0", options, "10", out var message);

            //Assert
            Assert.True(testStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
        }

        [Fact]
        public void Test_ThrowsJsonPatchException_IfTestFails()
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var expectedErrorMessage = "The current value '20' at position '1' is not equal to the test value '10'.";
            var options = new JsonSerializerOptions();

            // Act
            var testStatus = listAdapter.TryTest(targetObject, typeof(List<int>), "1", options, "10", out var errorMessage);

            //Assert
            Assert.False(testStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void Test_ThrowsJsonPatchException_IfListPositionOutOfBounds()
        {
            // Arrange
            var targetObject = new List<int> {10, 20};
            var listAdapter = new ListAdapter();
            var expectedErrorMessage = "The index value provided by path segment '2' is out of bounds of the array size.";
            var options = new JsonSerializerOptions();

            // Act
            var testStatus = listAdapter.TryTest(targetObject, typeof(List<int>), "2", options, "10", out var errorMessage);

            //Assert
            Assert.False(testStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }
    }
}
