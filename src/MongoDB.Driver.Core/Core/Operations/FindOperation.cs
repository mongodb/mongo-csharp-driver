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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a Find operation.
    /// </summary>
    public class FindOperation : FindOperation<BsonDocument>
    {
        // constructors
        public FindOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, query, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    /// <summary>
    /// Represents a Find operation.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
    public class FindOperation<TDocument> : QueryOperationBase, IReadOperation<Cursor<TDocument>>
    {
        // fields
        private BsonDocument _additionalOptions;
        private bool _awaitData = true;
        private int? _batchSize;
        private CollectionNamespace _collectionNamespace;
        private string _comment;
        private BsonDocument _fields;
        private string _hint;
        private int? _limit;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool _noCursorTimeout;
        private bool _partialOk;
        private BsonDocument _query;
        private IBsonSerializer<TDocument> _serializer;
        private int? _skip;
        private bool? _snapshot;
        private BsonDocument _sort;
        private bool _tailableCursor;

        // constructors
        public FindOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _query = Ensure.IsNotNull(query, "query");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
            set { _additionalOptions = value; }
        }

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
            set { _collectionNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public BsonDocument Fields
        {
            get { return _fields; }
            set { _fields = value; }
        }

        public string Hint
        {
            get { return _hint; }
            set { _hint = value; }
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
            set { _messageEncoderSettings = value; }
        }

        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }

        public bool PartialOk
        {
            get { return _partialOk; }
            set { _partialOk = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = Ensure.IsNotNull(value, "value"); }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
            set { _serializer = Ensure.IsNotNull(value, "value"); }
        }

        public int? Skip
        {
            get { return _skip; }
            set { _skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public bool? Snapshot
        {
            get { return _snapshot; }
            set { _snapshot = value; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public bool TailableCursor
        {
            get { return _tailableCursor; }
            set { _tailableCursor = value; }
        }

        // methods
        private int CalculateFirstBatchSize()
        {
            int firstBatchSize;

            var limit = _limit ?? 0;
            var batchSize = _batchSize ?? 0;
            if (limit < 0)
            {
                firstBatchSize = limit;
            }
            else if (limit == 0)
            {
                firstBatchSize = batchSize;
            }
            else if (batchSize == 0)
            {
                firstBatchSize = limit;
            }
            else if (limit < batchSize)
            {
                firstBatchSize = limit;
            }
            else
            {
                firstBatchSize = batchSize;
            }

            return firstBatchSize;
        }

        public FindOperation<TOtherDocument> Clone<TOtherDocument>(IBsonSerializer<TOtherDocument> serializer)
        {
            return new FindOperation<TOtherDocument>(_collectionNamespace, _query, serializer, _messageEncoderSettings)
            {
                AdditionalOptions = _additionalOptions,
                AwaitData = _awaitData,
                BatchSize = _batchSize,
                Comment = _comment,
                Fields = _fields,
                Hint = _hint,
                Limit = _limit,
                MaxTime = _maxTime,
                NoCursorTimeout = _noCursorTimeout,
                PartialOk = _partialOk,
                Skip = _skip,
                Snapshot = _snapshot,
                Sort = _sort,
                TailableCursor = _tailableCursor
            };
        }

        private QueryWireProtocol<TDocument> CreateProtocol(ServerDescription serverDescription, ReadPreference readPreference)
        {
            var wrappedQuery = CreateWrappedQuery(serverDescription, readPreference);
            var slaveOk = readPreference != null && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary;
            var firstBatchSize = CalculateFirstBatchSize();

            return new QueryWireProtocol<TDocument>(
                _collectionNamespace,
                wrappedQuery,
                _fields,
                _skip ?? 0,
                firstBatchSize,
                slaveOk,
                _partialOk,
                _noCursorTimeout,
                _tailableCursor,
                _awaitData,
                _serializer,
                _messageEncoderSettings);
        }

        private BsonDocument CreateWrappedQuery(ServerDescription serverDescription, ReadPreference readPreference)
        {
            BsonDocument readPreferenceDocument = null;
            if (serverDescription.Type == ServerType.ShardRouter)
            {
                readPreferenceDocument = CreateReadPreferenceDocument(readPreference);
            }

            var wrappedQuery = new BsonDocument
            {
                { "$query", _query ?? new BsonDocument() },
                { "$readPreference", readPreferenceDocument, readPreferenceDocument != null },
                { "$orderby", () =>_sort, _sort != null },
                { "$hint", () => _hint, _hint != null },
                { "$snapshot", () => _snapshot.Value, _snapshot.HasValue },
                { "$comment", () => _comment, _comment != null },
                { "$maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
            if (_additionalOptions != null)
            {
                wrappedQuery.AddRange(_additionalOptions);
            }

            return wrappedQuery;
        }

        public async Task<Cursor<TDocument>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var slidingTimeout = new SlidingTimeout(timeout);

            using (var connectionSource = await binding.GetReadConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                var protocol = CreateProtocol(connectionSource.ServerDescription, binding.ReadPreference);
                var batch = await protocol.ExecuteAsync(connectionSource, slidingTimeout, cancellationToken);

                return new Cursor<TDocument>(
                    connectionSource.Fork(),
                    _collectionNamespace,
                    _query,
                    batch.Documents,
                    batch.CursorId,
                    _batchSize ?? 0,
                    Math.Abs(_limit ?? 0),
                    _serializer,
                    _messageEncoderSettings,
                    timeout,
                    cancellationToken);
            }
        }

        public async Task<BsonDocument> ExplainAsync(IReadBinding binding, bool verbose = false, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");

            var additionalOptions = new BsonDocument();
            if (_additionalOptions != null)
            {
                additionalOptions.AddRange(_additionalOptions);
            }
            additionalOptions.Add("$explain", true);

            var operation = Clone<BsonDocument>(BsonDocumentSerializer.Instance);
            operation.AdditionalOptions = additionalOptions;
            operation.Limit = -Math.Abs(_limit ?? 0);
            var cursor = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            await cursor.MoveNextAsync();
            return cursor.Current.First();
        }
    }
}
