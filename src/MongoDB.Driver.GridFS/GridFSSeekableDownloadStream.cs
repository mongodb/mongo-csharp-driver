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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.GridFS
{
    internal class GridFSSeekableDownloadStream : GridFSDownloadStreamBase
    {
        // private fields
        private byte[] _chunk;
        private long _n;
        private long _position;

        // constructors
        public GridFSSeekableDownloadStream(
            GridFSBucket bucket,
            IReadBinding binding,
            GridFSFileInfo fileInfo)
            : base(bucket, binding, fileInfo)
        {
        }

        // public properties
        public override bool CanSeek
        {
            get { return true; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                Ensure.IsGreaterThanOrEqualToZero(value, nameof(value));
                _position = value;
            }
        }


        // methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));
            ThrowIfClosedOrDisposed();

            var bytesRead = 0;
            while (count > 0 && _position < FileInfo.Length)
            {
                var segment = GetSegment(CancellationToken.None);

                var partialCount = Math.Min(count, segment.Count);
                Buffer.BlockCopy(segment.Array, segment.Offset, buffer, offset, partialCount);

                bytesRead += partialCount;
                offset += partialCount;
                count -= partialCount;
                _position += partialCount;
            }

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));
            ThrowIfClosedOrDisposed();

            var bytesRead = 0;
            while (count > 0 && _position < FileInfo.Length)
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
            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin: newPosition = offset; break;
                case SeekOrigin.Current: newPosition = _position + offset; break;
                case SeekOrigin.End: newPosition = _position + offset; break;
                default: throw new ArgumentException("Invalid origin.", "origin");
            }
            Position = newPosition;
            return newPosition;
        }

        // private methods
        private FindOperation<BsonDocument> CreateGetChunkOperation(long n)
        {
            var chunksCollectionNamespace = Bucket.GetChunksCollectionNamespace();
            var messageEncoderSettings = Bucket.GetMessageEncoderSettings();
#pragma warning disable 618
            var filter = new BsonDocument
            {
                { "files_id", FileInfo.IdAsBsonValue },
                { "n", n }
            };
#pragma warning restore

            return new FindOperation<BsonDocument>(
                chunksCollectionNamespace,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings)
            {
                Filter = filter,
                Limit = -1
            };

        }

        private void GetChunk(long n, CancellationToken cancellationToken)
        {
            var operation = CreateGetChunkOperation(n);
            using (var cursor = operation.Execute(Binding, cancellationToken))
            {
                var documents = cursor.ToList();
                _chunk = GetChunkHelper(n, documents);
                _n = n;
            }
        }

        private async Task GetChunkAsync(long n, CancellationToken cancellationToken)
        {
            var operation = CreateGetChunkOperation(n);
            using (var cursor = await operation.ExecuteAsync(Binding, cancellationToken).ConfigureAwait(false))
            {
                var documents = await cursor.ToListAsync().ConfigureAwait(false);
                _chunk = GetChunkHelper(n, documents);
                _n = n;
            }
        }

        private byte[] GetChunkHelper(long n, List<BsonDocument> documents)
        {
            if (documents.Count == 0)
            {
#pragma warning disable 618
                throw new GridFSChunkException(FileInfo.IdAsBsonValue, n, "missing");
#pragma warning restore
            }

            var document = documents[0];
            var data = document["data"].AsBsonBinaryData.Bytes;

            var chunkSizeBytes = FileInfo.ChunkSizeBytes;
            var lastChunk = 0;
            var expectedChunkSize = n == lastChunk ? FileInfo.Length % chunkSizeBytes : chunkSizeBytes;
            if (data.Length != expectedChunkSize)
            {
#pragma warning disable 618
                throw new GridFSChunkException(FileInfo.IdAsBsonValue, n, "the wrong size");
#pragma warning restore
            }

            return data;
        }

        private ArraySegment<byte> GetSegment(CancellationToken cancellationToken)
        {
            var n = _position / FileInfo.ChunkSizeBytes;
            if (_n != n)
            {
                GetChunk(n, cancellationToken);
            }

            var segmentOffset = (int)(_position % FileInfo.ChunkSizeBytes);
            var segmentCount = _chunk.Length - segmentOffset;

            return new ArraySegment<byte>(_chunk, segmentOffset, segmentCount);
        }

        private async Task<ArraySegment<byte>> GetSegmentAsync(CancellationToken cancellationToken)
        {
            var n = _position / FileInfo.ChunkSizeBytes;
            if (_n != n)
            {
                await GetChunkAsync(n, cancellationToken).ConfigureAwait(false);
            }

            var segmentOffset = (int)(_position % FileInfo.ChunkSizeBytes);
            var segmentCount = _chunk.Length - segmentOffset;

            return new ArraySegment<byte>(_chunk, segmentOffset, segmentCount);
        }
    }
}
