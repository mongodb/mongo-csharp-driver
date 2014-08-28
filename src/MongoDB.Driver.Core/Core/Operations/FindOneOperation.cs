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
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class FindOneOperation : FindOperation<BsonDocument>
    {
        // constructors
        public FindOneOperation(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, query, BsonDocumentSerializer.Instance, messageEncoderSettings)
        {
        }
    }

    public class FindOneOperation<TDocument> : IReadOperation<TDocument>
    {
        // fields
        private BsonDocument _additionalOptions;
        private CollectionNamespace _collectionNamespace;
        private string _comment;
        private BsonDocument _fields;
        private string _hint;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool _partialOk;
        private BsonDocument _query;
        private IBsonSerializer<TDocument> _serializer;
        private int? _skip;
        private BsonDocument _sort;

        // constructors
        public FindOneOperation(
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

        public string Hint
        {
            get { return _hint; }
            set { _hint = value; }
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

        public BsonDocument Sort
        {
            get { return _sort; }
            set {_sort = value; }
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
            var tailableCursor = false;

            var operation = new FindOperation<TDocument>(_collectionNamespace, _query, _serializer, _messageEncoderSettings)
            {
                AdditionalOptions = _additionalOptions,
                AwaitData = awaitData,
                BatchSize = batchSize,
                Comment = _comment,
                Fields = _fields,
                Hint = _hint,
                Limit = limit,
                MaxTime = _maxTime,
                NoCursorTimeout = noCursorTimeout,
                PartialOk = _partialOk,
                Skip = _skip,
                Snapshot = snapshot,
                Sort = _sort,
                TailableCursor = tailableCursor
            };

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
    }
}
