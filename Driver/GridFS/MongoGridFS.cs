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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS {
    public class MongoGridFS {
        #region private static fields
        private static MongoGridFSSettings defaultSettings = new MongoGridFSSettings();
        #endregion

        #region private fields
        private MongoDatabase database;
        private MongoGridFSSettings settings;
        private MongoCollection<BsonDocument> chunks;
        private MongoCollection<BsonDocument> files;
        #endregion

        #region constructors
        public MongoGridFS(
            MongoDatabase database
        )
            : this(database, defaultSettings) {
        }

        public MongoGridFS(
            MongoDatabase database,
            MongoGridFSSettings settings
        ) {
            this.database = database;
            this.settings = settings.Freeze();
            this.chunks = database[settings.ChunksCollectionName, settings.SafeMode];
            this.files = database[settings.FilesCollectionName, settings.SafeMode];
        }
        #endregion

        #region public static properties
        public static MongoGridFSSettings DefaultSettings {
            get { return defaultSettings; }
            set { defaultSettings = value.Freeze(); }
        }
        #endregion

        #region public properties
        public MongoCollection<BsonDocument> Chunks {
            get { return chunks; }
        }

        public MongoDatabase Database {
            get { return database; }
        }

        public MongoCollection<BsonDocument> Files {
            get { return files; }
        }

        public MongoGridFSSettings Settings {
            get { return settings; }
        }
        #endregion

        #region public methods
        public StreamWriter AppendText(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.AppendText();
        }

        public MongoGridFSFileInfo CopyTo(
            string sourceFileName,
            string destFileName
        ) {
            var fileInfo = FindOne(sourceFileName);
            if (fileInfo == null) {
                var message = string.Format("GridFS file not found: {0}", sourceFileName);
                throw new FileNotFoundException(message);
            }
            return fileInfo.CopyTo(destFileName);
        }

        public MongoGridFSFileInfo CopyTo(
            string sourceFileName,
            string destFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = FindOne(sourceFileName);
            if (fileInfo == null) {
                var message = string.Format("GridFS file not found: {0}", sourceFileName);
                throw new FileNotFoundException(message);
            }
            return fileInfo.CopyTo(destFileName, createOptions);
        }

        public MongoGridFSStream Create(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Create();
        }

        public MongoGridFSStream Create(
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.Create();
        }

        public StreamWriter CreateText(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.CreateText();
        }

        public StreamWriter CreateText(
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.CreateText();
        }

        public void Delete(
            IMongoQuery query
        ) {
            foreach (var fileInfo in Find(query)) {
                fileInfo.Delete();
            }
        }

        public void Delete(
            string remoteFileName
        ) {
            files.EnsureIndex("filename", "uploadDate");
            Delete(Query.EQ("filename", remoteFileName));
        }

        public void DeleteById(
            BsonValue id
        ) {
            Delete(Query.EQ("_id", id));
        }

        public void Download(
            Stream stream,
            IMongoQuery query
        ) {
            Download(stream, query, -1); // most recent version
        }

        public void Download(
            Stream stream,
            IMongoQuery query,
            int version
        ) {
            var fileInfo = FindOne(query, version);
            if (fileInfo == null) {
                var jsonQuery = query.ToJson();
                string errorMessage = string.Format("GridFS file not found: {0}", jsonQuery);
                throw new FileNotFoundException(errorMessage, jsonQuery);
            }
            Download(stream, fileInfo);
        }

        public void Download(
            Stream stream,
            MongoGridFSFileInfo fileInfo
        ) {
            using (database.RequestStart()) {
                chunks.EnsureIndex("files_id", "n");

                var numberOfChunks = (fileInfo.Length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize;
                for (int n = 0; n < numberOfChunks; n++) {
                    var query = Query.And(
                        Query.EQ("files_id", fileInfo.Id),
                        Query.EQ("n", n)
                    );
                    var chunk = chunks.FindOne(query);
                    if (chunk == null) {
                        string errorMessage = string.Format("Chunk {0} missing for: {1}", n, fileInfo.Name);
                        throw new MongoGridFSException(errorMessage);
                    }
                    var data = chunk["data"].AsBsonBinaryData;
                    if (data.Bytes.Length != fileInfo.ChunkSize) {
                        // the last chunk only has as many bytes as needed to complete the file
                        if (n < numberOfChunks - 1 || data.Bytes.Length != fileInfo.Length % fileInfo.ChunkSize) {
                            string errorMessage = string.Format("Chunk {0} for {1} is the wrong size", n, fileInfo.Name);
                            throw new MongoGridFSException(errorMessage);
                        }
                    }
                    stream.Write(data.Bytes, 0, data.Bytes.Length);
                }
            }
        }

        public void Download(
            Stream stream,
            string remoteFileName
        ) {
            Download(stream, remoteFileName, -1); // most recent version
        }

        public void Download(
            Stream stream,
            string remoteFileName,
            int version
        ) {
            files.EnsureIndex("filename", "uploadDate");
            Download(stream, Query.EQ("filename", remoteFileName), version);
        }

        public void Download(
            string fileName
        ) {
            Download(fileName, -1); // most recent version
        }

        public void Download(
            string fileName,
            int version
        ) {
            Download(fileName, fileName, version); // same local and remote file names
        }

        public void Download(
            string localFileName,
            IMongoQuery query
        ) {
            Download(localFileName, query, -1); // most recent version
        }

        public void Download(
            string localFileName,
            IMongoQuery query,
            int version
        ) {
            using (Stream stream = File.Create(localFileName)) {
                Download(stream, query, version);
            }
        }

        public void Download(
            string localFileName,
            MongoGridFSFileInfo fileInfo
        ) {
            using (Stream stream = File.Create(localFileName)) {
                Download(stream, fileInfo);
            }
        }

        public void Download(
            string localFileName,
            string remoteFileName
        ) {
            Download(localFileName, remoteFileName, -1); // most recent version
        }

        public void Download(
            string localFileName,
            string remoteFileName,
            int version
        ) {
            using (Stream stream = File.Create(localFileName)) {
                Download(stream, remoteFileName, version);
            }
        }

        public bool Exists(
            IMongoQuery query
        ) {
            return files.Count(query) > 0;
        }

        public bool Exists(
            string fileName
        ) {
            files.EnsureIndex("filename", "uploadDate");
            return Exists(Query.EQ("filename", fileName));
        }

        public bool ExistsById(
            BsonValue id
        ) {
            return Exists(Query.EQ("_id", id));
        }

        public IEnumerable<MongoGridFSFileInfo> Find(
            IMongoQuery query
        ) {
            return files.Find(query).Select(fileInfo => new MongoGridFSFileInfo(this, fileInfo));
        }

        public IEnumerable<MongoGridFSFileInfo> Find(
            string fileName
        ) {
            files.EnsureIndex("filename", "uploadDate");
            return Find(Query.EQ("filename", fileName));
        }

        public IEnumerable<MongoGridFSFileInfo> FindAll() {
            return Find(Query.Null);
        }

        public MongoGridFSFileInfo FindOne(
            IMongoQuery query
        ) {
            return FindOne(query, -1); // most recent version
        }

        public MongoGridFSFileInfo FindOne(
            IMongoQuery query,
            int version // 1 is oldest, -1 is newest, 0 is no sort
        ) {
            BsonDocument fileInfo;
            if (version > 0) {
                fileInfo = files.Find(query).SetSortOrder(SortBy.Ascending("uploadDate")).SetSkip(version - 1).SetLimit(1).FirstOrDefault();
            } else if (version < 0) {
                fileInfo = files.Find(query).SetSortOrder(SortBy.Descending("uploadDate")).SetSkip(-version - 1).SetLimit(1).FirstOrDefault();
            } else {
                fileInfo = files.FindOne(query);
            }

            if (fileInfo != null) {
                return new MongoGridFSFileInfo(this, fileInfo);
            } else {
                return null;
            }
        }

        public MongoGridFSFileInfo FindOne(
            string remoteFileName
        ) {
            return FindOne(remoteFileName, -1); // most recent version
        }

        public MongoGridFSFileInfo FindOne(
            string remoteFileName,
            int version
        ) {
            files.EnsureIndex("filename", "uploadDate");
            return FindOne(Query.EQ("filename", remoteFileName), version);
        }

        public MongoGridFSFileInfo FindOneById(
            BsonValue id
        ) {
            return FindOne(Query.EQ("_id", id));
        }

        public void MoveTo(
            string sourceFileName,
            string destFileName
        ) {
            var fileInfo = FindOne(sourceFileName);
            if (fileInfo == null) {
                var message = string.Format("GridFS file not found: {0}", sourceFileName);
                throw new FileNotFoundException(message);
            }
            fileInfo.MoveTo(destFileName);
        }

        public MongoGridFSStream Open(
            string remoteFileName,
            FileMode mode
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Open(mode);
        }

        public MongoGridFSStream Open(
            string remoteFileName,
            FileMode mode,
            FileAccess access
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Open(mode, access);
        }

        public MongoGridFSStream Open(
            string remoteFileName,
            FileMode mode,
            FileAccess access,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.Open(mode, access);
        }

        public MongoGridFSStream OpenRead(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenRead();
        }

        public StreamReader OpenText(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenText();
        }

        public MongoGridFSStream OpenWrite(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenWrite();
        }

        public MongoGridFSStream OpenWrite(
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.OpenWrite();
        }

        public void SetAliases(
            MongoGridFSFileInfo fileInfo,
            string[] aliases
        ) {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (aliases == null) ? Update.Unset("aliases") : Update.Set("aliases", BsonArray.Create((IEnumerable<string>) aliases));
            files.Update(query, update);
        }

        public void SetContentType(
            MongoGridFSFileInfo fileInfo,
            string contentType
        ) {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (contentType == null) ? Update.Unset("contentType") : Update.Set("contentType", contentType);
            files.Update(query, update);
        }

        public void SetMetadata(
            MongoGridFSFileInfo fileInfo,
            BsonValue metadata
        ) {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (metadata == null) ? Update.Unset("metadata") : Update.Set("metadata", metadata);
            files.Update(query, update);
        }

        public MongoGridFSFileInfo Upload(
            Stream stream,
            string remoteFileName
        ) {
            var options = new MongoGridFSCreateOptions {
                ChunkSize = settings.DefaultChunkSize,
                Id = BsonObjectId.GenerateNewId(),
                UploadDate = DateTime.UtcNow
            };
            return Upload(stream, remoteFileName, options);
        }

        public MongoGridFSFileInfo Upload(
            Stream stream,
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            using (database.RequestStart()) {
                chunks.EnsureIndex("files_id", "n");

                var files_id = createOptions.Id ?? BsonObjectId.GenerateNewId();
                var chunkSize = createOptions.ChunkSize == 0 ? settings.DefaultChunkSize : createOptions.ChunkSize;
                var buffer = new byte[chunkSize];

                var length = 0;
                for (int n = 0; true; n++) {
                    int bytesRead = stream.Read(buffer, 0, chunkSize);
                    if (bytesRead == 0) {
                        break;
                    }
                    length += bytesRead;

                    byte[] data = buffer;
                    if (bytesRead < chunkSize) {
                        data = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
                    }

                    var chunk = new BsonDocument {
                        { "_id", BsonObjectId.GenerateNewId() },
                        { "files_id", files_id },
                        { "n", n },
                        { "data", new BsonBinaryData(data) }
                    };
                    chunks.Insert(chunk, settings.SafeMode);

                    if (bytesRead < chunkSize) {
                        break;
                    }
                }

                var md5Command = new CommandDocument {
                    { "filemd5", files_id },
                    { "root", settings.Root }
                };
                var md5Result = database.RunCommand(md5Command);
                var md5 = md5Result.Response["md5"].AsString;

                var uploadDate = createOptions.UploadDate == DateTime.MinValue ? DateTime.UtcNow : createOptions.UploadDate;
                BsonDocument fileInfo = new BsonDocument {
                    { "_id", files_id },
                    { "filename", remoteFileName },
                    { "length", length },
                    { "chunkSize", chunkSize },
                    { "uploadDate", uploadDate },
                    { "md5", md5 },
                    { "contentType", createOptions.ContentType }, // optional
                    { "aliases", BsonArray.Create((IEnumerable<string>) createOptions.Aliases) }, // optional
                    { "metadata", createOptions.Metadata } // optional
                };
                files.Insert(fileInfo, settings.SafeMode);

                return FindOneById(files_id);
            }
        }

        public MongoGridFSFileInfo Upload(
            string fileName
        ) {
            return Upload(fileName, fileName);
        }

        public MongoGridFSFileInfo Upload(
            string localFileName,
            string remoteFileName
        ) {
            using (Stream stream = File.OpenRead(localFileName)) {
                return Upload(stream, remoteFileName);
            }
        }
        #endregion
    }
}
