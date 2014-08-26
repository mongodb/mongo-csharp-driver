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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

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
        private BsonDocument _additionalOptions;
        private bool? _background;
        private string _collectionName;
        private string _databaseName;
        private bool? _dropDups;
        private string _indexName;
        private BsonDocument _keys;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool? _sparse;
        private TimeSpan? _timeToLive;
        private bool? _unique;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        // constructors
        public CreateIndexOperation(
            string databaseName,
            string collectionName,
            BsonDocument keys,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _keys = Ensure.IsNotNull(keys, "keys");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
            set { _additionalOptions = value; }
        }

        public bool? Background
        {
            get { return _background; }
            set { _background = value; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public bool? DropDups
        {
            get { return _dropDups; }
            set { _dropDups = value; }
        }

        public string IndexName
        {
            get { return _indexName; }
            set { _indexName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public BsonDocument Keys
        {
            get { return _keys; }
            set { _keys = Ensure.IsNotNull(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public bool? Sparse
        {
            get { return _sparse; }
            set { _sparse = value; }
        }

        public TimeSpan? TimeToLive
        {
            get { return _timeToLive; }
            set { _timeToLive = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public bool? Unique
        {
            get { return _unique; }
            set { _unique = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
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
            var operation = new InsertOpcodeOperation(_databaseName, "system.indexes", documentSource, _messageEncoderSettings)
            {
                WriteConcern = _writeConcern
            };
            var result = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            return result.Response;
        }
    }
}
