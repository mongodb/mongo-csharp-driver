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
        private string[] aliases;
        private bool cached; // true if info came from database
        private int chunkSize;
        private string contentType;
        private bool exists;
        private MongoGridFS gridFS;
        private BsonValue id; // usually a BsonObjectId but not required to be
        private int length;
        private string md5;
        private BsonDocument metadata;
        private string name;
        private DateTime uploadDate;
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
            this.id = BsonObjectId.GenerateNewId();
            this.name = remoteFileName;
        }
        #endregion

        #region public properties
        public string[] Aliases {
            get { return aliases; }
            set { aliases = value; }
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
            set {
                contentType = value;
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
            set {
                metadata = value;
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
                var database = gridFS.Database;
                var settings = gridFS.Settings;
                var files = database.GetCollection(settings.FilesCollectionName);
                var chunks = database.GetCollection(settings.ChunksCollectionName);
                using (database.RequestStart()) {
                    files.Remove(new BsonDocument("_id", id), gridFS.SafeMode);
                    chunks.Remove(new BsonDocument("files_id", id), gridFS.SafeMode);
                }
            }
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
            var files = gridFS.Database.GetCollection(gridFS.Settings.FilesCollectionName);
            MongoCursor<BsonDocument> cursor;
            if (id != null) {
                var query = new BsonDocument("_id", id);
                cursor = files.Find(query);
            } else {
                var query = new BsonDocument("filename", name);
                cursor = files.Find(query).SetSortOrder(SortBy.Descending("uploadDate"));
            }
            var fileInfo = cursor.SetLimit(1).FirstOrDefault();
            CacheFileInfo(fileInfo); // fileInfo will be null if file does not exist
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
                md5 = fileInfo["md5"].AsString;
                metadata = (BsonDocument) fileInfo["metadata", null];
                name = fileInfo["filename"].AsString;
                uploadDate = fileInfo["uploadDate"].AsDateTime;
            }
            cached = true;
        }
        #endregion
    }
}
