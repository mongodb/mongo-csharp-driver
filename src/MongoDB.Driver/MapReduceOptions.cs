using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for a map reduce operation.
    /// </summary>
    public sealed class MapReduceOptions<TResult>
    {
        // fields
        private object _filter;
        private BsonJavaScript _finalizer;
        private bool? _javaScriptMode;
        private long? _limit;
        private TimeSpan? _maxTime;
        private MapReduceOutput _out;
        private IBsonSerializer<TResult> _resultSerializer;
        private object _scope;
        private object _sort;
        private bool? _verbose;

        // properties
        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        public object Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        /// <summary>
        /// Gets or sets the finalizer.
        /// </summary>
        public BsonJavaScript Finalizer
        {
            get { return _finalizer; }
            set { _finalizer = value; }
        }

        /// <summary>
        /// Gets or sets the java script mode.
        /// </summary>
        public bool? JavaScriptMode
        {
            get { return _javaScriptMode; }
            set { _javaScriptMode = value; }
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
        /// Gets or sets the maximum time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the out.
        /// </summary>
        public MapReduceOutput Out
        {
            get { return _out; }
            set { _out = value; }
        }

        /// <summary>
        /// Gets or sets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
            set { _resultSerializer = value; }
        }

        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        public object Scope
        {
            get { return _scope; }
            set { _scope = value; }
        }

        /// <summary>
        /// Gets or sets the sort.
        /// </summary>
        public object Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }

        /// <summary>
        /// Gets or sets whether to include timing information.
        /// </summary>
        public bool? Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }
    }

    /// <summary>
    /// Indicates the type of output for a Map/Reduce operation.
    /// </summary>
    public abstract class MapReduceOutput
    {
        private static MapReduceOutput __inline = new InlineOutput();

        private MapReduceOutput()
        { }

        /// <summary>
        /// An inline map/reduce output.
        /// </summary>
        public static MapReduceOutput Inline
        {
            get { return __inline; }
        }

        /// <summary>
        /// A merge map/reduce output.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="sharded">The sharded.</param>
        /// <param name="nonAtomic">The non atomic.</param>
        /// <returns>A merge map/reduce output.</returns>
        public static MapReduceOutput Merge(string collectionName, string databaseName = null, bool? sharded = null, bool? nonAtomic = null)
        {
            Ensure.IsNotNull(collectionName, "collectionName");
            return new CollectionOutput(collectionName, Core.Operations.MapReduceOutputMode.Merge, databaseName, sharded, nonAtomic);
        }

        /// <summary>
        /// A reduce map/reduce output.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="sharded">The sharded.</param>
        /// <param name="nonAtomic">The non atomic.</param>
        /// <returns>A reduce map/reduce output.</returns>
        public static MapReduceOutput Reduce(string collectionName, string databaseName = null, bool? sharded = null, bool? nonAtomic = null)
        {
            Ensure.IsNotNull(collectionName, "collectionName");
            return new CollectionOutput(collectionName, Core.Operations.MapReduceOutputMode.Reduce, databaseName, sharded, nonAtomic);
        }

        /// <summary>
        /// A replace map/reduce output.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="sharded">The sharded.</param>
        /// <param name="nonAtomic">The non atomic.</param>
        /// <returns></returns>
        public static MapReduceOutput Replace(string collectionName, string databaseName = null, bool? sharded = null, bool? nonAtomic = null)
        {
            Ensure.IsNotNull(collectionName, "collectionName");
            return new CollectionOutput(collectionName, Core.Operations.MapReduceOutputMode.Replace, databaseName, sharded, nonAtomic);
        }

        internal sealed class InlineOutput : MapReduceOutput
        {
            internal InlineOutput()
            { }
        }

        internal sealed class CollectionOutput : MapReduceOutput
        {
            private readonly string _collectionName;
            private readonly string _databaseName;
            private readonly bool? _nonAtomic;
            private readonly Core.Operations.MapReduceOutputMode _outputMode;
            private readonly bool? _sharded;

            internal CollectionOutput(string collectionName, Core.Operations.MapReduceOutputMode outputMode, string databaseName = null, bool? sharded = null, bool? nonAtomic = null)
            {
                _collectionName = collectionName;
                _outputMode = outputMode;
                _databaseName = databaseName;
                _sharded = sharded;
                _nonAtomic = nonAtomic;
            }

            public string CollectionName
            {
                get { return _collectionName; }
            }

            public string DatabaseName
            {
                get { return _databaseName; }
            }

            public bool? NonAtomic
            {
                get { return _nonAtomic; }
            }

            public Core.Operations.MapReduceOutputMode OutputMode
            {
                get { return _outputMode; }
            }

            public bool? Sharded
            {
                get { return _sharded; }
            }
        }
    }
}
