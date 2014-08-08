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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexOperation : IWriteOperation<BsonDocument>
    {
        #region static
        // static methods
        public static string GetDefaultIndexName(BsonDocument keys)
        {
            Ensure.IsNotNull(keys, "keys");

            var parts = new List<string>();
            foreach (var key in keys)
            {
                var value = key.Value;
                string type;
                switch (value.BsonType)
                {
                    case BsonType.Double: type = ((BsonDouble)value).Value.ToString(); break;
                    case BsonType.Int32: type = ((BsonInt32)value).Value.ToString(); break;
                    case BsonType.Int64: type = ((BsonInt64)value).Value.ToString(); break;
                    case BsonType.String: type = ((BsonString)value).Value; break;
                    default: type = "x"; break;
                }
                var part = string.Format("{0}_{1}", key.Name, type).Replace(' ', '_');
                parts.Add(part);
            }

            return string.Join("_", parts.ToArray());
        }
        #endregion

        // fields
        private readonly BsonDocument _additionalOptions;
        private readonly bool? _background;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly bool? _dropDups;
        private readonly string _indexName;
        private readonly BsonDocument _keys;
        private readonly bool? _sparse;
        private readonly TimeSpan? _timeToLive;
        private readonly bool? _unique;
        private readonly WriteConcern _writeConcern;

        // constructors
        public CreateIndexOperation(
            string databaseName,
            string collectionName,
            BsonDocument keys)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _keys = Ensure.IsNotNull(keys, "keys");
            _writeConcern = WriteConcern.Acknowledged;
        }

        private CreateIndexOperation(
            BsonDocument additionalOptions,
            bool? background,
            string collectionName,
            string databaseName,
            bool? dropDups,
            string indexName,
            BsonDocument keys,
            bool? sparse,
            TimeSpan? timeToLive,
            bool? unique,
            WriteConcern writeConcern)
        {
            _additionalOptions = additionalOptions;
            _background = background;
            _collectionName = collectionName;
            _databaseName = databaseName;
            _dropDups = dropDups;
            _indexName = indexName;
            _keys = keys;
            _sparse = sparse;
            _timeToLive = timeToLive;
            _unique = unique;
            _writeConcern = writeConcern;
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
        }

        public bool? Background
        {
            get { return _background; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public bool? DropDups
        {
            get { return _dropDups; }
        }

        public string IndexName
        {
            get { return _indexName; }
        }

        public BsonDocument Keys
        {
            get { return _keys; }
        }

        public bool? Sparse
        {
            get { return _sparse; }
        }

        public TimeSpan? TimeToLive
        {
            get { return _timeToLive; }
        }

        public bool? Unique
        {
            get { return _unique; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
        }

        // methods
        public BsonDocument CreateIndexDocument()
        {
            var document = new BsonDocument
            {
                { "name", _indexName ?? GetDefaultIndexName(_keys) },
                { "ns", _databaseName + "." + _collectionName },
                { "key", _keys },
                { "background", () => _background.Value, _background.HasValue },
                { "dropDups", () => _dropDups.Value, _dropDups.HasValue },
                { "sparse", () => _sparse.Value, _sparse.HasValue },
                { "unique", () => _unique.Value, _unique.HasValue },
                { "expireAfterSeconds", () => _timeToLive.Value.TotalSeconds, _timeToLive.HasValue },
            };
            if (_additionalOptions != null)
            {
                document.AddRange(_additionalOptions);
            }
            return document;
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var indexDocument = CreateIndexDocument();
            var documentSource = new BatchableSource<BsonDocument>(new[] { indexDocument });
            var operation = new InsertOpcodeOperation(_databaseName, "system.indexes", documentSource).WithWriteConcern(_writeConcern);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public CreateIndexOperation WithAdditionalOptions(BsonDocument value)
        {
            return object.ReferenceEquals(_additionalOptions, value) ? this : new Builder(this) { _additionalOptions = value }.Build();
        }

        public CreateIndexOperation WithBackground(bool? value)
        {
            return (_background == value) ? this : new Builder(this) { _background = value }.Build();
        }

        public CreateIndexOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public CreateIndexOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public CreateIndexOperation WithDropDups(bool? value)
        {
            return (_dropDups == value) ? this : new Builder(this) { _dropDups = value }.Build();
        }

        public CreateIndexOperation WithIndexName(string value)
        {
            // value can be null
            return (_indexName == value) ? this : new Builder(this) { _indexName = value }.Build();
        }

        public CreateIndexOperation WithKeys(BsonDocument value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(_keys, value) ? this : new Builder(this) { _keys = value }.Build();
        }

        public CreateIndexOperation WithSparse(bool? value)
        {
            return (_sparse == value) ? this : new Builder(this) { _sparse = value }.Build();
        }

        public CreateIndexOperation WithTimeToLive(TimeSpan? value)
        {
            return (_timeToLive == value) ? this : new Builder(this) { _timeToLive = value }.Build();
        }

        public CreateIndexOperation WithUnique(bool? value)
        {
            return (_unique == value) ? this : new Builder(this) { _unique = value }.Build();
        }

        public CreateIndexOperation WithWriteConcern(WriteConcern value)
        {
            Ensure.IsNotNull(value, "value");
            return object.Equals(_writeConcern, value) ? this : new Builder(this) { _writeConcern = value }.Build();
        }

        // nested typed
        private struct Builder
        {
            // fields
            public BsonDocument _additionalOptions;
            public bool? _background;
            public string _collectionName;
            public string _databaseName;
            public bool? _dropDups;
            public string _indexName;
            public BsonDocument _keys;
            public bool? _sparse;
            public TimeSpan? _timeToLive;
            public bool? _unique;
            public WriteConcern _writeConcern;

            // constructors
            public Builder(CreateIndexOperation other)
            {
                _additionalOptions = other.AdditionalOptions;
                _background = other.Background;
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _dropDups = other.DropDups;
                _indexName = other.IndexName;
                _keys = other.Keys;
                _sparse = other.Sparse;
                _timeToLive = other.TimeToLive;
                _unique = other.Unique;
                _writeConcern = other.WriteConcern;
            }

            // methods
            public CreateIndexOperation Build()
            {
                return new CreateIndexOperation(
                    _additionalOptions,
                    _background,
                    _collectionName,
                    _databaseName,
                    _dropDups,
                    _indexName,
                    _keys,
                    _sparse,
                    _timeToLive,
                    _unique,
                    _writeConcern);
            }
        }
    }
}
