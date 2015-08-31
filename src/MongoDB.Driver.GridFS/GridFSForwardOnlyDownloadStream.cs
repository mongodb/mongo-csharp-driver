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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.GridFS
{
    internal class GridFSForwardOnlyDownloadStream : GridFSDownloadStreamBase
    {
        // private fields
        private List<BsonDocument> _batch;
        private long _batchPosition;
        private readonly bool _checkMD5;
        private bool _closed;
        private IAsyncCursor<BsonDocument> _cursor;
        private bool _disposed;
        private readonly int _lastChunkNumber;
        private readonly int _lastChunkSize;
        private readonly MD5 _md5;
        private int _nextChunkNumber;
        private long _position;

        // constructors
        public GridFSForwardOnlyDownloadStream(
            GridFSBucket bucket,
            IReadBinding binding,
            GridFSFilesCollectionDocument filesCollectionDocument,
            bool checkMD5)
            : base(bucket, binding, filesCollectionDocument)
        {
            _checkMD5 = checkMD5;
            if (_checkMD5)
            {
                _md5 = MD5.Create();
            }

            _lastChunkNumber = (int)((filesCollectionDocument.Length - 1) / filesCollectionDocument.ChunkSizeBytes);
            _lastChunkSize = (int)(filesCollectionDocument.Length % filesCollectionDocument.ChunkSizeBytes);

            if (_lastChunkSize == 0)
            {
                _lastChunkSize = filesCollectionDocument.ChunkSizeBytes;
            }
        }

        // public properties
        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        // methods
        public override Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_closed)
            {
                _closed = true;

                if (_checkMD5 && _position == FilesCollectionDocument.Length)
                {
                    _md5.TransformFinalBlock(new byte[0], 0, 0);
                    var md5 = BsonUtils.ToHexString(_md5.Hash);
                    if (!md5.Equals(FilesCollectionDocument.MD5, StringComparison.OrdinalIgnoreCase))
                    {
#pragma warning disable 618
                        throw new GridFSMD5Exception(FilesCollectionDocument.IdAsBsonValue);
#pragma warning restore
                    }
                }
            }

            return base.CloseAsync(cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(buffer, "buffer");
            Ensure.IsBetween(offset, 0, buffer.Length, "offset");
            Ensure.IsBetween(count, 0, buffer.Length - offset, "count");
            ThrowIfClosedOrDisposed();

            var bytesRead = 0;
            while (count > 0 && _position < FilesCollectionDocument.Length)
            {
                var segment = await GetSegmentAsync(cancellationToken).ConfigureAwait(false);

                var partialCount = Math.Min(count, segment.Count);
                Buffer.BlockCopy(segment.Array, segment.Offset, buffer, offset, partialCount);

                bytesRead += partialCount;
                offset += partialCount;
                count -= partialCount;
                _position += partialCount;
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
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
                    if (_cursor != null)
                    {
                        _cursor.Dispose();
                    }
                    if (_md5 != null)
                    {
                        _md5.Dispose();
                    }
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            base.ThrowIfDisposed();
        }

        // private methods
        private async Task GetFirstBatchAsync(CancellationToken cancellationToken)
        {
            var chunksCollectionNamespace = Bucket.GetChunksCollectionNamespace();
            var messageEncoderSettings = Bucket.GetMessageEncoderSettings();
#pragma warning disable 618
            var filter = new BsonDocument("files_id", FilesCollectionDocument.IdAsBsonValue);
#pragma warning restore
            var sort = new BsonDocument("n", 1);

            var operation = new FindOperation<BsonDocument>(
                chunksCollectionNamespace,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings)
            {
                Filter = filter,
                Sort = sort
            };

            _cursor = await operation.ExecuteAsync(Binding, cancellationToken).ConfigureAwait(false);
            await GetNextBatchAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task GetNextBatchAsync(CancellationToken cancellationToken)
        {
            var previousBatch = _batch;

            var hasMore = await _cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false);
            if (!hasMore)
            {
#pragma warning disable 618
                throw new GridFSChunkException(FilesCollectionDocument.IdAsBsonValue, _nextChunkNumber, "missing");
#pragma warning restore
            }

            _batch = _cursor.Current.ToList();
            if (previousBatch != null)
            {
                _batchPosition += previousBatch.Count * FilesCollectionDocument.ChunkSizeBytes; ;
            }

            var lastChunkInBatch = _batch.Last();
            if (lastChunkInBatch["n"].ToInt32() == _lastChunkNumber + 1 && lastChunkInBatch["data"].AsBsonBinaryData.Bytes.Length == 0)
            {
                _batch.RemoveAt(_batch.Count - 1);
            }

            foreach (var chunk in _batch)
            {
                var n = chunk["n"].ToInt32();
                var bytes = chunk["data"].AsBsonBinaryData.Bytes;

                if (n != _nextChunkNumber)
                {
#pragma warning disable 618
                    throw new GridFSChunkException(FilesCollectionDocument.IdAsBsonValue, _nextChunkNumber, "missing");
#pragma warning restore
                }
                _nextChunkNumber++;

                var expectedChunkSize = n == _lastChunkNumber ? _lastChunkSize : FilesCollectionDocument.ChunkSizeBytes;
                if (bytes.Length != expectedChunkSize)
                {
#pragma warning disable 618
                    throw new GridFSChunkException(FilesCollectionDocument.IdAsBsonValue, _nextChunkNumber, "the wrong size");
#pragma warning restore
                }

                if (_checkMD5)
                {
                    _md5.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
            }
        }

        private async Task<ArraySegment<byte>> GetSegmentAsync(CancellationToken cancellationToken)
        {
            var batchIndex = (int)((_position - _batchPosition) / FilesCollectionDocument.ChunkSizeBytes);

            if (_cursor == null)
            {
                await GetFirstBatchAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (batchIndex == _batch.Count)
            {
                await GetNextBatchAsync(cancellationToken).ConfigureAwait(false);
                batchIndex = 0;
            }

            var bytes = _batch[batchIndex]["data"].AsBsonBinaryData.Bytes;
            var segmentOffset = (int)(_position % FilesCollectionDocument.ChunkSizeBytes);
            var segmentCount = bytes.Length - segmentOffset;
            return new ArraySegment<byte>(bytes, segmentOffset, segmentCount);
        }
    }
}
