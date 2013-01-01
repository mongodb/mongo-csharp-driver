/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents options used when creating a GridFS file.
    /// </summary>
    public class MongoGridFSCreateOptions
    {
        // private fields
        private string[] _aliases;
        private int _chunkSize;
        private string _contentType;
        private BsonValue _id; // usually a BsonObjectId but not required to be
        private BsonDocument _metadata;
        private DateTime _uploadDate;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoGridFSCreateOptions class.
        /// </summary>
        public MongoGridFSCreateOptions()
        {
        }

        // public properties
        /// <summary>
        /// Gets or sets the aliases.
        /// </summary>
        public string[] Aliases
        {
            get { return _aliases; }
            set { _aliases = value; }
        }

        /// <summary>
        /// Gets or sets the chunk size.
        /// </summary>
        public int ChunkSize
        {
            get { return _chunkSize; }
            set { _chunkSize = value; }
        }

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        public string ContentType
        {
            get { return _contentType; }
            set { _contentType = value; }
        }

        /// <summary>
        /// Gets or sets the file Id.
        /// </summary>
        public BsonValue Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public BsonDocument Metadata
        {
            get { return _metadata; }
            set { _metadata = value; }
        }

        /// <summary>
        /// Gets or sets the upload date.
        /// </summary>
        public DateTime UploadDate
        {
            get { return _uploadDate; }
            set { _uploadDate = value; }
        }
    }
}
