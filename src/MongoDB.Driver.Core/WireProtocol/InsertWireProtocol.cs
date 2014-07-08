/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class InsertWireProtocol<TDocument> : WriteWireProtocolBase
    {
        // fields
        private readonly Batch<TDocument> _batch;
        private readonly bool _continueOnError;
        private readonly int? _maxBatchCount;
        private readonly int? _maxMessageSize;
        private readonly IBsonSerializer<TDocument> _serializer;

        // constructors
        public InsertWireProtocol(
            string databaseName,
            string collectionName,
            WriteConcern writeConcern,
            IBsonSerializer<TDocument> serializer,
            Batch<TDocument> batch,
            int? maxBatchCount,
            int? maxMessageSize,
            bool continueOnError)
            : base(databaseName, collectionName, writeConcern)
        {
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _batch = Ensure.IsNotNull(batch, "batch");
            _maxBatchCount = Ensure.IsNullOrGreaterThanOrEqualToZero(maxBatchCount, "maxBatchCount");
            _maxMessageSize = Ensure.IsNullOrGreaterThanOrEqualToZero(maxMessageSize, "maxMessageSize");
            _continueOnError = continueOnError;
        }

        // methods
        protected override RequestMessage CreateWriteMessage(IConnection connection)
        {
            return new InsertMessage<TDocument>(
                RequestMessage.GetNextRequestId(),
                DatabaseName,
                CollectionName,
                _serializer,
                _batch,
                _maxBatchCount ?? int.MaxValue,
                _maxMessageSize ?? connection.Description.MaxMessageSize,
                _continueOnError);
        }
    }
}
