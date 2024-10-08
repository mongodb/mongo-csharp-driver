﻿/* Copyright 2015-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.GridFS
{
    /// <summary>
    /// Represents options for a GridFS upload operation.
    /// </summary>
    public class GridFSUploadOptions
    {
        // fields
        private int? _batchSize;
        private int? _chunkSizeBytes;
        private BsonDocument _metadata;

        /// <summary>
        /// Gets or sets the batch size.
        /// </summary>
        /// <value>
        /// The batch size.
        /// </value>
        public int? BatchSize
        {
            get { return _batchSize; }
            set
            {
                _batchSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the chunk size in bytes.
        /// </summary>
        /// <value>
        /// The chunk size in bytes.
        /// </value>
        public int? ChunkSizeBytes
        {
            get { return _chunkSizeBytes; }
            set { _chunkSizeBytes = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        public BsonDocument Metadata
        {
            get { return _metadata; }
            set { _metadata = value; }
        }
    }
}
