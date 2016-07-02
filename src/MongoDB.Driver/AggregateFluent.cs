/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal class AggregateFluent<TDocument, TResult> : AggregateFluentBase<TResult>
    {
        // consts
        private const string OPERATOR_GROUP_NAME = "$group";
        private const string OPERATOR_LOOKUP_NAME = "$lookup";
        private const string OPERATOR_MATCH_NAME = "$match";
        private const string OPERATOR_PROJECT_NAME = "$project";
        private const string OPERATOR_SORT_NAME = "$sort";
        private const string OPERATOR_UNWIND_NAME = "$unwind";
        
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly List<IPipelineStageDefinition> _stages;
        

        // constructors
        public AggregateFluent(IMongoCollection<TDocument> collection, IEnumerable<IPipelineStageDefinition> stages, AggregateOptions options)
        {
            this._collection = Ensure.IsNotNull(collection, nameof(collection));
            this._stages = Ensure.IsNotNull(stages, nameof(stages)).ToList();
            this._options = Ensure.IsNotNull(options, nameof(options));
        }

        // properties
        public override AggregateOptions Options
        {
            get { return this._options; }
        }

        public override IList<IPipelineStageDefinition> Stages
        {
            get { return this._stages; }
        }

        // methods
        public override IAggregateFluent<TNewResult> AppendStage<TNewResult>(PipelineStageDefinition<TResult, TNewResult> stage)
        {
            return new AggregateFluent<TDocument, TNewResult>(
                this._collection,
                this._stages.Concat(new[] { stage }),
                this._options);
        }

        public override IAggregateFluent<TNewResult> As<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer)
        {
            var projection = Builders<TResult>.Projection.As<TNewResult>(newResultSerializer);
            return this.Project(projection);
        }

        public override IAggregateFluent<TNewResult> Group<TNewResult>(ProjectionDefinition<TResult, TNewResult> group)
        {
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                OPERATOR_GROUP_NAME,
                (s, sr) =>
                {
                    var renderedProjection = group.Render(s, sr);
                    return new RenderedPipelineStageDefinition<TNewResult>(OPERATOR_GROUP_NAME, new BsonDocument(OPERATOR_GROUP_NAME, renderedProjection.Document), renderedProjection.ProjectionSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TResult> Limit(int limit)
        {
            return AppendStage<TResult>(new BsonDocument("$limit", limit));
        }

        public override IAggregateFluent<TNewResult> Lookup<TForeignDocument, TNewResult>(string foreignCollectionName, FieldDefinition<TResult> localField, FieldDefinition<TForeignDocument> foreignField, FieldDefinition<TNewResult> @as, AggregateLookupOptions<TForeignDocument, TNewResult> options)
        {
            options = options ?? new AggregateLookupOptions<TForeignDocument, TNewResult>();
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                OPERATOR_LOOKUP_NAME,
                (localSerializer, sr) =>
                {
                    var foreignSerializer = options.ForeignSerializer ?? (localSerializer as IBsonSerializer<TForeignDocument>) ?? sr.GetSerializer<TForeignDocument>();
                    var newResultSerializer = options.ResultSerializer ?? (localSerializer as IBsonSerializer<TNewResult>) ?? sr.GetSerializer<TNewResult>();
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        OPERATOR_LOOKUP_NAME, new BsonDocument(OPERATOR_LOOKUP_NAME, new BsonDocument
                        {
                            { "from", foreignCollectionName },
                            { "localField", localField.Render(localSerializer, sr).FieldName },
                            { "foreignField", foreignField.Render(foreignSerializer, sr).FieldName },
                            { "as", @as.Render(newResultSerializer, sr).FieldName }
                        }),
                        newResultSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TResult> Match(FilterDefinition<TResult> filter)
        {
            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                OPERATOR_MATCH_NAME,
                (s, sr) => new RenderedPipelineStageDefinition<TResult>(OPERATOR_MATCH_NAME, new BsonDocument(OPERATOR_MATCH_NAME, filter.Render(s, sr)), s));

            return AppendStage<TResult>(stage);
        }

        public override IAggregateFluent<TNewResult> OfType<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer)
        {
            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(TResult));
            if (discriminatorConvention == null)
            {
                var message = string.Format("OfType requires that a discriminator convention exist for type: {0}.", BsonUtils.GetFriendlyTypeName(typeof(TResult)));
                throw new NotSupportedException(message);
            }

            BsonValue discriminatorValue = discriminatorConvention.GetDiscriminator(typeof(TResult), typeof(TNewResult));
            BsonDocument ofTypeFilter = new BsonDocument(discriminatorConvention.ElementName, discriminatorValue);

            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                OPERATOR_MATCH_NAME,
                (s, sr) =>
                {
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        OPERATOR_MATCH_NAME,
                        new BsonDocument(OPERATOR_MATCH_NAME, ofTypeFilter),
                        newResultSerializer ?? (s as IBsonSerializer<TNewResult>) ?? sr.GetSerializer<TNewResult>());
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAsyncCursor<TResult> Out(string collectionName, CancellationToken cancellationToken)
        {
            return AppendStage<TResult>(new BsonDocument("$out", collectionName))
                .ToCursor(cancellationToken);
        }

        public override Task<IAsyncCursor<TResult>> OutAsync(string collectionName, CancellationToken cancellationToken)
        {
            return AppendStage<TResult>(new BsonDocument("$out", collectionName))
                .ToCursorAsync(cancellationToken);
        }

        public override IAggregateFluent<TNewResult> Project<TNewResult>(ProjectionDefinition<TResult, TNewResult> projection)
        {
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                OPERATOR_PROJECT_NAME,
                (s, sr) =>
                {
                    var renderedProjection = projection.Render(s, sr);
                    BsonDocument document;
                    if (renderedProjection.Document == null)
                    {
                        document = new BsonDocument();
                    }
                    else
                    {
                        document = new BsonDocument(OPERATOR_PROJECT_NAME, renderedProjection.Document);
                    }
                    return new RenderedPipelineStageDefinition<TNewResult>(OPERATOR_PROJECT_NAME, document, renderedProjection.ProjectionSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TResult> Skip(int skip)
        {
            return AppendStage<TResult>(new BsonDocument("$skip", skip));
        }

        public override IAggregateFluent<TResult> Sort(SortDefinition<TResult> sort)
        {
            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                OPERATOR_SORT_NAME,
                (s, sr) => new RenderedPipelineStageDefinition<TResult>(OPERATOR_SORT_NAME, new BsonDocument(OPERATOR_SORT_NAME, sort.Render(s, sr)), s));


            return AppendStage(stage);
        }

        public override IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, IBsonSerializer<TNewResult> newResultSerializer)
        {
            return Unwind(field, new AggregateUnwindOptions<TNewResult> { ResultSerializer = newResultSerializer });
        }

        public override IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, AggregateUnwindOptions<TNewResult> options)
        {
            options = options ?? new AggregateUnwindOptions<TNewResult>();

            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                OPERATOR_UNWIND_NAME,
                (s, sr) =>
                {
                    var newResultSerializer = options.ResultSerializer ?? (s as IBsonSerializer<TNewResult>) ?? sr.GetSerializer<TNewResult>();

                    string fieldName = String.Concat("$", field.Render(s, sr).FieldName);
                    string includeArrayIndexFieldName = null;
                    if (options.IncludeArrayIndex != null)
                    {
                        includeArrayIndexFieldName = options.IncludeArrayIndex.Render(newResultSerializer, sr).FieldName;
                    }

                    BsonValue value = fieldName;
                    if (options.PreserveNullAndEmptyArrays.HasValue || includeArrayIndexFieldName != null)
                    {
                        value = new BsonDocument
                        {
                            { "path", fieldName },
                            { "preserveNullAndEmptyArrays", options.PreserveNullAndEmptyArrays, options.PreserveNullAndEmptyArrays.HasValue },
                            { "includeArrayIndex", includeArrayIndexFieldName, includeArrayIndexFieldName != null }
                        };
                    }
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        OPERATOR_UNWIND_NAME,
                        new BsonDocument(OPERATOR_UNWIND_NAME, value),
                        newResultSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAsyncCursor<TResult> ToCursor(CancellationToken cancellationToken)
        {
            var pipeline = new PipelineStagePipelineDefinition<TDocument, TResult>(this._stages);
            return this._collection.Aggregate(pipeline, this._options, cancellationToken);
        }

        public override Task<IAsyncCursor<TResult>> ToCursorAsync(CancellationToken cancellationToken)
        {
            var pipeline = new PipelineStagePipelineDefinition<TDocument, TResult>(this._stages);
            return this._collection.AggregateAsync(pipeline, this._options, cancellationToken);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("aggregate([");
            if (this._stages.Count > 0)
            {
                var pipeline = new PipelineStagePipelineDefinition<TDocument, TResult>(this._stages);
                var renderedPipeline = pipeline.Render(this._collection.DocumentSerializer, _collection.Settings.SerializerRegistry);
                sb.Append(string.Join(", ", renderedPipeline.Documents.Select(x => x.ToString())));
            }
            sb.Append("])");
            return sb.ToString();
        }
    }
}
