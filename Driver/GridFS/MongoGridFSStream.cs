/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Driver.GridFS {
    public class MongoGridFSStream : Stream {
        #region private fields
        private bool disposed = false;
        private MongoGridFS gridFS;
        private MongoGridFSFileInfo fileInfo;
        private FileMode mode;
        private FileAccess access;
        private long length;
        private long position;
        private byte[] chunk;
        private int chunkIndex;
        private BsonValue chunkId;
        private bool chunkIsDirty;
        #endregion

        #region constructors
        public MongoGridFSStream(
            MongoGridFSFileInfo fileInfo,
            FileMode mode
        )
            : this(fileInfo, mode, FileAccess.ReadWrite) {
        }

        public MongoGridFSStream(
            MongoGridFSFileInfo fileInfo,
            FileMode mode,
            FileAccess access
        ) {
            this.gridFS = fileInfo.GridFS;
            this.fileInfo = fileInfo;
            this.mode = mode;
            this.access = access;

            var exists = fileInfo.Exists;
            string message;
            switch (mode) {
                case FileMode.Append:
                    if (exists) {
                        Append();
                    } else {
                        Create();
                    }
                    break;
                case FileMode.Create:
                    if (exists) {
                        Truncate();
                    } else {
                        Create();
                    }
                    break;
                case FileMode.CreateNew:
                    if (exists) {
                        message = string.Format("File already exists: {0}", fileInfo.Name);
                        throw new IOException(message);
                    } else {
                        Create();
                    }
                    break;
                case FileMode.Open:
                    if (exists) {
                        Open();
                    } else {
                        message = string.Format("File not found: {0}", fileInfo.Name);
                        throw new FileNotFoundException(message);
                    }
                    break;
                case FileMode.OpenOrCreate:
                    if (exists) {
                        Open();
                    } else {
                        Create();
                    }
                    break;
                case FileMode.Truncate:
                    if (exists) {
                        Truncate();
                    } else {
                        message = string.Format("File not found: {0}", fileInfo.Name);
                        throw new FileNotFoundException(message);
                    }
                    break;
                default:
                    message = string.Format("Invalid FileMode: {0}", fileInfo.Name);
                    throw new ArgumentException(message, "mode");
            }
            gridFS.Chunks.EnsureIndex("files_id", "n");
        }
        #endregion

        #region public properties
        public override bool CanRead {
            get {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return access == FileAccess.Read || access == FileAccess.ReadWrite;
            }
        }

        public override bool CanSeek {
            get {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return true;
            }
        }

        public override bool CanWrite {
            get {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return access == FileAccess.Write || access == FileAccess.ReadWrite;
            }
        }

        public override long Length {
            get {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return length;
            }
            // there is no set accessor to override, Stream defines SetLength instead
        }

        public override long Position {
            get {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                return position;
            }
            set {
                if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
                position = value;
                if (length < position) {
                    length = position;
                }
            }
        }
        #endregion

        #region public methods
        public override void Close() {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (chunkIsDirty) { SaveChunk(); }
            AddMissingChunks();
            UpdateFileInfo();
        }

        public override void Flush() {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (chunkIsDirty) { SaveChunk(); }
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var available = length - position;
            if (count > available) { count = (int) available; }
            var chunkIndex = (int) (position / fileInfo.ChunkSize);
            var chunkOffset = (int) (position % fileInfo.ChunkSize);
            var bytesRead = count;
            while (count > 0) {
                if (this.chunkIndex != chunkIndex) { LoadChunk(chunkIndex); }
                var partialCount = fileInfo.ChunkSize - chunkOffset;
                if (partialCount > count) { partialCount = count; }
                Buffer.BlockCopy(chunk, chunkOffset, buffer, offset, partialCount);
                position += partialCount;
                chunkIndex += 1;
                chunkOffset = 0;
                offset += partialCount;
                count -= partialCount;
            }
            return bytesRead;
        }

        public override int ReadByte() {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            if (position < length) {
                var chunkIndex = (int) (position / fileInfo.ChunkSize);
                var chunkOffset = (int) (position % fileInfo.ChunkSize);
                if (this.chunkIndex != chunkIndex) { LoadChunk(chunkIndex); }
                var b = chunk[chunkOffset];
                position += 1;
                return b;
            } else {
                return -1;
            }
        }

        public override long Seek(
            long offset,
            SeekOrigin origin
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            switch (origin) {
                case SeekOrigin.Begin: position = offset; break;
                case SeekOrigin.Current: position += offset; break;
                case SeekOrigin.End: position = length + offset; break;
                default: throw new ArgumentException("origin");
            }
            if (length < position) {
                length = position;
            }
            return position;
        }

        public override void SetLength(
            long value
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            length = value;
            if (position > length) {
                position = length;
            }
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var chunkIndex = (int) (position / fileInfo.ChunkSize);
            var chunkOffset = (int) (position % fileInfo.ChunkSize);
            while (count > 0) {
                if (this.chunkIndex != chunkIndex) {
                    if (chunkIsDirty) { SaveChunk(); }
                    if (chunkOffset == 0 && count >= fileInfo.ChunkSize) {
                        LoadChunkNoData(chunkIndex); // don't need the data because we're going to overwrite all of it
                    } else {
                        LoadChunk(chunkIndex);
                    }
                }

                var partialCount = fileInfo.ChunkSize - chunkOffset;
                if (partialCount > count) { partialCount = count; }
                Buffer.BlockCopy(buffer, offset, chunk, chunkOffset, partialCount);
                chunkIsDirty = true;

                position += partialCount;
                offset += partialCount;
                count -= partialCount;
                if (count > 0) {
                    SaveChunk();
                    chunkIndex += 1;
                    chunkOffset = 0;
                }
            }
            if (length < position) {
                length = position;
            }
        }

        public override void WriteByte(
            byte value
        ) {
            if (disposed) { throw new ObjectDisposedException("MongoGridFSStream"); }
            var chunkIndex = (int) (position / fileInfo.ChunkSize);
            var chunkOffset = (int) (position % fileInfo.ChunkSize);
            if (this.chunkIndex != chunkIndex) {
                if (chunkIsDirty) { SaveChunk(); }
                LoadChunk(chunkIndex);
            }
            chunk[chunkOffset] = value;
            chunkIsDirty = true;
            position += 1;
            if (length < position) {
                length = position;
            }
        }
        #endregion

        #region protected methods
        protected override void Dispose(
            bool disposing
        ) {
            try {
                if (!disposed) {
                    if (disposing) {
                        Close();
                    }
                    disposed = true;
                }
            } finally {
                base.Dispose(disposing);
            }
        }
        #endregion

        #region private methods
        private void AddMissingChunks() {
            var chunkCount = (int) ((length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize);
            var chunksFound = new HashSet<int>();
            var foundExtraChunks = false;
            foreach (var chunk in gridFS.Chunks.Find(Query.EQ("files_id", fileInfo.Id)).SetFields(Fields.Include("n"))) {
                var n = chunk["n"].AsInt32;
                chunksFound.Add(n);
                if (n >= chunkCount) {
                    foundExtraChunks = true;
                }
            }

            BsonBinaryData zeros = null;
            for (int n = 0; n < chunkCount; n++) {
                if (!chunksFound.Contains(n)) {
                    if (zeros == null) {
                        zeros = new byte[fileInfo.ChunkSize];
                    }
                    var chunk = new BsonDocument {
                        { "_id", ObjectId.GenerateNewId() },
                        { "files_id", fileInfo.Id },
                        { "n", n },
                        { "data", zeros }
                    };
                }
            }

            if (foundExtraChunks) {
                var query = Query.And(
                    Query.EQ("files_id", fileInfo.Id),
                    Query.GTE("n", chunkCount)
                );
                gridFS.Chunks.Remove(query);
            }
        }

        private void Append() {
            length = fileInfo.Length;
            position = fileInfo.Length;
            chunkIndex = -1;
        }

        private void Create() {
            var file = new BsonDocument {
                { "_id", fileInfo.Id },
                { "filename", fileInfo.Name },
                { "length", 0 },
                { "chunkSize", fileInfo.ChunkSize },
                { "uploadDate", fileInfo.UploadDate },
                { "contentType", fileInfo.ContentType, !string.IsNullOrEmpty(fileInfo.ContentType) },
                { "aliases", new BsonArray((IEnumerable<string>) fileInfo.Aliases), fileInfo.Aliases != null && fileInfo.Aliases.Length > 0 }
            };
            gridFS.Files.Insert(file);
            length = 0;
            position = 0;
            chunkIndex = -1;
        }

        private void EnsureChunksIndex() {
            var keys = IndexKeys.Ascending("files_id", "n");
            gridFS.Chunks.EnsureIndex(keys);
        }

        private void LoadChunk(
            int chunkIndex
        ) {
            if (chunkIsDirty) { SaveChunk(); }
            var query = Query.And(
                Query.EQ("files_id", fileInfo.Id),
                Query.EQ("n", chunkIndex)
            );
            var document = gridFS.Chunks.FindOne(query);
            if (document == null) {
                if (chunk == null) {
                    chunk = new byte[fileInfo.ChunkSize];
                } else {
                    Array.Clear(chunk, 0, chunk.Length);
                }
                chunkId = ObjectId.GenerateNewId();
            } else {
                var bytes = document["data"].AsBsonBinaryData.Bytes;
                if (bytes.Length == fileInfo.ChunkSize) {
                    chunk = bytes;
                } else {
                    if (chunk == null) {
                        chunk = new byte[fileInfo.ChunkSize];
                    }
                    Buffer.BlockCopy(bytes, 0, chunk, 0, bytes.Length);
                    Array.Clear(chunk, bytes.Length, chunk.Length - bytes.Length);
                }
                chunkId = document["_id"];
            }
            this.chunkIndex = chunkIndex;
        }

        private void LoadChunkNoData(
            int chunkIndex
        ) {
            if (chunkIsDirty) { SaveChunk(); }
            if (chunk == null) {
                chunk = new byte[fileInfo.ChunkSize];
            } else {
                Array.Clear(chunk, 0, chunk.Length);
            }

            var query = Query.And(
                Query.EQ("files_id", fileInfo.Id),
                Query.EQ("n", chunkIndex)
            );
            var fields = Fields.Include("_id");
            var document = gridFS.Chunks.Find(query).SetFields(fields).SetLimit(1).FirstOrDefault();
            if (document == null) {
                chunkId = ObjectId.GenerateNewId();
            } else {
                chunkId = document["_id"];
            }
            this.chunkIndex = chunkIndex;
        }

        private void Open() {
            length = fileInfo.Length;
            position = 0;
            chunkIndex = -1;
        }

        private void SaveChunk() {
            var chunkCount = (int) (length / fileInfo.ChunkSize);
            var lastChunkSize = (int) (length % fileInfo.ChunkSize);
            BsonBinaryData data;
            if (chunkIndex < chunkCount || lastChunkSize == 0) {
                data = new BsonBinaryData(chunk);
            } else {
                var lastChunk = new byte[lastChunkSize];
                Buffer.BlockCopy(chunk, 0, lastChunk, 0, lastChunkSize);
                data = new BsonBinaryData(lastChunk);
            }

            var query = Query.EQ("_id", chunkId);
            var update = new BsonDocument {
                { "_id", chunkId },
                { "files_id", fileInfo.Id },
                { "n", chunkIndex },
                { "data", data }
            };
            gridFS.Chunks.Update(query, update, UpdateFlags.Upsert);
            chunkIsDirty = false;
        }

        private void Truncate() {
            gridFS.Chunks.Remove(Query.EQ("files_id", fileInfo.Id));
            gridFS.Files.Update(Query.EQ("_id", fileInfo.Id), Update.Set("length", 0).Set("md5", BsonNull.Value));
            length = fileInfo.Length;
            position = 0;
            chunkIndex = -1;
        }

        private void UpdateFileInfo() {
            var md5Command = new BsonDocument {
                    { "filemd5", fileInfo.Id },
                    { "root", gridFS.Settings.Root }
                };
            var md5Result = gridFS.Database.RunCommand(md5Command);
            var md5 = md5Result["md5"].AsString;

            var query = Query.EQ("_id", fileInfo.Id);
            var update = Update
                .Set("length", length)
                .Set("md5", md5);
            gridFS.Files.Update(query, update);
        }
        #endregion
    }
}
