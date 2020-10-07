// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using AspNetCore.JsonPatch.Exceptions;

namespace AspNetCore.JsonPatch.Internal
{
    /// <summary>
    ///     This API supports infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public readonly struct ParsedPath
    {
        private readonly string[] _segments;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParsedPath"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public ParsedPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            _segments = ParsePath(path);
        }

        /// <summary>
        ///     The last segment in the path.
        /// </summary>
        public string? LastSegment => _segments.Length == 0 ? null : _segments[^1];

        /// <summary>
        ///     The collection of segments in the path.
        /// </summary>
        public IReadOnlyList<string> Segments => _segments;

        private static string[] ParsePath(string path)
        {
            var strings = new List<string>();
            var sb = new StringBuilder(path.Length);

            for (var i = 0; i < path.Length; i++)
            {
                switch (path[i])
                {
                    case '/' when sb.Length <= 0:
                        continue;
                    case '/':
                        strings.Add(sb.ToString());
                        sb.Length = 0;
                        break;
                    case '~':
                    {
                        ++i;

                        if (i >= path.Length)
                            throw new JsonPatchException($"The provided string '{path}' is an invalid path.");

                        switch (path[i])
                        {
                            case '0':
                                sb.Append('~');
                                break;
                            case '1':
                                sb.Append('/');
                                break;
                            default:
                                throw new JsonPatchException($"The provided string '{path}' is an invalid path.");
                        }

                        break;
                    }
                    default:
                        sb.Append(path[i]);
                        break;
                }
            }

            if (sb.Length > 0)
                strings.Add(sb.ToString());

            return strings.ToArray();
        }
    }
}
