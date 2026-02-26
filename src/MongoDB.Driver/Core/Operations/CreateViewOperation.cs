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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class CreateViewOperation : IWriteOperation<BsonDocument>
    {
        private Collation _collation;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private readonly string _viewName;
        private readonly string _viewOn;
        private WriteConcern _writeConcern;

        public CreateViewOperation(
            DatabaseNamespace databaseNamespace,
            string viewName,
            string viewOn,
            IEnumerable<BsonDocument> pipeline,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _viewName = Ensure.IsNotNull(viewName, nameof(viewName));
            _viewOn = Ensure.IsNotNull(viewOn, nameof(viewOn));
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline)).ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "createView";

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        public string ViewName
        {
            get { return _viewName; }
        }

        public string ViewOn
        {
            get { return _viewOn; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = binding.GetWriteChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription);
                return operation.Execute(operationContext, channelBinding);
            }
        }

        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session, channel.ConnectionDescription);
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, _writeConcern);
            return new BsonDocument
            {
                { "create", _viewName },
                { "viewOn", _viewOn },
                { "pipeline", new BsonArray(_pipeline) },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "writeConcern", writeConcern, writeConcern != null }
            };
        }

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session, ConnectionDescription connectionDescription)
        {
            var command = CreateCommand(operationContext, session, connectionDescription);
            return new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings, OperationName);
        }
    }
}
