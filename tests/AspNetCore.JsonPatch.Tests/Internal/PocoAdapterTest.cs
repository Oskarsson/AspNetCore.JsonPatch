// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using AspNetCore.JsonPatch.Internal;
using Xunit;

namespace AspNetCore.JsonPatch.Tests.Internal
{
    public class PocoAdapterTest
    {
        [Fact]
        public void TryAdd_ReplacesExistingProperty()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };

            // Act
            var addStatus = adapter.TryAdd(model, typeof(Customer), "Name", options, "John", out var errorMessage);

            // Assert
            Assert.Equal("John", model.Name);
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(errorMessage), "Expected no error message");
        }

        [Fact]
        public void TryAdd_ThrowsJsonPatchException_IfPropertyDoesNotExist()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };
            var expectedErrorMessage = "The target location specified by path segment 'LastName' was not found.";

            // Act
            var addStatus = adapter.TryAdd(model, typeof(Customer), "LastName", options, "Smith", out var errorMessage);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryAdd_ThrowsJsonPatchException_IfPropertyDoesNotExist_Inheritance()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer();
            var expectedErrorMessage = "The target location specified by path segment 'CreatedDate' was not found.";

            // Act
            var addStatus = adapter.TryAdd(model, typeof(UpdatableCustomer), "CreatedDate", options, "2020-10-04T23:00:00.000Z", out var errorMessage);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryGet_ExistingProperty()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };

            // Act
            var getStatus = adapter.TryGet(model, typeof(Customer), "Name", options, out var value, out var errorMessage);

            // Assert
            Assert.Equal("Joana", value);
            Assert.True(getStatus);
            Assert.True(string.IsNullOrEmpty(errorMessage), "Expected no error message");
        }

        [Fact]
        public void TryGet_ThrowsJsonPatchException_IfPropertyDoesNotExist()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };
            var expectedErrorMessage = "The target location specified by path segment 'LastName' was not found.";

            // Act
            var getStatus = adapter.TryGet(model, typeof(Customer), "LastName", options, out var value, out var errorMessage);

            // Assert
            Assert.Null(value);
            Assert.False(getStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryGet_ThrowsJsonPatchException_IfPropertyDoesNotExist_Inheritance()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                CreatedDate = new DateTime(2020, 10, 4, 23, 0, 0)
            };
            var expectedErrorMessage = "The target location specified by path segment 'CreatedDate' was not found.";

            // Act
            var getStatus = adapter.TryGet(model, typeof(UpdatableCustomer), "CreatedDate", options, out var value, out var errorMessage);

            // Assert
            Assert.Null(value);
            Assert.False(getStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryRemove_SetsPropertyToNull()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };

            // Act
            var removeStatus = adapter.TryRemove(model, typeof(Customer), "Name", options, out var errorMessage);

            // Assert
            Assert.Null(model.Name);
            Assert.True(removeStatus);
            Assert.True(string.IsNullOrEmpty(errorMessage), "Expected no error message");
        }

        [Fact]
        public void TryRemove_ThrowsJsonPatchException_IfPropertyDoesNotExist()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };
            var expectedErrorMessage = "The target location specified by path segment 'LastName' was not found.";

            // Act
            var removeStatus = adapter.TryRemove(model, typeof(Customer), "LastName", options, out var errorMessage);

            // Assert
            Assert.False(removeStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryRemove_ThrowsJsonPatchException_IfPropertyDoesNotExist_Inheritance()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                CreatedDate = new DateTime(2020, 10, 4, 23, 0, 0)
            };
            var expectedErrorMessage = "The target location specified by path segment 'CreatedDate' was not found.";

            // Act
            var removeStatus = adapter.TryRemove(model, typeof(UpdatableCustomer), "CreatedDate", options, out var errorMessage);

            // Assert
            Assert.False(removeStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryReplace_OverwritesExistingValue()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };

            // Act
            var replaceStatus = adapter.TryReplace(model, typeof(Customer), "Name", options, "John", out var errorMessage);

            // Assert
            Assert.Equal("John", model.Name);
            Assert.True(replaceStatus);
            Assert.True(string.IsNullOrEmpty(errorMessage), "Expected no error message");
        }

        [Fact]
        public void TryReplace_ThrowsJsonPatchException_IfNewValueIsInvalidType()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Age = 25
            };

            var expectedErrorMessage = "The value 'TwentySix' is invalid for target location.";

            // Act
            var replaceStatus = adapter.TryReplace(model, typeof(Customer), "Age", options, "TwentySix", out var errorMessage);

            // Assert
            Assert.Equal(25, model.Age);
            Assert.False(replaceStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryReplace_ThrowsJsonPatchException_IfPropertyDoesNotExist()
        {
            // Arrange
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };
            var expectedErrorMessage = "The target location specified by path segment 'LastName' was not found.";

            // Act
            var replaceStatus = adapter.TryReplace(model, typeof(Customer), "LastName", options, "Smith", out var errorMessage);

            // Assert
            Assert.Equal("Joana", model.Name);
            Assert.False(replaceStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryReplace_ThrowsJsonPatchException_IfPropertyDoesNotExist_Inheritance()
        {
            // Arrange
            var expectedDate = new DateTime(2020, 10, 4, 23, 0, 0);

            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                CreatedDate = new DateTime(2020, 10, 4, 23, 0, 0)
            };
            var expectedErrorMessage = "The target location specified by path segment 'CreatedDate' was not found.";

            // Act
            var replaceStatus = adapter.TryReplace(model, typeof(UpdatableCustomer), "CreatedDate", options, "2020-10-04T23:00:00.001", out var errorMessage);

            // Assert
            Assert.Equal(expectedDate, model.CreatedDate);
            Assert.False(replaceStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryTest_DoesNotThrowException_IfTestSuccessful()
        {
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };

            // Act
            var testStatus = adapter.TryTest(model, typeof(Customer), "Name", options, "Joana", out var errorMessage);

            // Assert
            Assert.Equal("Joana", model.Name);
            Assert.True(testStatus);
            Assert.True(string.IsNullOrEmpty(errorMessage), "Expected no error message");
        }

        [Fact]
        public void TryTest_ThrowsJsonPatchException_IfTestFails()
        {
            // Arrange            
            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                Name = "Joana"
            };
            var expectedErrorMessage = "The current value 'Joana' at path 'Name' is not equal to the test value 'John'.";

            // Act
            var testStatus = adapter.TryTest(model, typeof(Customer), "Name", options, "John", out var errorMessage);

            // Assert
            Assert.False(testStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        [Fact]
        public void TryTest_ThrowsJsonPatchException_IfPropertyDoesNotExist_Inheritance()
        {
            // Arrange
            var expectedDate = new DateTime(2020, 10, 4, 23, 0, 0);

            var adapter = new PocoAdapter();
            var options = new JsonSerializerOptions();
            var model = new Customer
            {
                CreatedDate = expectedDate
            };
            var expectedErrorMessage = "The target location specified by path segment 'CreatedDate' was not found.";

            // Act
            var replaceStatus = adapter.TryTest(model, typeof(UpdatableCustomer), "CreatedDate", options, expectedDate, out var errorMessage);

            // Assert
            Assert.False(replaceStatus);
            Assert.Equal(expectedErrorMessage, errorMessage);
        }

        private class Customer : UpdatableCustomer
        {
            public DateTime CreatedDate { get; set; }
        }

        private class UpdatableCustomer
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
