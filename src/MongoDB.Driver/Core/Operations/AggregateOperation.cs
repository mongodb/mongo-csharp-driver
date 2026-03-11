/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class AggregateOperation<TResult> : IReadOperation<IAsyncCursor<TResult>>, IExecutableInRetryableReadContext<IAsyncCursor<TResult>>
    {
        // fields
        private bool? _allowDiskUse;
        private int? _batchSize;
        private Collation _collation;
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private readonly DatabaseNamespace _databaseNamespace;
        private BsonValue _hint;
        private BsonDocument _let;
        private TimeSpan? _maxAwaitTime;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private ReadConcern _readConcern = ReadConcern.Default;
        private readonly IBsonSerializer<TResult> _resultSerializer;
        private bool _retryRequested;
        private bool? _useCursor;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOperation{TResult}"/> class.
        /// </summary>
        /// <param name="databaseNamespace">The database namespace.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="resultSerializer">The result value serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public AggregateOperation(DatabaseNamespace databaseNamespace, IEnumerable<BsonDocument> pipeline, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : this(pipeline, resultSerializer, messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOperation{TResult}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="resultSerializer">The result value serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public AggregateOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : this(pipeline, resultSerializer, messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        }

        private AggregateOperation(IEnumerable<BsonDocument> pipeline, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
        {
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline)).ToList();
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether the server is allowed to use the disk.
        /// </summary>
        /// <value>
        /// A value indicating whether the server is allowed to use the disk.
        /// </value>
        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
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
            set { _batchSize = Ensure.IsNullOrGreaterThanOrEqualToZero(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
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
        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        /// <summary>
        /// Gets or sets the hint. This must either be a BsonString representing the index name or a BsonDocument representing the key pattern of the index.
        /// </summary>
        /// <value>
        /// The hint.
        /// </value>
        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        /// <summary>
        /// Gets or sets the "let" definition.
        /// </summary>
        /// <value>
        /// The "let" definition.
        /// </value>
        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        /// <summary>
        /// Gets or sets the maximum await time.
        /// </summary>
        /// <value>
        /// The maximum await time.
        /// </value>
        public TimeSpan? MaxAwaitTime
        {
            get { return _maxAwaitTime; }
            set { _maxAwaitTime = value; }
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
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
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
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName => "aggregate";

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        /// <value>
        /// The pipeline.
        /// </value>
        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        /// <value>
        /// The read concern.
        /// </value>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set { _readConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        /// <summary>
        /// Gets the result value serializer.
        /// </summary>
        /// <value>
        /// The result value serializer.
        /// </value>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to retry.
        /// </summary>
        /// <value>Whether to retry.</value>
        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server should use a cursor to return the results.
        /// </summary>
        /// <value>
        /// A value indicating whether the server should use a cursor to return the results.
        /// </value>
        [Obsolete("Server versions 3.6 and newer always use a cursor.")]
        public bool? UseCursor
        {
            get { return _useCursor; }
            set { _useCursor = value; }
        }

        // methods
        /// <inheritdoc/>
        public IAsyncCursor<TResult> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                return Execute(operationContext, context);
            }
        }

        /// <inheritdoc/>
        public IAsyncCursor<TResult> Execute(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));
            EnsureIsReadOnlyPipeline();

            using (EventContext.BeginOperation())
            {
                var operation = CreateOperation(operationContext, context);
                var result = operation.Execute(operationContext, context);

                context.ChannelSource.Session.SetSnapshotTimeIfNeeded(result.AtClusterTime);

                return CreateCursor(context.ChannelSource, context.Channel, result);
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncCursor<TResult>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                return await ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncCursor<TResult>> ExecuteAsync(OperationContext operationContext, RetryableReadContext context)
        {
            Ensure.IsNotNull(context, nameof(context));
            EnsureIsReadOnlyPipeline();

            using (EventContext.BeginOperation())
            {
                var operation = CreateOperation(operationContext, context);
                var result = await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);

                context.ChannelSource.Session.SetSnapshotTimeIfNeeded(result.AtClusterTime);

                return CreateCursor(context.ChannelSource, context.Channel, result);
            }
        }

        internal BsonDocument CreateCommand(OperationContext operationContext, ICoreSession session, ConnectionDescription connectionDescription)
        {
            var readConcern = ReadConcernHelper.GetReadConcernForCommand(session, connectionDescription, _readConcern);
            var command = new BsonDocument
            {
                { "aggregate", _collectionNamespace == null ? (BsonValue)1 : _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue && !operationContext.IsRootContextTimeoutConfigured() },
                { "collation", () => _collation.ToBsonDocument(), _collation != null },
                { "hint", _hint, _hint != null },
                { "let", _let, _let != null },
                { "comment", _comment, _comment != null },
                { "readConcern", readConcern, readConcern != null },
                {
                    "cursor",
                    new BsonDocument
                    {
                        { "batchSize", () => _batchSize.Value, _batchSize.HasValue }
                    }
                }
            };

            return command;
        }

        private EventContext.OperationIdDisposer BeginOperation() => EventContext.BeginOperation(null, OperationName);

        private ReadCommandOperation<AggregateResult> CreateOperation(OperationContext operationContext, RetryableReadContext context)
        {
            var databaseNamespace = _collectionNamespace == null ? _databaseNamespace : _collectionNamespace.DatabaseNamespace;
            var command = CreateCommand(operationContext, context.Binding.Session, context.Channel.ConnectionDescription);
            var serializer = new AggregateResultDeserializer(_resultSerializer);
            return new ReadCommandOperation<AggregateResult>(databaseNamespace, command, serializer, MessageEncoderSettings, OperationName)
            {
                RetryRequested = _retryRequested // might be overridden by retryable read context
            };
        }

        private AsyncCursor<TResult> CreateCursor(IChannelSourceHandle channelSource, IChannelHandle channel, AggregateResult result)
        {
            if (result.CursorId.HasValue)
            {
                return CreateCursorFromCursorResult(channelSource, channel, result);
            }
            else
            {
                // don't need connection pinning
                return CreateCursorFromInlineResult(result);
            }
        }

        private AsyncCursor<TResult> CreateCursorFromCursorResult(IChannelSourceHandle channelSource, IChannelHandle channel, AggregateResult result)
        {
            var cursorId = result.CursorId.GetValueOrDefault(0);
            var getMoreChannelSource = ChannelPinningHelper.CreateGetMoreChannelSource(channelSource, channel, cursorId);
            return new AsyncCursor<TResult>(
                getMoreChannelSource,
                result.CollectionNamespace,
                _comment,
                result.Results,
                cursorId,
                result.PostBatchResumeToken,
                _batchSize,
                null, // limit
                _resultSerializer,
                MessageEncoderSettings,
                _maxAwaitTime);
        }

        private AsyncCursor<TResult> CreateCursorFromInlineResult(AggregateResult result)
        {
            return new AsyncCursor<TResult>(
                null, // channelSource
                CollectionNamespace,
                _comment,
                result.Results,
                0, // cursorId
                null, // postBatchResumeToken
                null, // batchSize
                null, // limit
                _resultSerializer,
                MessageEncoderSettings,
                _maxAwaitTime);
        }

        private void EnsureIsReadOnlyPipeline()
        {
            if (Pipeline.Any(s => { var n = s.GetElement(0).Name; return n == "$out" || n == "$merge"; }))
            {
                throw new ArgumentException("The pipeline for an AggregateOperation contains a $out or $merge operator. Use AggregateToCollectionOperation instead.", "pipeline");
            }
        }

        internal class AggregateResult
        {
            public BsonTimestamp AtClusterTime;
            public long? CursorId;
            public CollectionNamespace CollectionNamespace;
            public BsonDocument PostBatchResumeToken;
            public TResult[] Results;
        }

        internal class AggregateResultDeserializer : SerializerBase<AggregateResult>
        {
            private readonly IBsonSerializer<TResult> _resultSerializer;

            public AggregateResultDeserializer(IBsonSerializer<TResult> resultSerializer)
            {
                _resultSerializer = resultSerializer;
            }

            public override AggregateResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                AggregateResult result = null;
                reader.ReadStartDocument();
                while (reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    if (elementName == "cursor")
                    {
                        var cursorDeserializer = new CursorDeserializer(_resultSerializer);
                        result = cursorDeserializer.Deserialize(context);
                    }
                    else if (elementName == "result")
                    {
                        var arraySerializer = new ArraySerializer<TResult>(_resultSerializer);
                        result = new AggregateResult();
                        result.Results = arraySerializer.Deserialize(context);
                    }
                    else
                    {
                        reader.SkipValue();
                    }
                }
                reader.ReadEndDocument();
                return result;
            }

            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(obj, null)) { return false; }
                if (object.ReferenceEquals(this, obj)) { return true; }
                return
                    base.Equals(obj) &&
                    obj is AggregateResultDeserializer other &&
                    object.Equals(_resultSerializer, other._resultSerializer);
            }

            public override int GetHashCode() => 0;
        }

        internal class CursorDeserializer : SerializerBase<AggregateResult>
        {
            private readonly IBsonSerializer<TResult> _resultSerializer;

            public CursorDeserializer(IBsonSerializer<TResult> resultSerializer)
            {
                _resultSerializer = resultSerializer;
            }

            public override AggregateResult Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                var result = new AggregateResult();
                reader.ReadStartDocument();
                while (reader.ReadBsonType() != 0)
                {
                    var elementName = reader.ReadName();
                    switch (elementName)
                    {
                        case "atClusterTime":
                            result.AtClusterTime = BsonTimestampSerializer.Instance.Deserialize(context);
                            break;

                        case "id":
                            result.CursorId = new Int64Serializer().Deserialize(context);
                            break;

                        case "ns":
                            var ns = reader.ReadString();
                            result.CollectionNamespace = CollectionNamespace.FromFullName(ns);
                            break;

                        case "firstBatch":
                            var arraySerializer = new ArraySerializer<TResult>(_resultSerializer);
                            result.Results = arraySerializer.Deserialize(context);
                            break;

                        case "postBatchResumeToken":
                            result.PostBatchResumeToken = BsonDocumentSerializer.Instance.Deserialize(context);
                            break;

                        default:
                            reader.SkipValue();
                            break;
                    }
                }
                reader.ReadEndDocument();
                return result;
            }

            public override bool Equals(object obj)
            {
                if (object.ReferenceEquals(obj, null)) { return false; }
                if (object.ReferenceEquals(this, obj)) { return true; }
                return
                    base.Equals(obj) &&
                    obj is CursorDeserializer other &&
                    object.Equals(_resultSerializer, other._resultSerializer);
            }

            public override int GetHashCode() => 0;
        }
    }
}
