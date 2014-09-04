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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkInsertOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // constructors
        public BulkInsertOperationEmulator(
            CollectionNamespace collectionNamespace,
            IEnumerable<InsertRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        //  methods
        protected override IWireProtocol<WriteConcernResult> CreateProtocol(IConnectionHandle connection, WriteRequest request)
        {
            var insertRequest = (InsertRequest)request;
            var documentSource = new BatchableSource<BsonDocument>(new[] { insertRequest.Document });

            return new InsertWireProtocol<BsonDocument>(
                CollectionNamespace,
                WriteConcern,
                BsonDocumentSerializer.Instance,
                MessageEncoderSettings,
                documentSource,
                connection.Description.MaxBatchCount,
                connection.Description.MaxMessageSize,
                continueOnError: false);               
        }
    }
}
