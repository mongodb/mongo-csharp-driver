using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal class FindFluent<TDocument, TResult> : IOrderedFindFluent<TDocument, TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private object _filter;
        private readonly FindOptions<TResult> _options;

        // constructors
        public FindFluent(IMongoCollection<TDocument> collection, object filter, FindOptions<TResult> options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _filter = Ensure.IsNotNull(filter, "filter");
            _options = Ensure.IsNotNull(options, "options");
        }

        // properties
        public IMongoCollection<TDocument> Collection
        {
            get { return _collection; }
        }

        public object Filter
        {
            get { return _filter; }
            set { _filter = Ensure.IsNotNull(value, "value"); }
        }

        public FindOptions<TResult> Options
        {
            get { return _options; }
        }

        // methods
        public Task<long> CountAsync(CancellationToken cancellationToken)
        {
            BsonValue hint;
            _options.Modifiers.TryGetValue("$hint", out hint);
            var options = new CountOptions
            {
                Hint = hint,
                Limit = _options.Limit,
                MaxTime = _options.MaxTime,
                Skip = _options.Skip
            };

            return _collection.CountAsync(_filter, options, cancellationToken);
        }

        public IFindFluent<TDocument, TResult> Limit(int? limit)
        {
            _options.Limit = limit;
            return this;
        }

        public IFindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection)
        {
            return Projection<TNewResult>(projection, null);
        }

        public IFindFluent<TDocument, TNewResult> Projection<TNewResult>(object projection, IBsonSerializer<TNewResult> resultSerializer)
        {
            var newOptions = new FindOptions<TNewResult>
            {
                AllowPartialResults = _options.AllowPartialResults,
                BatchSize = _options.BatchSize,
                Comment = _options.Comment,
                CursorType = _options.CursorType,
                Limit = _options.Limit,
                MaxTime = _options.MaxTime,
                Modifiers = _options.Modifiers,
                NoCursorTimeout = _options.NoCursorTimeout,
                Projection = projection,
                ResultSerializer = resultSerializer ?? _collection.Settings.SerializerRegistry.GetSerializer<TNewResult>(),
                Skip = _options.Skip,
                Sort = _options.Sort,
            };
            return new FindFluent<TDocument, TNewResult>(_collection, _filter, newOptions);
        }

        public IFindFluent<TDocument, TResult> Skip(int? skip)
        {
            _options.Skip = skip;
            return this;
        }

        public IFindFluent<TDocument, TResult> Sort(object sort)
        {
            _options.Sort = sort;
            return this;
        }

        public Task<IAsyncCursor<TResult>> ToCursorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _collection.FindAsync(_filter, _options, cancellationToken);
        }
    }
}