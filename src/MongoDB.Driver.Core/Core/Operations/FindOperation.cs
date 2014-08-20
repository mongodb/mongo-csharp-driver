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

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a Find operation.
    /// </summary>
    public class FindOperation : FindOperation<BsonDocument>
    {
        // constructors
        public FindOperation(
            string databaseName,
            string collectionName,
            BsonDocument query = null)
            : base(databaseName, collectionName, BsonDocumentSerializer.Instance, query)
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
        private readonly BsonDocument _additionalOptions;
        private readonly bool _awaitData = true;
        private readonly int? _batchSize;
        private readonly string _collectionName;
        private readonly string _comment;
        private readonly string _databaseName;
        private readonly BsonDocument _fields;
        private readonly string _hint;
        private readonly int? _limit;
        private readonly bool _noCursorTimeout;
        private readonly bool _partialOk;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly int? _skip;
        private readonly bool? _snapshot;
        private readonly BsonDocument _sort;
        private readonly bool _tailableCursor;

        // constructors
        public FindOperation(
            string databaseName,
            string collectionName,
            IBsonSerializer<TDocument> serializer,
            BsonDocument query = null)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _query = query ?? new BsonDocument();
        }

        internal FindOperation(
            BsonDocument additionalOptions,
            bool awaitData,
            int? batchSize,
            string collectionName,
            string comment,
            string databaseName,
            BsonDocument fields,
            string hint,
            int? limit,
            bool noCursorTimeout,
            bool partialOk,
            BsonDocument query,
            IBsonSerializer<TDocument> serializer,
            int? skip,
            bool? snapshot,
            BsonDocument sort,
            bool tailableCursor)
        {
            _additionalOptions = additionalOptions;
            _awaitData = awaitData;
            _batchSize = batchSize;
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _comment = comment;
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _fields = fields;
            _hint = hint;
            _limit = limit;
            _noCursorTimeout = noCursorTimeout;
            _partialOk = partialOk;
            _query = query;
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _skip = skip;
            _snapshot = snapshot;
            _sort = sort;
            _tailableCursor = tailableCursor;
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
        }

        public bool AwaitData
        {
            get { return _awaitData; }
        }

        public int? BatchSize
        {
            get { return _batchSize; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string Comment
        {
            get { return _comment; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public BsonDocument Fields
        {
            get { return _fields; }
        }

        public string Hint
        {
            get { return _hint; }
        }

        public int? Limit
        {
            get { return _limit; }
        }

        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
        }

        public bool PartialOk
        {
            get { return _partialOk; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }

        public int? Skip
        {
            get { return _skip; }
        }

        public bool? Snapshot
        {
            get { return _snapshot; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
        }

        public bool TailableCursor
        {
            get { return _tailableCursor; }
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

        private QueryWireProtocol<TDocument> CreateProtocol(ServerDescription serverDescription, ReadPreference readPreference)
        {
            var wrappedQuery = CreateWrappedQuery(serverDescription, readPreference);
            var slaveOk = readPreference != null && readPreference.Mode != ReadPreferenceMode.Primary;
            var firstBatchSize = CalculateFirstBatchSize();

            return new QueryWireProtocol<TDocument>(
                _databaseName,
                _collectionName,
                wrappedQuery,
                _fields,
                _skip ?? 0,
                firstBatchSize,
                slaveOk,
                _partialOk,
                _noCursorTimeout,
                _tailableCursor,
                _awaitData,
                _serializer);
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
                { "$comment", () => _comment, _comment != null }
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
                    _databaseName,
                    _collectionName,
                    _query,
                    batch.Documents,
                    batch.CursorId,
                    _batchSize ?? 0,
                    _serializer,
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

            var operation = this
                .WithAdditionalOptions(additionalOptions)
                .WithLimit(-Math.Abs(_limit ?? 0))
                .WithSerializer<BsonDocument>(BsonDocumentSerializer.Instance);
            var cursor = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            await cursor.MoveNextAsync();
            return cursor.Current.First();
        }

        public FindOperation<TDocument> WithAdditionalOptions(BsonDocument value)
        {
            return object.ReferenceEquals(_additionalOptions, value) ? this : new Builder(this) { _additionalOptions = value }.Build();
        }

        public FindOperation<TDocument> WithAwaitData(bool value)
        {
            return (_awaitData == value) ? this : new Builder(this) { _awaitData = value }.Build();
        }

        public FindOperation<TDocument> WithBatchSize(int? value)
        {
            return (_batchSize == value) ? this : new Builder(this) { _batchSize = value }.Build();
        }

        public FindOperation<TDocument> WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public FindOperation<TDocument> WithComment(string value)
        {
            return (_comment == value) ? this : new Builder(this) { _comment = value }.Build();
        }

        public FindOperation<TDocument> WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public FindOperation<TDocument> WithFields(BsonDocument value)
        {
            return object.ReferenceEquals(_fields, value) ? this : new Builder(this) { _fields = value }.Build();
        }

        public FindOperation<TDocument> WithHint(BsonDocument value)
        {
            Ensure.IsNotNull(value, "value");
            var indexName = CreateIndexOperation.GetDefaultIndexName(value);
            return WithHint(indexName);
        }

        public FindOperation<TDocument> WithHint(string value)
        {
            return object.Equals(_hint, value) ? this : new Builder(this) { _hint = value }.Build();
        }

        public FindOperation<TDocument> WithLimit(int? value)
        {
            return (_limit == value) ? this : new Builder(this) { _limit = value }.Build();
        }

        public FindOperation<TDocument> WithNoCursorTimeout(bool value)
        {
            return (_noCursorTimeout == value) ? this : new Builder(this) { _noCursorTimeout = value }.Build();
        }

        public FindOperation<TDocument> WithPartialOk(bool value)
        {
            return (_partialOk == value) ? this : new Builder(this) { _partialOk = value }.Build();
        }

        public FindOperation<TDocument> WithQuery(BsonDocument value)
        {
            return object.ReferenceEquals(_query, value) ? this : new Builder(this) { _query = value ?? new BsonDocument() }.Build();
        }

        public FindOperation<TDocument> WithSerializer(IBsonSerializer<TDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_serializer, value) ? this : new Builder(this) { _serializer = value }.Build();
        }

        public FindOperation<TOther> WithSerializer<TOther>(IBsonSerializer<TOther> value)
        {
            Ensure.IsNotNull(value, "value");
            return new FindOperation<TOther>(
                _additionalOptions,
                _awaitData,
                _batchSize,
                _collectionName,
                _comment,
                _databaseName,
                _fields,
                _hint,
                _limit,
                _noCursorTimeout,
                _partialOk,
                _query,
                value,
                _skip,
                _snapshot,
                _sort,
                _tailableCursor);
        }

        public FindOperation<TDocument> WithSkip(int? value)
        {
            Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value");
            return (_skip == value) ? this : new Builder(this) { _skip = value }.Build();
        }

        public FindOperation<TDocument> WithSnapshot(bool? value)
        {
            return (_snapshot == value) ? this : new Builder(this) { _snapshot = value }.Build();
        }

        public FindOperation<TDocument> WithSort(BsonDocument value)
        {
            return object.ReferenceEquals(_sort, value) ? this : new Builder(this) { _sort = value }.Build();
        }

        public FindOperation<TDocument> WithTailableCursor(bool value)
        {
            return (_tailableCursor == value) ? this : new Builder(this) { _tailableCursor = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public BsonDocument _additionalOptions;
            public bool _awaitData;
            public int? _batchSize;
            public string _collectionName;
            public string _comment;
            public string _databaseName;
            public BsonDocument _fields;
            public string _hint;
            public int? _limit;
            public bool _noCursorTimeout;
            public bool _partialOk;
            public BsonDocument _query;
            public IBsonSerializer<TDocument> _serializer;
            public int? _skip;
            public bool? _snapshot;
            public BsonDocument _sort;
            public bool _tailableCursor;

            // constructors
            public Builder(FindOperation<TDocument> other)
            {
                _additionalOptions = other._additionalOptions;
                _awaitData = other._awaitData;
                _batchSize = other._batchSize;
                _collectionName = other._collectionName;
                _comment = other._comment;
                _databaseName = other._databaseName;
                _fields = other._fields;
                _hint = other._hint;
                _limit = other._limit;
                _noCursorTimeout = other._noCursorTimeout;
                _partialOk = other._partialOk;
                _query = other._query;
                _serializer = other._serializer;
                _skip = other._skip;
                _snapshot = other._snapshot;
                _sort = other._sort;
                _tailableCursor = other._tailableCursor;
            }

            // methods
            public FindOperation<TDocument> Build()
            {
                return new FindOperation<TDocument>(
                    _additionalOptions,
                    _awaitData,
                    _batchSize,
                    _collectionName,
                    _comment,
                    _databaseName,
                    _fields,
                    _hint,
                    _limit,
                    _noCursorTimeout,
                    _partialOk,
                    _query,
                    _serializer,
                    _skip,
                    _snapshot,
                    _sort,
                    _tailableCursor);
            }
        }
    }
}
