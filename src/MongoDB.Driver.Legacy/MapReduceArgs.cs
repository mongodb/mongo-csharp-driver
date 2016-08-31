/* Copyright 2010-2016 MongoDB Inc.
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
    /// Represents the output mode for a map-reduce operation.
    /// </summary>
    public enum MapReduceOutputMode
    {
        /// <summary>
        /// The output of the map-reduce operation is returned inline.
        /// </summary>
        Inline,
        /// <summary>
        /// The output of the map-reduce operation replaces an existing collection.
        /// </summary>
        Replace,
        /// <summary>
        /// The output of the map-reduce operation is merged with an existing collection.
        /// </summary>
        Merge,
        /// <summary>
        /// The output of the map-reduce operation is merged with an existing collection using the reduce function.
        /// </summary>
        Reduce
    }

    internal static class MapReduceOutputModeExtensionMethods
    {
        public static Core.Operations.MapReduceOutputMode ToCore(this MapReduceOutputMode outputMode)
        {
            switch (outputMode)
            {
                case MapReduceOutputMode.Merge:
                    return Core.Operations.MapReduceOutputMode.Merge;
                case MapReduceOutputMode.Reduce:
                    return Core.Operations.MapReduceOutputMode.Reduce;
                case MapReduceOutputMode.Replace:
                    return Core.Operations.MapReduceOutputMode.Replace;
                default:
                    var message = string.Format("Invalid output mode: {0}.", outputMode);
                    throw new ArgumentException(message, "outputMode");
            }
        }
    }

    /// <summary>
    /// Represents arguments for the MapReduce command helper method.
    /// </summary>
    public class MapReduceArgs
    {
        // private fields
        private bool? _bypassDocumentValidation;
        private Collation _collation;
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
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        /// <value>
        /// A value indicating whether to bypass document validation.
        /// </value>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        /// <summary>
        /// Gets or sets the finalize function.
        /// </summary>
        public BsonJavaScript FinalizeFunction
        {
            get { return _finalizeFunction; }
            set { _finalizeFunction = value; }
        }

        /// <summary>
        /// Gets or sets the JavaScript mode (if true all intermediate values are kept in memory as JavaScript objects).
        /// </summary>
        public bool? JsMode
        {
            get { return _jsMode; }
            set { _jsMode = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public long? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the map function.
        /// </summary>
        public BsonJavaScript MapFunction
        {
            get { return _mapFunction; }
            set { _mapFunction = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the name of the output collection.
        /// </summary>
        public string OutputCollectionName
        {
            get { return _outputCollectionName; }
            set { _outputCollectionName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the output database.
        /// </summary>
        public string OutputDatabaseName
        {
            get { return _outputDatabaseName; }
            set { _outputDatabaseName = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Merge and Reduce output should not be atomic.
        /// </summary>
        public bool? OutputIsNonAtomic
        {
            get { return _outputIsNonAtomic; }
            set { _outputIsNonAtomic = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the output is sharded.
        /// </summary>
        public bool? OutputIsSharded
        {
            get { return _outputIsSharded; }
            set { _outputIsSharded = value; }
        }

        /// <summary>
        /// Gets or sets the output mode.
        /// </summary>
        public MapReduceOutputMode OutputMode
        {
            get { return _outputMode; }
            set { _outputMode = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the reduce function.
        /// </summary>
        public BsonJavaScript ReduceFunction
        {
            get { return _reduceFunction; }
            set { _reduceFunction = value; }
        }

        /// <summary>
        /// Gets or sets the scope (variables available to the map-reduce functions);
        /// </summary>
        public IMongoScope Scope
        {
            get { return _scope; }
            set { _scope = value; }
        }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        public IMongoSortBy SortBy
        {
            get { return _sortBy; }
            set { _sortBy = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include extra information in the result (like timing).
        /// </summary>
        public bool? Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }
    }
}
