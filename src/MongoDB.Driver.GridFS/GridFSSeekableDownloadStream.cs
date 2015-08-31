﻿/* Copyright 2015 MongoDB Inc.
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
            GridFSFilesCollectionDocument filesCollectionDocument)
            : base(bucket, binding, filesCollectionDocument)
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
                Ensure.IsGreaterThanOrEqualToZero(value, "value");
                _position = value;
            }
        }


        // methods
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
        private async Task GetChunkAsync(long n, CancellationToken cancellationToken)
        {
            var chunksCollectionNamespace = Bucket.GetChunksCollectionNamespace();
            var messageEncoderSettings = Bucket.GetMessageEncoderSettings();
#pragma warning disable 618
            var filter = new BsonDocument
            {
                { "files_id", FilesCollectionDocument.IdAsBsonValue },
                { "n", n }
            };
#pragma warning restore

            var operation = new FindOperation<BsonDocument>(
                chunksCollectionNamespace,
                BsonDocumentSerializer.Instance,
                messageEncoderSettings)
            {
                Filter = filter,
                Limit = -1                
            };

            using (var cursor = await operation.ExecuteAsync(Binding, cancellationToken).ConfigureAwait(false))
            {
                var documents = await cursor.ToListAsync();
                if (documents.Count == 0)
                {
#pragma warning disable 618
                   throw new GridFSChunkException(FilesCollectionDocument.IdAsBsonValue, n, "missing");
#pragma warning restore
                }

                var document = documents[0];
                var data = document["data"].AsBsonBinaryData.Bytes;

                var chunkSizeBytes = FilesCollectionDocument.ChunkSizeBytes;
                var lastChunk = 0;
                var expectedChunkSize = n == lastChunk ? FilesCollectionDocument.Length % chunkSizeBytes : chunkSizeBytes;
                if (data.Length != expectedChunkSize)
                {
#pragma warning disable 618
                    throw new GridFSChunkException(FilesCollectionDocument.IdAsBsonValue, n, "the wrong size");
#pragma warning restore
                }

                _chunk = data;
                _n = n;
            }
        }

        private async Task<ArraySegment<byte>> GetSegmentAsync(CancellationToken cancellationToken)
        {
            var n = _position / FilesCollectionDocument.ChunkSizeBytes;
            if (_n != n)
            {
                await GetChunkAsync(n, cancellationToken).ConfigureAwait(false);
            }

            var segmentOffset = (int)(_position % FilesCollectionDocument.ChunkSizeBytes);
            var segmentCount = _chunk.Length - segmentOffset;

            return new ArraySegment<byte>(_chunk, segmentOffset, segmentCount);
        }
    }
}
