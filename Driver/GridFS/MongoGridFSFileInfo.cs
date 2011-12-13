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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
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
        private string[] aliases;
        private int chunkSize;
        private string contentType;
        private BsonValue id; // usually a BsonObjectId but not required to be
        private long length;
        private string md5;
        private BsonDocument metadata;
        private string name;
        private DateTime uploadDate;

        // these fields are not considered in Equals and GetHashCode
        private bool cached; // true if info came from database
        private bool exists;
        private MongoGridFS gridFS;

        // constructors
        // used by Deserialize
        private MongoGridFSFileInfo()
        {
        }

        internal MongoGridFSFileInfo(MongoGridFS gridFS, BsonDocument fileInfo)
        {
            this.gridFS = gridFS;
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
            this.gridFS = gridFS;
            this.chunkSize = chunkSize;
            this.name = remoteFileName;
        }

        /// <summary>
        /// Initializes a new instance of the GridFSFileInfo class.
        /// </summary>
        /// <param name="gridFS">The GridFS file system that contains the GridFS file.</param>
        /// <param name="remoteFileName">The remote file name.</param>
        /// <param name="createOptions">The create options.</param>
        public MongoGridFSFileInfo(MongoGridFS gridFS, string remoteFileName, MongoGridFSCreateOptions createOptions)
        {
            this.gridFS = gridFS;
            this.aliases = createOptions.Aliases;
            this.chunkSize = createOptions.ChunkSize == 0 ? gridFS.Settings.ChunkSize : createOptions.ChunkSize;
            this.contentType = createOptions.ContentType;
            this.id = createOptions.Id;
            this.metadata = createOptions.Metadata;
            this.name = remoteFileName;
            this.uploadDate = createOptions.UploadDate;
            this.cached = true; // prevent values from being overwritten by automatic Refresh
        }

        // public properties
        /// <summary>
        /// Gets the aliases.
        /// </summary>
        public string[] Aliases
        {
            get
            {
                if (!cached) { Refresh(); }
                return aliases;
            }
        }

        /// <summary>
        /// Gets the chunk size.
        /// </summary>
        public int ChunkSize
        {
            get
            {
                if (!cached) { Refresh(); }
                return chunkSize;
            }
        }

        /// <summary>
        /// Gets the content type.
        /// </summary>
        public string ContentType
        {
            get
            {
                if (!cached) { Refresh(); }
                return contentType;
            }
        }

        /// <summary>
        /// Gets whether the GridFS file exists.
        /// </summary>
        public bool Exists
        {
            get
            {
                if (!cached) { Refresh(); }
                return exists;
            }
        }

        /// <summary>
        /// Gets the GridFS file system that contains this GridFS file.
        /// </summary>
        public MongoGridFS GridFS
        {
            get { return gridFS; }
        }

        /// <summary>
        /// Gets the GridFS file Id.
        /// </summary>
        public BsonValue Id
        {
            get
            {
                if (!cached) { Refresh(); }
                return id;
            }
        }

        /// <summary>
        /// Gets the file lenth.
        /// </summary>
        public long Length
        {
            get
            {
                if (!cached) { Refresh(); }
                return length;
            }
        }

        /// <summary>
        /// Gets the MD5 hash of the file contents.
        /// </summary>
        public string MD5
        {
            get
            {
                if (!cached) { Refresh(); }
                return md5;
            }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public BsonDocument Metadata
        {
            get
            {
                if (!cached) { Refresh(); }
                return metadata;
            }
        }

        /// <summary>
        /// Gets the remote file name.
        /// </summary>
        public string Name
        {
            get
            {
                if (!cached) { Refresh(); }
                return name;
            }
        }

        /// <summary>
        /// Gets the upload date.
        /// </summary>
        public DateTime UploadDate
        {
            get
            {
                if (!cached) { Refresh(); }
                return uploadDate;
            }
        }

        // public operators
        /// <summary>
        /// Compares two MongoGridFSFileInfos.
        /// </summary>
        /// <param name="lhs">The first MongoGridFSFileInfo.</param>
        /// <param name="rhs">The other MongoGridFSFileInfo.</param>
        /// <returns>True if the two MongoGridFSFileInfos are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoGridFSFileInfo lhs, MongoGridFSFileInfo rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two MongoGridFSFileInfos.
        /// </summary>
        /// <param name="lhs">The first MongoGridFSFileInfo.</param>
        /// <param name="rhs">The other MongoGridFSFileInfo.</param>
        /// <returns>True if the two MongoGridFSFileInfos are equal (or both null).</returns>
        public static bool operator ==(MongoGridFSFileInfo lhs, MongoGridFSFileInfo rhs)
        {
            return object.Equals(lhs, rhs);
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
                ChunkSize = chunkSize,
                ContentType = contentType,
                Metadata = metadata,
                UploadDate = uploadDate
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
            return gridFS.Upload(stream, destFileName, createOptions);
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
                using (gridFS.Database.RequestStart(false)) // not slaveOk
                {
                    gridFS.EnsureIndexes();
                    gridFS.Files.Remove(Query.EQ("_id", id), gridFS.Settings.SafeMode);
                    gridFS.Chunks.Remove(Query.EQ("files_id", id), gridFS.Settings.SafeMode);
                }
            }
        }

        /// <summary>
        /// Compares this MongoGridFSFileInfo to another MongoGridFSFileInfo.
        /// </summary>
        /// <param name="rhs">The other MongoGridFSFileInfo.</param>
        /// <returns>True if the two MongoGridFSFileInfos are equal.</returns>
        public bool Equals(MongoGridFSFileInfo rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return
                (this.aliases == null && rhs.aliases == null || this.aliases != null && rhs.aliases != null && this.aliases.SequenceEqual(rhs.aliases)) &&
                this.chunkSize == rhs.chunkSize &&
                this.contentType == rhs.contentType &&
                this.id == rhs.id &&
                this.length == rhs.length &&
                this.md5 == rhs.md5 &&
                this.metadata == rhs.metadata &&
                this.name == rhs.name &&
                this.uploadDate == rhs.uploadDate;
        }

        /// <summary>
        /// Compares this MongoGridFSFileInfo to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other object is a MongoGridFSFileInfo and equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoGridFSFileInfo); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + ((aliases == null) ? 0 : aliases.GetHashCode());
            hash = 37 * hash + chunkSize.GetHashCode();
            hash = 37 * hash + ((contentType == null) ? 0 : contentType.GetHashCode());
            hash = 37 * hash + ((id == null) ? 0 : id.GetHashCode());
            hash = 37 * hash + length.GetHashCode();
            hash = 37 * hash + ((md5 == null) ? 0 : md5.GetHashCode());
            hash = 37 * hash + ((metadata == null) ? 0 : metadata.GetHashCode());
            hash = 37 * hash + name.GetHashCode();
            hash = 37 * hash + uploadDate.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Moves the most recent version of a GridFS file.
        /// </summary>
        /// <param name="destFileName">The destination file name.</param>
        public void MoveTo(string destFileName)
        {
            var query = Query.EQ("_id", id);
            var update = Update.Set("filename", destFileName);
            gridFS.Files.Update(query, update, gridFS.Settings.SafeMode);
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
            if (id != null)
            {
                cursor = gridFS.Files.Find(Query.EQ("_id", id));
            }
            else
            {
                gridFS.EnsureIndexes();
                cursor = gridFS.Files.Find(Query.EQ("filename", name)).SetSortOrder(SortBy.Descending("uploadDate"));
            }
            var fileInfo = cursor.SetLimit(1).FirstOrDefault();
            CacheFileInfo(fileInfo); // fileInfo will be null if file does not exist
        }

        // internal methods
        internal void SetId(BsonValue id)
        {
            if (this.id == null)
            {
                this.id = id;
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
                exists = false;
                length = 0;
                md5 = null;
                uploadDate = default(DateTime);
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
                    aliases = list.ToArray();
                }
                else
                {
                    aliases = null;
                }
                chunkSize = fileInfo["chunkSize"].ToInt32();
                var contentTypeValue = fileInfo["contentType", null];
                if (contentTypeValue != null && !contentTypeValue.IsBsonNull)
                {
                    contentType = contentTypeValue.AsString;
                }
                else
                {
                    contentType = null;
                }
                exists = true;
                id = fileInfo["_id"];
                length = fileInfo["length"].ToInt64();
                var md5Value = fileInfo["md5", null];
                if (md5Value != null && !md5Value.IsBsonNull)
                {
                    md5 = md5Value.AsString;
                }
                else
                {
                    md5 = null;
                }
                var metadataValue = fileInfo["metadata", null];
                if (metadataValue != null && !metadataValue.IsBsonNull)
                {
                    metadata = metadataValue.AsBsonDocument;
                }
                else
                {
                    metadata = null;
                }
                var filenameValue = fileInfo["filename", null];
                if (filenameValue != null && !filenameValue.IsBsonNull)
                {
                    name = filenameValue.AsString;
                }
                else
                {
                    name = null;
                }
                uploadDate = fileInfo["uploadDate"].AsDateTime;
            }
            cached = true;
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
        internal class SerializationOptions : IBsonSerializationOptions
        {
            internal MongoGridFS GridFS;
        }
    }
}
