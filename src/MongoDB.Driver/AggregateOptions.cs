using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for an aggregate operation.
    /// </summary>
    public abstract class AggregateOptionsBase
    {
        // fields
        private bool? _allowDiskUse;
        private int? _batchSize;
        private TimeSpan? _maxTime;
        private bool? _useCursor;

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether to allow disk use.
        /// </summary>
        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        /// <summary>
        /// Gets or sets the size of a batch.
        /// </summary>
        public int? BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = value; }
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
        /// Gets or sets a value indicating whether to use a cursor.
        /// </summary>
        public bool? UseCursor
        {
            get { return _useCursor; }
            set { _useCursor = value; }
        }
    }

    /// <summary>
    /// Options for an aggregate operation.
    /// </summary>
    public class AggregateOptions : AggregateOptionsBase
    { }

    /// <summary>
    /// Options for an aggregate operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class AggregateOptions<TResult> : AggregateOptionsBase
    {
        // fields
        private IBsonSerializer<TResult> _resultSerializer;

        // properties
        /// <summary>
        /// Gets or sets the result serializer.
        /// </summary>
        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
            set { _resultSerializer = value; }
        }
    }
}
