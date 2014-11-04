using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Fluent interface for find.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class FindFluent<TDocument, TResult> : IAsyncCursorFactory<TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private object _criteria;
        private readonly FindOptions<TResult> _options;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FindFluent{TDocument, TResult}"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="criteria">The criteria.</param>
        public FindFluent(IMongoCollection<TDocument> collection, object criteria)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _criteria = Ensure.IsNotNull(criteria, "criteria");
            _options = new FindOptions<TResult>();
        }

        // properties
        /// <summary>
        /// Gets the criteria.
        /// </summary>
        public object Criteria
        {
            get { return _criteria; }
            set { _criteria = Ensure.IsNotNull(value, "value"); }
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public FindOptions<TResult> Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public MongoCollectionSettings Settings
        {
            get { return _collection.Settings; }
        }

        // methods
        /// <summary>
        /// Sets the await data flag.
        /// </summary>
        /// <param name="awaitData">if set to <c>true</c> [await data].</param>
        /// <returns>
        /// The fluent interface.
        /// </returns>
        public FindFluent<TDocument, TResult> AwaitData(bool awaitData)
        {
            _options.AwaitData = awaitData;
            return this;
        }

        /// <summary>
        /// Sets the batch size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> BatchSize(int? size)
        {
            _options.BatchSize = size;
            return this;
        }

        /// <summary>
        /// Sets the comment.
        /// </summary>
        /// <param name="comment">The comment.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Comment(string comment)
        {
            _options.Comment = comment;
            return this;
        }

        /// <summary>
        /// Sets the limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Limit(int? limit)
        {
            _options.Limit = limit;
            return this;
        }

        /// <summary>
        /// Sets the max time.
        /// </summary>
        /// <param name="maxTime">The maximum time.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> MaxTime(TimeSpan? maxTime)
        {
            _options.MaxTime = maxTime;
            return this;
        }

        /// <summary>
        /// Sets the modifiers.
        /// </summary>
        /// <param name="modifiers">The modifiers.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Modifiers(BsonDocument modifiers)
        {
            _options.Modifiers = modifiers;
            return this;
        }

        /// <summary>
        /// Sets the noCursorTimeout flag.
        /// </summary>
        /// <param name="noCursorTimeout">if set to <c>true</c> [no cursor timeout].</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> NoCursorTimeout(bool noCursorTimeout)
        {
            _options.NoCursorTimeout = noCursorTimeout;
            return this;
        }

        /// <summary>
        /// Sets the partial flag.
        /// </summary>
        /// <param name="partial">if set to <c>true</c> [partial].</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Partial(bool partial)
        {
            _options.Partial = partial;
            return this;
        }

        /// <summary>
        /// Sets the projection.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection)
        {
            return Projection<TNewResult>(projection, null);
        }

        /// <summary>
        /// Sets the projection.
        /// </summary>
        /// <typeparam name="TNewResult">The type of the new result.</typeparam>
        /// <param name="projection">The projection.</param>
        /// <param name="resultSerializer">The result serializer.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection, IBsonSerializer<TNewResult> resultSerializer)
        {
            var newFluent = new FindFluent<TDocument, TNewResult>(_collection, _criteria);
            newFluent._options.AwaitData = _options.AwaitData;
            newFluent._options.BatchSize = _options.BatchSize;
            newFluent._options.Comment = _options.Comment;
            newFluent._options.Limit = _options.Limit;
            newFluent._options.MaxTime = _options.MaxTime;
            newFluent._options.Modifiers = _options.Modifiers;
            newFluent._options.NoCursorTimeout = _options.NoCursorTimeout;
            newFluent._options.Partial = _options.Partial;
            newFluent._options.Projection = projection;
            newFluent._options.ResultSerializer = resultSerializer ?? _collection.Settings.SerializerRegistry.GetSerializer<TNewResult>();
            newFluent._options.Skip = _options.Skip;
            newFluent._options.Sort = _options.Sort;
            newFluent._options.Tailable = _options.Tailable;
            return newFluent;
        }

        /// <summary>
        /// Sets the skip.
        /// </summary>
        /// <param name="skip">The skip.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Skip(int? skip)
        {
            _options.Skip = skip;
            return this;
        }

        /// <summary>
        /// Sets the sort.
        /// </summary>
        /// <param name="sort">The sort.</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Sort(object sort)
        {
            _options.Sort = sort;
            return this;
        }

        /// <summary>
        /// Sets the tailable flag.
        /// </summary>
        /// <param name="tailable">if set to <c>true</c> [tailable].</param>
        /// <returns>The fluent interface.</returns>
        public FindFluent<TDocument, TResult> Tailable(bool tailable)
        {
            _options.Tailable = tailable;
            return this;
        }

        /// <summary>
        /// To the asynchronous enumerable.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable.</returns>
        public Task<IAsyncCursor<TResult>> CreateCursor(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _collection.FindAsync(_criteria, _options, null, cancellationToken);
        }
    }
}