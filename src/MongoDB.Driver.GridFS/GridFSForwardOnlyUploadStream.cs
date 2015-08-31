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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.GridFS
{
    internal class GridFSForwardOnlyUploadStream : GridFSUploadStream
    {
        // fields
        private bool _aborted;
        private readonly List<string> _aliases;
        private List<byte[]> _batch;
        private long _batchPosition;
        private int _batchSize;
        private readonly IWriteBinding _binding;
        private readonly GridFSBucket _bucket;
        private readonly int _chunkSizeBytes;
        private bool _closed;
        private readonly string _contentType;
        private bool _disposed;
        private readonly string _filename;
        private readonly ObjectId _id;
        private long _length;
        private readonly MD5 _md5;
        private readonly BsonDocument _metadata;

        // constructors
        public GridFSForwardOnlyUploadStream(
            GridFSBucket bucket,
            IWriteBinding binding,
            ObjectId id,
            string filename,
            BsonDocument metadata,
            IEnumerable<string> aliases,
            string contentType,
            int chunkSizeBytes,
            int batchSize)
        {
            _bucket = bucket;
            _binding = binding;
            _id = id;
            _filename = filename;
            _metadata = metadata; // can be null
            _aliases = aliases == null ? null : aliases.ToList(); // can be null
            _contentType = contentType; // can be null
            _chunkSizeBytes = chunkSizeBytes;
            _batchSize = batchSize;

            _batch = new List<byte[]>();
            _md5 = MD5.Create();
        }

        // properties
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override ObjectId Id
        {
            get { return _id; }
        }

        public override long Length
        {
            get { return _length; }
        }

        public override long Position
        {
            get
            {
                return _length;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        // methods
        public override async Task AbortAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_aborted)
            {
                return;
            }
            ThrowIfClosedOrDisposed();
            _aborted = true;

            var chunksCollectionNamespace = _bucket.GetChunksCollectionNamespace();
            var filter = new BsonDocument("files_id", _id);
            var deleteRequest = new DeleteRequest(filter) { Limit = 0 };
            var requests = new WriteRequest[] { deleteRequest };
            var messageEncoderSettings = _bucket.GetMessageEncoderSettings();
            var operation = new BulkMixedWriteOperation(chunksCollectionNamespace, requests, messageEncoderSettings)
            {
                WriteConcern = _bucket.Options.WriteConcern
            };

            await operation.ExecuteAsync(_binding, cancellationToken);
        }

        public override async Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_closed)
            {
                return;
            }
            ThrowIfDisposed();
            _closed = true;

            if (!_aborted)
            {
                await WriteFinalBatchAsync(cancellationToken).ConfigureAwait(false);
                await WriteFilesCollectionDocumentAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
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

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfAbortedClosedOrDisposed();
            while (count > 0)
            {
                var chunk = await GetCurrentChunkAsync(cancellationToken).ConfigureAwait(false);
                var partialCount = Math.Min(count, chunk.Count);
                Buffer.BlockCopy(buffer, offset, chunk.Array, chunk.Offset, partialCount);
                offset += partialCount;
                count -= partialCount;
                _length += partialCount;
            }
        }

        // private methods
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    try
                    {
                        CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // ignore exceptions
                    }

                    if (_md5 != null)
                    {
                        _md5.Dispose();
                    }

                    _binding.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private IMongoCollection<BsonDocument> GetChunksCollection()
        {
            return GetCollection("chunks");
        }

        private IMongoCollection<BsonDocument> GetCollection(string suffix)
        {
            var database = _bucket.Database;
            var collectionName = _bucket.Options.BucketName + "." + suffix;
            var writeConcern = _bucket.Options.WriteConcern ?? database.Settings.WriteConcern;
            var settings = new MongoCollectionSettings { WriteConcern = writeConcern };
            return database.GetCollection<BsonDocument>(collectionName, settings);
        }

        private async Task<ArraySegment<byte>> GetCurrentChunkAsync(CancellationToken cancellationToken)
        {
            var batchIndex = (int)((_length - _batchPosition) / _chunkSizeBytes);

            if (batchIndex == _batchSize)
            {
                await WriteBatchAsync(cancellationToken).ConfigureAwait(false);
                _batch.Clear();
                batchIndex = 0;
            }

            if (_batch.Count <= batchIndex)
            {
                _batch.Add(new byte[_chunkSizeBytes]);
            }

            var chunk = _batch[batchIndex];
            var offset = (int)(_length % _chunkSizeBytes);
            var count = _chunkSizeBytes - offset;
            return new ArraySegment<byte>(chunk, offset, count);
        }

        private IMongoCollection<BsonDocument> GetFilesCollection()
        {
            return GetCollection("files");
        }
        
        private void ThrowIfAbortedClosedOrDisposed()
        {
            if (_aborted)
            {
                throw new InvalidOperationException("The upload was aborted.");
            }
            ThrowIfClosedOrDisposed();
        }

        private void ThrowIfClosedOrDisposed()
        {
            if (_closed)
            {
                throw new InvalidOperationException("The stream is closed.");
            }
            ThrowIfDisposed();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void TruncateFinalChunk()
        {
            var finalChunkSize = (int)(_length % _chunkSizeBytes);
            if (finalChunkSize > 0)
            {
                var finalChunk = _batch[_batch.Count - 1];
                if (finalChunk.Length != finalChunkSize)
                {
                    var truncatedFinalChunk = new byte[finalChunkSize];
                    Buffer.BlockCopy(finalChunk, 0, truncatedFinalChunk, 0, finalChunkSize);
                    _batch[_batch.Count - 1] = truncatedFinalChunk;
                }
            }
        }

        private async Task WriteBatchAsync(CancellationToken cancellationToken)
        {
            var chunkDocuments = new List<BsonDocument>();

            var n = (int)(_batchPosition / _chunkSizeBytes);
            foreach (var chunk in _batch)
            {
                var chunkDocument = new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "files_id", _id },
                    { "n", n++ },
                    { "data", new BsonBinaryData(chunk, BsonBinarySubType.Binary) }
                };
                chunkDocuments.Add(chunkDocument);

                _batchPosition += chunk.Length;
                _md5.TransformBlock(chunk, 0, chunk.Length, null, 0);
            }

            var chunksCollection = GetChunksCollection();
            await chunksCollection.InsertManyAsync(chunkDocuments, cancellationToken: cancellationToken).ConfigureAwait(false);

            _batch.Clear();
        }

        private async Task WriteFilesCollectionDocumentAsync(CancellationToken cancellationToken)
        {
            var uploadDateTime = DateTime.UtcNow;

            var filesCollectionDocument = new BsonDocument
            {
                { "_id", _id },
                { "length", _length },
                { "chunkSize", _chunkSizeBytes },
                { "uploadDate", uploadDateTime },
                { "md5", BsonUtils.ToHexString(_md5.Hash) },
                { "filename", _filename },
                { "contentType", _contentType, _contentType != null },
                { "aliases", () => new BsonArray(_aliases.Select(a => new BsonString(a))), _aliases != null },
                { "metadata", _metadata, _metadata != null }
            };

            var filesCollection = GetFilesCollection();
            await filesCollection.InsertOneAsync(filesCollectionDocument, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteFinalBatchAsync(CancellationToken cancellationToken)
        {
            if (_batch.Count > 0)
            {
                TruncateFinalChunk();
                await WriteBatchAsync(cancellationToken).ConfigureAwait(false);
            }
            _md5.TransformFinalBlock(new byte[0], 0, 0);
        }
    }
}
