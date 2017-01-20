/* Copyright 2010-2016 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Shared;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a GridFS file system.
    /// </summary>
    public class MongoGridFS
    {
        // private fields
        private readonly string _databaseName;
        private readonly MongoServer _server;
        private readonly MongoGridFSSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFS class.
        /// </summary>
        /// <param name="database">The database containing the GridFS collections.</param>
        [Obsolete("Use a different constructor instead.")]
        public MongoGridFS(MongoDatabase database)
            : this(database, new MongoGridFSSettings())
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the MongoGridFS class.
        /// </summary>
        /// <param name="database">The database containing the GridFS collections.</param>
        /// <param name="settings">The GridFS settings.</param>
        [Obsolete("Use a different constructor instead.")]
        public MongoGridFS(MongoDatabase database, MongoGridFSSettings settings)
            : this(GetServer(database), GetDatabaseName(database), ApplyDefaultValues(settings, database))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoGridFS"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="settings">The settings.</param>
        public MongoGridFS(MongoServer server, string databaseName, MongoGridFSSettings settings)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings = settings.Clone();
            settings.ApplyDefaultValues(server.Settings);
            settings.Freeze();

            _server = server;
            _databaseName = databaseName;
            _settings = settings;
        }

        // public properties
        /// <summary>
        /// Gets the chunks collection.
        /// </summary>
        public MongoCollection<BsonDocument> Chunks
        {
            get
            {
                var database = GetDatabase();
                return GetChunksCollection(database);
            }
        }

        /// <summary>
        /// Gets the database containing the GridFS collections.
        /// </summary>
        public MongoDatabase Database
        {
            get { return GetDatabase(); }
        }

        /// <summary>
        /// Gets the database containing the GridFS collections.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        /// <summary>
        /// Gets the files collection.
        /// </summary>
        public MongoCollection<BsonDocument> Files
        {
            get
            {
                var database = GetDatabase();
                return GetFilesCollection(database);
            }
        }

        /// <summary>
        /// Gets the server containing the GridFS collections.
        /// </summary>
        public MongoServer Server
        {
            get { return _server; }
        }

        /// <summary>
        /// Gets the GridFS settings.
        /// </summary>
        public MongoGridFSSettings Settings
        {
            get { return _settings; }
        }

        // private static methods
        private static MongoGridFSSettings ApplyDefaultValues(MongoGridFSSettings settings, MongoDatabase database)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            settings = settings.Clone();
            settings.ApplyDefaultValues(database.Settings);
            return settings;
        }

        private static string GetDatabaseName(MongoDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            return database.Name;
        }

        private static MongoServer GetServer(MongoDatabase database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            return database.Server;
        }

        // public methods
        /// <summary>
        /// Appends UTF-8 encoded text to an existing GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A StreamWriter.</returns>
        public StreamWriter AppendText(string remoteFileName)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.AppendText(remoteFileName);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.AppendText();
            }
        }

        /// <summary>
        /// Copies a GridFS file.
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="destFileName">The destination file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo CopyTo(string sourceFileName, string destFileName)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.CopyTo(sourceFileName, destFileName);
            }
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
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.CopyTo(sourceFileName, destFileName, createOptions);
            }
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
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.Create(remoteFileName);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.Create();
            }
        }

        /// <summary>
        /// Creates or overwrites a GridFS file.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Create(string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.Create(remoteFileName, createOptions);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName, createOptions);
                return fileInfo.Create();
            }
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream writer.</returns>
        public StreamWriter CreateText(string remoteFileName)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.CreateText(remoteFileName);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.CreateText();
            }
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream writer.</returns>
        public StreamWriter CreateText(string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.CreateText(remoteFileName, createOptions);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName, createOptions);
                return fileInfo.CreateText();
            }
        }

        /// <summary>
        /// Deletes GridFS files.
        /// </summary>
        /// <param name="query">A query that specifies the GridFS files to delete.</param>
        public void Delete(IMongoQuery query)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                gridFS.Delete(query);
                return;
            }
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
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                gridFS.Delete(remoteFileName);
                return;
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                EnsureIndexes();
                Delete(Query.EQ("filename", remoteFileName));
            }
        }

        /// <summary>
        /// Deletes a GridFS file.
        /// </summary>
        /// <param name="id">The GridFS file Id.</param>
        public void DeleteById(BsonValue id)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                gridFS.DeleteById(id);
                return;
            }
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
            using (_server.RequestStart(_settings.ReadPreference))
            {
                var connectionId = _server.RequestConnectionId;

                if (_settings.VerifyMD5 && fileInfo.MD5 == null)
                {
                    throw new MongoGridFSException(connectionId, "VerifyMD5 is true and file being downloaded has no MD5 hash.");
                }

                var database = GetDatabase();
                var chunksCollection = GetChunksCollection(database);

                string md5Client = null;
                using (var md5Algorithm = _settings.VerifyMD5 ? IncrementalMD5.Create() : null)
                {
                    var numberOfChunks = (fileInfo.Length + fileInfo.ChunkSize - 1) / fileInfo.ChunkSize;
                    for (var n = 0L; n < numberOfChunks; n++)
                    {
                        var query = Query.And(Query.EQ("files_id", fileInfo.Id), Query.EQ("n", n));
                        var chunk = chunksCollection.FindOne(query);
                        if (chunk == null)
                        {
                            string errorMessage = string.Format("Chunk {0} missing for GridFS file '{1}'.", n, fileInfo.Name);
                            throw new MongoGridFSException(connectionId, errorMessage);
                        }
                        var data = chunk["data"].AsBsonBinaryData;
                        if (data.Bytes.Length != fileInfo.ChunkSize)
                        {
                            // the last chunk only has as many bytes as needed to complete the file
                            if (n < numberOfChunks - 1 || data.Bytes.Length != fileInfo.Length % fileInfo.ChunkSize)
                            {
                                string errorMessage = string.Format("Chunk {0} for GridFS file '{1}' is the wrong size.", n, fileInfo.Name);
                                throw new MongoGridFSException(connectionId, errorMessage);
                            }
                        }
                        stream.Write(data.Bytes, 0, data.Bytes.Length);
                        if (_settings.VerifyMD5)
                        {
                            md5Algorithm.AppendData(data.Bytes, 0, data.Bytes.Length);
                        }
                    }

                    if (_settings.VerifyMD5)
                    {
                        md5Client = BsonUtils.ToHexString(md5Algorithm.GetHashAndReset());
                    }
                }

                if (_settings.VerifyMD5 && !md5Client.Equals(fileInfo.MD5, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MongoGridFSException(connectionId, "Download client and server MD5 hashes are not equal.");
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

            var database = GetDatabase(ReadPreference.Primary);
            var filesCollection = GetFilesCollection(database);
            var chunksCollection = GetChunksCollection(database);

            // only create indexes if number of GridFS files is still small (to avoid performance surprises)
            var count = filesCollection.Count();
            if (count < maxFiles)
            {
                filesCollection.CreateIndex("filename", "uploadDate");
                chunksCollection.CreateIndex(IndexKeys.Ascending("files_id", "n"), IndexOptions.SetUnique(true));
            }
        }

        /// <summary>
        /// Tests whether a GridFS file exists.
        /// </summary>
        /// <param name="query">The GridFS file.</param>
        /// <returns>True if the GridFS file exists.</returns>
        public bool Exists(IMongoQuery query)
        {
            var database = GetDatabase();
            var filesCollection = GetFilesCollection(database);
            return filesCollection.Count(query) > 0;
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
            using (_server.RequestStart(_settings.ReadPreference))
            {
                var serverInstance = _server.RequestServerInstance;
                var database = GetDatabase();
                var filesCollection = GetFilesCollection(database);
                var serializer = new MongoGridFSFileInfoSerializer(
                    _server,
                    serverInstance,
                    _databaseName,
                    _settings);
                return filesCollection.FindAs<MongoGridFSFileInfo>(query).SetSerializer(serializer);
            }
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
            using (_server.RequestStart(_settings.ReadPreference))
            {
                var serverInstance = _server.RequestServerInstance;
                var database = GetDatabase();
                var filesCollection = GetFilesCollection(database);

                BsonDocument fileInfo;
                if (version > 0)
                {
                    fileInfo = filesCollection.Find(query).SetSortOrder(SortBy.Ascending("uploadDate")).SetSkip(version - 1).SetLimit(1).FirstOrDefault();
                }
                else if (version < 0)
                {
                    fileInfo = filesCollection.Find(query).SetSortOrder(SortBy.Descending("uploadDate")).SetSkip(-version - 1).SetLimit(1).FirstOrDefault();
                }
                else
                {
                    fileInfo = filesCollection.FindOne(query);
                }

                if (fileInfo != null)
                {
                    return new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, fileInfo);
                }
                else
                {
                    return null;
                }
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
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                gridFS.MoveTo(sourceFileName, destFileName);
                return;
            }
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
            using (_server.RequestStart(DetermineReadPreference(mode)))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.Open(mode);
            }
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
            using (_server.RequestStart(DetermineReadPreference(mode, access)))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.Open(mode, access);
            }
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
            using (_server.RequestStart(DetermineReadPreference(mode, access)))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName, createOptions);
                return fileInfo.Open(mode, access);
            }
        }

        /// <summary>
        /// Opens an existing GridFS file for reading.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenRead(string remoteFileName)
        {
            using (_server.RequestStart(_settings.ReadPreference))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.OpenRead();
            }
        }

        /// <summary>
        /// Opens an existing UTF-8 encoded text GridFS file for reading.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream reader.</returns>
        public StreamReader OpenText(string remoteFileName)
        {
            using (_server.RequestStart(_settings.ReadPreference))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.OpenText();
            }
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite(string remoteFileName)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.OpenWrite(remoteFileName);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName);
                return fileInfo.OpenWrite();
            }
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite(string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.OpenWrite(remoteFileName, createOptions);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var serverInstance = _server.RequestServerInstance;
                var fileInfo = new MongoGridFSFileInfo(_server, serverInstance, _databaseName, _settings, remoteFileName, createOptions);
                return fileInfo.OpenWrite();
            }
        }

        /// <summary>
        /// Sets the aliases for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="aliases">The aliases.</param>
        public void SetAliases(MongoGridFSFileInfo fileInfo, string[] aliases)
        {
            var database = GetDatabase(ReadPreference.Primary);
            var filesCollection = GetFilesCollection(database);
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (aliases == null) ? Update.Unset("aliases") : Update.Set("aliases", new BsonArray(aliases));
            filesCollection.Update(query, update, _settings.WriteConcern);
        }

        /// <summary>
        /// Sets the content type for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="contentType">The content type.</param>
        public void SetContentType(MongoGridFSFileInfo fileInfo, string contentType)
        {
            var database = GetDatabase(ReadPreference.Primary);
            var filesCollection = GetFilesCollection(database);
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (contentType == null) ? Update.Unset("contentType") : Update.Set("contentType", contentType);
            filesCollection.Update(query, update, _settings.WriteConcern);
        }

        /// <summary>
        /// Sets the metadata for an existing GridFS file.
        /// </summary>
        /// <param name="fileInfo">The GridFS file.</param>
        /// <param name="metadata">The metadata.</param>
        public void SetMetadata(MongoGridFSFileInfo fileInfo, BsonValue metadata)
        {
            var database = GetDatabase(ReadPreference.Primary);
            var filesCollection = GetFilesCollection(database);
            var query = Query.EQ("_id", fileInfo.Id);
            var update = (metadata == null) ? Update.Unset("metadata") : Update.Set("metadata", metadata);
            filesCollection.Update(query, update, _settings.WriteConcern);
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
                Id = ObjectId.GenerateNewId(),
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
            if (_settings.ReadPreference != ReadPreference.Primary)
            {
                var gridFS = WithReadPreferencePrimary();
                return gridFS.Upload(stream, remoteFileName, createOptions);
            }
            using (_server.RequestStart(ReadPreference.Primary))
            {
                var connectionId = _server.RequestConnectionId;
                EnsureIndexes();

                var database = GetDatabase(ReadPreference.Primary);
                var chunksCollection = GetChunksCollection(database);
                var filesCollection = GetFilesCollection(database);

                var files_id = createOptions.Id ?? ObjectId.GenerateNewId();
                var chunkSize = (createOptions.ChunkSize == 0) ? _settings.ChunkSize : createOptions.ChunkSize;
                var buffer = new byte[chunkSize];

                var length = 0L;
                string md5Client = null;
                using (var md5Algorithm = _settings.VerifyMD5 ? IncrementalMD5.Create() : null)
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
                            { "_id", ObjectId.GenerateNewId() },
                            { "files_id", files_id },
                            { "n", n < int.MaxValue ? (BsonValue)(BsonInt32)(int)n : (BsonInt64)n },
                            { "data", new BsonBinaryData(data) }
                        };
                        chunksCollection.Insert(chunk, _settings.WriteConcern);

                        if (_settings.VerifyMD5)
                        {
                            md5Algorithm.AppendData(data, 0, data.Length);
                        }

                        if (bytesRead < chunkSize)
                        {
                            break; // EOF after partial chunk
                        }
                    }

                    if (_settings.VerifyMD5)
                    {
                        md5Client = BsonUtils.ToHexString(md5Algorithm.GetHashAndReset());
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
                    var md5Result = database.RunCommand(md5Command);
                    md5Server = md5Result.Response["md5"].AsString;
                }

                if ( _settings.VerifyMD5 && !md5Client.Equals(md5Server, StringComparison.OrdinalIgnoreCase))
                {
                    throw new MongoGridFSException(connectionId, "Upload client and server MD5 hashes are not equal.");
                }

                var uploadDate = (createOptions.UploadDate == DateTime.MinValue) ? DateTime.UtcNow : createOptions.UploadDate;
                var aliases = (createOptions.Aliases != null) ? new BsonArray(createOptions.Aliases) : null;
                BsonDocument fileInfo = new BsonDocument
                {
                    { "_id", files_id },
                    { "filename", remoteFileName, !string.IsNullOrEmpty(remoteFileName) }, // optional
                    { "length", length },
                    { "chunkSize", chunkSize },
                    { "uploadDate", uploadDate },
                    { "md5", (md5Server == null) ? (BsonValue)BsonNull.Value : new BsonString(md5Server) },
                    { "contentType", createOptions.ContentType, !string.IsNullOrEmpty(createOptions.ContentType) }, // optional
                    { "aliases", aliases, aliases != null }, // optional
                    { "metadata", createOptions.Metadata, createOptions.Metadata != null } // optional
                };
                filesCollection.Insert(fileInfo, _settings.WriteConcern);

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

        // internal methods
        internal MongoCollection<BsonDocument> GetChunksCollection(MongoDatabase database)
        {
            return database.GetCollection<BsonDocument>(_settings.Root + ".chunks");
        }

        internal MongoDatabase GetDatabase()
        {
            return GetDatabase(_settings.ReadPreference);
        }

        internal MongoDatabase GetDatabase(ReadPreference readPreference)
        {
            var databaseSettings = _settings.GetDatabaseSettings();
            databaseSettings.ReadPreference = readPreference;
            databaseSettings.Freeze();
            return _server.GetDatabase(_databaseName, databaseSettings);
        }

        internal MongoCollection<BsonDocument> GetFilesCollection(MongoDatabase database)
        {
            return database.GetCollection<BsonDocument>(_settings.Root + ".files");
        }

        // private methods
        private ReadPreference DetermineReadPreference(FileMode mode)
        {
            if (mode != FileMode.Open)
            {
                return ReadPreference.Primary;
            }

            return _settings.ReadPreference;
        }

        private ReadPreference DetermineReadPreference(FileMode mode, FileAccess access)
        {
            if (mode != FileMode.Open)
            {
                return ReadPreference.Primary;
            }

            if (access != FileAccess.Read)
            {
                return ReadPreference.Primary;
            }

            return _settings.ReadPreference;
        }

        private MongoGridFS WithReadPreferencePrimary()
        {
            var settings = _settings.Clone();
            settings.ReadPreference = ReadPreference.Primary;
            var gridFS = new MongoGridFS(_server, _databaseName, settings);
            return gridFS;
        }
    }
}
