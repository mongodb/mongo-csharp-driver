/* Copyright 2010-2014 MongoDB Inc.
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
    internal class AggregateFluent<TCollectionDocument, TDocument> : AggregateFluentBase<TDocument>
    {
        // fields
        private readonly IReadOnlyMongoCollection<TCollectionDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly List<AggregateStage> _pipeline;

        // constructors
        public AggregateFluent(IReadOnlyMongoCollection<TCollectionDocument> collection, IEnumerable<AggregateStage> pipeline, AggregateOptions options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _pipeline = Ensure.IsNotNull(pipeline, "pipeline").ToList();
            _options = Ensure.IsNotNull(options, "options");
        }

        // properties
        public override AggregateOptions Options
        {
            get { return _options; }
        }

        public override IList<AggregateStage> Pipeline
        {
            get { return _pipeline; }
        }

        // methods
        public override IAggregateFluent<TDocument> AppendStage(AggregateStage stage)
        {
            _pipeline.Add(stage);
            return this;
        }

        public override IAggregateFluent<TResult> AppendStage<TResult>(AggregateStage stage)
        {
            _pipeline.Add(stage);
            return new AggregateFluent<TCollectionDocument, TResult>(_collection, _pipeline, _options);
        }

        public override IAggregateFluent<TNewResult> Group<TNewResult>(Projection<TDocument, TNewResult> group)
        {
            var stage = new DelegatedAggregateStage(
                "$group",
                (s, sr) => 
                {
                    var renderedProjection = group.Render((IBsonSerializer<TDocument>)s, sr);
                    return new RenderedAggregateStage("$group", new BsonDocument("$group", renderedProjection.Document), renderedProjection.Serializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TDocument> Limit(int limit)
        {
            return AppendStage(new BsonDocument("$limit", limit));
        }

        public override IAggregateFluent<TDocument> Match(Filter<TDocument> filter)
        {
            var stage = new DelegatedAggregateStage(
                "$match",
                (s, sr) => new RenderedAggregateStage("$match", new BsonDocument("$match", filter.Render((IBsonSerializer<TDocument>)s, sr)), s));

            return AppendStage(stage);
        }

        public override Task<IAsyncCursor<TDocument>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            AppendStage(new BsonDocument("$out", collectionName));
            return ToCursorAsync(cancellationToken);
        }

        public override IAggregateFluent<TNewResult> Project<TNewResult>(Projection<TDocument, TNewResult> project)
        {
            var stage = new DelegatedAggregateStage(
                "$project",
                (s, sr) =>
                {
                    var renderedProjection = project.Render((IBsonSerializer<TDocument>)s, sr);
                    return new RenderedAggregateStage("$project", new BsonDocument("$project", renderedProjection.Document), renderedProjection.Serializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TDocument> Skip(int skip)
        {
            return AppendStage(new BsonDocument("$skip", skip));
        }

        public override IAggregateFluent<TDocument> Sort(Sort<TDocument> sort)
        {
            var stage = new DelegatedAggregateStage(
                "$sort",
                (s, sr) => new RenderedAggregateStage("$sort", new BsonDocument("$sort", sort.Render((IBsonSerializer<TDocument>)s, sr)), s));


            return AppendStage(stage);
        }

        public override IAggregateFluent<TResult> Unwind<TResult>(FieldName<TDocument> fieldName, IBsonSerializer<TResult> resultSerializer)
        {
            var stage = new DelegatedAggregateStage(
                "$unwind",
                (s, sr) => new RenderedAggregateStage("$unwind", new BsonDocument("$unwind", "$" + fieldName.Render((IBsonSerializer<TDocument>)s, sr)), resultSerializer ?? s));

            return AppendStage<TResult>(stage);
        }

        public override Task<IAsyncCursor<TDocument>> ToCursorAsync(CancellationToken cancellationToken)
        {
            return _collection.AggregateAsync(new AggregateStagePipeline<TDocument>(_pipeline), _options, cancellationToken);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("aggregate([");
            if (_pipeline.Count > 0)
            {
                var pipeline = new AggregateStagePipeline<TDocument>(_pipeline);
                var renderedPipeline = pipeline.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                sb.Append(string.Join(", ", renderedPipeline.Documents.Select(x => x.ToString())));
            }
            sb.Append("])");
            return sb.ToString();
        }
    }
}
