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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class BulkInsertOperationArgs : BulkWriteOperationArgs
    {
        // private fields
        private Action<InsertRequest> _assignId;
        private readonly bool _checkElementNames;

        // constructors
        public BulkInsertOperationArgs(
            Action<InsertRequest> assignId,
            bool checkElementNames,
            string collectionName,
            string databaseName,
            int maxBatchCount,
            int maxBatchLength,
            bool isOrdered,
            BsonBinaryReaderSettings readerSettings,
            IEnumerable<InsertRequest> requests,
            WriteConcern writeConcern,
            BsonBinaryWriterSettings writerSettings)
            : base(
                collectionName,
                databaseName,
                maxBatchCount,
                maxBatchLength,
                isOrdered,
                readerSettings,
                requests.Cast<WriteRequest>(),
                writeConcern,
                writerSettings)
        {
            _assignId = assignId;
            _checkElementNames = checkElementNames;
        }

        // public properties
        public Action<InsertRequest> AssignId
        {
            get { return _assignId; }
        }

        public bool CheckElementNames
        {
            get { return _checkElementNames; }
        }
    }
}
