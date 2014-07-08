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

namespace MongoDB.Driver.Core.Operations
{
    public abstract class MapReduceOperationBase
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly BsonJavaScript _finalizeFunction;
        private readonly bool? _javaScriptMode;
        private readonly long? _limit;
        private readonly BsonJavaScript _mapFunction;
        private readonly BsonDocument _query;
        private readonly BsonJavaScript _reduceFunction;
        private readonly BsonDocument _scope;
        private readonly BsonDocument _sort;
        private readonly bool? _verbose;

        // constructors
        protected MapReduceOperationBase(string databaseName, string collectionName, BsonJavaScript mapFunction, BsonJavaScript reduceFunction, BsonDocument query = null)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _mapFunction = Ensure.IsNotNull(mapFunction, "mapFunction");
            _reduceFunction = Ensure.IsNotNull(reduceFunction, "reduceFunction");
            _query = query; // can be null
        }

        protected MapReduceOperationBase(
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
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _finalizeFunction = finalizeFunction;
            _javaScriptMode = javaScriptMode;
            _limit = limit;
            _mapFunction = mapFunction;
            _query = query;
            _reduceFunction = reduceFunction;
            _scope = scope;
            _sort = sort;
            _verbose = verbose;
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
        }

        public bool? JavaScriptMode
        {
            get { return _javaScriptMode; }
        }

        public long? Limit
        {
            get { return _limit; }
        }

        public BsonJavaScript MapFunction
        {
            get { return _mapFunction; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
        }

        public BsonDocument Scope
        {
            get { return _scope; }
        }

        public BsonDocument Sort
        {
            get { return _sort; }
        }

        public bool? Verbose
        {
            get { return _verbose; }
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
                { "verbose", () => _verbose.Value, _verbose.HasValue }
            };
        }

        protected abstract BsonDocument CreateOutputOptions();
    }
}
