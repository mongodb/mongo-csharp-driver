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
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient {
    // this class is patterned after .NET's FileInfo
    public class MongoGridFSFileInfo {
        #region private fields
        private int chunkSize;
        private BsonObjectId id;
        private int length;
        private string md5;
        private string name;
        private DateTime uploadDate;
        #endregion

        #region constructors
        public MongoGridFSFileInfo(
            BsonDocument fileInfo
        ) {
            chunkSize = fileInfo.GetInt32("chunkSize");
            id = fileInfo.GetObjectId("_id");
            length = fileInfo.GetInt32("length");
            md5 = fileInfo.GetString("md5");
            name = fileInfo.GetString("filename");
            uploadDate = fileInfo.GetDateTime("uploadDate");
        }
        #endregion

        #region public properties
        public int ChunkSize {
            get { return chunkSize; }
            set { chunkSize = value; }
        }

        public BsonObjectId Id {
            get { return id; }
            set { id = value; }
        }

        public int Length {
            get { return length; }
            set { length = value; }
        }

        public string MD5 {
            get { return md5; }
            set { md5 = value; }
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public DateTime UploadDate {
            get { return uploadDate; }
            set { uploadDate = value; }
        }
        #endregion

        #region public methods
        #endregion
    }
}
