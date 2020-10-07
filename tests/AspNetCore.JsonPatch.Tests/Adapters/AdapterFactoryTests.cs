// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using AspNetCore.JsonPatch.Adapters;
using AspNetCore.JsonPatch.Internal;
using Xunit;

namespace AspNetCore.JsonPatch.Tests.Adapters
{
    public class AdapterFactoryTests
    {
        [Fact]
        public void GetListAdapterForListTargets()
        {
            // Arrange
            var factory = new AdapterFactory();

            //Act:
            var adapter = factory.Create(new List<string>(), typeof(List<string>));

            // Assert
            Assert.Equal(typeof(ListAdapter), adapter.GetType());
        }

        [Fact]
        public void GetDictionaryAdapterForDictionaryObjects()
        {
            // Arrange
            var factory = new AdapterFactory();

            //Act:
            var adapter = factory.Create(new Dictionary<string, string>(), typeof(Dictionary<string, string>));

            // Assert
            Assert.Equal(typeof(DictionaryAdapter<string, string>), adapter.GetType());
        }


        [Fact]
        public void GetPocoAdapterForGenericObjects()
        {
            // Arrange
            var factory = new AdapterFactory();

            //Act:
            var adapter = factory.Create(new PocoModel(), typeof(PocoModel));

            // Assert
            Assert.Equal(typeof(PocoAdapter), adapter.GetType());
        }

        private class PocoModel
        {
        }
    }
}
