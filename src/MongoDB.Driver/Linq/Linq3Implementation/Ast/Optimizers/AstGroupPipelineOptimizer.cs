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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers
{
    internal class AstGroupPipelineOptimizer
    {
        #region static
        public static AstPipeline Optimize(AstPipeline pipeline)
        {
            var optimizer = new AstGroupPipelineOptimizer();
            for (var i = 0; i < pipeline.Stages.Count; i++)
            {
                var stage = pipeline.Stages[i];
                if (stage is AstGroupStage groupStage)
                {
                    pipeline = optimizer.OptimizeGroupStage(pipeline, i, groupStage);
                }
            }

            return pipeline;
        }
        #endregion

        private readonly AccumulatorSet _accumulators = new AccumulatorSet();

        private AstPipeline OptimizeGroupStage(AstPipeline pipeline, int i, AstGroupStage groupStage)
        {
            try
            {
                if (IsOptimizableGroupStage(groupStage))
                {
                    var followingStages = GetFollowingStagesToOptimize(pipeline, i + 1);
                    if (followingStages == null)
                    {
                        return pipeline;
                    }

                    var mappings = OptimizeGroupAndFollowingStages(groupStage, followingStages);
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

            static bool IsOptimizableGroupStage(AstGroupStage groupStage)
            {
                // { $group : { _id : ?, _elements : { $push : "$$ROOT" } } }
                if (groupStage.Fields.Count == 1)
                {
                    var field = groupStage.Fields[0];
                    if (field.Path == "_elements" &&
                        field.Value.Operator == AstAccumulatorOperator.Push &&
                        field.Value.Arg is AstVarExpression varExpression &&
                        varExpression.Name == "ROOT")
                    {
                        return true;
                    }
                }

                return false;
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
                    return stage.NodeType switch
                    {
                        AstNodeType.ProjectStage => true,
                        _ => false
                    };
                }
            }
        }

        private (AstNode, AstNode)[] OptimizeGroupAndFollowingStages(AstGroupStage groupStage, List<AstStage> followingStages)
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

            var newGroupStage = AstStage.Group(groupStage.Id, _accumulators);
            mappings.Add((groupStage, newGroupStage));

            return mappings.ToArray();
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
            var optimizedFilter = AccumulatorMover.MoveAccumulators(_accumulators, stage.Filter);
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
            var optimizedValue = AccumulatorMover.MoveAccumulators(_accumulators, specification.Value);
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
            public static TNode MoveAccumulators<TNode>(AccumulatorSet accumulators, TNode node)
                where TNode : AstNode
            {
                var mover = new AccumulatorMover(accumulators);
                return mover.VisitAndConvert(node);
            }
            #endregion

            private readonly AccumulatorSet _accumulators;

            private AccumulatorMover(AccumulatorSet accumulator)
            {
                _accumulators = accumulator;
            }

            public override AstNode VisitFilterField(AstFilterField node)
            {
                // "_elements.0.X" => { __agg0 : { $first : "$$ROOT" } } + "__agg0.X"
                if (node.Path.StartsWith("_elements.0."))
                {
                    var accumulatorExpression = AstExpression.AccumulatorExpression(AstAccumulatorOperator.First, AstExpression.Var("ROOT"));
                    var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                    var restOfPath = node.Path.Substring("_elements.0.".Length);
                    var rewrittenPath = $"{accumulatorFieldName}.{restOfPath}";
                    return AstFilter.Field(rewrittenPath, node.Serializer);
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
                    constantFieldName.Value.AsString == "_elements" &&
                    node.Input is AstVarExpression varExpression &&
                    varExpression.Name == "ROOT")
                {
                    throw new UnableToRemoveReferenceToElementsException();
                }

                return base.VisitGetFieldExpression(node);
            }

            public override AstNode VisitMapExpression(AstMapExpression node)
            {
                // { $map : { input : { $getField : { input : "$$ROOT", field : "_elements" } }, as : "x", in : f(x) } } => { __agg0 : { $push : f(x => root) } } + "$__agg0"
                if (node.Input is AstGetFieldExpression mapInputGetFieldExpression &&
                    mapInputGetFieldExpression.FieldName is AstConstantExpression mapInputconstantFieldExpression &&
                    mapInputconstantFieldExpression.Value.IsString &&
                    mapInputconstantFieldExpression.Value.AsString == "_elements" &&
                    mapInputGetFieldExpression.Input is AstVarExpression mapInputGetFieldVarExpression &&
                    mapInputGetFieldVarExpression.Name == "ROOT")
                {
                    var root = AstExpression.Var("ROOT", isCurrent: true);
                    var rewrittenArg = (AstExpression)AstNodeReplacer.Replace(node.In, (node.As, root));
                    var accumulatorExpression = AstExpression.AccumulatorExpression(AstAccumulatorOperator.Push, rewrittenArg);
                    var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                    return AstExpression.GetField(root, accumulatorFieldName);
                }

                return base.VisitMapExpression(node);
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
                            var accumulatorExpression = AstExpression.AccumulatorExpression(AstAccumulatorOperator.Sum, 1);
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
                    // { $accumulator : { $getField : { input : "$$ROOT", field : "_elements" } } } => { __agg0 : { $accumulator : "$$ROOT" } } + "$__agg0"
                    if (node.Operator.IsAccumulator(out var accumulatorOperator) &&
                        node.Arg is AstGetFieldExpression getFieldExpression &&
                        getFieldExpression.FieldName is AstConstantExpression getFieldConstantFieldNameExpression &&
                        getFieldConstantFieldNameExpression.Value.IsString &&
                        getFieldConstantFieldNameExpression.Value == "_elements" &&
                        getFieldExpression.Input is AstVarExpression getFieldInputVarExpression &&
                        getFieldInputVarExpression.Name == "ROOT")
                    {
                        var accumulatorExpression = AstExpression.AccumulatorExpression(accumulatorOperator, root);
                        var accumulatorFieldName = _accumulators.AddAccumulatorExpression(accumulatorExpression);
                        optimizedExpression = AstExpression.GetField(root, accumulatorFieldName);
                        return true;
                    }

                    optimizedExpression = null;
                    return false;

                }

                bool TryOptimizeAccumulatorOfMappedElements(out AstExpression optimizedExpression)
                {
                    // { $accumulator : { $map : { input : { $getField : { input : "$$ROOT", field : "_elements" } }, as : "x", in : f(x) } } } => { __agg0 : { $accumulator : f(x => root) } } + "$__agg0"
                    if (node.Operator.IsAccumulator(out var accumulatorOperator) &&
                        node.Arg is AstMapExpression mapExpression &&
                        mapExpression.Input is AstGetFieldExpression mapInputGetFieldExpression &&
                        mapInputGetFieldExpression.FieldName is AstConstantExpression mapInputconstantFieldExpression &&
                        mapInputconstantFieldExpression.Value.IsString &&
                        mapInputconstantFieldExpression.Value.AsString == "_elements" &&
                        mapInputGetFieldExpression.Input is AstVarExpression mapInputGetFieldVarExpression &&
                        mapInputGetFieldVarExpression.Name == "ROOT")
                    {
                        var rewrittenArg = (AstExpression)AstNodeReplacer.Replace(mapExpression.In, (mapExpression.As, root));
                        var accumulatorExpression = AstExpression.AccumulatorExpression(accumulatorOperator, rewrittenArg);
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
