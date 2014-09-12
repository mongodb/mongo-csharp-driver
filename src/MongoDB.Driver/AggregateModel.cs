using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for running an aggregation pipeline.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class AggregateModel<TDocument, TResult>
    {
        // fields
        private bool? _allowDiskUse;
        private int? _batchSize;
        private TimeSpan? _maxTime;
        private readonly IReadOnlyList<object> _pipeline;
        private bool? _useCursor;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateModel{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public AggregateModel(IEnumerable<object> pipeline)
        {
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
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
        public IReadOnlyList<object> Pipeline
        {
            get { return _pipeline; }
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
