/* Copyright 2010-2014 MongoDB Inc.
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

using MongoDB.Bson;
namespace MongoDB.Driver
{
    /// <summary>
    /// Options for creating a collection.
    /// </summary>
    public class CreateCollectionOptions
    {
        // fields
        private bool? _autoIndexId;
        private bool? _capped;
        private long? _maxDocuments;
        private long? _maxSize;
        private BsonDocument _storageEngine;
        private bool? _usePowerOf2Sizes;

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether to automatically create an index on the _id.
        /// </summary>
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
            set { _autoIndexId = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the collection is capped.
        /// </summary>
        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of documents (used with capped collections).
        /// </summary>
        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size of the collection (used with capped collections).
        /// </summary>
        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

        /// <summary>
        /// Gets or sets the storage engine options.
        /// </summary>
        public BsonDocument StorageEngine
        {
            get { return _storageEngine; }
            set { _storageEngine = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use power of 2 sizes.
        /// </summary>
        public bool? UsePowerOf2Sizes
        {
            get { return _usePowerOf2Sizes; }
            set { _usePowerOf2Sizes = value; }
        }
    }
}
