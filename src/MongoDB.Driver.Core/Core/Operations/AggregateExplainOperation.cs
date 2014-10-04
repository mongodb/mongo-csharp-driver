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
    public class AggregateExplainOperation : IReadOperation<BsonDocument>
    {
        // fields
        private bool? _allowDiskUse;
        private CollectionNamespace _collectionNamespace;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private IReadOnlyList<BsonDocument> _pipeline;

        // constructors
        public AggregateExplainOperation(CollectionNamespace collectionNamespace, IEnumerable<BsonDocument> pipeline, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
        }

        // methods
        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "aggregate", _collectionNamespace.CollectionName },
                { "explain", true },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUse", () => _allowDiskUse.Value, _allowDiskUse.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public Task<BsonDocument> ExecuteAsync(IReadBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = CreateCommand();

            var operation = new ReadCommandOperation<BsonDocument>(
                _collectionNamespace.DatabaseNamespace,
                command,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);

            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
