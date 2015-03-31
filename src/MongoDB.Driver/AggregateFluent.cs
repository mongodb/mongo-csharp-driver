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
    internal class AggregateFluent<TDocument, TResult> : AggregateFluentBase<TResult>
    {
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly List<IPipelineStageDefinition> _stages;

        // constructors
        public AggregateFluent(IMongoCollection<TDocument> collection, IEnumerable<IPipelineStageDefinition> stages, AggregateOptions options)
        {
            _collection = Ensure.IsNotNull(collection, "collection");
            _stages = Ensure.IsNotNull(stages, "stages").ToList();
            _options = Ensure.IsNotNull(options, "options");
        }

        // properties
        public override AggregateOptions Options
        {
            get { return _options; }
        }

        public override IList<IPipelineStageDefinition> Stages
        {
            get { return _stages; }
        }

        // methods
        public override IAggregateFluent<TNewResult> AppendStage<TNewResult>(PipelineStageDefinition<TResult, TNewResult> stage)
        {
            return new AggregateFluent<TDocument, TNewResult>(
                _collection,
                _stages.Concat(new[] { stage }),
                _options);
        }

        public override IAggregateFluent<TNewResult> Group<TNewResult>(ProjectionDefinition<TResult, TNewResult> group)
        {
            const string operatorName = "$group";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) => 
                {
                    var renderedProjection = group.Render(s, sr);
                    return new RenderedPipelineStageDefinition<TNewResult>(operatorName, new BsonDocument(operatorName, renderedProjection.Document), renderedProjection.ProjectionSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TResult> Limit(int limit)
        {
            return AppendStage<TResult>(new BsonDocument("$limit", limit));
        }

        public override IAggregateFluent<TResult> Match(FilterDefinition<TResult> filter)
        {
            const string operatorName = "$match";
            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                operatorName,
                (s, sr) => new RenderedPipelineStageDefinition<TResult>(operatorName, new BsonDocument(operatorName, filter.Render(s, sr)), s));

            return AppendStage<TResult>(stage);
        }

        public override Task<IAsyncCursor<TResult>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            return AppendStage<TResult>(new BsonDocument("$out", collectionName))
                .ToCursorAsync(cancellationToken);
        }

        public override IAggregateFluent<TNewResult> Project<TNewResult>(ProjectionDefinition<TResult, TNewResult> projection)
        {
            const string operatorName = "$project";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var renderedProjection = projection.Render(s, sr);
                    return new RenderedPipelineStageDefinition<TNewResult>(operatorName, new BsonDocument(operatorName, renderedProjection.Document), renderedProjection.ProjectionSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TResult> Skip(int skip)
        {
            return AppendStage<TResult>(new BsonDocument("$skip", skip));
        }

        public override IAggregateFluent<TResult> Sort(SortDefinition<TResult> sort)
        {
            const string operatorName = "$sort";
            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                operatorName,
                (s, sr) => new RenderedPipelineStageDefinition<TResult>(operatorName, new BsonDocument(operatorName, sort.Render(s, sr)), s));


            return AppendStage(stage);
        }

        public override IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, IBsonSerializer<TNewResult> newResultSerializer)
        {
            const string operatorName = "$unwind";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) => new RenderedPipelineStageDefinition<TNewResult>(
                    operatorName, new BsonDocument(
                        operatorName, 
                        "$" + field.Render(s, sr).FieldName),
                    newResultSerializer ?? (s as IBsonSerializer<TNewResult>) ?? sr.GetSerializer<TNewResult>()));

            return AppendStage<TNewResult>(stage);
        }

        public override Task<IAsyncCursor<TResult>> ToCursorAsync(CancellationToken cancellationToken)
        {
            var pipeline = new PipelineStagePipelineDefinition<TDocument, TResult>(_stages);
            return _collection.AggregateAsync(pipeline, _options, cancellationToken);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("aggregate([");
            if (_stages.Count > 0)
            {
                var pipeline = new PipelineStagePipelineDefinition<TDocument, TResult>(_stages);
                var renderedPipeline = pipeline.Render(_collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                sb.Append(string.Join(", ", renderedPipeline.Documents.Select(x => x.ToString())));
            }
            sb.Append("])");
            return sb.ToString();
        }
    }
}
