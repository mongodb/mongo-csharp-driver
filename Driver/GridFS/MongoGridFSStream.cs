/* Copyright 2010-2011 10gen Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a stream interface to a GridFS file (patterned after .NET's Stream class).
    /// </summary>
    public class MongoGridFSStream : Stream
    {
        // private fields
        private bool disposed = false;
        private MongoGridFS gridFS;
        private MongoGridFSFileInfo fileInfo;
        private FileMode mode;
        private FileAccess access;
        private long length;
        private long position;
        private byte[] chunk;
        private int chunkIndex = -1; // -1 means no chunk is loaded
        private BsonValue chunkId;
        private bool chunkIsDirty;
        private bool updateMD5 = true;

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
            this.gridFS = fileInfo.GridFS;
            this.fileInfo = fileInfo;
            this.mode = mode;
            this.access = access;

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
            gridFS.EnsureIndexes();
        }

        // public properties
        /// <summary>
        /// Gets whether the GridFS stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return access == FileAccess.Read || access == FileAccess.ReadWrite;
            }
        }

        /// <summary>
        /// Gets whether the GridFS stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
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
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return access == FileAccess.Write || access == FileAccess.ReadWrite;
            }
        }

        /// <summary>
        /// Gets the current length (use SetLength to change the length).
        /// </summary>
        public override long Length
        {
            get
            {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return length;
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
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return position;
            }
            set
            {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                position = value;
                if (length < position)
                {
                    SetLength(position);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to compute and update the MD5 hash for the file when the stream is closed.
        /// </summary>
        public bool UpdateMD5
        {
            get { return updateMD5; }
            set { updateMD5 = value; }
        }

        // public methods
        /// <summary>
        /// Flushes any unsaved data in the buffers to the GridFS file.
        /// </summary>
        public override void Flush()
        {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (chunkIsDirty) { SaveChunk(); }
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
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var available = length - position;
            if (count > available) { count = (int)available; }
            var chunkIndex = (int)(position / fileInfo.ChunkSize);
            var chunkOffset = (int)(position % fileInfo.ChunkSize);
            var bytesRead = 0;
            while (count > 0)
            {
                if (this.chunkIndex != chunkIndex) { LoadChunk(chunkIndex); }
                var partialCount = fileInfo.ChunkSize - chunkOffset;
                if (partialCount > count) { partialCount = count; }
                Buffer.BlockCopy(chunk, chunkOffset, buffer, offset, partialCount);
                position += partialCount;
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
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (position < length)
            {
                var chunkIndex = (int)(position / fileInfo.ChunkSize);
                var chunkOffset = (int)(position % fileInfo.ChunkSize);
                if (this.chunkIndex != chunkIndex) { LoadChunk(chunkIndex); }
                var b = chunk[chunkOffset];
                position += 1;
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
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            switch (origin)
            {
                case SeekOrigin.Begin: position = offset; break;
                case SeekOrigin.Current: position += offset; break;
                case SeekOrigin.End: position = length + offset; break;
                default: throw new ArgumentException("origin");
            }
            if (length < position)
            {
                SetLength(position);
            }
            return position;
        }

        /// <summary>
        /// Sets the length of the GridFS file.
        /// </summary>
        /// <param name="value">The length.</param>
        public override void SetLength(long value)
        {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (length != value)
            {
                length = value;
                if (position > length)
                {
                    position = length;
                }

                var lastChunkIndex = (int)((length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize) - 1;
                if (chunkIndex == lastChunkIndex)
                {
                    // if the current chunk is the new last chunk mark it dirty (its size has changed)
                    chunkIsDirty = true;
                }
                else if (chunkIndex > lastChunkIndex)
                {
                    // if the current chunk is beyond the new last chunk throw it away
                    chunkIndex = -1;
                    chunkIsDirty = false;
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
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var chunkIndex = (int)(position / fileInfo.ChunkSize);
            var chunkOffset = (int)(position % fileInfo.ChunkSize);
            while (count > 0)
            {
                if (this.chunkIndex != chunkIndex)
                {
                    if (chunkOffset == 0 && count >= fileInfo.ChunkSize)
                    {
                        LoadChunkNoData(chunkIndex); // don't need the data because we're going to overwrite all of it
                    }
                    else
                    {
                        LoadChunk(chunkIndex);
                    }
                }

                var partialCount = fileInfo.ChunkSize - chunkOffset;
                if (partialCount > count) { partialCount = count; }
                Buffer.BlockCopy(buffer, offset, chunk, chunkOffset, partialCount);
                chunkIsDirty = true;

                position += partialCount;
                if (length < position)
                {
                    length = position; // direct assignment is OK here instead of calling SetLength
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
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var chunkIndex = (int)(position / fileInfo.ChunkSize);
            var chunkOffset = (int)(position % fileInfo.ChunkSize);
            if (this.chunkIndex != chunkIndex)
            {
                LoadChunk(chunkIndex);
            }
            chunk[chunkOffset] = value;
            chunkIsDirty = true;
            position += 1;
            if (length < position)
            {
                length = position; // direct assignment is OK here instead of calling SetLength
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
                if (!disposed)
                {
                    if (disposing)
                    {
                        Flush();
                        AddMissingChunks(); // also removes extra chunks
                        UpdateMetadata();
                    }
                    disposed = true;
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
            var query = Query.EQ("files_id", fileInfo.Id);
            var fields = Fields.Include("n");
            var chunkCount = (int)((length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize);
            var chunksFound = new HashSet<int>();
            var foundExtraChunks = false;
            foreach (var chunk in gridFS.Chunks.Find(query).SetFields(fields))
            {
                var n = chunk["n"].ToInt32();
                chunksFound.Add(n);
                if (n >= chunkCount)
                {
                    foundExtraChunks = true;
                }
            }

            if (foundExtraChunks)
            {
                var extraChunksQuery = Query.And(Query.EQ("files_id", fileInfo.Id), Query.GTE("n", chunkCount));
                gridFS.Chunks.Remove(extraChunksQuery);
            }

            BsonBinaryData zeros = null; // delay creating it until it's actually needed
            for (var n = 0; n < chunkCount; n++)
            {
                if (!chunksFound.Contains(n))
                {
                    if (zeros == null)
                    {
                        zeros = new BsonBinaryData(new byte[fileInfo.ChunkSize]);
                    }
                    var missingChunk = new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "files_id", fileInfo.Id },
                        { "n", n },
                        { "data", zeros }
                    };
                    gridFS.Chunks.Insert(missingChunk);
                }
            }
        }

        private void LoadChunk(int chunkIndex)
        {
            if (chunkIsDirty) { SaveChunk(); }
            var query = Query.And(Query.EQ("files_id", fileInfo.Id), Query.EQ("n", chunkIndex));
            var document = gridFS.Chunks.FindOne(query);
            if (document == null)
            {
                if (chunk == null)
                {
                    chunk = new byte[fileInfo.ChunkSize];
                }
                else
                {
                    Array.Clear(chunk, 0, chunk.Length);
                }
                chunkId = ObjectId.GenerateNewId();
            }
            else
            {
                var bytes = document["data"].AsBsonBinaryData.Bytes;
                if (bytes.Length == fileInfo.ChunkSize)
                {
                    chunk = bytes;
                }
                else
                {
                    if (chunk == null)
                    {
                        chunk = new byte[fileInfo.ChunkSize];
                    }
                    Buffer.BlockCopy(bytes, 0, chunk, 0, bytes.Length);
                    Array.Clear(chunk, bytes.Length, chunk.Length - bytes.Length);
                }
                chunkId = document["_id"];
            }
            this.chunkIndex = chunkIndex;
        }

        private void LoadChunkNoData(int chunkIndex)
        {
            if (chunkIsDirty) { SaveChunk(); }
            if (chunk == null)
            {
                chunk = new byte[fileInfo.ChunkSize];
            }
            else
            {
                Array.Clear(chunk, 0, chunk.Length);
            }

            var query = Query.And(Query.EQ("files_id", fileInfo.Id), Query.EQ("n", chunkIndex));
            var fields = Fields.Include("_id");
            var document = gridFS.Chunks.Find(query).SetFields(fields).SetLimit(1).FirstOrDefault();
            if (document == null)
            {
                chunkId = ObjectId.GenerateNewId();
            }
            else
            {
                chunkId = document["_id"];
            }
            this.chunkIndex = chunkIndex;
        }

        private void OpenAppend()
        {
            length = fileInfo.Length;
            position = fileInfo.Length;
        }

        private void OpenCreate()
        {
            if (fileInfo.Id == null)
            {
                fileInfo.SetId(ObjectId.GenerateNewId());
            }

            var file = new BsonDocument
            {
                { "_id", fileInfo.Id },
                { "filename", fileInfo.Name },
                { "length", 0 },
                { "chunkSize", fileInfo.ChunkSize },
                { "uploadDate", fileInfo.UploadDate },
                { "md5", BsonNull.Value }, // will be updated when the file is closed (unless UpdateMD5 is false)
                { "contentType", fileInfo.ContentType, !string.IsNullOrEmpty(fileInfo.ContentType) }, // optional
                { "aliases", BsonArray.Create(fileInfo.Aliases), fileInfo.Aliases != null && fileInfo.Aliases.Length > 0 }, // optional
                { "metadata", fileInfo.Metadata } // optional
            };
            gridFS.Files.Insert(file);
            length = 0;
            position = 0;
        }

        private void OpenExisting()
        {
            length = fileInfo.Length;
            position = 0;
        }

        private void OpenTruncate()
        {
            // existing chunks will be overwritten as needed and extra chunks will be removed on Close
            length = 0;
            position = 0;
        }

        private void SaveChunk()
        {
            var lastChunkIndex = (int)((length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize) - 1;
            if (chunkIndex == -1 || chunkIndex > lastChunkIndex)
            {
                var message = string.Format("Invalid chunk index {0}.", chunkIndex);
                throw new MongoGridFSException(message);
            }

            var lastChunkSize = (int)(length % fileInfo.ChunkSize);
            if (lastChunkSize == 0)
            {
                lastChunkSize = fileInfo.ChunkSize;
            }

            BsonBinaryData data;
            if (chunkIndex < lastChunkIndex || lastChunkSize == fileInfo.ChunkSize)
            {
                data = new BsonBinaryData(chunk);
            }
            else
            {
                var lastChunk = new byte[lastChunkSize];
                Buffer.BlockCopy(chunk, 0, lastChunk, 0, lastChunkSize);
                data = new BsonBinaryData(lastChunk);
            }

            var query = Query.EQ("_id", chunkId);
            var update = new UpdateDocument
            {
                { "_id", chunkId },
                { "files_id", fileInfo.Id },
                { "n", chunkIndex },
                { "data", data }
            };
            gridFS.Chunks.Update(query, update, UpdateFlags.Upsert);
            chunkIsDirty = false;
        }

        private void UpdateMetadata()
        {
            BsonValue md5 = BsonNull.Value;
            if (updateMD5)
            {
                var md5Command = new CommandDocument
                {
                    { "filemd5", fileInfo.Id },
                    { "root", gridFS.Settings.Root }
                };
                var md5Result = gridFS.Database.RunCommand(md5Command);
                md5 = md5Result.Response["md5"].AsString;
            }

            var query = Query.EQ("_id", fileInfo.Id);
            var update = Update
                .Set("length", length)
                .Set("md5", md5);
            gridFS.Files.Update(query, update);
        }
    }
}
