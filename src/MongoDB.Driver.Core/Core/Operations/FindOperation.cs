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
        private bool _allowPartialResults;
        private int? _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private string _comment;
        private CursorType _cursorType;
        private BsonDocument _filter;
        private int? _limit;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _modifiers;
        private bool _noCursorTimeout;
        private BsonDocument _projection;
        private readonly IBsonSerializer<TDocument> _resultSerializer;
        private int? _skip;
        private BsonDocument _sort;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindOperation{TDocument}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public FindOperation(
            CollectionNamespace collectionNamespace,
            IBsonSerializer<TDocument> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "serializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
            _cursorType = CursorType.NonTailable;
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether the server is allowed to return partial results if any shards are unavailable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the server is allowed to return partial results if any shards are unavailable; otherwise, <c>false</c>.
        /// </value>
        public bool AllowPartialResults
        {
            get { return _allowPartialResults; }
            set { _allowPartialResults = value; }
        }

        /// <summary>
        /// Gets or sets the size of a batch.
        /// </summary>
        /// <value>
        /// The size of a batch.
        /// </value>
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or sets the type of the cursor.
        /// </summary>
        /// <value>
        /// The type of the cursor.
        /// </value>
        public CursorType CursorType
        {
            get { return _cursorType; }
            set { _cursorType = value; }
        }

        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public BsonDocument Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>
        /// The limit.
        /// </value>
        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the maximum time the server should spend on this operation.
        /// </summary>
        /// <value>
        /// The maximum time the server should spend on this operation.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        /// <summary>
        /// Gets or sets any additional query modifiers.
        /// </summary>
        /// <value>
        /// The additional query modifiers.
        /// </value>
        public BsonDocument Modifiers
        {
            get { return _modifiers; }
            set { _modifiers = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server will not timeout the cursor.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the server will not timeout the cursor; otherwise, <c>false</c>.
        /// </value>
        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the projection.
        /// </summary>
        /// <value>
        /// The projection.
        /// </value>
        public BsonDocument Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        /// <summary>
        /// Gets the result serializer.
        /// </summary>
        /// <value>
        /// The result serializer.
        /// </value>
        public IBsonSerializer<TDocument> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <summary>
        /// Gets or sets the number of documents skip.
        /// </summary>
        /// <value>
        /// The number of documents skip.
        /// </value>
        public int? Skip
        {
            get { return _skip; }
            set { _skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, "value"); }
        }

        /// <summary>
        /// Gets or sets the sort specification.
        /// </summary>
        /// <value>
        /// The sort specification.
        /// </value>
        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        // methods
        private Task<CursorBatch<TDocument>> ExecuteProtocolAsync(IChannelHandle channel, BsonDocument wrappedQuery, bool slaveOk, CancellationToken cancellationToken)
        {
            var firstBatchSize = QueryHelper.CalculateFirstBatchSize(_limit, _batchSize);

            return channel.QueryAsync<TDocument>(
                _collectionNamespace,
                wrappedQuery,
                _projection,
                NoOpElementNameValidator.Instance,
                _skip ?? 0,
                firstBatchSize,
                slaveOk,
                _allowPartialResults,
                _noCursorTimeout,
                _cursorType != CursorType.NonTailable, // tailable
                _cursorType == CursorType.TailableAwait, //await data
                _resultSerializer,
                _messageEncoderSettings,
                cancellationToken);
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

        /// <inheritdoc/>
        public async Task<IAsyncCursor<TDocument>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                var readPreference = binding.ReadPreference;
                var serverDescription = channelSource.ServerDescription;
                var wrappedQuery = CreateWrappedQuery(serverDescription.Type, readPreference);
                var slaveOk = readPreference != null && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary;
                var batch = await ExecuteProtocolAsync(channel, wrappedQuery, slaveOk, cancellationToken).ConfigureAwait(false);

                return new AsyncCursor<TDocument>(
                    channelSource.Fork(),
                    _collectionNamespace,
                    wrappedQuery,
                    batch.Documents,
                    batch.CursorId,
                    _batchSize ?? 0,
                    Math.Abs(_limit ?? 0),
                    _resultSerializer,
                    _messageEncoderSettings);
            }
        }

        /// <summary>
        /// Returns an explain operation for this find operation.
        /// </summary>
        /// <param name="verbosity">The verbosity.</param>
        /// <returns>An explain operation.</returns>
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
                _allowPartialResults = _allowPartialResults,
                _batchSize = _batchSize,
                _comment = _comment,
                _cursorType = _cursorType,
                _filter = _filter,
                _limit = _limit,
                _maxTime = _maxTime,
                _modifiers = modifiers,
                _noCursorTimeout = _noCursorTimeout,
                _projection = _projection,
                _skip = _skip,
                _sort = _sort,
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
