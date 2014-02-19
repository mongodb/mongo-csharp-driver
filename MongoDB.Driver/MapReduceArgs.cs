/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the output mode for a map reduce operation.
    /// </summary>
    public enum MapReduceOutputMode
    {
        /// <summary>
        /// The output of the map reduce operation is returned inline.
        /// </summary>
        Inline,
        /// <summary>
        /// The output of the map reduce operation replaces an existing collection.
        /// </summary>
        Replace,
        /// <summary>
        /// The output of the map reduce operation is merged with an existing collection.
        /// </summary>
        Merge,
        /// <summary>
        /// The output of the map reduce operation is merged with an existing collection using the reduce function.
        /// </summary>
        Reduce
    }

    /// <summary>
    /// Represents arguments for the MapReduce command helper method.
    /// </summary>
    public class MapReduceArgs
    {
        // private fields
        private BsonJavaScript _finalizeFunction;
        private bool? _jsMode;
        private long? _limit;
        private BsonJavaScript _mapFunction;
        private TimeSpan? _maxTime;
        private string _outputCollectionName;
        private string _outputDatabaseName;
        private bool? _outputIsNonAtomic;
        private bool? _outputIsSharded;
        private MapReduceOutputMode _outputMode;
        private IMongoQuery _query;
        private BsonJavaScript _reduceFunction;
        private IMongoScope _scope;
        private IMongoSortBy _sortBy;
        private bool? _verbose;

        // public properties
        /// <summary>
        /// Gets or sets the finalize function.
        /// </summary>
        /// <value>
        /// The finalize function.
        /// </value>
        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
            set { _finalizeFunction = value; }
        }

        /// <summary>
        /// Gets or sets the JavaScript mode (if true all intermediate values are kept in memory as JavaScript objects).
        /// </summary>
        /// <value>
        /// The JavaScript mode.
        /// </value>
        public bool? JsMode
        {
            get { return _jsMode; }
            set { _jsMode = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>
        /// The limit.
        /// </value>
        public long? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the map function.
        /// </summary>
        /// <value>
        /// The map function.
        /// </value>
        public BsonJavaScript MapFunction
        {
            get { return _mapFunction; }
            set { _mapFunction = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        /// <value>
        /// The max time.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the name of the output collection.
        /// </summary>
        /// <value>
        /// The name of the output collection.
        /// </value>
        public string OutputCollectionName
        {
            get { return _outputCollectionName; }
            set { _outputCollectionName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the output database.
        /// </summary>
        /// <value>
        /// The name of the output database.
        /// </value>
        public string OutputDatabaseName
        {
            get { return _outputDatabaseName; }
            set { _outputDatabaseName = value; }
        }

        /// <summary>
        /// Gets or sets whether Merge and Reduce output should not be atomic.
        /// </summary>
        /// <value>
        /// Whether Merge and Reduce output should not be atomic.
        /// </value>
        public bool? OutputIsNonAtomic
        {
            get { return _outputIsNonAtomic; }
            set { _outputIsNonAtomic = value; }
        }

        /// <summary>
        /// Gets or sets whether the output is sharded.
        /// </summary>
        /// <value>
        /// Whether the output is sharded.
        /// </value>
        public bool? OutputIsSharded
        {
            get { return _outputIsSharded; }
            set { _outputIsSharded = value; }
        }

        /// <summary>
        /// Gets or sets the output mode.
        /// </summary>
        /// <value>
        /// The output mode.
        /// </value>
        public MapReduceOutputMode OutputMode
        {
            get { return _outputMode; }
            set { _outputMode = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the reduce function.
        /// </summary>
        /// <value>
        /// The reduce function.
        /// </value>
        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
            set { _reduceFunction = value; }
        }

        /// <summary>
        /// Gets or sets the scope (variables available to the map/reduce functions);
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        public IMongoScope Scope
        {
            get { return _scope; }
            set { _scope = value; }
        }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>
        /// The sort order.
        /// </value>
        public IMongoSortBy SortBy
        {
            get { return _sortBy; }
            set { _sortBy = value; }
        }

        /// <summary>
        /// Gets or sets whether to include extra information in the result (like timing).
        /// </summary>
        /// <value>
        /// Whether to include extra information in the result (like timing).
        /// </value>
        public bool? Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }
    }
}
