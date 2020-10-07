﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc
{
    public class NonSeekableReadStream : Stream
    {
        private readonly bool _allowSyncReads;
        private readonly Stream _inner;

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public NonSeekableReadStream(byte[] data, bool allowSyncReads = true)
            : this(new MemoryStream(data), allowSyncReads)
        {
        }

        public NonSeekableReadStream(Stream inner, bool allowSyncReads)
        {
            _inner = inner;
            _allowSyncReads = allowSyncReads;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_allowSyncReads)
                throw new InvalidOperationException("Cannot perform synchronous reads");

            count = Math.Max(count, 1);
            return _inner.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            count = Math.Max(count, 1);
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}
