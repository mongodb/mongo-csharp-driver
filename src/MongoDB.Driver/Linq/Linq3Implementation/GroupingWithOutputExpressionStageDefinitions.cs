﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal abstract class GroupingWithOutputExpressionStageDefinition<TInput, TGrouping, TOutput> : PipelineStageDefinition<TInput, TOutput>
    {
        protected readonly Expression<Func<TGrouping, TOutput>> _output;

        public GroupingWithOutputExpressionStageDefinition(Expression<Func<TGrouping, TOutput>> output)
        {
            _output = output;
        }

        public override RenderedPipelineStageDefinition<TOutput> Render(RenderArgs<TInput> args)
        {
            if (args.LinqProvider != LinqProvider.V3)
            {
                throw new InvalidOperationException($"{GetType().Name} is only intended for use with LINQ3.");
            }

            var inputSerializer = args.DocumentSerializer;
            var serializerRegistry = args.SerializerRegistry;
            var groupingStage = RenderGroupingStage(inputSerializer, serializerRegistry, out var groupingSerializer);
            var projectStage = RenderProjectStage(groupingSerializer, serializerRegistry, out var outputSerializer);
            var optimizedStages = OptimizeGroupingStages(groupingStage, projectStage, inputSerializer, outputSerializer);
            var renderedStages = optimizedStages.Select(x => x.Render().AsBsonDocument);

            return new RenderedPipelineStageDefinition<TOutput>(OperatorName, renderedStages, outputSerializer);
        }

        protected abstract AstStage RenderGroupingStage(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, out IBsonSerializer<TGrouping> groupingOutputSerializer);

        private AstStage RenderProjectStage(IBsonSerializer<TGrouping> inputSerializer, IBsonSerializerRegistry serializerRegistry, out IBsonSerializer<TOutput> outputSerializer)
        {
            var partiallyEvaluatedOutput = (Expression<Func<TGrouping, TOutput>>)PartialEvaluator.EvaluatePartially(_output);
            var context = TranslationContext.Create(partiallyEvaluatedOutput, inputSerializer);
            var outputTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, partiallyEvaluatedOutput, inputSerializer, asRoot: true);
            var (projectStage, projectSerializer) = ProjectionHelper.CreateProjectStage(outputTranslation);
            outputSerializer = (IBsonSerializer<TOutput>)projectSerializer;
            return projectStage;
        }

        private IReadOnlyList<AstStage> OptimizeGroupingStages(AstStage groupingStage, AstStage projectStage, IBsonSerializer inputSerializer, IBsonSerializer outputSerializer)
        {
            var pipeline = AstPipeline.Empty(inputSerializer).AddStages(outputSerializer, groupingStage, projectStage);
            var optimizedPipeline = AstPipelineOptimizer.Optimize(pipeline);
            return optimizedPipeline.Stages;
        }
    }

    internal sealed class BucketWithOutputExpressionStageDefinition<TInput, TValue, TOutput> : GroupingWithOutputExpressionStageDefinition<TInput, IGrouping<TValue, TInput>, TOutput>
    {
        private readonly IReadOnlyList<TValue> _boundaries;
        private readonly Expression<Func<TInput, TValue>> _groupBy;
        private readonly AggregateBucketOptions<TValue> _options;
        private readonly ExpressionTranslationOptions _translationOptions;

        public BucketWithOutputExpressionStageDefinition(
            Expression<Func<TInput, TValue>> groupBy,
            IEnumerable<TValue> boundaries,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> output,
            AggregateBucketOptions<TValue> options,
            ExpressionTranslationOptions translationOptions)
            : base(output)
        {
            _groupBy = groupBy;
            _boundaries = boundaries.ToArray();
            _options = options;
            _translationOptions = translationOptions;
        }

        public override string OperatorName => "$bucket";

        public override RenderedPipelineStageDefinition<TOutput> Render(RenderArgs<TInput> args)
        {
            if (args.LinqProvider == LinqProvider.V2)
            {
                var linq2Stage = PipelineStageDefinitionBuilder.Bucket(
                    new ExpressionAggregateExpressionDefinition<TInput, TValue>(_groupBy, _translationOptions),
                    _boundaries,
                    new ExpressionBucketOutputProjection<TInput, TValue, TOutput>(x => default(TValue), _output, _translationOptions),
                    _options);
                return linq2Stage.Render(args);
            }
            else
            {
                return base.Render(args);
            }
        }

        protected override AstStage RenderGroupingStage(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, out IBsonSerializer<IGrouping<TValue, TInput>> groupingOutputSerializer)
        {
            var partiallyEvaluatedGroupBy = (Expression<Func<TInput, TValue>>)PartialEvaluator.EvaluatePartially(_groupBy);
            var context = TranslationContext.Create(partiallyEvaluatedGroupBy, inputSerializer);
            var groupByTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, partiallyEvaluatedGroupBy, inputSerializer, asRoot: true);

            var valueSerializer = (IBsonSerializer<TValue>)groupByTranslation.Serializer;
            var serializedBoundaries = SerializationHelper.SerializeValues(valueSerializer, _boundaries);
            var serializedDefault = _options != null && _options.DefaultBucket.HasValue ? SerializationHelper.SerializeValue(valueSerializer, _options.DefaultBucket.Value) : null;
            var pushElements = AstExpression.AccumulatorField("_elements", AstUnaryAccumulatorOperator.Push, AstExpression.Var("ROOT", isCurrent: true));
            groupingOutputSerializer = IGroupingSerializer.Create(valueSerializer, inputSerializer);

            return AstStage.Bucket(
                groupByTranslation.Ast,
                serializedBoundaries,
                serializedDefault,
                new[] { pushElements });
        }
    }

    internal sealed class BucketAutoWithOutputExpressionStageDefinition<TInput, TValue, TOutput> : GroupingWithOutputExpressionStageDefinition<TInput, IGrouping<AggregateBucketAutoResultId<TValue>, TInput>, TOutput>
    {
        private readonly int _buckets;
        private readonly Expression<Func<TInput, TValue>> _groupBy;
        private readonly AggregateBucketAutoOptions _options;

        public BucketAutoWithOutputExpressionStageDefinition(
            Expression<Func<TInput, TValue>> groupBy,
            int buckets,
            Expression<Func<IGrouping<AggregateBucketAutoResultId<TValue>, TInput>, TOutput>> output,
            AggregateBucketAutoOptions options)
            : base(output)
        {
            _groupBy = groupBy;
            _buckets = buckets;
            _options = options;
        }

        public override string OperatorName => "$bucketAuto";

        protected override AstStage RenderGroupingStage(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, out IBsonSerializer<IGrouping<AggregateBucketAutoResultId<TValue>, TInput>> groupingOutputSerializer)
        {
            var partiallyEvaluatedGroupBy = (Expression<Func<TInput, TValue>>)PartialEvaluator.EvaluatePartially(_groupBy);
            var context = TranslationContext.Create(partiallyEvaluatedGroupBy, inputSerializer);
            var groupByTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, partiallyEvaluatedGroupBy, inputSerializer, asRoot: true);

            var valueSerializer = (IBsonSerializer<TValue>)groupByTranslation.Serializer;
            var keySerializer = AggregateBucketAutoResultIdSerializer.Create(valueSerializer);
            var serializedGranularity = _options != null && _options.Granularity.HasValue ? _options.Granularity.Value.Value : null;
            var pushElements = AstExpression.AccumulatorField("_elements", AstUnaryAccumulatorOperator.Push, AstExpression.Var("ROOT", isCurrent: true));
            groupingOutputSerializer = IGroupingSerializer.Create(keySerializer, inputSerializer);

            return AstStage.BucketAuto(
                groupByTranslation.Ast,
                _buckets,
                serializedGranularity,
                new[] { pushElements });
        }
    }

    internal sealed class GroupWithOutputExpressionStageDefinition<TInput, TValue, TOutput> : GroupingWithOutputExpressionStageDefinition<TInput, IGrouping<TValue, TInput>, TOutput>
    {
        private readonly Expression<Func<TInput, TValue>> _groupBy;
        private readonly ExpressionTranslationOptions _translationOptions;

        public GroupWithOutputExpressionStageDefinition(
            Expression<Func<TInput, TValue>> groupBy,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> output,
            ExpressionTranslationOptions translationOptions = null)
            : base(output)
        {
            _groupBy = groupBy;
            _translationOptions = translationOptions;
        }

        public override string OperatorName => "$group";

        public override RenderedPipelineStageDefinition<TOutput> Render(RenderArgs<TInput> args)
        {
            if (args.LinqProvider == LinqProvider.V2)
            {
                var linq2Stage = PipelineStageDefinitionBuilder.Group(new GroupExpressionProjection<TInput, TValue, TOutput>(_groupBy, _output, _translationOptions));
                return linq2Stage.Render(args);
            }
            else
            {
                return base.Render(args);
            }
        }

        protected override AstStage RenderGroupingStage(IBsonSerializer<TInput> inputSerializer, IBsonSerializerRegistry serializerRegistry, out IBsonSerializer<IGrouping<TValue, TInput>> groupingOutputSerializer)
        {
            var partiallyEvaluatedGroupBy = (Expression<Func<TInput, TValue>>)PartialEvaluator.EvaluatePartially(_groupBy);
            var context = TranslationContext.Create(partiallyEvaluatedGroupBy, inputSerializer);
            var groupByTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, partiallyEvaluatedGroupBy, inputSerializer, asRoot: true);
            var pushElements = AstExpression.AccumulatorField("_elements", AstUnaryAccumulatorOperator.Push, AstExpression.Var("ROOT", isCurrent: true));
            var groupBySerializer = (IBsonSerializer<TValue>)groupByTranslation.Serializer;
            groupingOutputSerializer = IGroupingSerializer.Create(groupBySerializer, inputSerializer);

            return AstStage.Group(groupByTranslation.Ast, pushElements);
        }
    }
}
