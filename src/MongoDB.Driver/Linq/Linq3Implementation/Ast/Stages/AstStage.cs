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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal abstract class AstStage : AstNode
    {
        public static AstStage AddFields(IEnumerable<AstComputedField> fields)
        {
            return new AstAddFieldsStage(fields);
        }

        public static AstStage Bucket(
            AstExpression groupBy,
            IEnumerable<BsonValue> boundaries,
            BsonValue @default = null,
            IEnumerable<AstComputedField> output = null)
        {
            return new AstBucketStage(groupBy, boundaries,  @default, output);
        }

        public static AstStage BucketAuto(
            AstExpression groupBy,
            int buckets,
            string granularity = null,
            IEnumerable<AstComputedField> output = null)
        {
            return new AstBucketAutoStage(groupBy, buckets, granularity, output);
        }

        public static AstStage CollStats(
            AstCollStatsStageLatencyStats latencyStats = null,
            AstCollStatsStageStorageStats storageStats = null,
            AstCollStatsStageCount count = null,
            AstCollStatsStageQueryExecStats queryExecStats = null)
        {
            return new AstCollStatsStage(latencyStats, storageStats, count, queryExecStats);
        }

        public static AstStage Count(string outputField)
        {
            return new AstCountStage(outputField);
        }

        public static AstStage CurrentOp(
            bool? allUsers = null,
            bool? idleConnections = null,
            bool? idleCursors = null,
            bool? idleSessions = null,
            bool? localOps = null)
        {
            return new AstCurrentOpStage(allUsers, idleConnections, idleCursors, idleSessions, localOps);
        }

        public static AstStage Facet(IEnumerable<AstFacetStageFacet> facets)
        {
            return new AstFacetStage(facets);
        }

        public static AstStage GeoNear(
            BsonValue near,
            string distanceField,
            bool? spherical,
            double? maxDistance,
            BsonDocument query,
            double? distanceMultiplier,
            string includeLocs,
            bool? uniqueDocs,
            double? minDistance,
            string key)
        {
            return new AstGeoNearStage(near, distanceField, spherical, maxDistance, query, distanceMultiplier, includeLocs, uniqueDocs, minDistance, key);
        }

        public static AstStage GraphLookup(
            string from,
            AstExpression startWith,
            string connectFromField,
            string connectToField,
            string @as,
            int? maxDepth = default,
            string depthField = default,
            AstFilter restrictSearchWithMatch = default)
        {
            return new AstGraphLookupStage(from, startWith, connectFromField, connectToField, @as, maxDepth, depthField, restrictSearchWithMatch);
        }

        public static AstStage Group(
            AstExpression id,
            IEnumerable<AstComputedField> fields)
        {
            return new AstGroupStage(id, fields);
        }

        public static AstStage Group(
            AstExpression id,
            params AstComputedField[] fields)
        {
            return AstStage.Group(id, (IEnumerable<AstComputedField>)fields);
        }

        public static AstStage IndexStats()
        {
            return new AstIndexStatsStage();
        }

        public static AstStage Limit(long limit)
        {
            return new AstLimitStage(limit);
        }

        public static AstStage ListLocalSessions(BsonDocument options)
        {
            return new AstListLocalSessionsStage(options);
        }

        public static AstStage ListSessions(BsonDocument options)
        {
            return new AstListSessionsStage(options);
        }

        public static AstStage Lookup(string from, AstLookupStageMatch match, string @as)
        {
            return new AstLookupStage(from, match, @as);
        }

        public static AstStage Match(AstFilter filter)
        {
            return new AstMatchStage(filter);
        }

        public static AstStage Merge(
            string intoDatabase,
            string intoCollection,
            IEnumerable<string> on = null,
            IEnumerable<AstVar> let = null,
            AstMergeStageWhenMatched? whenMatched = null,
            AstMergeStageWhenNotMatched? whenNotMatched = null)
        {
            return new AstMergeStage(intoDatabase, intoCollection, on, let, whenMatched, whenNotMatched);
        }

        public static AstStage Out(string outputDatabase, string outputCollection)
        {
            return new AstOutStage(outputDatabase, outputCollection);
        }

        public static AstStage PlanCache()
        {
            return new AstPlanCacheStatsStage();
        }

        public static AstProjectStage Project(IEnumerable<AstProjectStageSpecification> specifications)
        {
            return new AstProjectStage(specifications);
        }

        public static AstProjectStage Project(params AstProjectStageSpecification[] specifications)
        {
            return AstStage.Project((IEnumerable<AstProjectStageSpecification>)specifications);
        }

        public static AstStage Redact(AstExpression expression)
        {
            return new AstRedactStage(expression);
        }

        public static AstStage ReplaceRoot(AstExpression expression)
        {
            return new AstReplaceRootStage(expression);
        }

        public static AstStage ReplaceWith(AstExpression expression)
        {
            return new AstReplaceWithStage(expression);
        }

        public static AstStage Sample(long size)
        {
            return new AstSampleStage(size);
        }

        public static AstStage Set(IEnumerable<AstComputedField> fields)
        {
            return new AstSetStage(fields);
        }

        public static AstStage Skip(long skip)
        {
            return new AstSkipStage(skip);
        }

        public static AstSortStage Sort(IEnumerable<AstSortField> fields)
        {
            return new AstSortStage(fields);
        }

        public static AstSortStage Sort(params AstSortField[] fields)
        {
            return AstStage.Sort((IEnumerable<AstSortField>)fields);
        }

        public static AstStage UnionWith(string collection, AstPipeline pipeline)
        {
            return new AstUnionWithStage(collection, pipeline);
        }

        public static AstStage Unset(IEnumerable<string> fields)
        {
            return new AstUnsetStage(fields);
        }

        public static AstStage Unwind(
            string field,
            string includeArrayIndex = null,
            bool? preserveNullAndEmptyArrays = null)
        {
            return new AstUnwindStage(field, includeArrayIndex, preserveNullAndEmptyArrays);
        }
    }
}
