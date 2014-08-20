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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkInsertOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // fields
        private Action<InsertRequest> _assignId;
        private bool _checkElementNames = true;

        // constructors
        public BulkInsertOperationEmulator(
            string databaseName,
            string collectionName,
            IEnumerable<InsertRequest> requests)
            : base(databaseName, collectionName, requests)
        {
        }

        // properties
        public Action<InsertRequest> AssignId
        {
            get { return _assignId; }
            set { _assignId = value; }
        }

        public bool CheckElementNames
        {
            get { return _checkElementNames; }
            set { _checkElementNames = value; }
        }

        //  methods
        protected override IWireProtocol<BsonDocument> CreateProtocol(IConnectionHandle connection, WriteRequest request)
        {
            var insertRequest = (InsertRequest)request;
            var wrapper = new BsonDocumentWrapper(insertRequest.Document, insertRequest.Serializer);
            var documentSource = new BatchableSource<BsonDocument>(new[] { wrapper });
            return new InsertWireProtocol<BsonDocument>(
                DatabaseName,
                CollectionName,
                WriteConcern,
                BsonDocumentSerializer.Instance,
                documentSource,
                connection.Description.MaxBatchCount,
                connection.Description.MaxMessageSize,
                continueOnError: false);               
        }
    }
}
