/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a stream interface to a GridFS file (patterned after .NET's Stream class).
    /// </summary>
    public class MongoGridFSStream : Stream
    {
        // private fields
        private readonly MongoGridFSFileInfo _fileInfo;
        private readonly FileAccess _access;
        private long _length;
        private long _position;
        private byte[] _chunk;
        private long _chunkIndex = -1; // -1 means no chunk is loaded
        private BsonValue _chunkId;
        private bool _chunkIsDirty;
        private bool _fileIsDirty;
        private bool _updateMD5; // will eventually be removed, for now initialize from settings
        private bool _disposed = false;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFSStream class.
        /// </summary>
        /// <param name="fileInfo">The GridFS file info.</param>
        /// <param name="mode">The mode.</param>
        public MongoGridFSStream(MongoGridFSFileInfo fileInfo, FileMode mode)
            : this(fileInfo, mode, FileAccess.ReadWrite)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFSStream class.
        /// </summary>
        /// <param name="fileInfo">The GridFS file info.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The acess.</param>
        public MongoGridFSStream(MongoGridFSFileInfo fileInfo, FileMode mode, FileAccess access)
        {
            _fileInfo = fileInfo;
            _access = access;
            _updateMD5 = fileInfo.GridFSSettings.UpdateMD5;

            var exists = fileInfo.Exists;
            string message;
            switch (mode)
            {
                case FileMode.Append:
                    if (exists)
                    {
                        OpenAppend();
                    }
                    else
                    {
                        OpenCreate();
                    }
                    break;
                case FileMode.Create:
                    if (exists)
                    {
                        OpenTruncate();
                    }
                    else
                    {
                        OpenCreate();
                    }
                    break;
                case FileMode.CreateNew:
                    if (exists)
                    {
                        message = string.Format("File '{0}' already exists.", fileInfo.Name);
                        throw new IOException(message);
                    }
                    else
                    {
                        OpenCreate();
                    }
                    break;
                case FileMode.Open:
                    if (exists)
                    {
                        OpenExisting();
                    }
                    else
                    {
                        message = string.Format("File '{0}' not found.", fileInfo.Name);
                        throw new FileNotFoundException(message);
                    }
                    break;
                case FileMode.OpenOrCreate:
                    if (exists)
                    {
                        OpenExisting();
                    }
                    else
                    {
                        OpenCreate();
                    }
                    break;
                case FileMode.Truncate:
                    if (exists)
                    {
                        OpenTruncate();
                    }
                    else
                    {
                        message = string.Format("File '{0}' not found.", fileInfo.Name);
                        throw new FileNotFoundException(message);
                    }
                    break;
                default:
                    message = string.Format("Invalid FileMode {0}.", mode);
                    throw new ArgumentException(message, "mode");
            }
        }

        // public properties
        /// <summary>
        /// Gets whether the GridFS stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return _access == FileAccess.Read || _access == FileAccess.ReadWrite;
            }
        }

        /// <summary>
        /// Gets whether the GridFS stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return true;
            }
        }

        /// <summary>
        /// Gets whether the GridFS stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return _access == FileAccess.Write || _access == FileAccess.ReadWrite;
            }
        }

        /// <summary>
        /// Gets the current length (use SetLength to change the length).
        /// </summary>
        public override long Length
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return _length;
            }
            // there is no set accessor to override, Stream defines SetLength instead
        }

        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        public override long Position
        {
            get
            {
                if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return _position;
            }
            set
            {
                if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                _position = value;
                if (_length < _position)
                {
                    SetLength(_position);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to compute and update the MD5 hash for the file when the stream is closed.
        /// </summary>
        [Obsolete("Use UpdateMD5 on MongoGridFSSettings instead.")]
        public bool UpdateMD5
        {
            get { return _updateMD5; }
            set { _updateMD5 = value; }
        }

        // public methods
        /// <summary>
        /// Flushes any unsaved data in the buffers to the GridFS file.
        /// </summary>
        public override void Flush()
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (_chunkIsDirty) { SaveChunk(); }
        }

        /// <summary>
        /// Reads bytes from the GridFS stream.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="offset">The offset in the destination buffer at which to place the read bytes.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var available = _length - _position;
            if (count > available) { count = (int)available; }
            var chunkIndex = _position / _fileInfo.ChunkSize;
            var chunkOffset = (int)(_position % _fileInfo.ChunkSize);
            var bytesRead = 0;
            while (count > 0)
            {
                if (_chunkIndex != chunkIndex) { LoadChunk(chunkIndex); }
                var partialCount = _fileInfo.ChunkSize - chunkOffset;
                if (partialCount > count) { partialCount = count; }
                Buffer.BlockCopy(_chunk, chunkOffset, buffer, offset, partialCount);
                _position += partialCount;
                chunkIndex += 1;
                chunkOffset = 0;
                offset += partialCount;
                count -= partialCount;
                bytesRead += partialCount;
            }
            return bytesRead;
        }

        /// <summary>
        /// Reads one byte from the GridFS stream.
        /// </summary>
        /// <returns>The byte (-1 if at the end of the GridFS stream).</returns>
        public override int ReadByte()
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (_position < _length)
            {
                var chunkIndex = _position / _fileInfo.ChunkSize;
                var chunkOffset = (int)(_position % _fileInfo.ChunkSize);
                if (_chunkIndex != chunkIndex) { LoadChunk(chunkIndex); }
                var b = _chunk[chunkOffset];
                _position += 1;
                return b;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Seeks to a new position.
        /// </summary>
        /// <param name="offset">The seek offset.</param>
        /// <param name="origin">The seek origin.</param>
        /// <returns>The new position.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            switch (origin)
            {
                case SeekOrigin.Begin: _position = offset; break;
                case SeekOrigin.Current: _position += offset; break;
                case SeekOrigin.End: _position = _length + offset; break;
                default: throw new ArgumentException("origin");
            }
            if (_length < _position)
            {
                SetLength(_position);
            }
            return _position;
        }

        /// <summary>
        /// Sets the length of the GridFS file.
        /// </summary>
        /// <param name="value">The length.</param>
        public override void SetLength(long value)
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (_length != value)
            {
                _length = value;
                if (_position > _length)
                {
                    _position = _length;
                }
                _fileIsDirty = true;

                var lastChunkIndex = ((_length + _fileInfo.ChunkSize - 1) / _fileInfo.ChunkSize) - 1;
                if (_chunkIndex == lastChunkIndex)
                {
                    // if the current chunk is the new last chunk mark it dirty (its size has changed)
                    _chunkIsDirty = true;
                }
                else if (_chunkIndex > lastChunkIndex)
                {
                    // if the current chunk is beyond the new last chunk throw it away
                    _chunkIndex = -1;
                    _chunkIsDirty = false;
                }
            }
        }

        /// <summary>
        /// Writes bytes to the GridFS stream.
        /// </summary>
        /// <param name="buffer">The source buffer.</param>
        /// <param name="offset">The offset in the source buffer to the bytes.</param>
        /// <param name="count">The number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var chunkIndex = _position / _fileInfo.ChunkSize;
            var chunkOffset = (int)(_position % _fileInfo.ChunkSize);
            while (count > 0)
            {
                if (_chunkIndex != chunkIndex)
                {
                    if (chunkOffset == 0 && count >= _fileInfo.ChunkSize)
                    {
                        LoadChunkNoData(chunkIndex); // don't need the data because we're going to overwrite all of it
                    }
                    else
                    {
                        LoadChunk(chunkIndex);
                    }
                }

                var partialCount = _fileInfo.ChunkSize - chunkOffset;
                if (partialCount > count) { partialCount = count; }
                Buffer.BlockCopy(buffer, offset, _chunk, chunkOffset, partialCount);
                _chunkIsDirty = true;
                _fileIsDirty = true;

                _position += partialCount;
                if (_length < _position)
                {
                    _length = _position; // direct assignment is OK here instead of calling SetLength
                }

                offset += partialCount;
                count -= partialCount;
                if (count > 0)
                {
                    SaveChunk();
                    chunkIndex += 1;
                    chunkOffset = 0;
                }
            }
        }

        /// <summary>
        /// Writes one byte to the GridFS stream.
        /// </summary>
        /// <param name="value">The byte.</param>
        public override void WriteByte(byte value)
        {
            if (_disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var chunkIndex = _position / _fileInfo.ChunkSize;
            var chunkOffset = (int)(_position % _fileInfo.ChunkSize);
            if (_chunkIndex != chunkIndex)
            {
                LoadChunk(chunkIndex);
            }
            _chunk[chunkOffset] = value;
            _chunkIsDirty = true;
            _fileIsDirty = true;
            _position += 1;
            if (_length < _position)
            {
                _length = _position; // direct assignment is OK here instead of calling SetLength
            }
        }

        // protected methods
        /// <summary>
        /// Disposes of any resources used by the stream.
        /// </summary>
        /// <param name="disposing">True if called from Dispose.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        if (_fileIsDirty)
                        {
                            Flush();
                            AddMissingChunks(); // also removes extra chunks
                            UpdateMetadata();
                        }
                    }
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // private methods
        private void AddMissingChunks()
        {
            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                var database = gridFS.GetDatabase(ReadPreference.Primary);
                var chunksCollection = gridFS.GetChunksCollection(database);

                var query = Query.EQ("files_id", _fileInfo.Id);
                var fields = Fields.Include("n");
                var chunkCount = (_length + _fileInfo.ChunkSize - 1) / _fileInfo.ChunkSize;
                var chunksFound = new HashSet<long>();
                var foundExtraChunks = false;
                foreach (var chunk in chunksCollection.Find(query).SetFields(fields))
                {
                    var n = chunk["n"].ToInt64();
                    chunksFound.Add(n);
                    if (n >= chunkCount)
                    {
                        foundExtraChunks = true;
                    }
                }

                if (foundExtraChunks)
                {
                    var extraChunksQuery = Query.And(Query.EQ("files_id", _fileInfo.Id), Query.GTE("n", chunkCount));
                    chunksCollection.Remove(extraChunksQuery);
                }

                BsonBinaryData zeros = null; // delay creating it until it's actually needed
                for (var n = 0L; n < chunkCount; n++)
                {
                    if (!chunksFound.Contains(n))
                    {
                        if (zeros == null)
                        {
                            zeros = new BsonBinaryData(new byte[_fileInfo.ChunkSize]);
                        }
                        var missingChunk = new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "files_id", _fileInfo.Id },
                        { "n", n < int.MaxValue ? (BsonValue)(BsonInt32)(int)n : (BsonInt64)n },
                        { "data", zeros }
                    };
                        chunksCollection.Insert(missingChunk);
                    }
                }
            }
        }

        private void EnsureServerInstanceIsPrimary()
        {
            var serverInstance = _fileInfo.ServerInstance;
            if (!serverInstance.IsPrimary)
            {
                var message = string.Format("Server instance {0} is not the current primary.", serverInstance.Address);
                throw new InvalidOperationException(message);
            }
        }

        private void LoadChunk(long chunkIndex)
        {
            if (_chunkIsDirty) { SaveChunk(); }

            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                var database = gridFS.GetDatabase();
                var chunksCollection = gridFS.GetChunksCollection(database);

                var query = Query.And(Query.EQ("files_id", _fileInfo.Id), Query.EQ("n", chunkIndex));
                var document = chunksCollection.FindOne(query);
                if (document == null)
                {
                    if (_chunk == null)
                    {
                        _chunk = new byte[_fileInfo.ChunkSize];
                    }
                    else
                    {
                        Array.Clear(_chunk, 0, _chunk.Length);
                    }
                    _chunkId = ObjectId.GenerateNewId();
                }
                else
                {
                    var bytes = document["data"].AsBsonBinaryData.Bytes;
                    if (bytes.Length == _fileInfo.ChunkSize)
                    {
                        _chunk = bytes;
                    }
                    else
                    {
                        if (_chunk == null)
                        {
                            _chunk = new byte[_fileInfo.ChunkSize];
                        }
                        Buffer.BlockCopy(bytes, 0, _chunk, 0, bytes.Length);
                        Array.Clear(_chunk, bytes.Length, _chunk.Length - bytes.Length);
                    }
                    _chunkId = document["_id"];
                }
                _chunkIndex = chunkIndex;
            }
        }

        private void LoadChunkNoData(long chunkIndex)
        {
            if (_chunkIsDirty) { SaveChunk(); }

            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                var database = gridFS.GetDatabase();
                var chunksCollection = gridFS.GetChunksCollection(database);

                if (_chunk == null)
                {
                    _chunk = new byte[_fileInfo.ChunkSize];
                }
                else
                {
                    Array.Clear(_chunk, 0, _chunk.Length);
                }

                var query = Query.And(Query.EQ("files_id", _fileInfo.Id), Query.EQ("n", chunkIndex));
                var fields = Fields.Include("_id");
                var document = chunksCollection.Find(query).SetFields(fields).SetLimit(1).FirstOrDefault();
                if (document == null)
                {
                    _chunkId = ObjectId.GenerateNewId();
                }
                else
                {
                    _chunkId = document["_id"];
                }
                _chunkIndex = chunkIndex;
            }
        }

        private void OpenAppend()
        {
            EnsureServerInstanceIsPrimary();
            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                gridFS.EnsureIndexes();

                _length = _fileInfo.Length;
                _position = _fileInfo.Length;
            }
        }

        private void OpenCreate()
        {
            EnsureServerInstanceIsPrimary();
            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                var database = gridFS.GetDatabase(ReadPreference.Primary);
                var filesCollection = gridFS.GetFilesCollection(database);

                gridFS.EnsureIndexes();

                _fileIsDirty = true;
                if (_fileInfo.Id == null)
                {
                    _fileInfo.SetId(ObjectId.GenerateNewId());
                }

                var aliases = (_fileInfo.Aliases != null) ? new BsonArray(_fileInfo.Aliases) : null;
                var file = new BsonDocument
                {
                    { "_id", _fileInfo.Id },
                    { "filename", _fileInfo.Name, !string.IsNullOrEmpty(_fileInfo.Name) },
                    { "length", 0 },
                    { "chunkSize", _fileInfo.ChunkSize },
                    { "uploadDate", _fileInfo.UploadDate },
                    { "md5", BsonNull.Value }, // will be updated when the file is closed (unless UpdateMD5 is false)
                    { "contentType", _fileInfo.ContentType, !string.IsNullOrEmpty(_fileInfo.ContentType) }, // optional
                    { "aliases", aliases, aliases != null }, // optional
                    { "metadata", _fileInfo.Metadata, _fileInfo.Metadata != null } // optional
                };
                filesCollection.Insert(file);

                _length = 0;
                _position = 0;
            }
        }

        private void OpenExisting()
        {
            _length = _fileInfo.Length;
            _position = 0;
        }

        private void OpenTruncate()
        {
            EnsureServerInstanceIsPrimary();
            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                gridFS.EnsureIndexes();

                _fileIsDirty = true;
                // existing chunks will be overwritten as needed and extra chunks will be removed on Close
                _length = 0;
                _position = 0;
            }
        }

        private void SaveChunk()
        {
            using (_fileInfo.Server.RequestStart(_fileInfo.ServerInstance))
            {
                var connectionId = _fileInfo.Server.RequestConnectionId;
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                var database = gridFS.GetDatabase(ReadPreference.Primary);
                var chunksCollection = gridFS.GetChunksCollection(database);

                var lastChunkIndex = (_length + _fileInfo.ChunkSize - 1) / _fileInfo.ChunkSize - 1;
                if (_chunkIndex == -1 || _chunkIndex > lastChunkIndex)
                {
                    var message = string.Format("Invalid chunk index {0}.", _chunkIndex);
                    throw new MongoGridFSException(connectionId, message);
                }

                var lastChunkSize = (int)(_length % _fileInfo.ChunkSize);
                if (lastChunkSize == 0)
                {
                    lastChunkSize = _fileInfo.ChunkSize;
                }

                BsonBinaryData data;
                if (_chunkIndex < lastChunkIndex || lastChunkSize == _fileInfo.ChunkSize)
                {
                    data = new BsonBinaryData(_chunk);
                }
                else
                {
                    var lastChunk = new byte[lastChunkSize];
                    Buffer.BlockCopy(_chunk, 0, lastChunk, 0, lastChunkSize);
                    data = new BsonBinaryData(lastChunk);
                }

                var query = Query.EQ("_id", _chunkId);
                var update = new UpdateDocument
                {
                    { "_id", _chunkId },
                    { "files_id", _fileInfo.Id },
                    { "n", _chunkIndex < int.MaxValue ? (BsonValue)(BsonInt32)(int)_chunkIndex : (BsonInt64)_chunkIndex },
                    { "data", data }
                };
                chunksCollection.Update(query, update, UpdateFlags.Upsert);
                _chunkIsDirty = false;
            }
        }

        private void UpdateMetadata()
        {
            using (_fileInfo.Server.RequestStart(ReadPreference.Primary))
            {
                var gridFS = new MongoGridFS(_fileInfo.Server, _fileInfo.DatabaseName, _fileInfo.GridFSSettings);
                var database = gridFS.GetDatabase(ReadPreference.Primary);
                var filesCollection = gridFS.GetFilesCollection(database);

                BsonValue md5 = BsonNull.Value;
                if (_updateMD5)
                {
                    var md5Command = new CommandDocument
                {
                    { "filemd5", _fileInfo.Id },
                    { "root", gridFS.Settings.Root }
                };
                    var md5Result = database.RunCommand(md5Command);
                    md5 = md5Result.Response["md5"].AsString;
                }

                var query = Query.EQ("_id", _fileInfo.Id);
                var update = Update
                    .Set("length", _length)
                    .Set("md5", md5);

                filesCollection.Update(query, update);
            }
        }
    }
}
