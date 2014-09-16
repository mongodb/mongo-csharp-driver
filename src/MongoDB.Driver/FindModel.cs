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
    /// Model for finding documents.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class FindModel<TResult>
    {
        // fields
        private bool _awaitData;
        private int? _batchSize;
        private string _comment;
        private object _criteria;
        private int? _limit;
        private TimeSpan? _maxTime;
        private BsonDocument _modifiers;
        private bool _noCursorTimeout;
        private bool _partial;
        private object _projection;
        private IBsonSerializer<TResult> _resultSerializer;
        private int? _skip;
        private object _sort;
        private bool _tailable;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindModel{TResult}"/> class.
        /// </summary>
        public FindModel()
        {
            _awaitData = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [await data].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [await data]; otherwise, <c>false</c>.
        /// </value>
        public bool AwaitData
        {
            get { return _awaitData; }
            set { _awaitData = value; }
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
        /// Gets or sets the comment.
        /// </summary>
        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or sets the criteria.
        /// </summary>
        public object Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public int? Limit
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
        /// Gets or sets the modifiers.
        /// </summary>
        public BsonDocument Modifiers
        {
            get { return _modifiers; }
            set { _modifiers = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [no cursor timeout].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [no cursor timeout]; otherwise, <c>false</c>.
        /// </value>
        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [partial].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [partial]; otherwise, <c>false</c>.
        /// </value>
        public bool Partial
        {
            get { return _partial; }
            set { _partial = value; }
        }

        /// <summary>
        /// Gets or sets the projection.
        /// </summary>
        public object Projection
        {
            get { return _projection; }
            set { _projection = value; }
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
        /// Gets or sets the skip.
        /// </summary>
        public int? Skip
        {
            get { return _skip; }
            set { _skip = value; }
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
        /// Gets or sets a value indicating whether [tailable].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [tailable]; otherwise, <c>false</c>.
        /// </value>
        public bool Tailable
        {
            get { return _tailable; }
            set { _tailable = value; }
        }
    }
}
