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
using MongoDB.Driver.Builders;

namespace MongoDB.Driver.GridFS {
    public class MongoGridFSCreateOptions {
        #region private fields
        private string[] aliases;
        private int chunkSize;
        private string contentType;
        private BsonValue id; // usually a BsonObjectId but not required to be
        private BsonDocument metadata;
        private DateTime uploadDate;
        #endregion

        #region constructors
        public MongoGridFSCreateOptions() {
        }
        #endregion

        #region public properties
        public string[] Aliases {
            get { return aliases; }
            set { aliases = value; }
        }

        public int ChunkSize {
            get { return chunkSize; }
            set { chunkSize = value; }
        }

        public string ContentType {
            get { return contentType; }
            set { contentType = value; }
        }

        public BsonValue Id {
            get { return id; }
            set { id = value; }
        }

        public BsonDocument Metadata {
            get { return metadata; }
            set { metadata = value; }
        }

        public DateTime UploadDate {
            get { return uploadDate; }
            set { uploadDate = value; }
        }
        #endregion
    }
}
