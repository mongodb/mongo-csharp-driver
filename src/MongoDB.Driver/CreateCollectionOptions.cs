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
        private object _storageOptions;
        private bool? _usePowerOf2Sizes;

        // properties
        /// <summary>
        /// Gets or sets the automatic index identifier.
        /// </summary>
        public bool? AutoIndexId
        {
            get { return _autoIndexId; }
            set { _autoIndexId = value; }
        }

        /// <summary>
        /// Gets or sets the capped.
        /// </summary>
        public bool? Capped
        {
            get { return _capped; }
            set { _capped = value; }
        }

        /// <summary>
        /// Gets or sets the maximum documents.
        /// </summary>
        public long? MaxDocuments
        {
            get { return _maxDocuments; }
            set { _maxDocuments = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size.
        /// </summary>
        public long? MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = value; }
        }

        /// <summary>
        /// Gets or sets the storage options.
        /// </summary>
        public object StorageOptions
        {
            get { return _storageOptions; }
            set { _storageOptions = value; }
        }

        /// <summary>
        /// Gets or sets the use power of2 sizes.
        /// </summary>
        public bool? UsePowerOf2Sizes
        {
            get { return _usePowerOf2Sizes; }
            set { _usePowerOf2Sizes = value; }
        }
    }
}
