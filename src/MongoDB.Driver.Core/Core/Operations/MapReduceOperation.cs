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
    public class MapReduceOperation : MapReduceOperationBase, IReadOperation<IEnumerable<BsonValue>>
    {
        // constructors
        public MapReduceOperation(string databaseName, string collectionName, BsonJavaScript mapFunction, BsonJavaScript reduceFunction, BsonDocument query = null)
            : base(
                databaseName,
                collectionName,
                mapFunction,
                reduceFunction,
                query)
        {
        }

        private MapReduceOperation(
            string collectionName,
            string databaseName,
            BsonJavaScript finalizeFunction,
            bool? javaScriptMode,
            long? limit,
            BsonJavaScript mapFunction,
            BsonDocument query,
            BsonJavaScript reduceFunction,
            BsonDocument scope,
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
        }

        // methods
        protected override BsonDocument CreateOutputOptions()
        {
            return new BsonDocument("inline", 1);
        }

        public async Task<IEnumerable<BsonValue>> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var result = await ExecuteCommandAsync(binding, timeout, cancellationToken);
            return result["results"].AsBsonArray;
        }

        public async Task<BsonDocument> ExecuteCommandAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation(DatabaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public MapReduceOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (CollectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public MapReduceOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (DatabaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public MapReduceOperation WithFinalizeFunction(BsonJavaScript value)
        {
            return object.ReferenceEquals(FinalizeFunction, value) ? this : new Builder(this) { _finalizeFunction = value }.Build();
        }

        public MapReduceOperation WithJavaScriptMode(bool? value)
        {
            return (JavaScriptMode == value) ? this : new Builder(this) { _javaScriptMode = value }.Build();
        }

        public MapReduceOperation WithLimit(long? value)
        {
            return (Limit == value) ? this : new Builder(this) { _limit = value }.Build();
        }

        public MapReduceOperation WithMapFunction(BsonJavaScript value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(MapFunction, value) ? this : new Builder(this) { _mapFunction = value }.Build();
        }

        public MapReduceOperation WithQuery(BsonDocument value)
        {
            return object.ReferenceEquals(Query, value) ? this : new Builder(this) { _query = value }.Build();
        }

        public MapReduceOperation WithReduceFunction(BsonJavaScript value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(ReduceFunction, value) ? this : new Builder(this) { _reduceFunction = value }.Build();
        }

        public MapReduceOperation WithScope(BsonDocument value)
        {
            return object.ReferenceEquals(Scope, value) ? this : new Builder(this) { _scope = value }.Build();
        }

        public MapReduceOperation WithSort(BsonDocument value)
        {
            return object.ReferenceEquals(Sort, value) ? this : new Builder(this) { _sort = value }.Build();
        }

        public MapReduceOperation WithVerbose(bool? value)
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
            public BsonDocument _query;
            public BsonJavaScript _reduceFunction;
            public BsonDocument _scope;
            public BsonDocument _sort;
            public bool? _verbose;

            // constructors
            public Builder(MapReduceOperation other)
            {
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _finalizeFunction = other.FinalizeFunction;
                _javaScriptMode = other.JavaScriptMode;
                _limit = other.Limit;
                _mapFunction = other.MapFunction;
                _query = other.Query;
                _reduceFunction = other.ReduceFunction;
                _scope = other.Scope;
                _sort = other.Sort;
                _verbose = other.Verbose;
            }

            // methods
            public MapReduceOperation Build()
            {
                return new MapReduceOperation(
                    _collectionName,
                    _databaseName,
                    _finalizeFunction,
                    _javaScriptMode,
                    _limit,
                    _mapFunction,
                    _query,
                    _reduceFunction,
                    _scope,
                    _sort,
                    _verbose);
            }
        }
    }
}
