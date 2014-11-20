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
    /// Options for finding documents.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class FindOptions<TResult>
    {
        // fields
        private bool _allowPartialResults;
        private int? _batchSize;
        private string _comment;
        private CursorType _cursorType;
        private int? _limit;
        private TimeSpan? _maxTime;
        private BsonDocument _modifiers;
        private bool _noCursorTimeout;
        private object _projection;
        private IBsonSerializer<TResult> _resultSerializer;
        private int? _skip;
        private object _sort;

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether to allow partial results 
        /// in a sharded system in the case where 1 or more shards is down.
        /// </summary>
        /// <value>
        ///   <c>true</c> if to allow partial results; otherwise, <c>false</c>.
        /// </value>
        public bool AllowPartialResults
        {
            get { return _allowPartialResults; }
            set { _allowPartialResults = value; }
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
        /// Gets or sets the type of the cursor.
        /// </summary>
        /// <value>
        /// The type of the cursor.
        /// </value>
        public CursorType CursorType
        {
            get { return _cursorType; }
            set { _cursorType = value; }
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
    }
}
