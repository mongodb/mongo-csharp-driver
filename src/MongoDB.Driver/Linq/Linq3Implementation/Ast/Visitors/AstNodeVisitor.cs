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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors
{
    internal abstract class AstNodeVisitor
    {
        public virtual AstNode Visit(AstNode node)
        {
            return node?.Accept(this);
        }

        public IReadOnlyList<AstNode> Visit(IReadOnlyList<AstNode> nodes)
        {
            AstNode[] newNodes = null;

            var count = nodes.Count;
            for (var i = 0; i < count; i++)
            {
                var oldNode = nodes[i];
                var newNode = Visit(oldNode);

                if (newNode != oldNode)
                {
                    newNodes ??= InitializeNewNodes(nodes);
                    newNodes[i] = newNode;
                }
            }

            if (newNodes == null)
            {
                return nodes;
            }

            return new ReadOnlyCollection<AstNode>(newNodes);

            static AstNode[] InitializeNewNodes(IReadOnlyList<AstNode> oldNodes)
            {
                var count = oldNodes.Count;
                var newNodes = new AstNode[count];
                for (var i = 0; i < count; i++)
                {
                    newNodes[i] = oldNodes[i];
                }
                return newNodes;
            }
        }

        public virtual AstNode VisitAccumulatorField(AstAccumulatorField node)
        {
            return node.Update(VisitAndConvert(node.Value));
        }

        public virtual AstNode VisitAddFieldsStage(AstAddFieldsStage node)
        {
            return  node.Update(VisitAndConvert(node.Fields));
        }

        public virtual AstNode VisitAllFilterOperation(AstAllFilterOperation node)
        {
            return node;
        }

        public TNode VisitAndConvert<TNode>(TNode node)
            where TNode : AstNode
        {
            if (node == null)
            {
                return null;
            }

            var newNode = Visit(node);
            var convertedNewNode = newNode as TNode;
            if (newNode == null)
            {
                throw new InvalidOperationException($"Expected newNode to be a {typeof(TNode)}, not null.");
            }
            if (convertedNewNode == null)
            {
                throw new InvalidOperationException($"Expected newNode to be a {typeof(TNode)}, not a {newNode.GetType()}.");
            }

            return convertedNewNode;
        }

        public IReadOnlyList<TNode> VisitAndConvert<TNode>(IReadOnlyList<TNode> nodes)
            where TNode : AstNode
        {
            if (nodes == null)
            {
                return null;
            }

            TNode[] newNodes = null;

            var count = nodes.Count;
            for (var i = 0; i < count; i++)
            {
                var oldNode = nodes[i];
                var newNode = VisitAndConvert<TNode>(oldNode);

                if (newNode != oldNode)
                {
                    newNodes ??= InitializeNewNodes(nodes);
                    newNodes[i] = newNode;
                }
            }

            if (newNodes == null)
            {
                return nodes;
            }

            return new ReadOnlyCollection<TNode>(newNodes);

            static TNode[] InitializeNewNodes(IReadOnlyList<TNode> oldNodes)
            {
                var count = oldNodes.Count;
                var newNodes = new TNode[count];
                for (var i = 0; i < count; i++)
                {
                    newNodes[i] = oldNodes[i];
                }
                return newNodes;
            }
        }

        public virtual AstNode VisitAndFilter(AstAndFilter node)
        {
            return node.Update(VisitAndConvert(node.Filters));
        }

        public virtual AstNode VisitBinaryExpression(AstBinaryExpression node)
        {
            return node.Update(VisitAndConvert(node.Arg1), VisitAndConvert(node.Arg2));
        }

        public virtual AstNode VisitBinaryWindowExpression(AstBinaryWindowExpression node)
        {
            return node.Update(node.Operator, VisitAndConvert(node.Arg1), VisitAndConvert(node.Arg2), node.Window);
        }

        public virtual AstNode VisitBitsAllClearFilterOperation(AstBitsAllClearFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitBitsAllSetFilterOperation(AstBitsAllSetFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitBitsAnyClearFilterOperation(AstBitsAnyClearFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitBitsAnySetFilterOperation(AstBitsAnySetFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitBucketAutoStage(AstBucketAutoStage node)
        {
            return node.Update(VisitAndConvert(node.GroupBy), VisitAndConvert(node.Output));
        }

        public virtual AstNode VisitBucketStage(AstBucketStage node)
        {
            return node.Update(VisitAndConvert(node.GroupBy), VisitAndConvert(node.Output));
        }

        public virtual AstNode VisitCollStatsStage(AstCollStatsStage node)
        {
            return node;
        }

        public virtual AstNode VisitComparisonFilterOperation(AstComparisonFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitComputedArrayExpression(AstComputedArrayExpression node)
        {
            return node.Update(VisitAndConvert(node.Items));
        }

        public virtual AstNode VisitComputedDocumentExpression(AstComputedDocumentExpression node)
        {
            return node.Update(VisitAndConvert(node.Fields));
        }

        public virtual AstNode VisitComputedField(AstComputedField node)
        {
            return node.Update(VisitAndConvert(node.Value));
        }

        public virtual AstNode VisitCondExpression(AstCondExpression node)
        {
            return node.Update(VisitAndConvert(node.If), VisitAndConvert(node.Then), VisitAndConvert(node.Else));
        }

        public virtual AstNode VisitConstantExpression(AstConstantExpression node)
        {
            return node;
        }

        public virtual AstNode VisitConvertExpression(AstConvertExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.To), VisitAndConvert(node.OnError), VisitAndConvert(node.OnNull));
        }

        public virtual AstNode VisitCountStage(AstCountStage node)
        {
            return node;
        }

        public virtual AstNode VisitCurrentOpStage(AstCurrentOpStage node)
        {
            return node;
        }

        public virtual AstNode VisitCustomAccumulatorExpression(AstCustomAccumulatorExpression node)
        {
            return node.Update(VisitAndConvert(node.InitArgs), VisitAndConvert(node.AccumulateArgs));
        }

        public virtual AstNode VisitDateAddExpression(AstDateAddExpression node)
        {
            return node.Update(VisitAndConvert(node.StartDate), VisitAndConvert(node.Unit), VisitAndConvert(node.Amount), VisitAndConvert(node.Timezone));
        }

        public virtual AstNode VisitDateDiffExpression(AstDateDiffExpression node)
        {
            return node.Update(VisitAndConvert(node.StartDate), VisitAndConvert(node.EndDate), VisitAndConvert(node.Unit), VisitAndConvert(node.Timezone), VisitAndConvert(node.StartOfWeek));
        }

        public virtual AstNode VisitDateFromIsoWeekPartsExpression(AstDateFromIsoWeekPartsExpression node)
        {
            return node.Update(VisitAndConvert(node.IsoWeekYear), VisitAndConvert(node.IsoWeek), VisitAndConvert(node.IsoDayOfWeek), VisitAndConvert(node.Hour), VisitAndConvert(node.Minute), VisitAndConvert(node.Second), VisitAndConvert(node.Millisecond), VisitAndConvert(node.Timezone));
        }

        public virtual AstNode VisitDateFromPartsExpression(AstDateFromPartsExpression node)
        {
            return node.Update(VisitAndConvert(node.Year), VisitAndConvert(node.Month), VisitAndConvert(node.Day), VisitAndConvert(node.Hour), VisitAndConvert(node.Minute), VisitAndConvert(node.Second), VisitAndConvert(node.Millisecond), VisitAndConvert(node.Timezone));
        }

        public virtual AstNode VisitDateFromStringExpression(AstDateFromStringExpression node)
        {
            return node.Update(VisitAndConvert(node.DateString), VisitAndConvert(node.Format), VisitAndConvert(node.Timezone), VisitAndConvert(node.OnError), VisitAndConvert(node.OnNull));
        }

        public virtual AstNode VisitDatePartExpression(AstDatePartExpression node)
        {
            return node.Update(VisitAndConvert(node.Date), VisitAndConvert(node.Timezone));
        }

        public virtual AstNode VisitDateSubtractExpression(AstDateSubtractExpression node)
        {
            return node.Update(VisitAndConvert(node.StartDate), VisitAndConvert(node.Unit), VisitAndConvert(node.Amount), VisitAndConvert(node.Timezone));
        }

        public virtual AstNode VisitDateToPartsExpression(AstDateToPartsExpression node)
        {
            return node.Update(VisitAndConvert(node.Expression), VisitAndConvert(node.Timezone), VisitAndConvert(node.Iso8601));
        }

        public virtual AstNode VisitDateToStringExpression(AstDateToStringExpression node)
        {
            return node.Update(VisitAndConvert(node.Date), VisitAndConvert(node.Format), VisitAndConvert(node.Timezone), VisitAndConvert(node.OnNull));
        }

        public virtual AstNode VisitDateTruncExpression(AstDateTruncExpression node)
        {
            return node.Update(VisitAndConvert(node.Date), VisitAndConvert(node.Unit), VisitAndConvert(node.BinSize), VisitAndConvert(node.Timezone), VisitAndConvert(node.StartOfWeek));
        }

        public virtual AstNode VisitDensifyStage(AstDensifyStage node)
        {
            return node;
        }

        public virtual AstNode VisitDerivativeOrIntegralWindowExpression(AstDerivativeOrIntegralWindowExpression node)
        {
            return node.Update(node.Operator, VisitAndConvert(node.Arg), node.Unit, node.Window);
        }

        public virtual AstNode VisitDocumentsStage(AstDocumentsStage node)
        {
            return node.Update(VisitAndConvert(node.Documents));
        }

        public virtual AstNode VisitElemMatchFilterOperation(AstElemMatchFilterOperation node)
        {
            return node.Update(VisitAndConvert(node.Filter));
        }

        public virtual AstNode VisitExistsFilterOperation(AstExistsFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitExponentialMovingAverageWindowExpression(AstExponentialMovingAverageWindowExpression node)
        {
            return node.Update(VisitAndConvert(node.Arg), node.Weighting, node.Window);
        }

        public virtual AstNode VisitExprFilter(AstExprFilter node)
        {
            return node.Update(VisitAndConvert(node.Expression));
        }

        public virtual AstNode VisitFacetStage(AstFacetStage node)
        {
            return node.Update(VisitAndConvert(node.Facets));
        }

        public virtual AstNode VisitFacetStageFacet(AstFacetStageFacet node)
        {
            return node.Update(VisitAndConvert(node.Pipeline));
        }

        public virtual AstNode VisitFieldOperationFilter(AstFieldOperationFilter node)
        {
            return node.Update(VisitAndConvert(node.Field), VisitAndConvert(node.Operation));
        }

        public virtual AstNode VisitFieldPathExpression(AstFieldPathExpression node)
        {
            return node;
        }

        public virtual AstNode VisitFilterExpression(AstFilterExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Cond), VisitAndConvert(node.Limit));
        }

        public virtual AstNode VisitFilterField(AstFilterField node)
        {
            return node;
        }

        public virtual AstNode VisitFunctionExpression(AstFunctionExpression node)
        {
            return node.Update(VisitAndConvert(node.Args));
        }

        public virtual AstNode VisitGeoIntersectsFilterOperation(AstGeoIntersectsFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitGeoNearStage(AstGeoNearStage node)
        {
            return node;
        }

        public virtual AstNode VisitGeoWithinBoxFilterOperation(AstGeoWithinBoxFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitGeoWithinCenterFilterOperation(AstGeoWithinCenterFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitGeoWithinCenterSphereFilterOperation(AstGeoWithinCenterSphereFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitGeoWithinFilterOperation(AstGeoWithinFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitGetFieldExpression(AstGetFieldExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.FieldName));
        }

        public virtual AstNode VisitGraphLookupStage(AstGraphLookupStage node)
        {
            return node.Update(VisitAndConvert(node.StartWith), VisitAndConvert(node.RestrictSearchWithMatch));
        }

        public virtual AstNode VisitGroupStage(AstGroupStage node)
        {
            return node.Update(VisitAndConvert(node.Id), VisitAndConvert(node.Fields));
        }

        public virtual AstNode VisitImpliedOperationFilterOperation(AstImpliedOperationFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitIndexOfArrayExpression(AstIndexOfArrayExpression node)
        {
            return node.Update(VisitAndConvert(node.Array), VisitAndConvert(node.Value), VisitAndConvert(node.Start), VisitAndConvert(node.End));
        }

        public virtual AstNode VisitIndexOfBytesExpression(AstIndexOfBytesExpression node)
        {
            return node.Update(VisitAndConvert(node.String), VisitAndConvert(node.Value), VisitAndConvert(node.Start), VisitAndConvert(node.End));
        }

        public virtual AstNode VisitIndexOfCPExpression(AstIndexOfCPExpression node)
        {
            return node.Update(VisitAndConvert(node.String), VisitAndConvert(node.Value), VisitAndConvert(node.Start), VisitAndConvert(node.End));
        }

        public virtual AstNode VisitIndexStatsStage(AstIndexStatsStage node)
        {
            return node;
        }

        public virtual AstNode VisitInFilterOperation(AstInFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitJsonSchemaFilter(AstJsonSchemaFilter node)
        {
            return node;
        }

        public virtual AstNode VisitLetExpression(AstLetExpression node)
        {
            return node.Update(VisitAndConvert(node.Vars), VisitAndConvert(node.In));
        }

        public virtual AstNode VisitLimitStage(AstLimitStage node)
        {
            return node;
        }

        public virtual AstNode VisitListLocalSessionsStage(AstListLocalSessionsStage node)
        {
            return node;
        }

        public virtual AstNode VisitListSessionsStage(AstListSessionsStage node)
        {
            return node;
        }

        public virtual AstNode VisitLookupStage(AstLookupStage node)
        {
            return node;
        }

        public virtual AstNode VisitLookupWithMatchingFieldsAndPipelineStage(AstLookupWithMatchingFieldsAndPipelineStage node)
        {
            return node.Update(VisitAndConvert(node.Let), VisitAndConvert(node.Pipeline));
        }

        public virtual AstNode VisitLookupWithPipelineStage(AstLookupWithPipelineStage node)
        {
            return node.Update(VisitAndConvert(node.Let), VisitAndConvert(node.Pipeline));
        }

        public virtual AstNode VisitLTrimExpression(AstLTrimExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Chars));
        }

        public virtual AstNode VisitMapExpression(AstMapExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.As), VisitAndConvert(node.In));
        }

        public virtual AstNode VisitMatchesEverythingFilter(AstMatchesEverythingFilter node)
        {
            return node;
        }

        public virtual AstNode VisitMatchesNothingFilter(AstMatchesNothingFilter node)
        {
            return node;
        }

        public virtual AstNode VisitMatchStage(AstMatchStage node)
        {
            return node.Update(VisitAndConvert(node.Filter));
        }

        public virtual AstNode VisitMergeStage(AstMergeStage node)
        {
            return node.Update(VisitAndConvert(node.Let));
        }

        public virtual AstNode VisitModFilterOperation(AstModFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitNaryExpression(AstNaryExpression node)
        {
            return node.Update(VisitAndConvert(node.Args));
        }

        public virtual AstNode VisitNearFilterOperation(AstNearFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitNearSphereFilterOperation(AstNearSphereFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitNinFilterOperation(AstNinFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitNorFilter(AstNorFilter node)
        {
            return node.Update(VisitAndConvert(node.Filters));
        }

        public virtual AstNode VisitNotFilterOperation(AstNotFilterOperation node)
        {
            return node.Update(VisitAndConvert(node.Operation));
        }

        public virtual AstNode VisitNullaryWindowExpression(AstNullaryWindowExpression node)
        {
            return node;
        }

        public virtual AstNode VisitOrFilter(AstOrFilter node)
        {
            return node.Update(VisitAndConvert(node.Filters));
        }

        public virtual AstNode VisitOutStage(AstOutStage node)
        {
            return node;
        }

        public virtual AstNode VisitPickAccumulatorExpression(AstPickAccumulatorExpression node)
        {
            return node.Update(node.Operator, node.SortBy, VisitAndConvert(node.Selector), VisitAndConvert(node.N));
        }

        public virtual AstNode VisitPickExpression(AstPickExpression node)
        {
            return node.Update(node.Operator, VisitAndConvert(node.Source), VisitAndConvert(node.As), node.SortBy, VisitAndConvert(node.Selector), VisitAndConvert(node.N));
        }

        public virtual AstNode VisitPipeline(AstPipeline node)
        {
            return node.Update(VisitAndConvert(node.Stages));
        }

        public virtual AstNode VisitPlanCacheStatsStage(AstPlanCacheStatsStage node)
        {
            return node;
        }

        public virtual AstNode VisitProjectStage(AstProjectStage node)
        {
            return node.Update(VisitAndConvert(node.Specifications));
        }

        public virtual AstNode VisitProjectStageExcludeFieldSpecification(AstProjectStageExcludeFieldSpecification node)
        {
            return node;
        }

        public virtual AstNode VisitProjectStageIncludeFieldSpecification(AstProjectStageIncludeFieldSpecification node)
        {
            return node;
        }

        public virtual AstNode VisitProjectStageSetFieldSpecification(AstProjectStageSetFieldSpecification node)
        {
            return node.Update(VisitAndConvert(node.Value));
        }

        public virtual AstNode VisitRangeExpression(AstRangeExpression node)
        {
            return node.Update(VisitAndConvert(node.Start), VisitAndConvert(node.End), VisitAndConvert(node.Step));
        }

        public virtual AstNode VisitRawFilter(AstRawFilter node)
        {
            return node;
        }

        public virtual AstNode VisitRedactStage(AstRedactStage node)
        {
            return node.Update(VisitAndConvert(node.Expression));
        }

        public virtual AstNode VisitReduceExpression(AstReduceExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.InitialValue), VisitAndConvert(node.In));
        }

        public virtual AstNode VisitRegexExpression(AstRegexExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Regex), VisitAndConvert(node.Options));
        }

        public virtual AstNode VisitRegexFilterOperation(AstRegexFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitReplaceAllExpression(AstReplaceAllExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Find), VisitAndConvert(node.Replacement));
        }

        public virtual AstNode VisitReplaceOneExpression(AstReplaceOneExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Find), VisitAndConvert(node.Replacement));
        }

        public virtual AstNode VisitReplaceRootStage(AstReplaceRootStage node)
        {
            return node.Update(VisitAndConvert(node.Expression));
        }

        public virtual AstNode VisitReplaceWithStage(AstReplaceWithStage node)
        {
            return node.Update(VisitAndConvert(node.Expression));
        }

        public virtual AstNode VisitRTrimExpression(AstRTrimExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Chars));
        }

        public virtual AstNode VisitSampleStage(AstSampleStage node)
        {
            return node;
        }

        public virtual AstNode VisitSetStage(AstSetStage node)
        {
            return node.Update(VisitAndConvert(node.Fields));
        }

        public virtual AstNode VisitSetWindowFieldsStage(AstSetWindowFieldsStage node)
        {
            return node.Update(VisitAndConvert(node.PartitionBy), node.SortBy, VisitAndConvert(node.Output));
        }

        public virtual AstNode VisitShiftWindowExpression(AstShiftWindowExpression node)
        {
            return node.Update(VisitAndConvert(node.Arg), node.By, VisitAndConvert(node.DefaultValue));
        }

        public virtual AstNode VisitSizeFilterOperation(AstSizeFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitSkipStage(AstSkipStage node)
        {
            return node;
        }

        public virtual AstNode VisitSliceExpression(AstSliceExpression node)
        {
            return node.Update(VisitAndConvert(node.Array), VisitAndConvert(node.Position), VisitAndConvert(node.N));
        }

        public virtual AstNode VisitSortArrayExpression(AstSortArrayExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), node.Fields, node.Order);
        }

        public virtual AstNode VisitSortByCountStage(AstSortByCountStage node)
        {
            return node.Update(VisitAndConvert(node.Expression));
        }

        public virtual AstNode VisitSortStage(AstSortStage node)
        {
            return node;
        }

        public virtual AstNode VisitSwitchExpression(AstSwitchExpression node)
        {
            return node.Update(VisitAndConvert(node.Branches), VisitAndConvert(node.Default));
        }

        public virtual AstNode VisitSwitchExpressionBranch(AstSwitchExpressionBranch node)
        {
            return node.Update(VisitAndConvert(node.Case), VisitAndConvert(node.Then));
        }

        public virtual AstNode VisitTernaryExpression(AstTernaryExpression node)
        {
            return node.Update(VisitAndConvert(node.Arg1), VisitAndConvert(node.Arg2), VisitAndConvert(node.Arg3));
        }

        public virtual AstNode VisitTextFilter(AstTextFilter node)
        {
            return node;
        }

        public virtual AstNode VisitTrimExpression(AstTrimExpression node)
        {
            return node.Update(VisitAndConvert(node.Input), VisitAndConvert(node.Chars));
        }

        public virtual AstNode VisitTypeFilterOperation(AstTypeFilterOperation node)
        {
            return node;
        }

        public virtual AstNode VisitUnaryAccumulatorExpression(AstUnaryAccumulatorExpression node)
        {
            return node.Update(VisitAndConvert(node.Arg));
        }

        public virtual AstNode VisitUnaryExpression(AstUnaryExpression node)
        {
            return node.Update(VisitAndConvert(node.Arg));
        }

        public virtual AstNode VisitUnaryWindowExpression(AstUnaryWindowExpression node)
        {
            return node.Update(node.Operator, VisitAndConvert(node.Arg), node.Window);
        }

        public virtual AstNode VisitUnionWithStage(AstUnionWithStage node)
        {
            return node.Update(VisitAndConvert(node.Pipeline));
        }

        public virtual AstNode VisitUniversalStage(AstUniversalStage node)
        {
            return node.Update(node.Stage);
        }

        public virtual AstNode VisitUnsetStage(AstUnsetStage node)
        {
            return node;
        }

        public virtual AstNode VisitUnwindStage(AstUnwindStage node)
        {
            return node;
        }

        public virtual AstNode VisitVarBinding(AstVarBinding node)
        {
            return node.Update(VisitAndConvert(node.Var), VisitAndConvert(node.Value));
        }

        public virtual AstNode VisitVarExpression(AstVarExpression node)
        {
            return node;
        }

        public virtual AstNode VisitWhereFilter(AstWhereFilter node)
        {
            return node;
        }

        public virtual AstNode VisitWindowField(AstWindowField node)
        {
            return node.Update(node.Path, VisitAndConvert(node.Value));
        }

        public virtual AstNode VisitZipExpression(AstZipExpression node)
        {
            return node.Update(VisitAndConvert(node.Inputs), VisitAndConvert(node.Defaults));
        }
    }
}
