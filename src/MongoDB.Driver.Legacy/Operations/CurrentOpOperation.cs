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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Operations
{
    internal class CurrentOpOperation : IReadOperation<BsonDocument>
    {
        // private fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        public CurrentOpOperation(DatabaseNamespace databaseNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public properties
        public DatabaseNamespace DatabaseNamespace => _databaseNamespace;

        public MessageEncoderSettings MessageEncoderSettings => _messageEncoderSettings;

        // public methods
        public BsonDocument Execute(IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var channelSource = binding.GetReadChannelSource(cancellationToken))
            using (var channel = channelSource.GetChannel(cancellationToken))
            using (var channelBinding = new ChannelReadBinding(channelSource.Server, channel, binding.ReadPreference))
            {
                var operation = CreateOperation(channel.ConnectionDescription.ServerVersion);
                return operation.Execute(channelBinding, cancellationToken);
            }
        }

        public Task<BsonDocument> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotSupportedException();
        }

        // private methods
        internal IReadOperation<BsonDocument> CreateOperation(SemanticVersion serverVersion)
        {
            if (Feature.CurrentOpCommand.IsSupported(serverVersion))
            {
                return new CurrentOpUsingCommandOperation(_databaseNamespace, _messageEncoderSettings);
            }
            else
            {
                return new CurrentOpUsingFindOperation(_databaseNamespace, _messageEncoderSettings);
            }
        }
    }
}
