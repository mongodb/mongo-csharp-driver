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
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents information about a GridFS file (patterned after .NET's FileInfo class).
    /// </summary>
    public class MongoGridFSFileInfo : IEquatable<MongoGridFSFileInfo>
    {
        // private fields
        // these fields are considered in Equals and GetHashCode
        private string[] _aliases;
        private int _chunkSize;
        private string _contentType;
        private BsonValue _id; // usually a BsonObjectId but not required to be
        private long _length;
        private string _md5;
        private BsonDocument _metadata;
        private string _name;
        private DateTime _uploadDate;

        // these fields are not considered in Equals and GetHashCode
        private readonly MongoServer _server;
        private readonly MongoServerInstance _serverInstance;
        private readonly string _databaseName;
        private readonly MongoGridFSSettings _settings;
        private bool _cached; // true if info came from database
        private bool _exists;

        // constructors
        // used by Deserialize
        private MongoGridFSFileInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        [Obsolete("Use a different constructor instead.")]
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName)
            : this(GetServer(gridFS), GetServerInstance(gridFS), GetDatabaseName(gridFS), GetGridFSSettings(gridFS), remoteFileName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="chunkSize">The chunk size.</param>
        [Obsolete("Use a different constructor instead.")]
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName, int chunkSize)
            : this(GetServer(gridFS), GetServerInstance(gridFS), GetDatabaseName(gridFS), GetGridFSSettings(gridFS), remoteFileName)
        {
            _chunkSize = chunkSize;
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        [Obsolete("Use a different constructor instead.")]
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName, MongoGridFSCreateOptions createOptions)
            : this(GetServer(gridFS), GetServerInstance(gridFS), GetDatabaseName(gridFS), GetGridFSSettings(gridFS), remoteFileName, createOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoGridFSFileInfo"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="serverInstance">The server instance.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="gridFSSettings">The GridFS settings.</param>
        private MongoGridFSFileInfo(
            MongoServer server,
            MongoServerInstance serverInstance,
            string databaseName,
            MongoGridFSSettings gridFSSettings)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            if (gridFSSettings == null)
            {
                throw new ArgumentNullException("gridFSSettings");
            }

            _server = server;
            _serverInstance = serverInstance;
            _databaseName = databaseName;
            _settings = gridFSSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoGridFSFileInfo"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="serverInstance">The server instance.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="gridFSSettings">The GridFS settings.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        public MongoGridFSFileInfo(
            MongoServer server,
            MongoServerInstance serverInstance,
            string databaseName,
            MongoGridFSSettings gridFSSettings,
            string remoteFileName)
            : this(server, serverInstance, databaseName, gridFSSettings)
        {
            if (remoteFileName == null)
            {
                throw new ArgumentNullException("remoteFileName");
            }

            _chunkSize = gridFSSettings.ChunkSize;
            _name = remoteFileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoGridFSFileInfo"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="serverInstance">The server instance.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="gridFSSettings">The GridFS settings.</param>
        /// <param name="fileInfo">The fileInfo.</param>
        public MongoGridFSFileInfo(
            MongoServer server,
            MongoServerInstance serverInstance,
            string databaseName,
            MongoGridFSSettings gridFSSettings,
            BsonDocument fileInfo)
            : this(server, serverInstance, databaseName, gridFSSettings)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException("fileInfo");
            }

            _chunkSize = gridFSSettings.ChunkSize;
            CacheFileInfo(fileInfo);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoGridFSFileInfo"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="serverInstance">The server instance.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="gridFSSettings">The GridFS settings.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        public MongoGridFSFileInfo(
            MongoServer server,
            MongoServerInstance serverInstance,
            string databaseName,
            MongoGridFSSettings gridFSSettings,
            string remoteFileName,
            MongoGridFSCreateOptions createOptions)
            : this(server, serverInstance, databaseName, gridFSSettings)
        {
            if (remoteFileName == null)
            {
                throw new ArgumentNullException("remoteFileName");
            }
            if (createOptions == null)
            {
                throw new ArgumentNullException("createOptions");
            }

            _aliases = createOptions.Aliases;
            _chunkSize = (createOptions.ChunkSize == 0) ? gridFSSettings.ChunkSize : createOptions.ChunkSize;
            _contentType = createOptions.ContentType;
            _id = createOptions.Id;
            _metadata = createOptions.Metadata;
            _name = remoteFileName;
            _uploadDate = createOptions.UploadDate;
            _cached = true; // prevent values from being overwritten by automatic Refresh
        }

        // public properties
        /// <summary>
        /// Gets the aliases.
        /// </summary>
        public string[] Aliases
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _aliases;
            }
        }

        /// <summary>
        /// Gets the chunk size.
        /// </summary>
        public int ChunkSize
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _chunkSize;
            }
        }

        /// <summary>
        /// Gets the content type.
        /// </summary>
        public string ContentType
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _contentType;
            }
        }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        /// <summary>
        /// Gets a value indicating whether the GridFS file exists.
        /// </summary>
        public bool Exists
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _exists;
            }
        }

        /// <summary>
        /// Gets the GridFS file system that contains this GridFS file.
        /// </summary>
        public MongoGridFS GridFS
        {
            get { return new MongoGridFS(_server, _databaseName, _settings); }
        }

        /// <summary>
        /// Gets the GridFS settings.
        /// </summary>
        public MongoGridFSSettings GridFSSettings
        {
            get { return _settings; }
        }

        /// <summary>
        /// Gets the GridFS file Id.
        /// </summary>
        public BsonValue Id
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _id;
            }
        }

        /// <summary>
        /// Gets the file lenth.
        /// </summary>
        public long Length
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _length;
            }
        }

        /// <summary>
        /// Gets the MD5 hash of the file contents.
        /// </summary>
        public string MD5
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _md5;
            }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public BsonDocument Metadata
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _metadata;
            }
        }

        /// <summary>
        /// Gets the remote file name.
        /// </summary>
        public string Name
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _name;
            }
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        public MongoServer Server
        {
            get { return _server; }
        }

        /// <summary>
        /// Gets the server instance;
        /// </summary>
        public MongoServerInstance ServerInstance
        {
            get { return _serverInstance; }
        }

        /// <summary>
        /// Gets the upload date.
        /// </summary>
        public DateTime UploadDate
        {
            get
            {
                if (!_cached) { Refresh(); }
                return _uploadDate;
            }
        }

        // public operators
        /// <summary>
        /// Determines whether two specified MongoGridFSFileInfo objects have different values.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
        public static bool operator !=(MongoGridFSFileInfo lhs, MongoGridFSFileInfo rhs)
        {
            return !MongoGridFSFileInfo.Equals(lhs, rhs);
        }

        /// <summary>
        /// Determines whether two specified MongoGridFSFileInfo objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool operator ==(MongoGridFSFileInfo lhs, MongoGridFSFileInfo rhs)
        {
            return MongoGridFSFileInfo.Equals(lhs, rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether two specified MongoGridFSFileInfo objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool Equals(MongoGridFSFileInfo lhs, MongoGridFSFileInfo rhs)
        {
            if ((object)lhs == null) { return (object)rhs == null; }
            return lhs.Equals(rhs);
        }

        // private static methods
        private static string GetDatabaseName(MongoGridFS gridFS)
        {
            if (gridFS == null)
            {
                throw new ArgumentNullException("gridFS");
            }
            return gridFS.DatabaseName;
        }

        private static MongoGridFSSettings GetGridFSSettings(MongoGridFS gridFS)
        {
            if (gridFS == null)
            {
                throw new ArgumentNullException("gridFS");
            }
            return gridFS.Settings;
        }

        private static MongoServer GetServer(MongoGridFS gridFS)
        {
            if (gridFS == null)
            {
                throw new ArgumentNullException("gridFS");
            }
            return gridFS.Server;
        }

        private static MongoServerInstance GetServerInstance(MongoGridFS gridFS)
        {
            if (gridFS == null)
            {
                throw new ArgumentNullException("gridFS");
            }

            // bind to one of the nodes using the ReadPreference
            var server = gridFS.Server;
            using (server.RequestStart(gridFS.Settings.ReadPreference))
            {
                return server.RequestServerInstance;
            }
        }

        // public methods
        /// <summary>
        /// Appends UTF-8 encoded text to an existing GridFS file.
        /// </summary>
        /// <returns>A StreamWriter.</returns>
        public StreamWriter AppendText()
        {
            Stream stream = Open(FileMode.Append, FileAccess.Write);
            return new StreamWriter(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Copies a GridFS file.
        /// </summary>
        /// <param name="destFileName">The destination file name.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo CopyTo(string destFileName)
        {
            // copy all createOptions except Aliases (which are considered alternate filenames)
            var createOptions = new MongoGridFSCreateOptions
            {
                ChunkSize = _chunkSize,
                ContentType = _contentType,
                Metadata = _metadata,
                UploadDate = _uploadDate
            };
            return CopyTo(destFileName, createOptions);
        }

        /// <summary>
        /// Copies a GridFS file.
        /// </summary>
        /// <param name="destFileName">The destination file name.</param>
        /// <param name="createOptions">The create options.</param>
        /// <returns>The file info of the new GridFS file.</returns>
        public MongoGridFSFileInfo CopyTo(string destFileName, MongoGridFSCreateOptions createOptions)
        {
            EnsureServerInstanceIsPrimary();
            using (_server.RequestStart(_serverInstance))
            {
                // note: we are aware that the data is making a round trip from and back to the server
                // but we choose not to use a script to copy the data locally on the server
                // because that would lock the database for too long
                var gridFS = new MongoGridFS(_server, _databaseName, _settings);
                var stream = OpenRead();
                return gridFS.Upload(stream, destFileName, createOptions);
            }
        }

        /// <summary>
        /// Creates or overwrites a GridFS file.
        /// </summary>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Create()
        {
            return Open(FileMode.Create, FileAccess.ReadWrite);
        }

        /// <summary>
        /// Creates or opens a GridFS file for writing UTF-8 encoded text.
        /// </summary>
        /// <returns>A stream.</returns>
        public StreamWriter CreateText()
        {
            var stream = Create();
            return new StreamWriter(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Deletes a GridFS file.
        /// </summary>
        public void Delete()
        {
            EnsureServerInstanceIsPrimary();
            using (_server.RequestStart(_serverInstance))
            {
                var gridFS = new MongoGridFS(_server, _databaseName, _settings);
                gridFS.EnsureIndexes();

                if (Exists)
                {
                    var database = gridFS.GetDatabase(ReadPreference.Primary);
                    var filesCollection = gridFS.GetFilesCollection(database);
                    var chunksCollection = gridFS.GetChunksCollection(database);

                    filesCollection.Remove(Query.EQ("_id", _id), gridFS.Settings.WriteConcern);
                    chunksCollection.Remove(Query.EQ("files_id", _id), gridFS.Settings.WriteConcern);
                }
            }
        }

        /// <summary>
        /// Determines whether this instance and another specified MongoGridFSFileInfo object have the same value.
        /// </summary>
        /// <param name="rhs">The MongoGridFSFileInfo object to compare to this instance.</param>
        /// <returns>True if the value of the rhs parameter is the same as this instance; otherwise, false.</returns>
        public bool Equals(MongoGridFSFileInfo rhs)
        {
            if ((object)rhs == null || GetType() != rhs.GetType()) { return false; }
            if ((object)this == (object)rhs) { return true; }
            return
                (_aliases == null && rhs._aliases == null || _aliases != null && rhs._aliases != null && _aliases.SequenceEqual(rhs._aliases)) &&
                _chunkSize == rhs._chunkSize &&
                _contentType == rhs._contentType &&
                _id == rhs._id &&
                _length == rhs._length &&
                _md5 == rhs._md5 &&
                _metadata == rhs._metadata &&
                _name == rhs._name &&
                _uploadDate == rhs._uploadDate;
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a MongoGridFSFileInfo object, have the same value.
        /// </summary>
        /// <param name="obj">The MongoGridFSFileInfo object to compare to this instance.</param>
        /// <returns>True if obj is a MongoGridFSFileInfo object and its value is the same as this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoGridFSFileInfo); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Returns the hash code for this MongoGridFSFileInfo object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + ((_aliases == null) ? 0 : _aliases.GetHashCode());
            hash = 37 * hash + _chunkSize.GetHashCode();
            hash = 37 * hash + ((_contentType == null) ? 0 : _contentType.GetHashCode());
            hash = 37 * hash + ((_id == null) ? 0 : _id.GetHashCode());
            hash = 37 * hash + _length.GetHashCode();
            hash = 37 * hash + ((_md5 == null) ? 0 : _md5.GetHashCode());
            hash = 37 * hash + ((_metadata == null) ? 0 : _metadata.GetHashCode());
            hash = 37 * hash + ((_name == null) ? 0 : _name.GetHashCode());
            hash = 37 * hash + _uploadDate.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Moves the most recent version of a GridFS file.
        /// </summary>
        /// <param name="destFileName">The destination file name.</param>
        public void MoveTo(string destFileName)
        {
            EnsureServerInstanceIsPrimary();
            using (_server.RequestStart(_serverInstance))
            {
                var gridFS = new MongoGridFS(_server, _databaseName, _settings);
                var database = gridFS.GetDatabase(ReadPreference.Primary);
                var filesCollection = gridFS.GetFilesCollection(database);
                var query = Query.EQ("_id", _id);
                var update = Update.Set("filename", destFileName);
                filesCollection.Update(query, update);
            }
        }

        /// <summary>
        /// Opens a GridFS file with the specified mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Open(FileMode mode)
        {
            return Open(mode, FileAccess.ReadWrite);
        }

        /// <summary>
        /// Opens a GridFS file with the specified mode and access.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <returns>A stream.</returns>
        public MongoGridFSStream Open(FileMode mode, FileAccess access)
        {
            return new MongoGridFSStream(this, mode, access);
        }

        /// <summary>
        /// Opens an existing GridFS file for reading.
        /// </summary>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenRead()
        {
            return Open(FileMode.Open, FileAccess.Read);
        }

        /// <summary>
        /// Opens an existing UTF-8 encoded text GridFS file for reading.
        /// </summary>
        /// <returns>A stream reader.</returns>
        public StreamReader OpenText()
        {
            Stream stream = Open(FileMode.Open, FileAccess.Read);
            return new StreamReader(stream, Encoding.UTF8);
        }

        /// <summary>
        /// Opens an existing GridFS file for writing.
        /// </summary>
        /// <returns>A stream.</returns>
        public MongoGridFSStream OpenWrite()
        {
            return Open(FileMode.OpenOrCreate, FileAccess.Write);
        }

        /// <summary>
        /// Refreshes the GridFS file info from the server.
        /// </summary>
        public void Refresh()
        {
            using (_server.RequestStart(_serverInstance))
            {
                var gridFS = new MongoGridFS(_server, _databaseName, _settings);
                var database = gridFS.GetDatabase();
                var filesCollection = gridFS.GetFilesCollection(database);

                MongoCursor<BsonDocument> cursor;
                if (_id != null)
                {
                    cursor = filesCollection.Find(Query.EQ("_id", _id));
                }
                else if (_name != null)
                {
                    cursor = filesCollection.Find(Query.EQ("filename", _name)).SetSortOrder(SortBy.Descending("uploadDate"));
                }
                else
                {
                    throw new InvalidOperationException("Cannot refresh FileInfo when both Id and Name are missing.");
                }
                var fileInfo = cursor.SetLimit(1).FirstOrDefault();
                CacheFileInfo(fileInfo); // fileInfo will be null if file does not exist
            }
        }

        // internal methods
        internal void SetId(BsonValue id)
        {
            if (_id == null)
            {
                _id = id;
            }
            else
            {
                throw new InvalidOperationException("FileInfo already has an Id.");
            }
        }

        // private methods
        private void CacheFileInfo(BsonDocument fileInfo)
        {
            if (fileInfo == null)
            {
                // leave aliases, chunkSize, contentType, id, metadata and name alone (they might be needed to create a new file)
                _exists = false;
                _length = 0;
                _md5 = null;
                _uploadDate = default(DateTime);
            }
            else
            {
                BsonValue aliasesValue;
                if (fileInfo.TryGetValue("aliases", out aliasesValue) && !aliasesValue.IsBsonNull)
                {
                    var list = new List<string>();
                    foreach (var alias in aliasesValue.AsBsonArray)
                    {
                        list.Add(alias.AsString);
                    }
                    _aliases = list.ToArray();
                }
                else
                {
                    _aliases = null;
                }
                _chunkSize = fileInfo["chunkSize"].ToInt32();
                BsonValue contentTypeValue;
                if (fileInfo.TryGetValue("contentType", out contentTypeValue) && !contentTypeValue.IsBsonNull)
                {
                    _contentType = contentTypeValue.AsString;
                }
                else
                {
                    _contentType = null;
                }
                _exists = true;
                _id = fileInfo["_id"];
                _length = fileInfo["length"].ToInt64();
                BsonValue md5Value;
                if (fileInfo.TryGetValue("md5", out md5Value) && md5Value.IsString)
                {
                    _md5 = md5Value.AsString;
                }
                else
                {
                    _md5 = null;
                }
                BsonValue metadataValue;
                if (fileInfo.TryGetValue("metadata", out metadataValue) && !metadataValue.IsBsonNull)
                {
                    _metadata = metadataValue.AsBsonDocument;
                }
                else
                {
                    _metadata = null;
                }
                BsonValue filenameValue;
                if (fileInfo.TryGetValue("filename", out filenameValue) && !filenameValue.IsBsonNull)
                {
                    _name = filenameValue.AsString;
                }
                else
                {
                    _name = null;
                }
                _uploadDate = fileInfo["uploadDate"].ToUniversalTime();
            }
            _cached = true;
        }

        private void EnsureServerInstanceIsPrimary()
        {
            if (!_serverInstance.IsPrimary)
            {
                var message = string.Format("Server instance {0} is not the current primary.", _serverInstance.Address);
                throw new InvalidOperationException(message);
            }
        }
    }

    internal class MongoGridFSFileInfoSerializer : SerializerBase<MongoGridFSFileInfo>
    {
        // private fields
        private readonly MongoServer _server;
        private readonly MongoServerInstance _serverInstance;
        private readonly string _databaseName;
        private readonly MongoGridFSSettings _gridFSSettings;

        // constructors
        public MongoGridFSFileInfoSerializer(
            MongoServer server,
            MongoServerInstance serverInstance,
            string databaseName,
            MongoGridFSSettings gridFSSettings)
        {
            _server = server;
            _serverInstance = serverInstance;
            _databaseName = databaseName;
            _gridFSSettings = gridFSSettings;
        }

        // public methods
        public override MongoGridFSFileInfo Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var fileInfo = BsonDocumentSerializer.Instance.Deserialize(context);
            return new MongoGridFSFileInfo(
                _server,
                _serverInstance,
                _databaseName,
                _gridFSSettings,
                fileInfo);
        }
    }
}
