﻿/* Copyright 2015-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class FindOperation<TDocument> : IReadOperation<IAsyncCursor<TDocument>>, IExecutableInRetryableReadContext<IAsyncCursor<TDocument>>
    {
        #region static
        // private static fields
        private static IBsonSerializer<BsonDocument> __findCommandResultSerializer = new PartiallyRawBsonDocumentSerializer(
            "cursor", new PartiallyRawBsonDocumentSerializer(
                "firstBatch", new RawBsonArraySerializer()));
        #endregion

        // fields
        private bool? _allowDiskUse;
        private bool? _allowPartialResults;
        private int? _batchSize;
        private Collation _collation;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private CursorType _cursorType;
        private BsonDocument _filter;
        private int? _firstBatchSize;
        private BsonValue _hint;
        private BsonDocument _let;
        private int? _limit;
        private BsonDocument _max;
        private TimeSpan? _maxAwaitTime;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _min;
        private bool? _noCursorTimeout;
        private bool? _oplogReplay;
        private BsonDocument _projection;
        private ReadConcern _readConcern = ReadConcern.Default;
        private readonly IBsonSerializer<TDocument> _resultSerializer;
        private bool _retryRequested;
        private bool? _returnKey;
        private bool? _showRecordId;
        private bool? _singleBatch;
        private int? _skip;
        private BsonDocument _sort;

        public FindOperation(
            CollectionNamespace collectionNamespace,
            IBsonSerializer<TDocument> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
            _cursorType = CursorType.NonTailable;
        }

        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        public bool? AllowPartialResults
        {
            get { return _allowPartialResults; }
            set { _allowPartialResults = value; }
        }

        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public CursorType CursorType
        {
            get { return _cursorType; }
            set { _cursorType = value; }
        }

        public BsonDocument Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public int? FirstBatchSize
        {
            get { return _firstBatchSize; }
            set { _firstBatchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public BsonDocument Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public TimeSpan? MaxAwaitTime
        {
            get { return _maxAwaitTime; }
            set { _maxAwaitTime = value; }
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

        public BsonDocument Min
        {
            get { return _min; }
            set { _min = value; }
        }

        public bool? NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }

        [Obsolete("OplogReplay is ignored by server versions 4.4.0 and newer.")]
        public bool? OplogReplay
        {
            get { return _oplogReplay; }
            set { _oplogReplay = value; }
        }

        public BsonDocument Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public IBsonSerializer<TDocument> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public bool? ReturnKey
        {
            get { return _returnKey; }
            set { _returnKey = value; }
        }

        public bool? ShowRecordId
        {
            get { return _showRecordId; }
            set { _showRecordId = value; }
        }

        public bool? SingleBatch
        {
            get { return _singleBatch; }
            set { _singleBatch = value; }
        }

        public int? Skip
        {
            get { return _skip; }
            set { _skip = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public BsonDocument CreateCommand(ConnectionDescription connectionDescription, ICoreSession session)
        {
            var wireVersion = connectionDescription.MaxWireVersion;
            FindProjectionChecker.ThrowIfAggregationExpressionIsUsedWhenNotSupported(_projection, wireVersion);

            var firstBatchSize = _firstBatchSize ?? (_batchSize > 0 ? _batchSize : null);
            var isShardRouter = connectionDescription.HelloResult.ServerType == ServerType.ShardRouter;

            var effectiveComment = _comment;
            var effectiveHint = _hint;
            var effectiveMax = _max;
            var effectiveMaxTime = _maxTime;
            var effectiveMin = _min;
            var effectiveReturnKey = _returnKey;
            var effectiveShowRecordId = _showRecordId;
            var effectiveSort = _sort;

            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            return new BsonDocument
            {
                { "find", _collectionNamespace.CollectionName },
                { "filter", _filter, _filter != null },
                { "sort", effectiveSort, effectiveSort != null },
                { "projection", _projection, _projection != null },
                { "hint", effectiveHint, effectiveHint != null },
                { "skip", () => _skip.Value, _skip.HasValue },
                { "limit", () => Math.Abs(_limit.Value), _limit.HasValue && _limit != 0 },
                { "batchSize", () => firstBatchSize.Value, firstBatchSize.HasValue },
                { "singleBatch", () => _limit < 0 || _singleBatch.Value, _limit < 0 || _singleBatch.HasValue },
                { "comment", effectiveComment, effectiveComment != null },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(effectiveMaxTime.Value), effectiveMaxTime.HasValue },
                { "max", effectiveMax, effectiveMax != null },
                { "min", effectiveMin, effectiveMin != null },
                { "returnKey", () => effectiveReturnKey.Value, effectiveReturnKey.HasValue },
                { "showRecordId", () => effectiveShowRecordId.Value, effectiveShowRecordId.HasValue },
                { "tailable", true, _cursorType == CursorType.Tailable || _cursorType == CursorType.TailableAwait },
                { "oplogReplay", () => _oplogReplay.Value, _oplogReplay.HasValue },
                { "noCursorTimeout", () => _noCursorTimeout.Value, _noCursorTimeout.HasValue },
                { "awaitData", true, _cursorType == CursorType.TailableAwait },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "allowPartialResults", () => _allowPartialResults.Value, _allowPartialResults.HasValue && isShardRouter },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "readConcern", readConcern, readConcern != null },
                { "let", _let, _let != null }
            };
        }

        public IAsyncCursor<TDocument> Execute(IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(binding, _retryRequested, cancellationToken))
            {
                return Execute(context, cancellationToken);
            }
        }

        public IAsyncCursor<TDocument> Execute(RetryableReadContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginFind(_batchSize, _limit))
            {
                var operation = CreateOperation(context);
                var commandResult = operation.Execute(context, cancellationToken);
                return CreateCursor(context.ChannelSource, context.Channel, commandResult);
            }
        }

        public async Task<IAsyncCursor<TDocument>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(binding, _retryRequested, cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IAsyncCursor<TDocument>> ExecuteAsync(RetryableReadContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(context, nameof(context));

            using (EventContext.BeginFind(_batchSize, _limit))
            {
                var operation = CreateOperation(context);
                var commandResult = await operation.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                return CreateCursor(context.ChannelSource, context.Channel, commandResult);
            }
        }

        private AsyncCursor<TDocument> CreateCursor(IChannelSourceHandle channelSource, IChannelHandle channel, BsonDocument commandResult)
        {
            var cursorDocument = commandResult["cursor"].AsBsonDocument;
            var collectionNamespace = CollectionNamespace.FromFullName(cursorDocument["ns"].AsString);
            var firstBatch = CreateFirstCursorBatch(cursorDocument);
            var getMoreChannelSource = ChannelPinningHelper.CreateGetMoreChannelSource(channelSource, channel, firstBatch.CursorId);

            if (cursorDocument.TryGetValue("atClusterTime", out var atClusterTime))
            {
                channelSource.Session.SetSnapshotTimeIfNeeded(atClusterTime.AsBsonTimestamp);
            }

            return new AsyncCursor<TDocument>(
                getMoreChannelSource,
                collectionNamespace,
                _comment,
                firstBatch.Documents,
                firstBatch.CursorId,
                _batchSize,
                _limit < 0 ? Math.Abs(_limit.Value) : _limit,
                _resultSerializer,
                _messageEncoderSettings,
                _cursorType == CursorType.TailableAwait ? _maxAwaitTime : null);
        }

        private CursorBatch<TDocument> CreateFirstCursorBatch(BsonDocument cursorDocument)
        {
            var cursorId = cursorDocument["id"].ToInt64();
            var batch = (RawBsonArray)cursorDocument["firstBatch"];

            using (batch)
            {
                var documents = CursorBatchDeserializationHelper.DeserializeBatch(batch, _resultSerializer, _messageEncoderSettings);
                return new CursorBatch<TDocument>(cursorId, documents);
            }
        }

        private IDisposable BeginOperation() => EventContext.BeginOperation(null, "find");

        private ReadCommandOperation<BsonDocument> CreateOperation(RetryableReadContext context)
        {
            var command = CreateCommand(context.Channel.ConnectionDescription, context.Binding.Session);
            var operation = new ReadCommandOperation<BsonDocument>(
                _collectionNamespace.DatabaseNamespace,
                command,
                __findCommandResultSerializer,
                _messageEncoderSettings)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
            return operation;
        }
    }
}
