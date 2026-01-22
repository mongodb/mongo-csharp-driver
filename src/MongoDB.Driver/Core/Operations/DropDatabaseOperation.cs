/* Copyright 2010-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class DropDatabaseOperation : IWriteOperation<BsonDocument>
    {
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private WriteConcern _writeConcern;

        public DropDatabaseOperation(
            DatabaseNamespace databaseNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _messageEncoderSettings = messageEncoderSettings;
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "dropDatabase";

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, _writeConcern);
            return new BsonDocument
            {
                { "dropDatabase", 1 },
                { "writeConcern", writeConcern, writeConcern != null }
            };
        }

        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session);
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session);
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation(OperationName);

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session)
        {
            var command = CreateCommand(operationContext, session);
            return new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }
    }
}
