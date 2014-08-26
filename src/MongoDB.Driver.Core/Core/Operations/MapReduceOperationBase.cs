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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class MapReduceOperationBase
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private BsonJavaScript _finalizeFunction;
        private bool? _javaScriptMode;
        private long? _limit;
        private BsonJavaScript _mapFunction;
        private TimeSpan? _maxTime;
        private MessageEncoderSettings _messageEncoderSettings;
        private BsonDocument _query;
        private BsonJavaScript _reduceFunction;
        private BsonDocument _scope;
        private BsonDocument _sort;
        private bool? _verbose;

        // constructors
        protected MapReduceOperationBase(string databaseName, string collectionName, BsonJavaScript mapFunction, BsonJavaScript reduceFunction, BsonDocument query, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _mapFunction = Ensure.IsNotNull(mapFunction, "mapFunction");
            _reduceFunction = Ensure.IsNotNull(reduceFunction, "reduceFunction");
            _query = query;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
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

        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
            set { _finalizeFunction = value; }
        }

        public bool? JavaScriptMode
        {
            get { return _javaScriptMode; }
            set { _javaScriptMode = value; }
        }

        public long? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        public BsonJavaScript MapFunction
        {
            get { return _mapFunction; }
            set { _mapFunction = Ensure.IsNotNull(value, "value"); }
        }

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrGreaterThanZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
            set { _reduceFunction = value; }
        }

        public BsonDocument Scope
        {
            get { return _scope; }
            set { _scope = value; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        public bool? Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "mapReduce", _collectionName },
                { "map", _mapFunction },
                { "reduce", _reduceFunction },
                { "out" , CreateOutputOptions() },
                { "query", _query, _query != null },
                { "sort", _sort, _sort != null },
                { "limit", () => _limit.Value, _limit.HasValue },
                { "finalize", _finalizeFunction, _finalizeFunction != null },
                { "scope", _scope, _scope != null },
                { "jsMode", () => _javaScriptMode.Value, _javaScriptMode.HasValue },
                { "verbose", () => _verbose.Value, _verbose.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        protected abstract BsonDocument CreateOutputOptions();
    }
}
