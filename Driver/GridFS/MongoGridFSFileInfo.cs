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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents information about a GridFS file (patterned after .NET's FileInfo class).
    /// </summary>
    public class MongoGridFSFileInfo : IBsonSerializable, IEquatable<MongoGridFSFileInfo>
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
        private bool _cached; // true if info came from database
        private bool _exists;
        private MongoGridFS _gridFS;

        // constructors
        // used by Deserialize
        private MongoGridFSFileInfo()
        {
        }

        internal MongoGridFSFileInfo(MongoGridFS gridFS, BsonDocument fileInfo)
        {
            _gridFS = gridFS;
            CacheFileInfo(fileInfo);
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName)
            : this(gridFS, remoteFileName, gridFS.Settings.ChunkSize)
        {
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="chunkSize">The chunk size.</param>
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName, int chunkSize)
        {
            _gridFS = gridFS;
            _chunkSize = chunkSize;
            _name = remoteFileName;
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            _gridFS = gridFS;
            _aliases = createOptions.Aliases;
            _chunkSize = createOptions.ChunkSize == 0 ? gridFS.Settings.ChunkSize : createOptions.ChunkSize;
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
        /// Gets whether the GridFS file exists.
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
            get { return _gridFS; }
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
            // note: we are aware that the data is making a round trip from and back to the server
            // but we choose not to use a script to copy the data locally on the server
            // because that would lock the database for too long
            var stream = OpenRead();
            return _gridFS.Upload(stream, destFileName, createOptions);
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
            if (Exists)
            {
                using (_gridFS.Database.RequestStart(ReadPreference.Primary))
                {
                    _gridFS.EnsureIndexes();
                    _gridFS.Files.Remove(Query.EQ("_id", _id), _gridFS.Settings.WriteConcern);
                    _gridFS.Chunks.Remove(Query.EQ("files_id", _id), _gridFS.Settings.WriteConcern);
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
            hash = 37 * hash + _name.GetHashCode();
            hash = 37 * hash + _uploadDate.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Moves the most recent version of a GridFS file.
        /// </summary>
        /// <param name="destFileName">The destination file name.</param>
        public void MoveTo(string destFileName)
        {
            var query = Query.EQ("_id", _id);
            var update = Update.Set("filename", destFileName);
            _gridFS.Files.Update(query, update, _gridFS.Settings.WriteConcern);
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
            MongoCursor<BsonDocument> cursor;
            if (_id != null)
            {
                cursor = _gridFS.Files.Find(Query.EQ("_id", _id));
            }
            else
            {
                cursor = _gridFS.Files.Find(Query.EQ("filename", _name)).SetSortOrder(SortBy.Descending("uploadDate"));
            }
            var fileInfo = cursor.SetLimit(1).FirstOrDefault();
            CacheFileInfo(fileInfo); // fileInfo will be null if file does not exist
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
                var aliasesValue = fileInfo["aliases", null];
                if (aliasesValue != null && !aliasesValue.IsBsonNull)
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
                var contentTypeValue = fileInfo["contentType", null];
                if (contentTypeValue != null && !contentTypeValue.IsBsonNull)
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
                var md5Value = fileInfo["md5", null];
                if (md5Value != null && md5Value.IsString)
                {
                    _md5 = md5Value.AsString;
                }
                else
                {
                    _md5 = null;
                }
                var metadataValue = fileInfo["metadata", null];
                if (metadataValue != null && !metadataValue.IsBsonNull)
                {
                    _metadata = metadataValue.AsBsonDocument;
                }
                else
                {
                    _metadata = null;
                }
                var filenameValue = fileInfo["filename", null];
                if (filenameValue != null && !filenameValue.IsBsonNull)
                {
                    _name = filenameValue.AsString;
                }
                else
                {
                    _name = null;
                }
                _uploadDate = fileInfo["uploadDate"].AsDateTime;
            }
            _cached = true;
        }

        // explicit interface implementations
        object IBsonSerializable.Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            MongoGridFS gridFS = ((SerializationOptions)options).GridFS;
            var fileInfo = BsonDocument.ReadFrom(bsonReader);
            return new MongoGridFSFileInfo(gridFS, fileInfo);
        }

        bool IBsonSerializable.GetDocumentId(out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            throw new NotSupportedException();
        }

        void IBsonSerializable.Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            throw new NotSupportedException();
        }

        void IBsonSerializable.SetDocumentId(object id)
        {
            throw new NotSupportedException();
        }

        // nested classes
        internal class SerializationOptions : BsonBaseSerializationOptions
        {
            internal MongoGridFS GridFS;
        }
    }
}
