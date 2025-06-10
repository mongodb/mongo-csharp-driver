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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class DropIndexOperation : IWriteOperation<BsonDocument>
    {
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private readonly string _indexName;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private WriteConcern _writeConcern;

        public DropIndexOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument keys,
            MessageEncoderSettings messageEncoderSettings)
            : this(collectionNamespace, IndexNameHelper.GetIndexName(keys), messageEncoderSettings)
        {
        }

        public DropIndexOperation(
            CollectionNamespace collectionNamespace,
            string indexName,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _indexName = Ensure.IsNotNullOrEmpty(indexName, nameof(indexName));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public string IndexName
        {
            get { return _indexName; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public BsonDocument CreateCommand(ICoreSessionHandle session)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(session, _writeConcern);
            return new BsonDocument
            {
                { "dropIndexes", _collectionNamespace.CollectionName },
                { "index", _indexName },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue },
                { "writeConcern", writeConcern, writeConcern != null },
                { "comment", _comment, _comment != null }
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
                var operation = CreateOperation(channelBinding.Session);
                BsonDocument result;
                try
                {
                    result = operation.Execute(operationContext, channelBinding);
                }
                catch (MongoCommandException ex)
                {
                    if (!ShouldIgnoreException(ex))
                    {
                        throw;
                    }
                    result = ex.Result;
                }
                return result;
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
                var operation = CreateOperation(channelBinding.Session);
                BsonDocument result;
                try
                {
                    result = await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
                }
                catch (MongoCommandException ex)
                {
                    if (!ShouldIgnoreException(ex))
                    {
                        throw;
                    }
                    result = ex.Result;
                }
                return result;
            }
        }

        private IDisposable BeginOperation() => EventContext.BeginOperation("dropIndexes");

        private WriteCommandOperation<BsonDocument> CreateOperation(ICoreSessionHandle session)
        {
            var command = CreateCommand(session);
            return new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }

        private bool ShouldIgnoreException(MongoCommandException ex)
        {
            return
                ex.Code == (int)ServerErrorCode.NamespaceNotFound ||
                ex.ErrorMessage != null && ex.ErrorMessage.Contains("ns not found");
        }
    }
}
