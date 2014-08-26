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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class AggregateOperationBase
    {
        // fields
        private bool? _allowDiskUsage;
        private string _collectionName;
        private string _databaseName;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private IReadOnlyList<BsonDocument> _pipeline;

        // constructors
        protected AggregateOperationBase(
            string databaseName,
            string collectionName,
            IEnumerable<BsonDocument> pipeline,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public bool? AllowDiskUsage
        {
            get { return _allowDiskUsage; }
            set { _allowDiskUsage = value; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
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

        public IReadOnlyList<BsonDocument> Pipeline
        {
            get { return _pipeline; }
            set { _pipeline = Ensure.IsNotNull(value, "value").ToList(); }
        }

        // methods
        public virtual BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "aggregate", _collectionName },
                { "pipeline", new BsonArray(_pipeline) },
                { "allowDiskUsage", () => _allowDiskUsage.Value, _allowDiskUsage.HasValue }
            };
        }
    }
}
