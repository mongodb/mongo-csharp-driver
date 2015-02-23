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
        private readonly List<IPipelineStage> _pipeline;

        // constructors
        public AggregateFluent(IReadOnlyMongoCollection<TCollectionDocument> collection, IEnumerable<IPipelineStage> pipeline, AggregateOptions options)
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

        public override IList<IPipelineStage> Stages
        {
            get { return _pipeline; }
        }

        // methods
        public override IAggregateFluent<TResult> AppendStage<TResult>(PipelineStage<TDocument, TResult> stage)
        {
            return new AggregateFluent<TCollectionDocument, TResult>(
                _collection, 
                _pipeline.Concat(new [] { stage }), 
                _options);
        }

        public override IAggregateFluent<TResult> Group<TResult>(Projection<TDocument, TResult> group)
        {
            const string stageName = "$group";
            var stage = new DelegatedAggregateStage<TDocument, TResult>(
                stageName,
                (s, sr) => 
                {
                    var renderedProjection = group.Render(s, sr);
                    return new RenderedPipelineStage<TResult>(stageName, new BsonDocument(stageName, renderedProjection.Document), renderedProjection.Serializer);
                });

            return AppendStage<TResult>(stage);
        }

        public override IAggregateFluent<TDocument> Limit(int limit)
        {
            return AppendStage<TDocument>(new BsonDocument("$limit", limit));
        }

        public override IAggregateFluent<TDocument> Match(Filter<TDocument> filter)
        {
            const string stageName = "$match";
            var stage = new DelegatedAggregateStage<TDocument, TDocument>(
                stageName,
                (s, sr) => new RenderedPipelineStage<TDocument>(stageName, new BsonDocument(stageName, filter.Render(s, sr)), s));

            return AppendStage<TDocument>(stage);
        }

        public override Task<IAsyncCursor<TDocument>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            return AppendStage<TDocument>(new BsonDocument("$out", collectionName))
                .ToCursorAsync(cancellationToken);
        }

        public override IAggregateFluent<TResult> Project<TResult>(Projection<TDocument, TResult> project)
        {
            const string stageName = "$project";
            var stage = new DelegatedAggregateStage<TDocument, TResult>(
                stageName,
                (s, sr) =>
                {
                    var renderedProjection = project.Render(s, sr);
                    return new RenderedPipelineStage<TResult>(stageName, new BsonDocument(stageName, renderedProjection.Document), renderedProjection.Serializer);
                });

            return AppendStage<TResult>(stage);
        }

        public override IAggregateFluent<TDocument> Skip(int skip)
        {
            return AppendStage<TDocument>(new BsonDocument("$skip", skip));
        }

        public override IAggregateFluent<TDocument> Sort(Sort<TDocument> sort)
        {
            const string stageName = "$sort";
            var stage = new DelegatedAggregateStage<TDocument, TDocument>(
                stageName,
                (s, sr) => new RenderedPipelineStage<TDocument>(stageName, new BsonDocument(stageName, sort.Render(s, sr)), s));


            return AppendStage(stage);
        }

        public override IAggregateFluent<TResult> Unwind<TResult>(FieldName<TDocument> fieldName, IBsonSerializer<TResult> resultSerializer)
        {
            const string stageName = "$unwind";
            var stage = new DelegatedAggregateStage<TDocument, TResult>(
                stageName,
                (s, sr) => new RenderedPipelineStage<TResult>(
                    stageName, new BsonDocument(
                        stageName, 
                        "$" + fieldName.Render(s, sr)), 
                    resultSerializer ?? (s as IBsonSerializer<TResult>) ?? sr.GetSerializer<TResult>()));

            return AppendStage<TResult>(stage);
        }

        public override Task<IAsyncCursor<TDocument>> ToCursorAsync(CancellationToken cancellationToken)
        {
            return _collection.AggregateAsync(new PipelineStagePipeline<TCollectionDocument, TDocument>(_pipeline), _options, cancellationToken);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("aggregate([");
            if (_pipeline.Count > 0)
            {
                var pipeline = new PipelineStagePipeline<TCollectionDocument, TDocument>(_pipeline);
                var renderedPipeline = pipeline.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                sb.Append(string.Join(", ", renderedPipeline.Documents.Select(x => x.ToString())));
            }
            sb.Append("])");
            return sb.ToString();
        }
    }
}
