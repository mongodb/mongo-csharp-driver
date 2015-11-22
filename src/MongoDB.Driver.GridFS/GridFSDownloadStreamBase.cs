/* Copyright 2015 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;

namespace MongoDB.Driver.GridFS
{
    internal abstract class GridFSDownloadStreamBase : GridFSDownloadStream
    {
        // private fields
        private readonly IReadBinding _binding;
        private readonly GridFSBucket _bucket;
        private bool _closed;
        private bool _disposed;
        private readonly GridFSFileInfo _fileInfo;

        // constructors
        protected GridFSDownloadStreamBase(
            GridFSBucket bucket,
            IReadBinding binding,
            GridFSFileInfo fileInfo)
        {
            _bucket = bucket;
            _binding = binding;
            _fileInfo = fileInfo;
        }

        // public properties
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override GridFSFileInfo FileInfo
        {
            get { return _fileInfo; }
        }

        public override long Length
        {
            get { return _fileInfo.Length; }
        }

        // protected properties
        protected IReadBinding Binding
        {
            get { return _binding; }
        }

        protected GridFSBucket Bucket
        {
            get { return _bucket; }
        }

        // public methods
        public override void Close()
        {
            Close(CancellationToken.None);
        }

        public override void Close(CancellationToken cancellationToken)
        {
            _closed = true;
            base.Close();
        }

        public override Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _closed = true;
            base.Close();
            return Task.FromResult(true);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
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

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        // protected methods
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _binding.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected void ThrowIfClosedOrDisposed()
        {
            if (_closed)
            {
                throw new InvalidOperationException("Stream is closed.");
            }
            ThrowIfDisposed();
        }

        protected virtual void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
