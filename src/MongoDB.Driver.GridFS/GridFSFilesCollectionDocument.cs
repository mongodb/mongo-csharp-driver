/* Copyright 2015 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents a GridFS files collection document.
    /// </summary>
    public class GridFSFilesCollectionDocument
    {
        // fields
        private readonly IList<string> _aliases;
        private readonly int _chunkSizeBytes;
        private readonly string _contentType;
        private readonly BsonDocument _extraElements;
        private readonly string _filename;
        private readonly BsonValue _id;
        private readonly long _length;
        private readonly string _md5;
        private readonly BsonDocument _metadata;
        private readonly DateTime _uploadDateTime;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GridFSFilesCollectionDocument" /> class.
        /// </summary>
        /// <param name="aliases">The aliases.</param>
        /// <param name="chunkSizeBytes">Size of the chunk.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="extraElements">The extra elements.</param>
        /// <param name="idAsBsonValue">The identifier.</param>
        /// <param name="length">The length.</param>
        /// <param name="md5">The MD5.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="uploadDateTime">The upload date time.</param>
        [BsonConstructor]
        public GridFSFilesCollectionDocument(
            IEnumerable<string> aliases,
            int chunkSizeBytes,
            string contentType,
            BsonDocument extraElements,
            BsonValue idAsBsonValue,
            long length,
            string md5,
            BsonDocument metadata,
            DateTime uploadDateTime)
        {
            _aliases = aliases == null ? null : ((aliases as IList<string>) ?? aliases.ToList()); // can be null
            _chunkSizeBytes = chunkSizeBytes;
            _contentType = contentType; // can be null
            _extraElements = extraElements;
            _filename = Filename;
            _id = idAsBsonValue;
            _length = length;
            _md5 = md5;
            _metadata = metadata;
            _uploadDateTime = uploadDateTime;
        }

        // properties
        /// <summary>
        /// Gets the aliases.
        /// </summary>
        /// <value>
        /// The aliases.
        /// </value>
        [BsonElement("aliases")]
        [BsonDefaultValue(null)]
        [BsonIgnoreIfDefault]
        [Obsolete("Place aliases inside metadata instead.")]
        public IEnumerable<string> Aliases
        {
            get { return _aliases; }
        }

        /// <summary>
        /// Gets the size of a chunk.
        /// </summary>
        /// <value>
        /// The size of a chunk.
        /// </value>
        [BsonElement("chunkSize")]
        public int ChunkSizeBytes
        {
            get { return _chunkSizeBytes; }
        }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        [BsonElement("contentType")]
        [BsonDefaultValue(null)]
        [BsonIgnoreIfDefault]
        [Obsolete("Place contentType inside metadata instead.")]
        public string ContentType
        {
            get { return _contentType; }
        }

        /// <summary>
        /// Gets the extra elements.
        /// </summary>
        /// <value>
        /// The extra elements.
        /// </value>
        [BsonExtraElements]
        [BsonDefaultValue(null)]
        public BsonDocument ExtraElements
        {
            get { return _extraElements; }
        }

        /// <summary>
        /// Gets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        [BsonElement("filename")]
        public string Filename
        {
            get { return _filename; }
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [BsonIgnore]
        public ObjectId Id
        {
            get { return _id.AsObjectId; }
        }

        /// <summary>
        /// Gets the identifier as a BsonValue.
        /// </summary>
        /// <value>
        /// The identifier as a BsonValue.
        /// </value>
        [BsonId]
        [Obsolete("All new GridFS files should use an ObjectId as the Id.")]
        public BsonValue IdAsBsonValue
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        [BsonElement("length")]
        public long Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Gets the MD5 checksum.
        /// </summary>
        /// <value>
        /// The MD5 checksum.
        /// </value>
        [BsonElement("md5")]
        public string MD5
        {
            get { return _md5; }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        [BsonElement("metadata")]
        [BsonDefaultValue(null)]
        [BsonIgnoreIfDefault]
        public BsonDocument Metadata
        {
            get { return _metadata; }
        }

        /// <summary>
        /// Gets the upload date time.
        /// </summary>
        /// <value>
        /// The upload date time.
        /// </value>
        [BsonElement("uploadDate")]
        public DateTime UploadDateTime
        {
            get { return _uploadDateTime; }
        }
    }
}
