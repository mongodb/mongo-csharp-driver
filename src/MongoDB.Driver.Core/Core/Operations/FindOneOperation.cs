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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class FindOneOperation : FindOperation<BsonDocument>
    {
        // constructors
        public FindOneOperation(
            string databaseName,
            string collectionName,
            BsonDocument query = null)
            : base(databaseName, collectionName, BsonDocumentSerializer.Instance, query)
        {
        }
    }

    public class FindOneOperation<TDocument> : IReadOperation<TDocument>
    {
        // fields
        private readonly BsonDocument _additionalOptions;
        private readonly string _collectionName;
        private readonly string _comment;
        private readonly string _databaseName;
        private readonly BsonDocument _fields;
        private readonly string _hint;
        private readonly bool _partialOk;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly int? _skip;
        private readonly BsonDocument _sort;

        // constructors
        public FindOneOperation(
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

        internal FindOneOperation(
            BsonDocument additionalOptions,
            string collectionName,
            string comment,
            string databaseName,
            BsonDocument fields,
            string hint,
            bool partialOk,
            BsonDocument query,
            IBsonSerializer<TDocument> serializer,
            int? skip,
            BsonDocument sort)
        {
            _additionalOptions = additionalOptions;
            _collectionName = collectionName;
            _comment = comment;
            _databaseName = databaseName;
            _fields = fields;
            _hint = hint;
            _partialOk = partialOk;
            _query = query;
            _serializer = serializer;
            _skip = skip;
            _sort = sort;
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
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

        public BsonDocument Sort
        {
            get { return _sort; }
        }

        // methods
        public async Task<TDocument> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");

            var awaitData = false;
            var batchSize = 0;
            var limit = -1;
            var noCursorTimeout = false;
            var snapshot = false;
            var tailable = false;

            var operation = new FindOperation<TDocument>(
                _additionalOptions,
                awaitData,
                batchSize,
                _collectionName,
                _comment,
                _databaseName,
                _fields,
                _hint,
                limit,
                noCursorTimeout,
                _partialOk,
                _query,
                _serializer,
                _skip,
                snapshot,
                _sort,
                tailable);

            var cursor = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            if (await cursor.MoveNextAsync())
            {
                return cursor.Current.FirstOrDefault();
            }
            else
            {
                return default(TDocument);
            }
        }

        public Task<BsonDocument> ExplainAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            throw new NotImplementedException();
        }

        public FindOneOperation<TDocument> WithAdditionalOptions(BsonDocument value)
        {
            return object.ReferenceEquals(_additionalOptions, value) ? this : new Builder(this) { _additionalOptions = value }.Build();
        }

        public FindOneOperation<TDocument> WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public FindOneOperation<TDocument> WithComment(string value)
        {
            return (_comment == value) ? this : new Builder(this) { _comment = value }.Build();
        }

        public FindOneOperation<TDocument> WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public FindOneOperation<TDocument> WithFields(BsonDocument value)
        {
            return object.ReferenceEquals(_fields, value) ? this : new Builder(this) { _fields = value }.Build();
        }

        public FindOneOperation<TDocument> WithHint(BsonDocument value)
        {
            return value == null ? WithHint((string)null) : WithHint(CreateIndexOperation.GetDefaultIndexName(value));
        }

        public FindOneOperation<TDocument> WithHint(string value)
        {
            return object.Equals(_hint, value) ? this : new Builder(this) { _hint = value }.Build();
        }

        public FindOneOperation<TDocument> WithPartialOk(bool value)
        {
            return (_partialOk == value) ? this : new Builder(this) { _partialOk = value }.Build();
        }

        public FindOneOperation<TDocument> WithQuery(BsonDocument value)
        {
            return object.ReferenceEquals(_query, value) ? this : new Builder(this) { _query = value ?? new BsonDocument() }.Build();
        }

        public FindOneOperation<TDocument> WithSerializer(IBsonSerializer<TDocument> value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_serializer, value) ? this : new Builder(this) { _serializer = value }.Build();
        }

        public FindOneOperation<TOther> WithSerializer<TOther>(IBsonSerializer<TOther> value)
        {
            Ensure.IsNotNull(value, "value");
            return new FindOneOperation<TOther>(
                _additionalOptions,
                _collectionName,
                _comment,
                _databaseName,
                _fields,
                _hint,
                _partialOk,
                _query,
                value,
                _skip,
                _sort);
        }

        public FindOneOperation<TDocument> WithSkip(int? value)
        {
            Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value");
            return (_skip == value) ? this : new Builder(this) { _skip = value }.Build();
        }

        public FindOneOperation<TDocument> WithSort(BsonDocument value)
        {
            return object.ReferenceEquals(_sort, value) ? this : new Builder(this) { _sort = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public BsonDocument _additionalOptions;
            public string _collectionName;
            public string _comment;
            public string _databaseName;
            public BsonDocument _fields;
            public string _hint;
            public bool _partialOk;
            public BsonDocument _query;
            public IBsonSerializer<TDocument> _serializer;
            public int? _skip;
            public BsonDocument _sort;

            // constructors
            public Builder(FindOneOperation<TDocument> other)
            {
                _additionalOptions = other.AdditionalOptions;
                _collectionName = other.CollectionName;
                _comment = other.Comment;
                _databaseName = other.DatabaseName;
                _fields = other.Fields;
                _hint = other.Hint;
                _partialOk = other.PartialOk;
                _query = other.Query;
                _serializer = other.Serializer;
                _skip = other.Skip;
                _sort = other.Sort;
            }

            // methods
            public FindOneOperation<TDocument> Build()
            {
                return new FindOneOperation<TDocument>(
                    _additionalOptions,
                    _collectionName,
                    _comment,
                    _databaseName,
                    _fields,
                    _hint,
                    _partialOk,
                    _query,
                    _serializer,
                    _skip,
                    _sort);
            }
        }
    }
}
