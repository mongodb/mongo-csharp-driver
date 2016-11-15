/* Copyright 2010-2016 MongoDB Inc.
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
using System.Reflection;
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
        // fields
        private readonly IMongoCollection<TDocument> _collection;
        private readonly AggregateOptions _options;
        private readonly List<IPipelineStageDefinition> _stages;

        // constructors
        public AggregateFluent(IMongoCollection<TDocument> collection, IEnumerable<IPipelineStageDefinition> stages, AggregateOptions options)
        {
            _collection = Ensure.IsNotNull(collection, nameof(collection));
            _stages = Ensure.IsNotNull(stages, nameof(stages)).ToList();
            _options = Ensure.IsNotNull(options, nameof(options));
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

        public override IAggregateFluent<TNewResult> As<TNewResult>(IBsonSerializer<TNewResult> newResultSerializer)
        {
            var projection = Builders<TResult>.Projection.As<TNewResult>(newResultSerializer);
            return Project(projection);
        }

        public override IAggregateFluent<AggregateBucketResult<TValue>> Bucket<TValue>(
            AggregateExpressionDefinition<TResult, TValue> groupBy,
            IEnumerable<TValue> boundaries,
            AggregateBucketOptions<TValue> options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(boundaries, nameof(boundaries));

            const string operatorName = "$bucket";
            var stage = new DelegatedPipelineStageDefinition<TResult, AggregateBucketResult<TValue>>(
                operatorName,
                (s, sr) =>
                {
                    var valueSerializer = sr.GetSerializer<TValue>();
                    var renderedGroupBy = groupBy.Render(s, sr);
                    var serializedBoundaries = boundaries.Select(b => valueSerializer.ToBsonValue(b));
                    var serializedDefaultBucket = options != null && options.DefaultBucket.HasValue ? valueSerializer.ToBsonValue(options.DefaultBucket.Value) : null;
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "boundaries", new BsonArray(serializedBoundaries) },
                                { "default", serializedDefaultBucket, serializedDefaultBucket != null }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<AggregateBucketResult<TValue>>(
                        operatorName,
                        document,
                        sr.GetSerializer<AggregateBucketResult<TValue>>());
                });

            return AppendStage(stage);
        }

        public override IAggregateFluent<TNewResult> Bucket<TValue, TNewResult>(
            AggregateExpressionDefinition<TResult, TValue> groupBy,
            IEnumerable<TValue> boundaries,
            ProjectionDefinition<TResult, TNewResult> output,
            AggregateBucketOptions<TValue> options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsNotNull(boundaries, nameof(boundaries));
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$bucket";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var valueSerializer = sr.GetSerializer<TValue>();
                    var newResultSerializer = sr.GetSerializer<TNewResult>();
                    var renderedGroupBy = groupBy.Render(s, sr);
                    var serializedBoundaries = boundaries.Select(b => valueSerializer.ToBsonValue(b));
                    var serializedDefaultBucket = options != null && options.DefaultBucket.HasValue ? valueSerializer.ToBsonValue(options.DefaultBucket.Value) : null;
                    var renderedOutput = output.Render(s, sr);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "boundaries", new BsonArray(serializedBoundaries) },
                                { "default", serializedDefaultBucket, serializedDefaultBucket != null },
                                { "output", renderedOutput.Document }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        operatorName,
                        document,
                        newResultSerializer);
                });

            return AppendStage(stage);
        }

        public override IAggregateFluent<AggregateBucketAutoResult<TValue>> BucketAuto<TValue>(
            AggregateExpressionDefinition<TResult, TValue> groupBy,
            int buckets,
            AggregateBucketAutoOptions options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsGreaterThanZero(buckets, nameof(buckets));

            const string operatorName = "$bucketAuto";
            var stage = new DelegatedPipelineStageDefinition<TResult, AggregateBucketAutoResult<TValue>>(
                operatorName,
                (s, sr) =>
                {
                    var renderedGroupBy = groupBy.Render(s, sr);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "buckets", buckets },
                                { "granularity", () => options.Granularity.Value.Value, options != null && options.Granularity.HasValue }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<AggregateBucketAutoResult<TValue>>(
                        operatorName,
                        document,
                        sr.GetSerializer<AggregateBucketAutoResult<TValue>>());
                });

            return AppendStage(stage);
        }

        public override IAggregateFluent<TNewResult> BucketAuto<TValue, TNewResult>(
            AggregateExpressionDefinition<TResult, TValue> groupBy,
            int buckets,
            ProjectionDefinition<TResult, TNewResult> output,
            AggregateBucketAutoOptions options = null)
        {
            Ensure.IsNotNull(groupBy, nameof(groupBy));
            Ensure.IsGreaterThanZero(buckets, nameof(buckets));
            Ensure.IsNotNull(output, nameof(output));

            const string operatorName = "$bucketAuto";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var newResultSerializer = sr.GetSerializer<TNewResult>();
                    var renderedGroupBy = groupBy.Render(s, sr);
                    var renderedOutput = output.Render(s, sr);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "groupBy", renderedGroupBy },
                                { "buckets", buckets },
                                { "output", renderedOutput.Document },
                                { "granularity", () => options.Granularity.Value.Value, options != null && options.Granularity.HasValue }
                           }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        operatorName,
                        document,
                        newResultSerializer);
                });

            return AppendStage(stage);
        }

        public override IAggregateFluent<AggregateCountResult> Count()
        {
            const string operatorName = "$count";
            var stage = new DelegatedPipelineStageDefinition<TResult, AggregateCountResult>(
                operatorName,
                (s, sr) =>
                {
                    return new RenderedPipelineStageDefinition<AggregateCountResult>(
                        operatorName,
                        new BsonDocument(operatorName, "count"),
                        sr.GetSerializer<AggregateCountResult>());
                });

            return AppendStage<AggregateCountResult>(stage);
        }

        public override IAggregateFluent<TNewResult> Facet<TNewResult>(
            IEnumerable<AggregateFacet<TResult>> facets,
            AggregateFacetOptions<TNewResult> options = null)
        {
            const string operatorName = "$facet";
            var materializedFacets = facets.ToArray();
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var facetsDocument = new BsonDocument();
                    foreach (var facet in materializedFacets)
                    {
                        var renderedPipeline = facet.RenderPipeline(s, sr);
                        facetsDocument.Add(facet.Name, renderedPipeline);
                    }
                    var document = new BsonDocument("$facet", facetsDocument);
                    var resultSerializer = options?.NewResultSerializer ?? sr.GetSerializer<TNewResult>();
                    return new RenderedPipelineStageDefinition<TNewResult>(operatorName, document, resultSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TNewResult> GraphLookup<TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TAs, TAsEnumerable>(
            IMongoCollection<TFrom> from,
            FieldDefinition<TFrom, TConnectFrom> connectFromField,
            FieldDefinition<TFrom, TConnect> connectToField,
            AggregateExpressionDefinition<TResult, TStartWith> startWith,
            FieldDefinition<TNewResult, TAsEnumerable> @as,
            FieldDefinition<TAs, int> depthField,
            AggregateGraphLookupOptions<TNewResult, TFrom, TConnect, TConnectFrom, TStartWith, TAs, TAsEnumerable> options = null)
        {
            Ensure.IsNotNull(from, nameof(from));
            Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            Ensure.IsNotNull(connectToField, nameof(connectToField));
            Ensure.IsNotNull(startWith, nameof(startWith));
            Ensure.IsNotNull(@as, nameof(@as));
            Ensure.That(from.Database.DatabaseNamespace.Equals(_collection.Database.DatabaseNamespace), "From collection must be from the same database.", nameof(from));
            Ensure.That(IsTConnectOrEnumerableTConnect<TConnectFrom, TConnect>(), "TConnectFrom must be either TConnect or a type that implements IEnumerable<TConnect>.", nameof(TConnectFrom));
            Ensure.That(IsTConnectOrEnumerableTConnect<TStartWith, TConnect>(), "TStartWith must be either TConnect or a type that implements IEnumerable<TConnect>.", nameof(TStartWith));

            const string operatorName = "$graphLookup";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var resultSerializer = s;
                    var newResultSerializer = options?.NewResultSerializer ?? sr.GetSerializer<TNewResult>();
                    var fromSerializer = options?.FromSerializer ?? sr.GetSerializer<TFrom>();
                    var asSerializer = options?.AsSerializer ?? sr.GetSerializer<TAs>();
                    var renderedConnectToField = connectToField.Render(fromSerializer, sr);
                    var renderedStartWith = startWith.Render(resultSerializer, sr);
                    var renderedConnectFromField = connectFromField.Render(fromSerializer, sr);
                    var renderedAs = @as.Render(newResultSerializer, sr);
                    var renderedDepthField = depthField?.Render(asSerializer, sr);
                    var renderedRestrictSearchWithMatch = options?.RestrictSearchWithMatch?.Render(fromSerializer, sr);
                    var document = new BsonDocument
                    {
                        { operatorName, new BsonDocument
                            {
                                { "from", from.CollectionNamespace.CollectionName },
                                { "connectFromField", renderedConnectFromField.FieldName },
                                { "connectToField", renderedConnectToField.FieldName },
                                { "startWith", renderedStartWith },
                                { "as", renderedAs.FieldName },
                                { "depthField", () => renderedDepthField.FieldName, renderedDepthField != null },
                                { "maxDepth", () => options.MaxDepth.Value, options != null && options.MaxDepth.HasValue },
                                { "restrictSearchWithMatch", renderedRestrictSearchWithMatch, renderedRestrictSearchWithMatch != null }
                            }
                        }
                    };
                    return new RenderedPipelineStageDefinition<TNewResult>(operatorName, document, newResultSerializer);
                });

            return AppendStage<TNewResult>(stage);
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

        public override IAggregateFluent<TNewResult> Lookup<TForeignDocument, TNewResult>(string foreignCollectionName, FieldDefinition<TResult> localField, FieldDefinition<TForeignDocument> foreignField, FieldDefinition<TNewResult> @as, AggregateLookupOptions<TForeignDocument, TNewResult> options)
        {
            options = options ?? new AggregateLookupOptions<TForeignDocument, TNewResult>();
            const string operatorName = "$lookup";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (localSerializer, sr) =>
                {
                    var foreignSerializer = options.ForeignSerializer ?? (localSerializer as IBsonSerializer<TForeignDocument>) ?? sr.GetSerializer<TForeignDocument>();
                    var newResultSerializer = options.ResultSerializer ?? (localSerializer as IBsonSerializer<TNewResult>) ?? sr.GetSerializer<TNewResult>();
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        operatorName, new BsonDocument(operatorName, new BsonDocument
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
            const string operatorName = "$match";
            var stage = new DelegatedPipelineStageDefinition<TResult, TResult>(
                operatorName,
                (s, sr) => new RenderedPipelineStageDefinition<TResult>(operatorName, new BsonDocument(operatorName, filter.Render(s, sr)), s));

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

            var discriminatorValue = discriminatorConvention.GetDiscriminator(typeof(TResult), typeof(TNewResult));
            var ofTypeFilter = new BsonDocument(discriminatorConvention.ElementName, discriminatorValue);

            const string operatorName = "$match";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    return new RenderedPipelineStageDefinition<TNewResult>(
                        operatorName,
                        new BsonDocument(operatorName, ofTypeFilter),
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
            const string operatorName = "$project";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
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
                        document = new BsonDocument(operatorName, renderedProjection.Document);
                    }
                    return new RenderedPipelineStageDefinition<TNewResult>(operatorName, document, renderedProjection.ProjectionSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAggregateFluent<TNewResult> ReplaceRoot<TNewResult>(AggregateExpressionDefinition<TResult, TNewResult> newRoot)
        {
            const string operatorName = "$replaceRoot";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var document = new BsonDocument(operatorName, new BsonDocument("newRoot", newRoot.Render(s, sr)));
                    var outputSerializer = sr.GetSerializer<TNewResult>();
                    return new RenderedPipelineStageDefinition<TNewResult>(operatorName, document, outputSerializer);
                });

            return AppendStage(stage);
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

        public override IAggregateFluent<AggregateSortByCountResult<TId>> SortByCount<TId>(AggregateExpressionDefinition<TResult, TId> id)
        {
            const string operatorName = "$sortByCount";
            var stage = new DelegatedPipelineStageDefinition<TResult, AggregateSortByCountResult<TId>>(
                operatorName,
                (s, sr) =>
                {
                    var outputSerializer = sr.GetSerializer<AggregateSortByCountResult<TId>>();
                    return new RenderedPipelineStageDefinition<AggregateSortByCountResult<TId>>(operatorName, new BsonDocument(operatorName, id.Render(s, sr)), outputSerializer);
                });

            return AppendStage(stage);
        }

        public override IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, IBsonSerializer<TNewResult> newResultSerializer)
        {
            return Unwind(field, new AggregateUnwindOptions<TNewResult> { ResultSerializer = newResultSerializer });
        }

        public override IAggregateFluent<TNewResult> Unwind<TNewResult>(FieldDefinition<TResult> field, AggregateUnwindOptions<TNewResult> options)
        {
            options = options ?? new AggregateUnwindOptions<TNewResult>();

            const string operatorName = "$unwind";
            var stage = new DelegatedPipelineStageDefinition<TResult, TNewResult>(
                operatorName,
                (s, sr) =>
                {
                    var newResultSerializer = options.ResultSerializer ?? (s as IBsonSerializer<TNewResult>) ?? sr.GetSerializer<TNewResult>();

                    var fieldName = "$" + field.Render(s, sr).FieldName;
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
                        operatorName,
                        new BsonDocument(operatorName, value),
                        newResultSerializer);
                });

            return AppendStage<TNewResult>(stage);
        }

        public override IAsyncCursor<TResult> ToCursor(CancellationToken cancellationToken)
        {
            var pipeline = new PipelineStagePipelineDefinition<TDocument, TResult>(_stages);
            return _collection.Aggregate(pipeline, _options, cancellationToken);
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

        // private methods
        private bool IsTConnectOrEnumerableTConnect<TConnectFrom, TConnect>()
        {
            if (typeof(TConnect) == typeof(TConnectFrom))
            {
                return true;
            }

            var ienumerableTConnect = typeof(IEnumerable<>).MakeGenericType(typeof(TConnect));
            if (typeof(TConnectFrom).GetTypeInfo().GetInterfaces().Contains(ienumerableTConnect))
            {
                return true;
            }

            return false;
        }
    }
}
