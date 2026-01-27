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
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a create search indexes operation.
    /// </summary>
    internal sealed class CreateSearchIndexesOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IEnumerable<CreateSearchIndexRequest> _requests;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateIndexesOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="requests">The requests.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public CreateSearchIndexesOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<CreateSearchIndexRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _requests = Ensure.IsNotNull(requests, nameof(requests)).ToArray();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        // public properties
        public string OperationName => "createSearchIndexes";

        // public methods
        /// <inheritdoc/>
        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            using (EventContext.BeginOperation(OperationName))
            using (var channelSource = binding.GetWriteChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation();
                return operation.Execute(operationContext, channelBinding);
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            using (EventContext.BeginOperation(OperationName))
            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation();
                return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
            }
        }

        // private methods
        private WriteCommandOperation<BsonDocument> CreateOperation()
        {
            var command = new BsonDocument()
            {
                { "createSearchIndexes", _collectionNamespace.CollectionName },
                { "indexes", new BsonArray(_requests.Select(request => request.CreateIndexDocument())) }
            };

            return new(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }
    }
}
