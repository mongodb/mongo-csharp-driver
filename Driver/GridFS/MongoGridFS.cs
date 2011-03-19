﻿/* Copyright 2010-2011 10gen Inc.
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
    /// <summary>
    /// Represents a GridFS file system.
    /// </summary>
    public class MongoGridFS {
        #region private fields
        private MongoDatabase database;
        private MongoGridFSSettings settings;
        private MongoCollection<BsonDocument> chunks;
        private MongoCollection<BsonDocument> files;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFS class.
        /// </summary>
        /// <param name="database">The database containing the GridFS collections.</param>
        public MongoGridFS(
            MongoDatabase database
        )
            : this(database, MongoGridFSSettings.Defaults) {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFS class.
        /// </summary>
        /// <param name="database">The database containing the GridFS collections.</param>
        /// <param name="settings">The GridFS settings.</param>
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

        #region public properties
        /// <summary>
        /// Gets the chunks collection.
        /// </summary>
        public MongoCollection<BsonDocument> Chunks {
            get { return chunks; }
        }

        /// <summary>
        /// Gets the database containing the GridFS collections.
        /// </summary>
        public MongoDatabase Database {
            get { return database; }
        }

        /// <summary>
        /// Gets the files collection.
        /// </summary>
        public MongoCollection<BsonDocument> Files {
            get { return files; }
        }

        /// <summary>
        /// Gets the GridFS settings.
        /// </summary>
        public MongoGridFSSettings Settings {
            get { return settings; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Appends UTF-8 encoded text to an existing GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A StreamWriter.</returns>
        public StreamWriter AppendText(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.AppendText();
        }

        /// <summary>
        /// Copies a GridFS file.
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="destFileName">The destination file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
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

        /// <summary>
        /// Copies a GridFS file.
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="destFileName">The destination file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>The file info of the new GridFS file.</returns>
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

        /// <summary>
        /// Creates or overwrites a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Create(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Create();
        }

        /// <summary>
        /// Creates or overwrites a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Create(
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.Create();
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream writer.</returns>
        public StreamWriter CreateText(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.CreateText();
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream writer.</returns>
        public StreamWriter CreateText(
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.CreateText();
        }

        /// <summary>
        /// Deletes GridFS files.
        /// </summary>
        /// <param name="query">A query that specifies the GridFS files to delete.</param>
        public void Delete(
            IMongoQuery query
        ) {
            foreach (var fileInfo in Find(query)) {
                fileInfo.Delete();
            }
        }

        /// <summary>
        /// Deletes all versions of a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        public void Delete(
            string remoteFileName
        ) {
            EnsureIndexes();
            Delete(Query.EQ("filename", remoteFileName));
        }

        /// <summary>
        /// Deletes a GridFS file.
        /// </summary>
        /// <param name="id">The GridFS file Id.</param>
        public void DeleteById(
            BsonValue id
        ) {
            Delete(Query.EQ("_id", id));
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="query">The GridFS file.</param>
        public void Download(
            Stream stream,
            IMongoQuery query
        ) {
            Download(stream, query, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="query">The GridFS file.</param>
        /// <param name="version">The version to download.</param>
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

        /// <summary>
        /// Downloads a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="fileInfo">The GridFS file.</param>
        public void Download(
            Stream stream,
            MongoGridFSFileInfo fileInfo
        ) {
            using (database.RequestStart()) {
                EnsureIndexes();

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

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        public void Download(
            Stream stream,
            string remoteFileName
        ) {
            Download(stream, remoteFileName, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="version">The version to download.</param>
        public void Download(
            Stream stream,
            string remoteFileName,
            int version
        ) {
            EnsureIndexes();
            Download(stream, Query.EQ("filename", remoteFileName), version);
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="fileName">The file name (same local and remote names).</param>
        public void Download(
            string fileName
        ) {
            Download(fileName, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="fileName">The file name (same local and remote names).</param>
        /// <param name="version">The version to download.</param>
        public void Download(
            string fileName,
            int version
        ) {
            Download(fileName, fileName, version); // same local and remote file names
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="query">The GridFS file.</param>
        public void Download(
            string localFileName,
            IMongoQuery query
        ) {
            Download(localFileName, query, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="query">The GridFS file.</param>
        /// <param name="version">The version to download.</param>
        public void Download(
            string localFileName,
            IMongoQuery query,
            int version
        ) {
            using (Stream stream = File.Create(localFileName)) {
                Download(stream, query, version);
            }
        }

        /// <summary>
        /// Downloads a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="fileInfo">The GridFS file.</param>
        public void Download(
            string localFileName,
            MongoGridFSFileInfo fileInfo
        ) {
            using (Stream stream = File.Create(localFileName)) {
                Download(stream, fileInfo);
            }
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        public void Download(
            string localFileName,
            string remoteFileName
        ) {
            Download(localFileName, remoteFileName, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="version">The version to download.</param>
        public void Download(
            string localFileName,
            string remoteFileName,
            int version
        ) {
            using (Stream stream = File.Create(localFileName)) {
                Download(stream, remoteFileName, version);
            }
        }

        /// <summary>
        /// Ensures that the proper indexes for GridFS exist (only creates the new indexes if there are fewer than 1000 GridFS files).
        /// </summary>
        public void EnsureIndexes() {
            EnsureIndexes(1000);
        }

        /// <summary>
        /// Ensures that the proper indexes for GridFS exist.
        /// </summary>
        /// <param name="maxFiles">Only create new indexes if there are fewer than this number of GridFS files).</param>
        public void EnsureIndexes(
            int maxFiles
        ) {
            // don't try to create indexes on secondaries
            if (files.Settings.SlaveOk) {
                return;
            }

            // avoid round trip to count files if possible
            var indexCache = database.Server.IndexCache;
            if (
                indexCache.Contains(files, "filename_1_uploadDate_1") &&
                indexCache.Contains(chunks, "files_id_1_n_1")
            ) {
                return;
            }

            // only create indexes if number of GridFS files is still small (to avoid performance surprises)
            var count = files.Count();
            if (count < maxFiles) {
                files.EnsureIndex("filename", "uploadDate");
                chunks.EnsureIndex("files_id", "n");
            } else {
                // at least check to see if the indexes exist so we can stop calling files.Count()
                if (files.IndexExistsByName("filename_1_uploadDate_1")) {
                    indexCache.Add(files, "filename_1_uploadDate_1");
                }
                if (chunks.IndexExistsByName("files_id_1_n_")) {
                    indexCache.Add(chunks, "files_id_1_n_");
                }
            }
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool Exists(
            IMongoQuery query
        ) {
            return files.Count(query) > 0;
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="remoteFileName">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool Exists(
            string remoteFileName
        ) {
            EnsureIndexes();
            return Exists(Query.EQ("filename", remoteFileName));
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="id">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool ExistsById(
            BsonValue id
        ) {
            return Exists(Query.EQ("_id", id));
        }

        /// <summary>
        /// Finds matching GridFS files.
        /// </summary>
        /// <param name="query">A query.</param>
        /// <returns>The matching GridFS files.</returns>
        public IEnumerable<MongoGridFSFileInfo> Find(
            IMongoQuery query
        ) {
            return files.Find(query).Select(fileInfo => new MongoGridFSFileInfo(this, fileInfo));
        }

        /// <summary>
        /// Finds matching GridFS files.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The matching GridFS files.</returns>
        public IEnumerable<MongoGridFSFileInfo> Find(
            string remoteFileName
        ) {
            EnsureIndexes();
            return Find(Query.EQ("filename", remoteFileName));
        }

        /// <summary>
        /// Finds all GridFS files.
        /// </summary>
        /// <returns>The matching GridFS files.</returns>
        public IEnumerable<MongoGridFSFileInfo> FindAll() {
            return Find(Query.Null);
        }

        /// <summary>
        /// Finds the most recent version of a GridFS file.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(
            IMongoQuery query
        ) {
            return FindOne(query, -1); // most recent version
        }

        /// <summary>
        /// Finds a specific version of a GridFS file.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <param name="version">The version to find.</param>
        /// <returns>The matching GridFS file.</returns>
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

        /// <summary>
        /// Finds the most recent version of a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(
            string remoteFileName
        ) {
            return FindOne(remoteFileName, -1); // most recent version
        }

        /// <summary>
        /// Finds a specific version of a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="version">The version to find.</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(
            string remoteFileName,
            int version
        ) {
            EnsureIndexes();
            return FindOne(Query.EQ("filename", remoteFileName), version);
        }

        /// <summary>
        /// Finds a GridFS file.
        /// </summary>
        /// <param name="id">The GridFS file Id.</param>
        /// <returns>The GridFS file.</returns>
        public MongoGridFSFileInfo FindOneById(
            BsonValue id
        ) {
            return FindOne(Query.EQ("_id", id));
        }

        /// <summary>
        /// Moves the most recent version of a GridFS file.
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="destFileName">The destination file name.</param>
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

        /// <summary>
        /// Opens a GridFS file with the specified mode.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Open(
            string remoteFileName,
            FileMode mode
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Open(mode);
        }

        /// <summary>
        /// Opens a GridFS file with the specified mode and access.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Open(
            string remoteFileName,
            FileMode mode,
            FileAccess access
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Open(mode, access);
        }

        /// <summary>
        /// Opens a GridFS file with the specified mode, access and create options.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Open(
            string remoteFileName,
            FileMode mode,
            FileAccess access,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.Open(mode, access);
        }

        /// <summary>
        /// Opens an existing GridFS file for reading.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenRead(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenRead();
        }

        /// <summary>
        /// Opens an existing UTF-8 encoded text GridFS file for reading.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream reader.</returns>
        public StreamReader OpenText(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenText();
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite(
            string remoteFileName
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenWrite();
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite(
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.OpenWrite();
        }

        /// <summary>
        /// Sets the aliases for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="aliases">The aliases.</param>
        public void SetAliases(
            MongoGridFSFileInfo fileInfo,
            string[] aliases
        ) {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (aliases == null) ? Update.Unset("aliases") : Update.Set("aliases", BsonArray.Create((IEnumerable<string>) aliases));
            files.Update(query, update);
        }

        /// <summary>
        /// Sets the content type for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="contentType">The content type.</param>
        public void SetContentType(
            MongoGridFSFileInfo fileInfo,
            string contentType
        ) {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (contentType == null) ? Update.Unset("contentType") : Update.Set("contentType", contentType);
            files.Update(query, update);
        }

        /// <summary>
        /// Sets the metadata for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="metadata">The metadata.</param>
        public void SetMetadata(
            MongoGridFSFileInfo fileInfo,
            BsonValue metadata
        ) {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (metadata == null) ? Update.Unset("metadata") : Update.Set("metadata", metadata);
            files.Update(query, update);
        }

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo Upload(
            Stream stream,
            string remoteFileName
        ) {
            var options = new MongoGridFSCreateOptions {
                ChunkSize = settings.ChunkSize,
                Id = BsonObjectId.GenerateNewId(),
                UploadDate = DateTime.UtcNow
            };
            return Upload(stream, remoteFileName, options);
        }

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo Upload(
            Stream stream,
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            using (database.RequestStart()) {
                EnsureIndexes();

                var files_id = createOptions.Id ?? BsonObjectId.GenerateNewId();
                var chunkSize = createOptions.ChunkSize == 0 ? settings.ChunkSize : createOptions.ChunkSize;
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

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="fileName">The file name (same local and remote names).</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo Upload(
            string fileName
        ) {
            return Upload(fileName, fileName);
        }

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
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
