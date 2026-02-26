/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal interface IChangeStreamOperation<TResult> : IReadOperation<IChangeStreamCursor<TResult>>
    {
        BsonDocument ResumeAfter { get; set; }
        bool? ShowExpandedEvents { get; set; }
        BsonDocument StartAfter { get; set; }
        BsonTimestamp StartAtOperationTime { get; set; }

        IAsyncCursor<RawBsonDocument> Resume(OperationContext operationContext, IReadBinding binding);
        Task<IAsyncCursor<RawBsonDocument>> ResumeAsync(OperationContext operationContext, IReadBinding binding);
    }

    internal sealed class ChangeStreamOperation<TResult> : IChangeStreamOperation<TResult>
    {
        private int? _batchSize;
        private Collation _collation;
        private BsonValue _comment;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly DatabaseNamespace _databaseNamespace;
        private ChangeStreamFullDocumentOption _fullDocument = ChangeStreamFullDocumentOption.Default;
        private ChangeStreamFullDocumentBeforeChangeOption _fullDocumentBeforeChangeOption = ChangeStreamFullDocumentBeforeChangeOption.Default;
        private TimeSpan? _maxAwaitTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private ReadConcern _readConcern = ReadConcern.Default;
        private readonly IBsonSerializer<TResult> _resultSerializer;
        private BsonDocument _resumeAfter;
        private bool _retryRequested;
        private bool? _showExpandedEvents;
        private BsonDocument _startAfter;
        private BsonTimestamp _startAtOperationTime;

        public ChangeStreamOperation(
            IEnumerable<BsonDocument> pipeline,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _pipeline = Ensure.IsNotNull(pipeline, nameof(pipeline)).ToList();
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public ChangeStreamOperation(
            DatabaseNamespace databaseNamespace,
            IEnumerable<BsonDocument> pipeline,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : this(pipeline, resultSerializer, messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
        }

        public ChangeStreamOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<BsonDocument> pipeline,
            IBsonSerializer<TResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : this(pipeline, resultSerializer, messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        }

        // public properties
        /// <summary>
        /// Gets or sets the size of the batch.
        /// </summary>
        /// <value>
        /// The size of the batch.
        /// </value>
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = value; }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        /// <value>
        /// The collation.
        /// </value>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public CollectionNamespace CollectionNamespace => _collectionNamespace;

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public DatabaseNamespace DatabaseNamespace => _databaseNamespace;

        /// <summary>
        /// Gets or sets the full document option.
        /// </summary>
        /// <value>
        /// The full document option.
        /// </value>
        public ChangeStreamFullDocumentOption FullDocument
        {
            get { return _fullDocument; }
            set { _fullDocument = value; }
        }

        /// <summary>
        /// Gets or sets the full document before change option.
        /// </summary>
        /// <value>
        /// The full document before change option.
        /// </value>
        public ChangeStreamFullDocumentBeforeChangeOption FullDocumentBeforeChange
        {
            get { return _fullDocumentBeforeChangeOption; }
            set { _fullDocumentBeforeChangeOption = value; }
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
        /// Gets the message encoder settings.
        /// </summary>
        /// <value>
        /// The message encoder settings.
        /// </value>
        public MessageEncoderSettings MessageEncoderSettings => _messageEncoderSettings;

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName => "changeStream";

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        /// <value>
        /// The pipeline.
        /// </value>
        public IReadOnlyList<BsonDocument> Pipeline => _pipeline;

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
        /// Gets the result serializer.
        /// </summary>
        /// <value>
        /// The result serializer.
        /// </value>
        public IBsonSerializer<TResult> ResultSerializer => _resultSerializer;

        /// <inheritdoc />
        public BsonDocument ResumeAfter
        {
            get { return _resumeAfter; }
            set { _resumeAfter = value; }
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

        /// <inheritdoc />
        public bool? ShowExpandedEvents
        {
            get => _showExpandedEvents;
            set => _showExpandedEvents = value;
        }

        /// <inheritdoc />
        public BsonDocument StartAfter
        {
            get { return _startAfter; }
            set { _startAfter = value; }
        }

        /// <inheritdoc />
        public BsonTimestamp StartAtOperationTime
        {
            get { return _startAtOperationTime; }
            set { _startAtOperationTime = value; }
        }

        // public methods
        /// <inheritdoc />
        public IChangeStreamCursor<TResult> Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            var bindingHandle = binding as IReadBindingHandle;
            if (bindingHandle == null)
            {
                throw new ArgumentException("The binding value passed to ChangeStreamOperation.Execute must implement IReadBindingHandle.", nameof(binding));
            }

            IAsyncCursor<RawBsonDocument> cursor;
            ICursorBatchInfo cursorBatchInfo;
            BsonTimestamp initialOperationTime;
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                cursor = ExecuteAggregateOperation(operationContext, context);
                cursorBatchInfo = (ICursorBatchInfo)cursor;
                initialOperationTime = GetInitialOperationTimeIfRequired(context, cursorBatchInfo);

                var postBatchResumeToken = GetInitialPostBatchResumeTokenIfRequired(cursorBatchInfo);

                return new ChangeStreamCursor<TResult>(
                    cursor,
                    _resultSerializer,
                    bindingHandle.Fork(),
                    this,
                    postBatchResumeToken,
                    initialOperationTime,
                    _startAfter,
                    _resumeAfter,
                    _startAtOperationTime,
                    context.Channel.ConnectionDescription.MaxWireVersion);
            }
        }

        /// <inheritdoc />
        public async Task<IChangeStreamCursor<TResult>> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            var bindingHandle = binding as IReadBindingHandle;
            if (bindingHandle == null)
            {
                throw new ArgumentException("The binding value passed to ChangeStreamOperation.ExecuteAsync must implement IReadBindingHandle.", nameof(binding));
            }

            IAsyncCursor<RawBsonDocument> cursor;
            ICursorBatchInfo cursorBatchInfo;
            BsonTimestamp initialOperationTime;
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, _retryRequested).ConfigureAwait(false))
            {
                cursor = await ExecuteAggregateOperationAsync(operationContext, context).ConfigureAwait(false);
                cursorBatchInfo = (ICursorBatchInfo)cursor;
                initialOperationTime = GetInitialOperationTimeIfRequired(context, cursorBatchInfo);

                var postBatchResumeToken = GetInitialPostBatchResumeTokenIfRequired(cursorBatchInfo);

                return new ChangeStreamCursor<TResult>(
                    cursor,
                    _resultSerializer,
                    bindingHandle.Fork(),
                    this,
                    postBatchResumeToken,
                    initialOperationTime,
                    _startAfter,
                    _resumeAfter,
                    _startAtOperationTime,
                    context.Channel.ConnectionDescription.MaxWireVersion);
            }
        }

        /// <inheritdoc />
        public IAsyncCursor<RawBsonDocument> Resume(OperationContext operationContext, IReadBinding binding)
        {
            using (var context = RetryableReadContext.Create(operationContext, binding, retryRequested: false))
            {
                return ExecuteAggregateOperation(operationContext, context);
            }
        }

        /// <inheritdoc />
        public async Task<IAsyncCursor<RawBsonDocument>> ResumeAsync(OperationContext operationContext, IReadBinding binding)
        {
            using (var context = await RetryableReadContext.CreateAsync(operationContext, binding, retryRequested: false).ConfigureAwait(false))
            {
                return await ExecuteAggregateOperationAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        // private methods
        private AggregateOperation<RawBsonDocument> CreateAggregateOperation()
        {
            var changeStreamStage = CreateChangeStreamStage();
            var combinedPipeline = CreateCombinedPipeline(changeStreamStage);

            AggregateOperation<RawBsonDocument> operation;
            if (_collectionNamespace != null)
            {
                operation = new AggregateOperation<RawBsonDocument>(_collectionNamespace, combinedPipeline, RawBsonDocumentSerializer.Instance, _messageEncoderSettings)
                {
                    RetryRequested = _retryRequested // might be overridden by retryable read context
                };
            }
            else
            {
                var databaseNamespace = _databaseNamespace ?? DatabaseNamespace.Admin;
                operation = new AggregateOperation<RawBsonDocument>(databaseNamespace, combinedPipeline, RawBsonDocumentSerializer.Instance, _messageEncoderSettings)
                {
                    RetryRequested = _retryRequested // might be overridden by retryable read context
                };
            }

            operation.BatchSize = _batchSize;
            operation.Collation = _collation;
            operation.Comment = _comment;
            operation.MaxAwaitTime = _maxAwaitTime;
            operation.ReadConcern = _readConcern;

            return operation;
        }

        private BsonDocument CreateChangeStreamStage()
        {
            var changeStreamOptions = new BsonDocument
            {
                { "fullDocument", () => ToString(_fullDocument), _fullDocument != ChangeStreamFullDocumentOption.Default },
                { "fullDocumentBeforeChange", () => ToString(_fullDocumentBeforeChangeOption), _fullDocumentBeforeChangeOption != ChangeStreamFullDocumentBeforeChangeOption.Default },
                { "allChangesForCluster", true, _collectionNamespace == null && _databaseNamespace == null },
                { "showExpandedEvents", _showExpandedEvents, _showExpandedEvents.HasValue },
                { "startAfter", _startAfter, _startAfter != null },
                { "startAtOperationTime", _startAtOperationTime, _startAtOperationTime != null },
                { "resumeAfter", _resumeAfter, _resumeAfter != null }
            };
            return new BsonDocument("$changeStream", changeStreamOptions);
        }

        private List<BsonDocument> CreateCombinedPipeline(BsonDocument changeStreamStage)
        {
            var combinedPipeline = new List<BsonDocument>();
            combinedPipeline.Add(changeStreamStage);
            combinedPipeline.AddRange(_pipeline);
            return combinedPipeline;
        }

        private IAsyncCursor<RawBsonDocument> ExecuteAggregateOperation(OperationContext operationContext, RetryableReadContext context)
        {
            var aggregateOperation = CreateAggregateOperation();
            return aggregateOperation.Execute(operationContext, context);
        }

        private Task<IAsyncCursor<RawBsonDocument>> ExecuteAggregateOperationAsync(OperationContext operationContext, RetryableReadContext context)
        {
            var aggregateOperation = CreateAggregateOperation();
            return aggregateOperation.ExecuteAsync(operationContext, context);
        }

        private BsonDocument GetInitialPostBatchResumeTokenIfRequired(ICursorBatchInfo cursorBatchInfo)
        {
            // If the initial aggregate returns an empty batch, but includes a `postBatchResumeToken`, then we should return that token.
            return cursorBatchInfo.WasFirstBatchEmpty ? cursorBatchInfo.PostBatchResumeToken : null;
        }

        private BsonTimestamp GetInitialOperationTimeIfRequired(RetryableReadContext context, ICursorBatchInfo cursorBatchInfo)
        {
            if (_startAtOperationTime == null && _resumeAfter == null && _startAfter == null)
            {
                var maxWireVersion = context.Channel.ConnectionDescription.HelloResult.MaxWireVersion;
                if (maxWireVersion >= 7)
                {
                    if (cursorBatchInfo.PostBatchResumeToken == null && cursorBatchInfo.WasFirstBatchEmpty)
                    {
                        return context.Binding.Session.OperationTime;
                    }
                }
            }

            return null;
        }

        private string ToString(ChangeStreamFullDocumentOption fullDocument) =>
            fullDocument switch
            {
                ChangeStreamFullDocumentOption.UpdateLookup => "updateLookup",
                ChangeStreamFullDocumentOption.WhenAvailable => "whenAvailable",
                ChangeStreamFullDocumentOption.Required => "required",
                _ => throw new ArgumentException($"Invalid FullDocument option: {fullDocument}.", nameof(fullDocument))
            };

        private string ToString(ChangeStreamFullDocumentBeforeChangeOption fullDocumentBeforeChange) =>
            fullDocumentBeforeChange switch
            {
                ChangeStreamFullDocumentBeforeChangeOption.Off => "off",
                ChangeStreamFullDocumentBeforeChangeOption.WhenAvailable => "whenAvailable",
                ChangeStreamFullDocumentBeforeChangeOption.Required => "required",
                _ => throw new ArgumentException($"Invalid FullDocumentBeforeChange option: {fullDocumentBeforeChange}.", nameof(fullDocumentBeforeChange))
            };
    }
}
