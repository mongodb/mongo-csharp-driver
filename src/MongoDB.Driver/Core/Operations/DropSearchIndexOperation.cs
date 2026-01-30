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
    /// Represents a drop index operation.
    /// </summary>
    internal sealed class DropSearchIndexOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly string _indexName;
        private readonly MessageEncoderSettings _messageEncoderSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DropIndexOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public DropSearchIndexOperation(
            CollectionNamespace collectionNamespace,
            string indexName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _indexName = Ensure.IsNotNullOrEmpty(indexName, nameof(indexName));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        // public properties
        public string OperationName => "dropSearchIndex";

        // methods
        private BsonDocument CreateCommand() =>
            new()
            {
                { "dropSearchIndex", _collectionNamespace.CollectionName },
                { "name", _indexName },
            };

        private WriteCommandOperation<BsonDocument> CreateOperation() =>
            new(_collectionNamespace.DatabaseNamespace, CreateCommand(), BsonDocumentSerializer.Instance, _messageEncoderSettings);

        /// <inheritdoc/>
        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (EventContext.BeginOperation(OperationName))
            using (var channelSource = binding.GetWriteChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation();

                try
                {
                    return operation.Execute(operationContext, channelBinding);
                }
                catch (MongoCommandException ex) when (ShouldIgnoreException(ex))
                {
                    return ex.Result;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (EventContext.BeginOperation(OperationName))
            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation();

                try
                {
                    return await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
                }
                catch (MongoCommandException ex) when (ShouldIgnoreException(ex))
                {
                    return ex.Result;
                }
            }
        }

        private bool ShouldIgnoreException(MongoCommandException ex) =>
            ex?.Code == (int)ServerErrorCode.NamespaceNotFound ||
            ex?.ErrorMessage == "ns not found";
    }
}
