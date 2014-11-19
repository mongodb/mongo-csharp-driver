/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a Find operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
    public class FindOperation<TDocument> : IReadOperation<IAsyncCursor<TDocument>>
    {
        // fields
        private bool _awaitData;
        private int? _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private string _comment;
        private BsonDocument _filter;
        private int? _limit;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _modifiers;
        private bool _noCursorTimeout;
        private bool _partial;
        private BsonDocument _projection;
        private readonly IBsonSerializer<TDocument> _resultSerializer;
        private int? _skip;
        private BsonDocument _sort;
        private bool _tailable;

        // constructors
        public FindOperation(
            CollectionNamespace collectionNamespace,
            IBsonSerializer<TDocument> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "serializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");

            _awaitData = true;
        }

        // properties
        public bool AwaitData
        {
            get { return _awaitData; }
            set { _awaitData = value; }
        }

        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public BsonDocument Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public BsonDocument Modifiers
        {
            get { return _modifiers; }
            set { _modifiers = value; }
        }

        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }

        public bool Partial
        {
            get { return _partial; }
            set { _partial = value; }
        }

        public BsonDocument Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        public IBsonSerializer<TDocument> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public int? Skip
        {
            get { return _skip; }
            set { _skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public bool Tailable
        {
            get { return _tailable; }
            set { _tailable = value; }
        }

        // methods
        private QueryWireProtocolArgs<TDocument> CreateProtocolArgs(BsonDocument wrappedQuery, ReadPreference readPreference)
        {
            var slaveOk = readPreference != null && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary;
            var firstBatchSize = QueryHelper.CalculateFirstBatchSize(_limit, _batchSize);

            return new QueryWireProtocolArgs<TDocument>(
                _collectionNamespace,
                wrappedQuery,
                _projection,
                NoOpElementNameValidator.Instance,
                _skip ?? 0,
                firstBatchSize,
                slaveOk,
                _partial,
                _noCursorTimeout,
                _tailable,
                _awaitData,
                _resultSerializer,
                _messageEncoderSettings);
        }

        internal BsonDocument CreateWrappedQuery(ServerType serverType, ReadPreference readPreference)
        {
            var readPreferenceDocument = QueryHelper.CreateReadPreferenceDocument(serverType, readPreference);

            var wrappedQuery = new BsonDocument
            {
                { "$query", _filter ?? new BsonDocument() },
                { "$readPreference", readPreferenceDocument, readPreferenceDocument != null },
                { "$orderby", _sort, _sort != null },
                { "$comment", _comment, _comment != null },
                { "$maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };

            if (_modifiers != null)
            {
                wrappedQuery.Merge(_modifiers, overwriteExistingElements: false);
            }

            return wrappedQuery;
        }

        public async Task<IAsyncCursor<TDocument>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                var query = CreateWrappedQuery(channelSource.ServerDescription.Type, binding.ReadPreference);
                var protocolArgs = CreateProtocolArgs(query, binding.ReadPreference);
                var batch = await channel.QueryAsync(protocolArgs, cancellationToken).ConfigureAwait(false);

                return new AsyncCursor<TDocument>(
                    channelSource.Fork(),
                    _collectionNamespace,
                    query,
                    batch.Documents,
                    batch.CursorId,
                    _batchSize ?? 0,
                    Math.Abs(_limit ?? 0),
                    _resultSerializer,
                    _messageEncoderSettings);
            }
        }

        public IReadOperation<BsonDocument> ToExplainOperation(ExplainVerbosity verbosity)
        {
            BsonDocument modifiers;
            if (_modifiers == null)
            {
                modifiers = new BsonDocument();
            }
            else
            {
                modifiers = (BsonDocument)_modifiers.DeepClone();
            }
            modifiers["$explain"] = true;
            var operation = new FindOperation<BsonDocument>(_collectionNamespace, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                _awaitData = _awaitData,
                _batchSize = _batchSize,
                _comment = _comment,
                _filter = _filter,
                _limit = _limit,
                _maxTime = _maxTime,
                _modifiers = modifiers,
                _noCursorTimeout = _noCursorTimeout,
                _partial = _partial,
                _projection = _projection,
                _skip = _skip,
                _sort = _sort,
                _tailable = _tailable,
            };

            return new FindExplainOperation(operation);
        }

        private class FindExplainOperation : IReadOperation<BsonDocument>
        {
            private readonly FindOperation<BsonDocument> _explainOperation;

            public FindExplainOperation(FindOperation<BsonDocument> explainOperation)
            {
                _explainOperation = explainOperation;
            }

            public async Task<BsonDocument> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
            {
                using (var cursor = await _explainOperation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false))
                {
                    if (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var batch = cursor.Current;
                        return batch.Single();
                    }
                }

                throw new MongoException("No explanation was returned.");
            }
        }
    }
}
