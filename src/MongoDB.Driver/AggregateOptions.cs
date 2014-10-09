using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for running an aggregation pipeline.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class AggregateOptions<TResult>
    {
        // fields
        private bool? _allowDiskUse;
        private int? _batchSize;
        private TimeSpan? _maxTime;
        private IList<object> _pipeline;
        private IBsonSerializer<TResult> _resultSerializer;
        private bool? _useCursor;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateOptions{TResult}" /> class.
        /// </summary>
        public AggregateOptions()
        {
            _pipeline = new List<object>();
        }

        // properties
        /// <summary>
        /// Gets or sets the allow disk use.
        /// </summary>
        public bool? AllowDiskUse
        {
            get { return _allowDiskUse; }
            set { _allowDiskUse = value; }
        }

        /// <summary>
        /// Gets or sets the size of the batch.
        /// </summary>
        /// <value>
        /// The size of the batch.
        /// </value>
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
        /// Gets the pipeline.
        /// </summary>
        public IList<object> Pipeline
        {
            get { return _pipeline; }
            set { _pipeline = value ?? new List<object>(); }
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
        /// Gets or sets the use cursor.
        /// </summary>
        public bool? UseCursor
        {
            get { return _useCursor; }
            set { _useCursor = value; }
        }
    }
}
