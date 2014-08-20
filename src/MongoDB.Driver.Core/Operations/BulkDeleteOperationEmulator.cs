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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkDeleteOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // constructors
        public BulkDeleteOperationEmulator(
            string databaseName,
            string collectionName,
            IEnumerable<DeleteRequest> requests)
            : base(databaseName, collectionName, requests)
        {
        }

        // methods
        protected override IWireProtocol<BsonDocument> CreateProtocol(IConnectionHandle connection, WriteRequest request)
        {
            var deleteRequest = (DeleteRequest)request;
            return new DeleteWireProtocol(
               DatabaseName,
               CollectionName,
               WriteConcern,
               deleteRequest.Query,
               isMulti: false);
        }
    }
}
