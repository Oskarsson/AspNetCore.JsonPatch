// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCore.JsonPatch.Exceptions;
using AspNetCore.JsonPatch.Internal;
using Xunit;

namespace AspNetCore.JsonPatch.Tests.Internal
{
    public class ParsedPathTests
    {
        [Theory]
        [InlineData("foo/bar~0baz", new[] {"foo", "bar~baz"})]
        [InlineData("foo/bar~00baz", new[] {"foo", "bar~0baz"})]
        [InlineData("foo/bar~01baz", new[] {"foo", "bar~1baz"})]
        [InlineData("foo/bar~10baz", new[] {"foo", "bar/0baz"})]
        [InlineData("foo/bar~1baz", new[] {"foo", "bar/baz"})]
        [InlineData("foo/bar~0/~0/~1~1/~0~0/baz", new[] {"foo", "bar~", "~", "//", "~~", "baz"})]
        [InlineData("~0~1foo", new[] {"~/foo"})]
        public void ParsingValidPathShouldSucceed(string path, string[] expected)
        {
            // Arrange & Act
            var parsedPath = new ParsedPath(path);

            // Assert
            Assert.Equal(expected, parsedPath.Segments);
        }

        [Theory]
        [InlineData("foo/bar~")]
        [InlineData("~")]
        [InlineData("~2")]
        [InlineData("foo~3bar")]
        public void PathWithInvalidEscapeSequenceShouldFail(string path)
        {
            // Arrange, Act & Assert
            Assert.Throws<JsonPatchException>(() =>
            {
                var parsedPath = new ParsedPath(path);
            });
        }
    }
}
