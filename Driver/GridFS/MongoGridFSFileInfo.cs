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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS {
    // this class is patterned after .NET's FileInfo
    public class MongoGridFSFileInfo {
        #region private fields
        // these fields are considered in Equals and GetHashCode
        private string[] aliases;
        private int chunkSize;
        private string contentType;
        private BsonValue id; // usually a BsonObjectId but not required to be
        private int length;
        private string md5;
        private BsonDocument metadata;
        private string name;
        private DateTime uploadDate;

        // these fields are not considered in Equals and GetHashCode
        private bool cached; // true if info came from database
        private bool exists;
        private MongoGridFS gridFS;
        #endregion

        #region constructors
        internal MongoGridFSFileInfo(
            MongoGridFS gridFS,
            BsonDocument fileInfo
        ) {
            this.gridFS = gridFS;
            CacheFileInfo(fileInfo);
        }

        public MongoGridFSFileInfo(
            MongoGridFS gridFS,
            string remoteFileName
        ) 
            : this(gridFS, remoteFileName, gridFS.Settings.DefaultChunkSize) {
        }

        public MongoGridFSFileInfo(
            MongoGridFS gridFS,
            string remoteFileName,
            int chunkSize
        ) {
            this.gridFS = gridFS;
            this.chunkSize = chunkSize;
            this.name = remoteFileName;
        }

        public MongoGridFSFileInfo(
            MongoGridFS gridFS,
            string remoteFileName,
            MongoGridFSCreateOptions createOptions
        ) {
            this.gridFS = gridFS;
            this.aliases = createOptions.Aliases;
            this.chunkSize = createOptions.ChunkSize;
            this.contentType = createOptions.ContentType;
            this.id = createOptions.Id;
            this.metadata = createOptions.Metadata;
            this.name = remoteFileName;
            this.uploadDate = createOptions.UploadDate;
            this.cached = true; // prevent values from being overwritten by automatic Refresh
        }
        #endregion

        #region public properties
        public string[] Aliases {
            get {
                if (!cached) { Refresh(); }
                return aliases;
            }
        }

        public int ChunkSize {
            get {
                if (!cached) { Refresh(); }
                return chunkSize;
            }
        }

        public string ContentType {
            get {
                if (!cached) { Refresh(); }
                return contentType;
            }
        }

        public bool Exists {
            get {
                if (!cached) { Refresh(); }
                return exists;
            }
        }

        public MongoGridFS GridFS {
            get { return gridFS; }
        }

        public BsonValue Id {
            get {
                if (!cached) { Refresh(); }
                return id;
            }
        }

        public int Length {
            get {
                if (!cached) { Refresh(); }
                return length;
            }
        }

        public string MD5 {
            get {
                if (!cached) { Refresh(); }
                return md5;
            }
        }

        public BsonDocument Metadata {
            get {
                if (!cached) { Refresh(); }
                return metadata;
            }
        }

        public string Name {
            get {
                if (!cached) { Refresh(); }
                return name;
            }
        }

        public DateTime UploadDate {
            get {
                if (!cached) { Refresh(); }
                return uploadDate;
            }
        }
        #endregion

        #region public operators
        public static bool operator !=(
            MongoGridFSFileInfo lhs,
            MongoGridFSFileInfo rhs
        ) {
            return !(lhs == rhs);
        }

        public static bool operator ==(
            MongoGridFSFileInfo lhs,
            MongoGridFSFileInfo rhs
        ) {
            return object.Equals(lhs, rhs);
        }
        #endregion

        #region public methods
        public StreamWriter AppendText() {
            Stream stream = Open(FileMode.Append, FileAccess.Write);
            return new StreamWriter(stream, Encoding.UTF8);
        }

        public MongoGridFSFileInfo CopyTo(
            string destFileName
        ) {
            throw new NotImplementedException();
        }

        public MongoGridFSFileInfo CopyTo(
            string destFileName,
            bool overwrite
        ) {
            throw new NotImplementedException();
        }

        public MongoGridFSStream Create() {
            return Open(FileMode.Create, FileAccess.ReadWrite);
        }

        public StreamWriter CreateText() {
            var stream = Create();
            return new StreamWriter(stream, Encoding.UTF8);
        }

        public void Delete() {
            if (Exists) {
                using (gridFS.Database.RequestStart()) {
                    gridFS.Chunks.EnsureIndex("files_id", "n");
                    gridFS.Files.Remove(Query.EQ("_id", id), gridFS.Settings.SafeMode);
                    gridFS.Chunks.Remove(Query.EQ("files_id", id), gridFS.Settings.SafeMode);
                }
            }
       }

        public bool Equals(
            MongoGridFSFileInfo rhs
        ) {
            if (rhs == null) { return false; }
            return
                (this.aliases == rhs.aliases && (this.aliases == null || this.aliases.SequenceEqual(rhs.aliases))) &&
                this.chunkSize == rhs.chunkSize &&
                this.contentType == rhs.contentType &&
                this.id == rhs.id &&
                this.length == rhs.length &&
                this.md5 == rhs.md5 &&
                this.metadata == rhs.metadata &&
                this.name == rhs.name &&
                this.uploadDate == rhs.uploadDate;
        }

        public override bool Equals(object obj) {
            return Equals(obj as MongoGridFSFileInfo); // works even if obj is null
        }

        public override int GetHashCode() {
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

        public void MoveTo(
            string destFileName
        ) {
            throw new NotImplementedException();
        }

        public MongoGridFSStream Open(
            FileMode mode
        ) {
            return Open(mode, FileAccess.ReadWrite);
        }

        public MongoGridFSStream Open(
            FileMode mode,
            FileAccess access
        ) {
            return new MongoGridFSStream(this, mode, access);
        }

        public MongoGridFSStream OpenRead() {
            return Open(FileMode.Open, FileAccess.Read);
        }

        public StreamReader OpenText() {
            Stream stream = Open(FileMode.Open, FileAccess.Read);
            return new StreamReader(stream, Encoding.UTF8);
        }

        public MongoGridFSStream OpenWrite() {
            return Open(FileMode.OpenOrCreate, FileAccess.Write);
        }

        public void Refresh() {
            MongoCursor<BsonDocument> cursor;
            if (id != null) {
                cursor = gridFS.Files.Find(Query.EQ("_id", id));
            } else {
                gridFS.Files.EnsureIndex("filename", "uploadDate");
                cursor = gridFS.Files.Find(Query.EQ("filename", name)).SetSortOrder(SortBy.Descending("uploadDate"));
            }
            var fileInfo = cursor.SetLimit(1).FirstOrDefault();
            CacheFileInfo(fileInfo); // fileInfo will be null if file does not exist
        }

        public void SetId(
            BsonValue id
        ) {
            if (this.id == null) {
                this.id = id;
            } else {
                throw new InvalidOperationException("FileInfo already has an Id");
            }
        }
        #endregion

        #region private methods
        private void CacheFileInfo(
            BsonDocument fileInfo
        ) {
            if (fileInfo == null) {
                // leave aliases, chunkSize, contentType, id, metadata and name alone (they might be needed to create a new file)
                exists = false;
                length = 0;
                md5 = null;
                uploadDate = default(DateTime);
            } else {
                if (fileInfo.Contains("aliases")) {
                    var list = new List<string>();
                    foreach (var alias in fileInfo["aliases"].AsBsonArray) {
                        list.Add(alias.AsString);
                    }
                    aliases = list.ToArray();
                }
                chunkSize = fileInfo["chunkSize"].ToInt32();
                contentType = (string) fileInfo["contentType", null];
                exists = true;
                id = fileInfo["_id"];
                length = fileInfo["length"].ToInt32();
                md5 = (string) fileInfo["md5", null];
                metadata = (BsonDocument) fileInfo["metadata", null];
                name = fileInfo["filename"].AsString;
                uploadDate = fileInfo["uploadDate"].AsDateTime;
            }
            cached = true;
        }
        #endregion
    }
}
