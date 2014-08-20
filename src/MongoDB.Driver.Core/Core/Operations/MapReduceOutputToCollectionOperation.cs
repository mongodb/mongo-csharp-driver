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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class MapReduceOutputToCollectionOperation : MapReduceOperationBase, IWriteOperation<BsonDocument>
    {
        // fields
        private readonly bool? _nonAtomicOutput;
        private readonly string _outputCollectionName;
        private readonly string _outputDatabaseName;
        private readonly MapReduceOutputMode _outputMode;
        private readonly bool? _shardedOutput;

        // constructors
        public MapReduceOutputToCollectionOperation(
            string databaseName,
            string collectionName,
            string outputCollectionName,
            BsonJavaScript mapFunction,
            BsonJavaScript reduceFunction,
            BsonDocument query = null)
            : base(
                databaseName,
                collectionName,
                mapFunction,
                reduceFunction,
                query)
        {
            _outputCollectionName = Ensure.IsNotNullOrEmpty(outputCollectionName, "outputCollectionName");
            _outputMode = MapReduceOutputMode.Replace;
        }

        private MapReduceOutputToCollectionOperation(
            string collectionName,
            string databaseName,
            BsonJavaScript finalizeFunction,
            bool? javaScriptMode,
            long? limit,
            BsonJavaScript mapFunction,
            bool? nonAtomicOutput,
            string outputCollectionName,
            string outputDatabaseName,
            MapReduceOutputMode outputMode,
            BsonDocument query,
            BsonJavaScript reduceFunction,
            BsonDocument scope,
            bool? shardedOutput,
            BsonDocument sort,
            bool? verbose)
            : base(
                collectionName,
                databaseName,
                finalizeFunction,
                javaScriptMode,
                limit,
                mapFunction,
                query,
                reduceFunction,
                scope,
                sort,
                verbose)
        {
            _nonAtomicOutput = nonAtomicOutput;
            _outputCollectionName = outputCollectionName;
            _outputDatabaseName = outputDatabaseName;
            _outputMode = outputMode;
            _shardedOutput = shardedOutput;
        }

        // properties
        public bool? NonAtomicOutput
        {
            get { return _nonAtomicOutput; }
        }

        public string OutputCollectionName
        {
            get { return _outputCollectionName; }
        }

        public string OutputDatabaseName
        {
            get { return _outputDatabaseName; }
        }

        public MapReduceOutputMode OutputMode
        {
            get { return _outputMode; }
        }

        public bool? ShardedOutput
        {
            get { return _shardedOutput; }
        }

        // methods
        protected override BsonDocument CreateOutputOptions()
        {
            Ensure.IsNullOrNotEmpty(_outputDatabaseName, "DatabaseName");
            Ensure.IsNotNullOrEmpty(_outputCollectionName, "OutputCollectionName");

            var action = _outputMode.ToString().ToLowerInvariant();
            return new BsonDocument
            {
                { action, _outputCollectionName },
                { "db", _outputDatabaseName, _outputDatabaseName != null },
                { "sharded", () => _shardedOutput.Value, _shardedOutput.HasValue },
                { "nonAtomic", () => _nonAtomicOutput.Value, _nonAtomicOutput.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(DatabaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public MapReduceOutputToCollectionOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (CollectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (DatabaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithFinalizeFunction(BsonJavaScript value)
        {
            return object.ReferenceEquals(FinalizeFunction, value) ? this : new Builder(this) { _finalizeFunction = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithJavaScriptMode(bool? value)
        {
            return (JavaScriptMode == value) ? this : new Builder(this) { _javaScriptMode = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithLimit(long? value)
        {
            return (Limit == value) ? this : new Builder(this) { _limit = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithMapFunction(BsonJavaScript value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(MapFunction, value) ? this : new Builder(this) { _mapFunction = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithNonAtomicOutput(bool? value)
        {
            return (_nonAtomicOutput == value) ? this : new Builder(this) { _nonAtomicOutput = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithOutputCollectionName(string value)
        {
            Ensure.IsNullOrNotEmpty(value, "value");
            return (_outputCollectionName == value) ? this : new Builder(this) { _outputCollectionName = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithOutputDatabaseName(string value)
        {
            Ensure.IsNullOrNotEmpty(value, "value");
            return (_outputDatabaseName == value) ? this : new Builder(this) { _outputDatabaseName = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithOutputMode(MapReduceOutputMode value)
        {
            return (_outputMode == value) ? this : new Builder(this) { _outputMode = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithQuery(BsonDocument value)
        {
            return object.ReferenceEquals(Query, value) ? this : new Builder(this) { _query = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithReduceFunction(BsonJavaScript value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(ReduceFunction, value) ? this : new Builder(this) { _reduceFunction = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithScope(BsonDocument value)
        {
            return object.ReferenceEquals(Scope, value) ? this : new Builder(this) { _scope = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithShardedOutput(bool? value)
        {
            return (_shardedOutput == value) ? this : new Builder(this) { _shardedOutput = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithSort(BsonDocument value)
        {
            return object.ReferenceEquals(Sort, value) ? this : new Builder(this) { _sort = value }.Build();
        }

        public MapReduceOutputToCollectionOperation WithVerbose(bool? value)
        {
            return (Verbose == value) ? this : new Builder(this) { _verbose = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public BsonJavaScript _finalizeFunction;
            public bool? _javaScriptMode;
            public long? _limit;
            public BsonJavaScript _mapFunction;
            public bool? _nonAtomicOutput;
            public string _outputCollectionName;
            public string _outputDatabaseName;
            public MapReduceOutputMode _outputMode;
            public BsonDocument _query;
            public BsonJavaScript _reduceFunction;
            public BsonDocument _scope;
            public bool? _shardedOutput;
            public BsonDocument _sort;
            public bool? _verbose;

            // constructors
            public Builder(MapReduceOutputToCollectionOperation other)
            {
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _finalizeFunction = other.FinalizeFunction;
                _javaScriptMode = other.JavaScriptMode;
                _limit = other.Limit;
                _mapFunction = other.MapFunction;
                _nonAtomicOutput = other.NonAtomicOutput;
                _outputCollectionName = other.OutputCollectionName;
                _outputDatabaseName = other.OutputDatabaseName;
                _outputMode = other.OutputMode;
                _query = other.Query;
                _reduceFunction = other.ReduceFunction;
                _scope = other.Scope;
                _shardedOutput = other.ShardedOutput;
                _sort = other.Sort;
                _verbose = other.Verbose;
            }

            // methods
            public MapReduceOutputToCollectionOperation Build()
            {
                return new MapReduceOutputToCollectionOperation(
                    _collectionName,
                    _databaseName,
                    _finalizeFunction,
                    _javaScriptMode,
                    _limit,
                    _mapFunction,
                    _nonAtomicOutput,
                    _outputCollectionName,
                    _outputDatabaseName,
                    _outputMode,
                    _query,
                    _reduceFunction,
                    _scope,
                    _shardedOutput,
                    _sort,
                    _verbose);
            }
        }
    }
}
