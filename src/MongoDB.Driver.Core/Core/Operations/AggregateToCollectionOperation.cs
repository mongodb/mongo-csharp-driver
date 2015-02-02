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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an aggregate operation that writes the results to an output collection.
    /// </summary>
    public class AggregateToCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private bool? _allowDiskUse;
        private readonly CollectionNamespace _collectionNamespace;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IReadOnlyList<BsonDocument> _pipeline;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateToCollectionOperation"/> class.
        /// </summary>
        /// <param name="collectionNamespace">The collection namespace.</param>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="messageEncoderSettings">The message encoder settings.</param>
        public AggregateToCollectionOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");

            EnsureIsOutputToCollectionPipeline();
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

        // methods
        /// <inheritdoc/>
        public Task<BsonDocument> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");

            var command = CreateCommand();
            var operation = new WriteCommandOperation<BsonDocument>(CollectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, MessageEncoderSettings);
            return operation.ExecuteAsync(binding, cancellationToken);
        }

        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        private void EnsureIsOutputToCollectionPipeline()
        {
            var lastStage = _pipeline.LastOrDefault();
            if (lastStage == null || lastStage.GetElement(0).Name != "$out")
            {
                throw new ArgumentException("The last stage of the pipeline for an AggregateOutputToCollectionOperation must have a $out operator.", "pipeline");
            }
        }
    }
}
