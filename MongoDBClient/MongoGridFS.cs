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
using System.Linq;
using System.IO;
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient {
    public class MongoGridFS {
        #region private fields
        private MongoDatabase database;
        private MongoGridFSSettings settings;
        #endregion

        #region constructors
        public MongoGridFS(
            MongoDatabase database,
            MongoGridFSSettings settings
        ) {
            this.database = database;
            this.settings = settings;
        }
        #endregion

        #region public properties
        public MongoGridFSSettings Settings {
            get { return settings; }
            set { settings = value; }
        }
        #endregion

        #region public methods
        public void Delete(
            BsonDocument query
        ) {
            var files = database.GetCollection(settings.CollectionName + ".files");
            var chunks = database.GetCollection(settings.CollectionName + ".chunks");

            var fileIds = files.Find<BsonDocument>(query).Select(f => f.GetObjectId("_id"));
            foreach (var fileId in fileIds) {
                var fileQuery = new BsonDocument("_id", fileId);
                files.Remove(fileQuery, true);
                var chunksQuery = new BsonDocument("files_id", fileId);
                chunks.Remove(chunksQuery, true);
            }
        }

        public void Delete(
            string remoteFileName
        ) {
            var query = new BsonDocument("filename", remoteFileName);
            Delete(query);
        }

        public void Download(
            string fileName
        ) {
            Download(fileName, fileName);
        }

        public void Download(
            string localFileName,
            BsonDocument remoteFileQuery
        ) {
            throw new NotImplementedException();
        }

        public void Download(
            string localFileName,
            string remoteFileName
        ) {
            throw new NotImplementedException();
        }

        public List<MongoGridFSFileInfo> Find() {
            return Find((BsonDocument) null);
        }

        public List<MongoGridFSFileInfo> Find(
            BsonDocument query
        ) {
            var fileInfos = new List<MongoGridFSFileInfo>();
            var files = database.GetCollection(settings.CollectionName + ".files");
            using (var cursor = files.Find<BsonDocument>(query)) {
                foreach (var file in cursor) {
                    var fileInfo = new MongoGridFSFileInfo(file);
                    fileInfos.Add(fileInfo);
                }
            }
            return fileInfos;
        }

        public List<MongoGridFSFileInfo> Find(
            string fileName
        ) {
            var query = new BsonDocument("filename", fileName);
            return Find(query);
        }

        public MongoGridFSFileInfo FindOne(
            BsonDocument query
        ) {
            var files = database.GetCollection(settings.CollectionName + ".files");
            var fileInfo = files.FindOne<BsonDocument>(query);
            return new MongoGridFSFileInfo(fileInfo);
        }

        public MongoGridFSFileInfo FindOne(
            BsonObjectId objectId
        ) {
            var query = new BsonDocument("_id", objectId);
            return FindOne(query);
        }

        public MongoGridFSFileInfo Upload(
            Stream stream,
            string remoteFileName
        ) {
            MongoCollection files = database.GetCollection(settings.CollectionName + ".files");
            MongoCollection chunks = database.GetCollection(settings.CollectionName + ".chunks");
            chunks.EnsureIndex("files_id", "n");

            BsonObjectId files_id = BsonObjectId.GenerateNewId();
            var chunkSize = settings.DefaultChunkSize;
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
                    { "n", n },
                    { "data", new BsonBinaryData(data) },
                    { "files_id", files_id }
                };
                chunks.Insert(chunk, true);

                if (bytesRead < chunkSize) {
                    break;
                }
            }

            var md5Command = new BsonDocument {
                { "filemd5", files_id },
                { "root", settings.CollectionName }
            };
            var md5Result = database.RunCommand(md5Command);
            var md5 = md5Result.GetString("md5");

            BsonDocument fileInfo = new BsonDocument {
                { "_id", files_id },
                { "filename", remoteFileName },
                { "length", length },
                { "chunkSize", chunkSize },
                { "uploadDate", DateTime.UtcNow },
                { "md5", md5 }
            };
            files.Insert(fileInfo, true);

            return FindOne(files_id);
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
            Stream stream = File.OpenRead(localFileName);
            return Upload(stream, remoteFileName);
        }
        #endregion
    }
}
