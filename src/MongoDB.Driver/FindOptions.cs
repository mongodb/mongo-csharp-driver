﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for a find operation.
    /// </summary>
    public abstract class FindOptionsBase
    {
        // fields
        private bool _allowPartialResults;
        private int? _batchSize;
        private string _comment;
        private CursorType _cursorType;
        private TimeSpan? _maxTime;
        private BsonDocument _modifiers;
        private bool _noCursorTimeout;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindOptionsBase"/> class.
        /// </summary>
        public FindOptionsBase()
        {
            _cursorType = CursorType.NonTailable;
        }

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether to allow partial results when some shards are unavailable.
        /// </summary>
        public bool AllowPartialResults
        {
            get { return _allowPartialResults; }
            set { _allowPartialResults = value; }
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
        public CursorType CursorType
        {
            get { return _cursorType; }
            set { _cursorType = value; }
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
        /// Gets or sets whether a cursor will time out.
        /// </summary>
        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
            set { _noCursorTimeout = value; }
        }
    }

    /// <summary>
    /// Options for finding documents.
    /// </summary>
    public class FindOptions : FindOptionsBase
    { }

    /// <summary>
    /// Options for finding documents.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TProjection">The type of the projection (same as TDocument if there is no projection).</typeparam>
    public class FindOptions<TDocument, TProjection> : FindOptionsBase
    {
        // fields
        private int? _limit;
        private ProjectionDefinition<TDocument, TProjection> _projection;
        private int? _skip;
        private SortDefinition<TDocument> _sort;

        // properties
        /// <summary>
        /// Gets or sets how many documents to return.
        /// </summary>
        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the projection.
        /// </summary>
        public ProjectionDefinition<TDocument, TProjection> Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        /// <summary>
        /// Gets or sets how many documents to skip before returning the rest.
        /// </summary>
        public int? Skip
        {
            get { return _skip; }
            set { _skip = value; }
        }

        /// <summary>
        /// Gets or sets the sort.
        /// </summary>
        public SortDefinition<TDocument> Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }

    /// <summary>
    /// Options for finding documents.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document and the result.</typeparam>
    public class FindOptions<TDocument> : FindOptions<TDocument, TDocument>
    {
    }
}
