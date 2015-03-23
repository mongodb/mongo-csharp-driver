﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal class FindFluent<TDocument, TProjection> : FindFluentBase<TDocument, TProjection>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private FilterDefinition<TDocument> _filter;
        private readonly FindOptions<TDocument, TProjection> _options;

        // constructors
        public FindFluent(IMongoCollection<TDocument> collection, FilterDefinition<TDocument> filter, FindOptions<TDocument, TProjection> options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _filter = Ensure.IsNotNull(filter, "filter");
            _options = Ensure.IsNotNull(options, "options");
        }

        // properties
        public override FilterDefinition<TDocument> Filter
        {
            get { return _filter; }
            set { _filter = Ensure.IsNotNull(value, "value"); }
        }

        public override FindOptions<TDocument, TProjection> Options
        {
            get { return _options; }
        }

        // methods
        public override Task<long> CountAsync(CancellationToken cancellationToken)
        {
            BsonValue hint = null;
            if (_options.Modifiers != null)
            {
                _options.Modifiers.TryGetValue("$hint", out hint);
            }
            var options = new CountOptions
            {
                Hint = hint,
                Limit = _options.Limit,
                MaxTime = _options.MaxTime,
                Skip = _options.Skip
            };

            return _collection.CountAsync(_filter, options, cancellationToken);
        }

        public override IFindFluent<TDocument, TProjection> Limit(int? limit)
        {
            _options.Limit = limit;
            return this;
        }

        public override IFindFluent<TDocument, TNewProjection> Project<TNewProjection>(ProjectionDefinition<TDocument, TNewProjection> projection)
        {
            var newOptions = new FindOptions<TDocument, TNewProjection>
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
                Skip = _options.Skip,
                Sort = _options.Sort,
            };
            return new FindFluent<TDocument, TNewProjection>(_collection, _filter, newOptions);
        }

        public override IFindFluent<TDocument, TProjection> Skip(int? skip)
        {
            _options.Skip = skip;
            return this;
        }

        public override IFindFluent<TDocument, TProjection> Sort(SortDefinition<TDocument> sort)
        {
            _options.Sort = sort;
            return this;
        }

        public override Task<IAsyncCursor<TProjection>> ToCursorAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return _collection.FindAsync(_filter, _options, cancellationToken);
        }
    }
}