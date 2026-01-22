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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class CountOperation : IReadOperation<long>, IExecutableInRetryableReadContext<long>
    {
        private Collation _collation;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private BsonDocument _filter;
        private BsonValue _hint;
        private long? _limit;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private ReadConcern _readConcern = ReadConcern.Default;
        private bool _retryRequested;
        private long? _skip;

        public CountOperation(CollectionNamespace collectionNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
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

        public BsonDocument Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        public long? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "count";

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public long? Skip
        {
            get { return _skip; }
            set { _skip = value; }
        }

        public BsonDocument CreateCommand(OperationContext operationContext, ICoreSession session, ConnectionDescription connectionDescription)
        {
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            return new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "query", _filter, _filter != null },
                { "limit", () => _limit.Value, _limit.HasValue },
                { "skip", () => _skip.Value, _skip.HasValue },
                { "hint", _hint, _hint != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "comment", _comment, _comment != null },
                { "readConcern", readConcern, readConcern != null }
            };
        }

        public long Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                return Execute(operationContext, context);
            }
        }

        public long Execute(OperationContext operationContext, RetryableReadContext context)
        {
            var operation = CreateOperation(operationContext, context);
            var document = operation.Execute(operationContext, context);
            return document["n"].ToInt64();
        }

        public async Task<long> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        public async Task<long> ExecuteAsync(OperationContext operationContext, RetryableReadContext context)
        {
            var operation = CreateOperation(operationContext, context);
            var document = await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);
            return document["n"].ToInt64();
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation(OperationName);

        private ReadCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, RetryableReadContext context)
        {
            var command = CreateCommand(operationContext, context.Binding.Session, context.Channel.ConnectionDescription);
            return new ReadCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
        }
    }
}
