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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal abstract class BulkWriteOperationArgs
    {
        // private fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly bool _isOrdered;
        private readonly int _maxBatchCount;
        private readonly int _maxBatchLength;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly IEnumerable<WriteRequest> _requests;
        private readonly WriteConcern _writeConcern;
        private readonly BsonBinaryWriterSettings _writerSettings;

        // constructors
        protected BulkWriteOperationArgs(
            string collectionName,
            string databaseName,
            int maxBatchCount,
            int maxBatchLength,
            bool isOrdered,
            BsonBinaryReaderSettings readerSettings,
            IEnumerable<WriteRequest> requests,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _maxBatchCount = maxBatchCount;
            _maxBatchLength = maxBatchLength;
            _isOrdered = isOrdered;
            _readerSettings = readerSettings;
            _requests = requests;
            _writeConcern = writeConcern;
            _writerSettings = writerSettings;
        }

        // public properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        public int MaxBatchLength
        {
            get { return _maxBatchLength; }
        }

        public bool IsOrdered
        {
            get { return _isOrdered; }
        }

        public BsonBinaryReaderSettings ReaderSettings
        {
            get { return _readerSettings; }
        }

        public IEnumerable<WriteRequest> Requests
        {
            get { return _requests; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
        }
    }
}
