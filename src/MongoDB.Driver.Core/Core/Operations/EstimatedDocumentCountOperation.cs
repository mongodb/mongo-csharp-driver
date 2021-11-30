/* Copyright 2021-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an estimated document count operation.
    /// </summary>
    public class EstimatedDocumentCountOperation : IReadOperation<long>
    {
        // private fields
        private readonly CollectionNamespace _collectionNamespace;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private ReadConcern _readConcern = ReadConcern.Default;
        private bool _retryRequested;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EstimatedDocumentCountOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public EstimatedDocumentCountOperation(CollectionNamespace collectionNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        // public properties
        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        public CollectionNamespace CollectionNamespace => _collectionNamespace;

        /// <summary>
        /// Gets or sets the maximum time the server should spend on this operation.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get => _maxTime;
            set => _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value));
        }

        /// <summary>
        /// Gets the message encoder settings.
        /// </summary>
        public MessageEncoderSettings MessageEncoderSettings => _messageEncoderSettings;

        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        public ReadConcern ReadConcern
        {
            get => _readConcern;
            set => _readConcern = Ensure.IsNotNull(value, nameof(value));
        }

        /// <summary>
        /// Gets or sets a value indicating whether to retry.
        /// </summary>
        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        /// <inheritdoc/>
        public long Execute(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableReadContext.Create(binding, _retryRequested, cancellationToken))
            {
                if (Feature.EstimatedDocumentCountByCollStats.IsSupported(context.Channel.ConnectionDescription.ServerVersion))
                {
                    var operation = CreateAggregationOperation();
                    IAsyncCursor<BsonDocument> cursor;
                    try
                    {
                        cursor = operation.Execute(context, cancellationToken);
                    }
                    catch (MongoCommandException ex) when (ex.Code == (int)ServerErrorCode.NamespaceNotFound)
                    {
                        // In the event this aggregation is run against a non-existent namespace, a NamespaceNotFound(26) error will be returned during execution.
                        return 0;
                    }
                    var results = cursor.ToList(cancellationToken);

                    return ExtractCountFromAggregationResults(results);
                }
                else
                {
                    var operation = CreateCountOperation();

                    return operation.Execute(context, cancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<long> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableReadContext.Create(binding, _retryRequested, cancellationToken))
            {
                if (Feature.EstimatedDocumentCountByCollStats.IsSupported(context.Channel.ConnectionDescription.ServerVersion))
                {
                    var operation = CreateAggregationOperation();
                    IAsyncCursor<BsonDocument> cursor;
                    try
                    {
                        cursor = await operation.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                    }
                    catch (MongoCommandException ex) when (ex.Code == (int)ServerErrorCode.NamespaceNotFound)
                    {
                        // In the event this aggregation is run against a non-existent namespace, a NamespaceNotFound(26) error will be returned during execution.
                        return 0;
                    }
                    var results = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);

                    return ExtractCountFromAggregationResults(results);
                }
                else
                {
                    var operation = CreateCountOperation();

                    return await operation.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // private methods
        private IExecutableInRetryableReadContext<IAsyncCursor<BsonDocument>> CreateAggregationOperation()
        {
            var pipeline = CreateAggregationPipeline();
            var aggregateOperation = new AggregateOperation<BsonDocument>(_collectionNamespace, pipeline, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                MaxTime = _maxTime,
                ReadConcern = _readConcern,
                RetryRequested = _retryRequested
            };
            return aggregateOperation;

            IEnumerable<BsonDocument> CreateAggregationPipeline() =>
                new BsonDocument[]
                {
                    new BsonDocument("$collStats", new BsonDocument("count", new BsonDocument())),
                    new BsonDocument(
                        "$group",
                        new BsonDocument
                        {
                            { "_id", 1 },
                            { "n", new BsonDocument("$sum", "$count") }
                        })
                };
        }

        private IExecutableInRetryableReadContext<long> CreateCountOperation()
        {
            var countOperation = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                MaxTime = _maxTime,
                ReadConcern = _readConcern,
                RetryRequested = _retryRequested
            };
            return countOperation;
        }

        private long ExtractCountFromAggregationResults(List<BsonDocument> results) =>
            results.Count switch
            {
                0 => 0,
                1 => results[0]["n"].ToInt64(),
                _ => throw new MongoClientException($"Expected aggregate command for {nameof(EstimatedDocumentCountOperation)} to return 1 document, but got {results.Count}."),
            };
    }
}
