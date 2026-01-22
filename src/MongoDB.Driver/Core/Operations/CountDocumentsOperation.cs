/* Copyright 2018-present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class CountDocumentsOperation : IReadOperation<long>
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

        public CountDocumentsOperation(CollectionNamespace collectionNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
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

        public string OperationName => "countDocuments";

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

        public long Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            {
                var operation = CreateOperation();
                var cursor = operation.Execute(operationContext, binding);
                var result = cursor.ToList(operationContext.CancellationToken);
                return ExtractCountFromResult(result);
            }
        }

        public async Task<long> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            {
                var operation = CreateOperation();
                var cursor = await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
                var result = await cursor.ToListAsync(operationContext.CancellationToken).ConfigureAwait(false);
                return ExtractCountFromResult(result);
            }
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation("aggregate");

        private AggregateOperation<BsonDocument> CreateOperation()
        {
            var pipeline = CreatePipeline();
            var operation = new AggregateOperation<BsonDocument>(_collectionNamespace, pipeline, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Collation = _collation,
                Comment = _comment,
                Hint = _hint,
                MaxTime = _maxTime,
                ReadConcern = _readConcern,
                RetryRequested = _retryRequested
            };
            return operation;
        }

        private List<BsonDocument> CreatePipeline()
        {
            var pipeline = new List<BsonDocument>();
            pipeline.Add(new BsonDocument("$match", _filter ?? new BsonDocument()));
            if (_skip.HasValue)
            {
                pipeline.Add(new BsonDocument("$skip", _skip.Value));
            }
            if (_limit.HasValue)
            {
                pipeline.Add(new BsonDocument("$limit", _limit.Value));
            }
            pipeline.Add(new BsonDocument("$group", new BsonDocument { { "_id", 1 }, { "n", new BsonDocument("$sum", 1) } }));
            return pipeline;
        }

        private long ExtractCountFromResult(List<BsonDocument> result)
        {
            switch (result.Count)
            {
                case 0:
                    return 0;

                case 1:
                    return result[0]["n"].ToInt64();

                default:
                    throw new MongoClientException($"Expected aggregate command for CountDocuments to return 1 document, but got {result.Count}.");
            }
        }
    }
}
