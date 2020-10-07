// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public class JsonMvcCoreBuilderExtensionsTests
    {
        [Fact]
        public void AddJsonPatch_ConfiguresOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMvcCore()
                .AddJsonPatch();

            // Assert
            Assert.Single(services, d => d.ImplementationType == typeof(JsonPatchMvcOptionsSetup));
        }
    }
}
