/* Copyright 2010-present MongoDB Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers
{
    internal class AstGroupingPipelineOptimizer
    {
        #region static
        public static AstPipeline Optimize(AstPipeline pipeline)
        {
            var optimizer = new AstGroupingPipelineOptimizer();
            for (var i = 0; i < pipeline.Stages.Count; i++)
            {
                var stage = pipeline.Stages[i];
                if (IsGroupingStage(stage))
                {
                    pipeline = optimizer.OptimizeGroupingStage(pipeline, i, stage);
                }
            }

            return pipeline;

            static bool IsGroupingStage(AstStage stage)
            {
                return stage.NodeType switch
                {
                    AstNodeType.GroupStage or AstNodeType.BucketStage or AstNodeType.BucketAutoStage => true,
                    _ => false
                };
            }
        }
        #endregion

        private readonly AccumulatorSet _accumulators = new AccumulatorSet();
        private AstExpression _element; // normally either "$$ROOT" or "$_v"

        private AstPipeline OptimizeGroupingStage(AstPipeline pipeline, int i, AstStage groupingStage)
        {
            try
            {
                if (IsOptimizableGroupingStage(groupingStage, out _element))
                {
                    var followingStages = GetFollowingStagesToOptimize(pipeline, i + 1);
                    if (followingStages == null)
                    {
                        return pipeline;
                    }

                    var mappings = OptimizeGroupingAndFollowingStages(groupingStage, followingStages);
                    if (mappings.Length > 0)
                    {
                        return (AstPipeline)AstNodeReplacer.Replace(pipeline, mappings);
                    }
                }
            }
            catch (UnableToRemoveReferenceToElementsException)
            {
                // wasn't able to optimize away all references to _elements
            }

            return pipeline;

            static bool IsOptimizableGroupingStage(AstStage groupingStage, out AstExpression element)
            {
                if (groupingStage is AstGroupStage groupStage)
                {
                    // { $group : { _id : ?, _elements : { $push : element } } }
                    if (groupStage.Fields.Count == 1)
                    {
                        var field = groupStage.Fields[0];
                        return IsElementsPush(field, out element);
                    }
                }

                if (groupingStage is AstBucketStage bucketStage)
                {
                    // { $bucket : { groupBy : ?, boundaries : ?, default : ?, output : { _elements : { $push : element } } } }
                    if (bucketStage.Output.Count == 1)
                    {
                        var output = bucketStage.Output[0];
                        return IsElementsPush(output, out element);
                    }
                }

                if (groupingStage is AstBucketAutoStage bucketAutoStage)
                {
                    // { $bucketAuto : { groupBy : ?, buckets : ?, granularity : ?, output : { _elements : { $push : element } } } }
                    if (bucketAutoStage.Output.Count == 1)
                    {
                        var output = bucketAutoStage.Output[0];
                        return IsElementsPush(output, out element);
                    }
                }

                element = null;
                return false;

                static bool IsElementsPush(AstAccumulatorField field, out AstExpression element)
                {
                    if (
                        field.Path == "_elements" &&
                        field.Value is AstUnaryAccumulatorExpression unaryAccumulatorExpression &&
                        unaryAccumulatorExpression.Operator == AstUnaryAccumulatorOperator.Push)
                    {
                        element = unaryAccumulatorExpression.Arg;
                        return true;
                    }
                    else
                    {
                        element = null;
                        return false;
                    }
                }
            }

            static List<AstStage> GetFollowingStagesToOptimize(AstPipeline pipeline, int from)
            {
                var stages = new List<AstStage>();

                for (var j = from; j < pipeline.Stages.Count; j++)
                {
                    var stage = pipeline.Stages[j];
                    if (StageCanBeOptimized(stage))
                    {
                        stages.Add(stage);
                    }

                    if (IsLastStageThatCanBeOptimized(stage))
                    {
                        return stages;
                    }
                }

                return null;

                static bool StageCanBeOptimized(AstStage stage)
                {
                    return stage.NodeType switch
                    {
                        AstNodeType.LimitStage => true,
                        AstNodeType.MatchStage => true,
                        AstNodeType.ProjectStage => true,
                        AstNodeType.SampleStage => true,
                        AstNodeType.SkipStage => true,
                        _ => false
                    };
                }

                static bool IsLastStageThatCanBeOptimized(AstStage stage)
                {
                    return stage switch
                    {
                        AstProjectStage projectStage => !ProjectsRoot(projectStage),
                        _ => false
                    };

                    static bool ProjectsRoot(AstProjectStage projectStage)
                    {
                        return projectStage.Specifications.Any(
                            specification =>
                                specification is AstProjectStageSetFieldSpecification setFieldSpecification &&
                                setFieldSpecification.Value is AstVarExpression varExpression &&
                                varExpression.Name == "ROOT");
                    }
                }
            }
        }

        private (AstNode, AstNode)[] OptimizeGroupingAndFollowingStages(AstStage groupingStage, List<AstStage> followingStages)
        {
            var mappings = new List<(AstNode, AstNode)>();

            foreach (var stage in followingStages)
            {
                var optimizedStage = OptimizeFollowingStage(stage);
                if (optimizedStage != stage)
                {
                    mappings.Add((stage, optimizedStage));
                }
            }

            var newGroupingStage = CreateNewGroupingStage(groupingStage, _accumulators);
            mappings.Add((groupingStage, newGroupingStage));

            return mappings.ToArray();

            static AstStage CreateNewGroupingStage(AstStage groupingStage, AccumulatorSet accumulators)
            {
                return groupingStage switch
                {
                    AstGroupStage groupStage => AstStage.Group(groupStage.Id, accumulators),
                    AstBucketStage bucketStage => AstStage.Bucket(bucketStage.GroupBy, bucketStage.Boundaries, bucketStage.Default, accumulators),
                    AstBucketAutoStage bucketAutoStage => AstStage.BucketAuto(bucketAutoStage.GroupBy, bucketAutoStage.Buckets, bucketAutoStage.Granularity, accumulators),
                    _ => throw new Exception($"Unexpected {nameof(groupingStage)} node type: {groupingStage.NodeType}.")
                };
            }
        }

        private AstStage OptimizeFollowingStage(AstStage stage)
        {
            return stage switch
            {
                AstLimitStage limitStage => OptimizeLimitStage(limitStage),
                AstMatchStage matchStage => OptimizeMatchStage(matchStage),
                AstProjectStage projectStage => OptimizeProjectStage(projectStage),
                AstSampleStage sampleStage => OptimizeSampleStage(sampleStage),
                AstSkipStage skipStage => OptimizeSkipStage(skipStage),
                _ => throw new InvalidOperationException($"Unexpected node type: {stage.NodeType}.")
            };
        }

        private AstStage OptimizeLimitStage(AstLimitStage stage)
        {
            return stage;
        }

        private AstStage OptimizeMatchStage(AstMatchStage stage)
        {
            var optimizedFilter = AccumulatorMover.MoveAccumulators(_accumulators, _element, stage.Filter);
            return stage.Update(optimizedFilter);
        }

        private AstStage OptimizeProjectStage(AstProjectStage stage)
        {
            var optimizedSpecifications = new List<AstProjectStageSpecification>();

            foreach (var specification in stage.Specifications)
            {
                var optimizedSpecification = OptimizeProjectStageSpecification(specification);
                optimizedSpecifications.Add(optimizedSpecification);
            }

            return stage.Update(optimizedSpecifications);
        }

        private AstProjectStageSpecification OptimizeProjectStageSpecification(AstProjectStageSpecification specification)
        {
            return specification switch
            {
                AstProjectStageSetFieldSpecification setFieldSpecification => OptimizeProjectStageSetFieldSpecification(setFieldSpecification),
                _ => specification
            };
        }

        private AstProjectStageSpecification OptimizeProjectStageSetFieldSpecification(AstProjectStageSetFieldSpecification specification)
        {
            var optimizedValue = AccumulatorMover.MoveAccumulators(_accumulators, _element, specification.Value);
            return specification.Update(optimizedValue);
        }

        private AstStage OptimizeSampleStage(AstSampleStage stage)
        {
            return stage;
        }

        private AstStage OptimizeSkipStage(AstSkipStage stage)
        {
            return stage;
        }

        private class AccumulatorSet : IEnumerable<AstAccumulatorField>
        {
            private int _accumulatorCounter;
            private readonly List<AstAccumulatorField> _accumulatorFields = new();
            private readonly List<BsonValue> _renderedAccumulatorExpressions = new();

            public int Count => _accumulatorFields.Count;

            public string AddAccumulatorExpression(AstAccumulatorExpression value)
            {
                var renderedAccumulatorExpression = value.Render();

                for (var i = 0; i < _renderedAccumulatorExpressions.Count; i++)
                {
                    if (_renderedAccumulatorExpressions[i].Equals(renderedAccumulatorExpression))
                    {
                        return _accumulatorFields[i].Path;
                    }
                }

                var accumulatorFieldName = $"__agg{_accumulatorCounter++}";
                var accumulatorField = AstExpression.AccumulatorField(accumulatorFieldName, value);
                _accumulatorFields.Add(accumulatorField);
                _renderedAccumulatorExpressions.Add(renderedAccumulatorExpression);
                return accumulatorFieldName;
            }

            public IEnumerator<AstAccumulatorField> GetEnumerator() => _accumulatorFields.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class AccumulatorMover : AstNodeVisitor
        {
            #region static
            public static TNode MoveAccumulators<TNode>(AccumulatorSet accumulators, AstExpression element, TNode node)
                where TNode : AstNode
            {
                var mover = new AccumulatorMover(accumulators, element);
                return mover.VisitAndConvert(node);
            }
            #endregion

            private readonly AccumulatorSet _accumulators;
            private readonly AstExpression _element;

            private AccumulatorMover(AccumulatorSet accumulator, AstExpression element)
            {
                _accumulators = accumulator;
                _element = element;
            }

            public override AstNode VisitFilterField(AstFilterField node)
            {
                // "_elements.0.X" => { __agg0 : { $first : element } } + "__agg0.X"
                if (node.Path.StartsWith("_elements.0."))
                {
                    var accumulatorExpression = AstExpression.UnaryAccumulator(AstUnaryAccumulatorOperator.First, _element);
                    var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                    var restOfPath = node.Path.Substring("_elements.0.".Length);
                    var rewrittenPath = $"{accumulatorFieldName}.{restOfPath}";
                    return AstFilter.Field(rewrittenPath);
                }

                if (node.Path == "_elements" || node.Path.StartsWith("_elements."))
                {
                    throw new UnableToRemoveReferenceToElementsException();
                }

                return base.VisitFilterField(node);
            }

            public override AstNode VisitGetFieldExpression(AstGetFieldExpression node)
            {
                if (node.FieldName is AstConstantExpression constantFieldName &&
                    constantFieldName.Value.IsString &&
                    constantFieldName.Value.AsString == "_elements")
                {
                    throw new UnableToRemoveReferenceToElementsException();
                }

                return base.VisitGetFieldExpression(node);
            }

            public override AstNode VisitMapExpression(AstMapExpression node)
            {
                // { $map : { input : { $getField : { input : "$$ROOT", field : "_elements" } }, as : "x", in : f(x) } } => { __agg0 : { $push : f(x => element) } } + "$__agg0"
                if (node.Input is AstGetFieldExpression mapInputGetFieldExpression &&
                    mapInputGetFieldExpression.FieldName is AstConstantExpression mapInputconstantFieldExpression &&
                    mapInputconstantFieldExpression.Value.IsString &&
                    mapInputconstantFieldExpression.Value.AsString == "_elements" &&
                    mapInputGetFieldExpression.Input is AstVarExpression mapInputGetFieldVarExpression &&
                    mapInputGetFieldVarExpression.Name == "ROOT")
                {
                    var rewrittenArg = (AstExpression)AstNodeReplacer.Replace(node.In, (node.As, _element));
                    var accumulatorExpression = AstExpression.UnaryAccumulator(AstUnaryAccumulatorOperator.Push, rewrittenArg);
                    var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                    var root = AstExpression.Var("ROOT", isCurrent: true);
                    return AstExpression.GetField(root, accumulatorFieldName);
                }

                return base.VisitMapExpression(node);
            }

            public override AstNode VisitPickExpression(AstPickExpression node)
            {
                // { $pickOperator : { source : { $getField : { input : "$$ROOT", field : "_elements" } }, as : "x", sortBy : s, selector : f(x) } }
                // => { __agg0 : { $pickAccumulatorOperator : { sortBy : s, selector : f(x => element) } } } + "$__agg0"
                if (node.Source is AstGetFieldExpression getFieldExpression &&
                    getFieldExpression.Input is AstVarExpression varExpression &&
                    varExpression.Name == "ROOT" &&
                    getFieldExpression.FieldName is AstConstantExpression constantFieldNameExpression &&
                    constantFieldNameExpression.Value.IsString &&
                    constantFieldNameExpression.Value.AsString == "_elements")
                {
                    var @operator = node.Operator.ToAccumulatorOperator();
                    var rewrittenSelector = (AstExpression)AstNodeReplacer.Replace(node.Selector, (node.As, _element));
                    var accumulatorExpression = new AstPickAccumulatorExpression(@operator, node.SortBy, rewrittenSelector, node.N);
                    var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                    var root = AstExpression.Var("ROOT", isCurrent: true);
                    return AstExpression.GetField(root, accumulatorFieldName);
                }

                return base.VisitPickExpression(node);
            }

            public override AstNode VisitUnaryExpression(AstUnaryExpression node)
            {
                var root = AstExpression.Var("ROOT", isCurrent: true);

                if (TryOptimizeSizeOfElements(out var optimizedExpression))
                {
                    return optimizedExpression;
                }

                if (TryOptimizeAccumulatorOfElements(out optimizedExpression))
                {
                    return optimizedExpression;
                }

                if (TryOptimizeAccumulatorOfMappedElements(out optimizedExpression))
                {
                    return optimizedExpression;
                }

                return base.VisitUnaryExpression(node);

                bool TryOptimizeSizeOfElements(out AstExpression optimizedExpression)
                {
                    // { $size : "$_elements" } => { __agg0 : { $sum : 1 } } + "$__agg0"
                    if (node.Operator == AstUnaryOperator.Size)
                    {
                        if (node.Arg is AstGetFieldExpression argGetFieldExpression &&
                            argGetFieldExpression.FieldName is AstConstantExpression constantFieldNameExpression &&
                            constantFieldNameExpression.Value.IsString &&
                            constantFieldNameExpression.Value.AsString == "_elements")
                        {
                            var accumulatorExpression = AstExpression.UnaryAccumulator(AstUnaryAccumulatorOperator.Sum, 1);
                            var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                            optimizedExpression = AstExpression.GetField(root, accumulatorFieldName);
                            return true;
                        }
                    }

                    optimizedExpression = null;
                    return false;
                }

                bool TryOptimizeAccumulatorOfElements(out AstExpression optimizedExpression)
                {
                    // { $accumulator : { $getField : { input : "$$ROOT", field : "_elements" } } } => { __agg0 : { $accumulator : element } } + "$__agg0"
                    if (node.Operator.IsAccumulator(out var accumulatorOperator) &&
                        node.Arg is AstGetFieldExpression getFieldExpression &&
                        getFieldExpression.FieldName is AstConstantExpression getFieldConstantFieldNameExpression &&
                        getFieldConstantFieldNameExpression.Value.IsString &&
                        getFieldConstantFieldNameExpression.Value == "_elements" &&
                        getFieldExpression.Input is AstVarExpression getFieldInputVarExpression &&
                        getFieldInputVarExpression.Name == "ROOT")
                    {
                        var accumulatorExpression = AstExpression.UnaryAccumulator(accumulatorOperator, _element);
                        var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                        optimizedExpression = AstExpression.GetField(root, accumulatorFieldName);
                        return true;
                    }

                    optimizedExpression = null;
                    return false;

                }

                bool TryOptimizeAccumulatorOfMappedElements(out AstExpression optimizedExpression)
                {
                    // { $accumulator : { $map : { input : { $getField : { input : "$$ROOT", field : "_elements" } }, as : "x", in : f(x) } } } => { __agg0 : { $accumulator : f(x => element) } } + "$__agg0"
                    if (node.Operator.IsAccumulator(out var accumulatorOperator) &&
                        node.Arg is AstMapExpression mapExpression &&
                        mapExpression.Input is AstGetFieldExpression mapInputGetFieldExpression &&
                        mapInputGetFieldExpression.FieldName is AstConstantExpression mapInputconstantFieldExpression &&
                        mapInputconstantFieldExpression.Value.IsString &&
                        mapInputconstantFieldExpression.Value.AsString == "_elements" &&
                        mapInputGetFieldExpression.Input is AstVarExpression mapInputGetFieldVarExpression &&
                        mapInputGetFieldVarExpression.Name == "ROOT")
                    {
                        var rewrittenArg = (AstExpression)AstNodeReplacer.Replace(mapExpression.In, (mapExpression.As, _element));
                        var accumulatorExpression = AstExpression.UnaryAccumulator(accumulatorOperator, rewrittenArg);
                        var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                        optimizedExpression = AstExpression.GetField(root, accumulatorFieldName);
                        return true;
                    }

                    optimizedExpression = null;
                    return false;
                }
            }
        }

        public class UnableToRemoveReferenceToElementsException : Exception
        {
        }
    }
}
