/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class ListDatabasesOperation : IReadOperation<IAsyncCursor<BsonDocument>>
    {
        private bool? _authorizedDatabases;
        private BsonValue _comment;
        private BsonDocument _filter;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool? _nameOnly;
        private bool _retryRequested;

        public ListDatabasesOperation(MessageEncoderSettings messageEncoderSettings)
        {
            _messageEncoderSettings = messageEncoderSettings;
        }

        public bool? AuthorizedDatabases
        {
            get { return _authorizedDatabases; }
            set { _authorizedDatabases = value; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public BsonDocument Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "listDatabases";

        public bool? NameOnly
        {
            get { return _nameOnly; }
            set { _nameOnly = value; }
        }

        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "listDatabases", 1 },
                { "filter", _filter, _filter != null },
                { "nameOnly", _nameOnly, _nameOnly != null },
                { "authorizedDatabases", _authorizedDatabases, _authorizedDatabases != null },
                { "comment", _comment, _comment != null }
            };
        }

        public IAsyncCursor<BsonDocument> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            {
                var operation = CreateOperation();
                var reply = operation.Execute(operationContext, binding);
                return CreateCursor(reply);
            }
        }

        public async Task<IAsyncCursor<BsonDocument>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            {
                var operation = CreateOperation();
                var reply = await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
                return CreateCursor(reply);
            }
        }

        private EventContext.OperationIdDisposer BeginOperation() => EventContext.BeginOperation(null, "listDatabases");

        private IAsyncCursor<BsonDocument> CreateCursor(BsonDocument reply)
        {
            var databases = reply["databases"].AsBsonArray.OfType<BsonDocument>();
            return new SingleBatchAsyncCursor<BsonDocument>(databases.ToList());
        }

        private ReadCommandOperation<BsonDocument> CreateOperation()
        {
            var command = CreateCommand();
            return new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                RetryRequested = _retryRequested
            };
        }
    }
}
