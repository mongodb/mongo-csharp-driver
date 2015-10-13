/* Copyright 2015 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Operations
{
    internal class CurrentOpUsingCommandOperation : IReadOperation<BsonDocument>
    {
        // private fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public CurrentOpUsingCommandOperation(DatabaseNamespace databaseNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public properties
        public DatabaseNamespace DatabaseNamespace => _databaseNamespace;

        public MessageEncoderSettings MessageEncoderSettings => _messageEncoderSettings;

        // public methods
        public BsonDocument Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            var operation = CreateOperation();
            return operation.Execute(binding, cancellationToken);
        }

        public Task<BsonDocument> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        // private methods
        internal ReadCommandOperation<BsonDocument> CreateOperation()
        {
            var command = new BsonDocument("currentOp", 1);
            return new ReadCommandOperation<BsonDocument>(
                _databaseNamespace,
                command,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);
        }
    }
}
