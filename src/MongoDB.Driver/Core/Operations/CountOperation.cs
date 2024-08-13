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
using System.Threading;
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

        public BsonDocument CreateCommand(ConnectionDescription connectionDescription, ICoreSession session)
        {
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            return new BsonDocument
            {
                { "count", _collectionNamespace.CollectionName },
                { "query", _filter, _filter != null },
                { "limit", () => _limit.Value, _limit.HasValue },
                { "skip", () => _skip.Value, _skip.HasValue },
                { "hint", _hint, _hint != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "comment", _comment, _comment != null },
                { "readConcern", readConcern, readConcern != null }
            };
        }

        public long Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(binding, _retryRequested, cancellationToken))
            {
                return Execute(context, cancellationToken);
            }
        }

        public long Execute(RetryableReadContext context, CancellationToken cancellationToken)
        {
            var operation = CreateOperation(context);
            var document = operation.Execute(context, cancellationToken);
            return document["n"].ToInt64();
        }

        public async Task<long> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(binding, _retryRequested, cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<long> ExecuteAsync(RetryableReadContext context, CancellationToken cancellationToken)
        {
            var operation = CreateOperation(context);
            var document = await operation.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            return document["n"].ToInt64();
        }

        private IDisposable BeginOperation() => EventContext.BeginOperation("count");

        private ReadCommandOperation<BsonDocument> CreateOperation(RetryableReadContext context)
        {
            var command = CreateCommand(context.Channel.ConnectionDescription, context.Binding.Session);
            return new ReadCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
        }
    }
}
