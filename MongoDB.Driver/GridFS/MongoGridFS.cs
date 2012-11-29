/* Copyright 2010-2012 10gen Inc.
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
using System.Security.Cryptography;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a GridFS file system.
    /// </summary>
    public class MongoGridFS
    {
        // private fields
        private MongoDatabase _database;
        private MongoGridFSSettings _settings;
        private MongoCollection<BsonDocument> _chunks;
        private MongoCollection<BsonDocument> _files;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFS class.
        /// </summary>
        /// <param name="database">The database containing the GridFS collections.</param>
        public MongoGridFS(MongoDatabase database)
            : this(database, new MongoGridFSSettings(database))
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoGridFS class.
        /// </summary>
        /// <param name="database">The database containing the GridFS collections.</param>
        /// <param name="settings">The GridFS settings.</param>
        public MongoGridFS(MongoDatabase database, MongoGridFSSettings settings)
        {
            _database = database;
            _settings = settings.FrozenCopy();
            _chunks = database.GetCollection(settings.ChunksCollectionName);
            _files = database.GetCollection(settings.FilesCollectionName);
        }

        // public properties
        /// <summary>
        /// Gets the chunks collection.
        /// </summary>
        public MongoCollection<BsonDocument> Chunks
        {
            get { return _chunks; }
        }

        /// <summary>
        /// Gets the database containing the GridFS collections.
        /// </summary>
        public MongoDatabase Database
        {
            get { return _database; }
        }

        /// <summary>
        /// Gets the files collection.
        /// </summary>
        public MongoCollection<BsonDocument> Files
        {
            get { return _files; }
        }

        /// <summary>
        /// Gets the GridFS settings.
        /// </summary>
        public MongoGridFSSettings Settings
        {
            get { return _settings; }
        }

        // public methods
        /// <summary>
        /// Appends UTF-8 encoded text to an existing GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A StreamWriter.</returns>
        public StreamWriter AppendText(string remoteFileName)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.AppendText();
        }

        /// <summary>
        /// Copies a GridFS file.
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="destFileName">The destination file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo CopyTo(string sourceFileName, string destFileName)
        {
            var fileInfo = FindOne(sourceFileName);
            if (fileInfo == null)
            {
                var message = string.Format("GridFS file '{0}' not found.", sourceFileName);
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
            MongoGridFSCreateOptions createOptions)
        {
            var fileInfo = FindOne(sourceFileName);
            if (fileInfo == null)
            {
                var message = string.Format("GridFS file '{0}' not found.", sourceFileName);
                throw new FileNotFoundException(message);
            }
            return fileInfo.CopyTo(destFileName, createOptions);
        }

        /// <summary>
        /// Creates or overwrites a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Create(string remoteFileName)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.Create();
        }

        /// <summary>
        /// Creates or overwrites a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Create(string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.Create();
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream writer.</returns>
        public StreamWriter CreateText(string remoteFileName)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.CreateText();
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream writer.</returns>
        public StreamWriter CreateText(string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.CreateText();
        }

        /// <summary>
        /// Deletes GridFS files.
        /// </summary>
        /// <param name="query">A query that specifies the GridFS files to delete.</param>
        public void Delete(IMongoQuery query)
        {
            foreach (var fileInfo in Find(query))
            {
                fileInfo.Delete();
            }
        }

        /// <summary>
        /// Deletes all versions of a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        public void Delete(string remoteFileName)
        {
            EnsureIndexes();
            Delete(Query.EQ("filename", remoteFileName));
        }

        /// <summary>
        /// Deletes a GridFS file.
        /// </summary>
        /// <param name="id">The GridFS file Id.</param>
        public void DeleteById(BsonValue id)
        {
            Delete(Query.EQ("_id", id));
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="query">The GridFS file.</param>
        public void Download(Stream stream, IMongoQuery query)
        {
            Download(stream, query, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="query">The GridFS file.</param>
        /// <param name="version">The version to download.</param>
        public void Download(Stream stream, IMongoQuery query, int version)
        {
            var fileInfo = FindOne(query, version);
            if (fileInfo == null)
            {
                var jsonQuery = query.ToJson();
                string errorMessage = string.Format("GridFS file '{0}' not found.", jsonQuery);
                throw new FileNotFoundException(errorMessage, jsonQuery);
            }
            Download(stream, fileInfo);
        }

        /// <summary>
        /// Downloads a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="fileInfo">The GridFS file.</param>
        public void Download(Stream stream, MongoGridFSFileInfo fileInfo)
        {
            if (_settings.VerifyMD5 && fileInfo.MD5 == null)
            {
                throw new MongoGridFSException("VerifyMD5 is true and file being downloaded has no MD5 hash.");
            }

            using (_database.RequestStart(_database.Settings.ReadPreference))
            {
                string md5Client = null;
                using (var md5Algorithm = _settings.VerifyMD5 ? MD5.Create() : null)
                {
                    var numberOfChunks = (fileInfo.Length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize;
                    for (var n = 0L; n < numberOfChunks; n++)
                    {
                        var query = Query.And(Query.EQ("files_id", fileInfo.Id), Query.EQ("n", n));
                        var chunk = _chunks.FindOne(query);
                        if (chunk == null)
                        {
                            string errorMessage = string.Format("Chunk {0} missing for GridFS file '{1}'.", n, fileInfo.Name);
                            throw new MongoGridFSException(errorMessage);
                        }
                        var data = chunk["data"].AsBsonBinaryData;
                        if (data.Bytes.Length != fileInfo.ChunkSize)
                        {
                            // the last chunk only has as many bytes as needed to complete the file
                            if (n < numberOfChunks - 1 || data.Bytes.Length != fileInfo.Length % fileInfo.ChunkSize)
                            {
                                string errorMessage = string.Format("Chunk {0} for GridFS file '{1}' is the wrong size.", n, fileInfo.Name);
                                throw new MongoGridFSException(errorMessage);
                            }
                        }
                        stream.Write(data.Bytes, 0, data.Bytes.Length);
                        if (_settings.VerifyMD5)
                        {
                            md5Algorithm.TransformBlock(data.Bytes, 0, data.Bytes.Length, null, 0);
                        }
                    }

                    if (_settings.VerifyMD5)
                    {
                        md5Algorithm.TransformFinalBlock(new byte[0], 0, 0);
                        md5Client = BsonUtils.ToHexString(md5Algorithm.Hash);
                    }
                }

                if (_settings.VerifyMD5 && !md5Client.Equals(fileInfo.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MongoGridFSException("Download client and server MD5 hashes are not equal.");
                }
            }
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        public void Download(Stream stream, string remoteFileName)
        {
            Download(stream, remoteFileName, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="version">The version to download.</param>
        public void Download(Stream stream, string remoteFileName, int version)
        {
            Download(stream, Query.EQ("filename", remoteFileName), version);
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="fileName">The file name (same local and remote names).</param>
        public void Download(string fileName)
        {
            Download(fileName, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="fileName">The file name (same local and remote names).</param>
        /// <param name="version">The version to download.</param>
        public void Download(string fileName, int version)
        {
            Download(fileName, fileName, version); // same local and remote file names
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="query">The GridFS file.</param>
        public void Download(string localFileName, IMongoQuery query)
        {
            Download(localFileName, query, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="query">The GridFS file.</param>
        /// <param name="version">The version to download.</param>
        public void Download(string localFileName, IMongoQuery query, int version)
        {
            using (Stream stream = File.Create(localFileName))
            {
                Download(stream, query, version);
            }
        }

        /// <summary>
        /// Downloads a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="fileInfo">The GridFS file.</param>
        public void Download(string localFileName, MongoGridFSFileInfo fileInfo)
        {
            using (Stream stream = File.Create(localFileName))
            {
                Download(stream, fileInfo);
            }
        }

        /// <summary>
        /// Downloads the most recent version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        public void Download(string localFileName, string remoteFileName)
        {
            Download(localFileName, remoteFileName, -1); // most recent version
        }

        /// <summary>
        /// Downloads a specific version of a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="version">The version to download.</param>
        public void Download(string localFileName, string remoteFileName, int version)
        {
            using (Stream stream = File.Create(localFileName))
            {
                Download(stream, remoteFileName, version);
            }
        }

        /// <summary>
        /// Ensures that the proper indexes for GridFS exist (only creates the new indexes if there are fewer than 1000 GridFS files).
        /// </summary>
        public void EnsureIndexes()
        {
            EnsureIndexes(1000);
        }

        /// <summary>
        /// Ensures that the proper indexes for GridFS exist.
        /// </summary>
        /// <param name="maxFiles">Only create new indexes if there are fewer than this number of GridFS files).</param>
        public void EnsureIndexes(int maxFiles)
        {
            // EnsureIndexes should only be called for update operations
            // read-only operations shouldn't call EnsureIndexes because:
            // 1. we might be reading from a secondary
            // 2. we might be authenticating as a read-only uaser

            // avoid round trip to count files if possible
            var indexCache = _database.Server.IndexCache;
            if (indexCache.Contains(_files, "filename_1_uploadDate_1") &&
                indexCache.Contains(_chunks, "files_id_1_n_1"))
            {
                return;
            }

            // only create indexes if number of GridFS files is still small (to avoid performance surprises)
            var count = _files.Count();
            if (count < maxFiles)
            {
                _files.EnsureIndex("filename", "uploadDate");
                _chunks.EnsureIndex(IndexKeys.Ascending("files_id", "n"), IndexOptions.SetUnique(true));
            }
            else
            {
                // at least check to see if the indexes exist so we can stop calling files.Count()
                if (_files.IndexExistsByName("filename_1_uploadDate_1"))
                {
                    indexCache.Add(_files, "filename_1_uploadDate_1");
                }
                if (_chunks.IndexExistsByName("files_id_1_n_1"))
                {
                    indexCache.Add(_chunks, "files_id_1_n_1");
                }
            }
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool Exists(IMongoQuery query)
        {
            return _files.Count(query) > 0;
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="remoteFileName">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool Exists(string remoteFileName)
        {
            return Exists(Query.EQ("filename", remoteFileName));
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="id">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool ExistsById(BsonValue id)
        {
            return Exists(Query.EQ("_id", id));
        }

        /// <summary>
        /// Finds matching GridFS files.
        /// </summary>
        /// <param name="query">A query.</param>
        /// <returns>The matching GridFS files.</returns>
        public MongoCursor<MongoGridFSFileInfo> Find(IMongoQuery query)
        {
            var serializationOptions = new MongoGridFSFileInfo.SerializationOptions { GridFS = this };
            return _files.FindAs<MongoGridFSFileInfo>(query).SetSerializationOptions(serializationOptions);
        }

        /// <summary>
        /// Finds matching GridFS files.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The matching GridFS files.</returns>
        public MongoCursor<MongoGridFSFileInfo> Find(string remoteFileName)
        {
            return Find(Query.EQ("filename", remoteFileName));
        }

        /// <summary>
        /// Finds all GridFS files.
        /// </summary>
        /// <returns>The matching GridFS files.</returns>
        public MongoCursor<MongoGridFSFileInfo> FindAll()
        {
            return Find(Query.Null);
        }

        /// <summary>
        /// Finds the most recent version of a GridFS file.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(IMongoQuery query)
        {
            return FindOne(query, -1); // most recent version
        }

        /// <summary>
        /// Finds a specific version of a GridFS file.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <param name="version">The version to find (1 is oldest, -1 is newest, 0 is no sort).</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(IMongoQuery query, int version)
        {
            BsonDocument fileInfo;
            if (version > 0)
            {
                fileInfo = _files.Find(query).SetSortOrder(SortBy.Ascending("uploadDate")).SetSkip(version - 1).SetLimit(1).FirstOrDefault();
            }
            else if (version < 0)
            {
                fileInfo = _files.Find(query).SetSortOrder(SortBy.Descending("uploadDate")).SetSkip(-version - 1).SetLimit(1).FirstOrDefault();
            }
            else
            {
                fileInfo = _files.FindOne(query);
            }

            if (fileInfo != null)
            {
                return new MongoGridFSFileInfo(this, fileInfo);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the most recent version of a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(string remoteFileName)
        {
            return FindOne(remoteFileName, -1); // most recent version
        }

        /// <summary>
        /// Finds a specific version of a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="version">The version to find.</param>
        /// <returns>The matching GridFS file.</returns>
        public MongoGridFSFileInfo FindOne(string remoteFileName, int version)
        {
            return FindOne(Query.EQ("filename", remoteFileName), version);
        }

        /// <summary>
        /// Finds a GridFS file.
        /// </summary>
        /// <param name="id">The GridFS file Id.</param>
        /// <returns>The GridFS file.</returns>
        public MongoGridFSFileInfo FindOneById(BsonValue id)
        {
            return FindOne(Query.EQ("_id", id));
        }

        /// <summary>
        /// Moves the most recent version of a GridFS file.
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="destFileName">The destination file name.</param>
        public void MoveTo(string sourceFileName, string destFileName)
        {
            var fileInfo = FindOne(sourceFileName);
            if (fileInfo == null)
            {
                var message = string.Format("GridFS file '{0}' not found.", sourceFileName);
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
        public MongoGridFSStream Open(string remoteFileName, FileMode mode)
        {
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
        public MongoGridFSStream Open(string remoteFileName, FileMode mode, FileAccess access)
        {
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
            MongoGridFSCreateOptions createOptions)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.Open(mode, access);
        }

        /// <summary>
        /// Opens an existing GridFS file for reading.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenRead(string remoteFileName)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenRead();
        }

        /// <summary>
        /// Opens an existing UTF-8 encoded text GridFS file for reading.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream reader.</returns>
        public StreamReader OpenText(string remoteFileName)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenText();
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite(string remoteFileName)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName);
            return fileInfo.OpenWrite();
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite(string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            var fileInfo = new MongoGridFSFileInfo(this, remoteFileName, createOptions);
            return fileInfo.OpenWrite();
        }

        /// <summary>
        /// Sets the aliases for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="aliases">The aliases.</param>
        public void SetAliases(MongoGridFSFileInfo fileInfo, string[] aliases)
        {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (aliases == null) ? Update.Unset("aliases") : Update.Set("aliases", BsonArray.Create(aliases));
            _files.Update(query, update, _settings.WriteConcern);
        }

        /// <summary>
        /// Sets the content type for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="contentType">The content type.</param>
        public void SetContentType(MongoGridFSFileInfo fileInfo, string contentType)
        {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (contentType == null) ? Update.Unset("contentType") : Update.Set("contentType", contentType);
            _files.Update(query, update, _settings.WriteConcern);
        }

        /// <summary>
        /// Sets the metadata for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="metadata">The metadata.</param>
        public void SetMetadata(MongoGridFSFileInfo fileInfo, BsonValue metadata)
        {
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (metadata == null) ? Update.Unset("metadata") : Update.Set("metadata", metadata);
            _files.Update(query, update, _settings.WriteConcern);
        }

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo Upload(Stream stream, string remoteFileName)
        {
            var options = new MongoGridFSCreateOptions
            {
                ChunkSize = _settings.ChunkSize,
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
            MongoGridFSCreateOptions createOptions)
        {
            using (_database.RequestStart(ReadPreference.Primary))
            {
                EnsureIndexes();

                var files_id = createOptions.Id ?? BsonObjectId.GenerateNewId();
                var chunkSize = createOptions.ChunkSize == 0 ? _settings.ChunkSize : createOptions.ChunkSize;
                var buffer = new byte[chunkSize];

                var length = 0L;
                string md5Client = null;
                using (var md5Algorithm = _settings.VerifyMD5 ? MD5.Create() : null)
                {
                    for (var n = 0L; true; n++)
                    {
                        // might have to call Stream.Read several times to get a whole chunk
                        var bytesNeeded = chunkSize;
                        var bytesRead = 0;
                        while (bytesNeeded > 0)
                        {
                            var partialRead = stream.Read(buffer, bytesRead, bytesNeeded);
                            if (partialRead == 0)
                            {
                                break; // EOF may or may not have a partial chunk
                            }
                            bytesNeeded -= partialRead;
                            bytesRead += partialRead;
                        }
                        if (bytesRead == 0)
                        {
                            break; // EOF no partial chunk
                        }
                        length += bytesRead;

                        byte[] data = buffer;
                        if (bytesRead < chunkSize)
                        {
                            data = new byte[bytesRead];
                            Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
                        }

                        var chunk = new BsonDocument
                        {
                            { "_id", BsonObjectId.GenerateNewId() },
                            { "files_id", files_id },
                            { "n", (n < int.MaxValue) ? (BsonValue)BsonInt32.Create((int)n) : BsonInt64.Create(n) },
                            { "data", new BsonBinaryData(data) }
                        };
                        _chunks.Insert(chunk, _settings.WriteConcern);

                        if (_settings.VerifyMD5)
                        {
                            md5Algorithm.TransformBlock(data, 0, data.Length, null, 0);
                        }

                        if (bytesRead < chunkSize)
                        {
                            break; // EOF after partial chunk
                        }
                    }

                    if (_settings.VerifyMD5)
                    {
                        md5Algorithm.TransformFinalBlock(new byte[0], 0, 0);
                        md5Client = BsonUtils.ToHexString(md5Algorithm.Hash);
                    }
                }

                string md5Server = null;
                if (_settings.UpdateMD5 || _settings.VerifyMD5)
                {
                    var md5Command = new CommandDocument
                    {
                        { "filemd5", files_id },
                        { "root", _settings.Root }
                    };
                    var md5Result = _database.RunCommand(md5Command);
                    md5Server = md5Result.Response["md5"].AsString;
                }

                if ( _settings.VerifyMD5 && !md5Client.Equals(md5Server, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MongoGridFSException("Upload client and server MD5 hashes are not equal.");
                }

                var uploadDate = createOptions.UploadDate == DateTime.MinValue ? DateTime.UtcNow : createOptions.UploadDate;
                BsonDocument fileInfo = new BsonDocument
                {
                    { "_id", files_id },
                    { "filename", remoteFileName },
                    { "length", length },
                    { "chunkSize", chunkSize },
                    { "uploadDate", uploadDate },
                    { "md5", (md5Server == null) ? (BsonValue)BsonNull.Value : new BsonString(md5Server) },
                    { "contentType", createOptions.ContentType }, // optional
                    { "aliases", BsonArray.Create(createOptions.Aliases) }, // optional
                    { "metadata", createOptions.Metadata } // optional
                };
                _files.Insert(fileInfo, _settings.WriteConcern);

                return FindOneById(files_id);
            }
        }

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="fileName">The file name (same local and remote names).</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo Upload(string fileName)
        {
            return Upload(fileName, fileName);
        }

        /// <summary>
        /// Uploads a GridFS file.
        /// </summary>
        /// <param name="localFileName">The local file name.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo Upload(string localFileName, string remoteFileName)
        {
            using (Stream stream = File.OpenRead(localFileName))
            {
                return Upload(stream, remoteFileName);
            }
        }
    }
}
