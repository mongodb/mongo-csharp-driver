﻿/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an aggregate operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result values.</typeparam>
    public class AggregateOperation<TResult> : IReadOperation<IAsyncCursor<TResult>>
    {
        // static fields
        private static readonly SemanticVersion __version26 = new SemanticVersion(2, 6, 0);

        // fields
        private bool? _allowDiskUse;
        private int? _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;
        private readonly IBsonSerializer<TResult> _resultSerializer;
        private bool? _useCursor;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOperation{TResult}"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="resultSerializer">The result value serializer.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public AggregateOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
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
        /// Gets or sets a value indicating whether the server should use a cursor to return the results.
        /// </summary>
        /// <value>
        /// A value indicating whether the server should use a cursor to return the results.
        /// </value>
        public bool? UseCursor
        {
            get { return _useCursor; }
            set { _useCursor = value; }
        }

        // methods
        /// <inheritdoc/>
        public async Task<IAsyncCursor<TResult>> ExecuteAsync(IReadBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            EnsureIsReadOnlyPipeline();

            using (var channelSource = await binding.GetReadChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                var command = CreateCommand(channelSource.ServerDescription.Version);

                var serializer = new AggregateResultDeserializer(_resultSerializer);
                var operation = new ReadCommandOperation<AggregateResult>(CollectionNamespace.DatabaseNamespace, command, serializer, MessageEncoderSettings);

                var result = await operation.ExecuteAsync(channelSource, binding.ReadPreference, cancellationToken).ConfigureAwait(false);

                return CreateCursor(channelSource, command, result);
            }
        }

        /// <summary>
        /// Returns an AggregateExplainOperation for this AggregateOperation.
        /// </summary>
        /// <param name="verbosity">The verbosity.</param>
        /// <returns>An AggregateExplainOperation.</returns>
        public IReadOperation<BsonDocument> ToExplainOperation(ExplainVerbosity verbosity)
        {
            return new AggregateExplainOperation(_collectionNamespace, _pipeline, _messageEncoderSettings)
            {
                AllowDiskUse = _allowDiskUse,
                MaxTime = _maxTime
            };
        }

        internal BsonDocument CreateCommand(SemanticVersion serverVersion)
        {
            var command = new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };

            if (serverVersion >= __version26 && _useCursor.GetValueOrDefault(true))
            {
                command["cursor"] = new BsonDocument
                {
                    { "batchSize", () => _batchSize.Value, _batchSize.HasValue }
                };
            }
            return command;
        }

        private AsyncCursor<TResult> CreateCursor(IChannelSourceHandle channelSource, BsonDocument command, AggregateResult result)
        {
            if (channelSource.ServerDescription.Version >= __version26 && _useCursor.GetValueOrDefault(true))
            {
                return CreateCursorFromCursorResult(channelSource, command, result);
            }

            return CreateCursorFromInlineResult(command, result);
        }

        private AsyncCursor<TResult> CreateCursorFromCursorResult(IChannelSourceHandle channelSource, BsonDocument command, AggregateResult result)
        {
            return new AsyncCursor<TResult>(
                channelSource.Fork(),
                CollectionNamespace,
                command,
                result.Results,
                result.CursorId.GetValueOrDefault(0),
                _batchSize ?? 0,
                0, // limit
                _resultSerializer,
                MessageEncoderSettings);
        }

        private AsyncCursor<TResult> CreateCursorFromInlineResult(BsonDocument command, AggregateResult result)
        {
            return new AsyncCursor<TResult>(
                null, // channelSource
                CollectionNamespace,
                command,
                result.Results,
                0,
                0, // batchSize
                0, // limit
                _resultSerializer,
                MessageEncoderSettings);
        }

        private void EnsureIsReadOnlyPipeline()
        {
            if (Pipeline.Any(s => s.GetElement(0).Name == "$out"))
            {
                throw new ArgumentException("The pipeline for an AggregateOperation contains a $out operator. Use AggregateOutputToCollectionOperation instead.");
            }
        }

        private class AggregateResult
        {
            public long? CursorId;
            public TResult[] Results;
        }

        private class AggregateResultDeserializer : SerializerBase<AggregateResult>
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
        }

        private class CursorDeserializer : SerializerBase<AggregateResult>
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
                    if (elementName == "id")
                    {
                        result.CursorId = new Int64Serializer().Deserialize(context);
                    }
                    else if (elementName == "firstBatch")
                    {
                        var arraySerializer = new ArraySerializer<TResult>(_resultSerializer);
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
        }
    }
}
