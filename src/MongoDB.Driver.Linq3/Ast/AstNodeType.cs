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

namespace MongoDB.Driver.Linq3.Ast
{
    public enum AstNodeType
    {
        AddFieldsStage,
        AndFilter,
        BinaryExpression,
        BucketAutoStage,
        BucketStage,
        CollStatsStage,
        ComparisonFilter,
        ComputedDocumentExpression,
        CondExpression,
        ConstantExpression,
        ConvertExpression,
        CountStage,
        CurrentOpStage,
        CustomAccumulatorExpression,
        DateFromIsoWeekPartsExpression,
        DateFromPartsExpression,
        DateFromStringExpression,
        DatePartExpression,
        DateToPartsExpression,
        DateToStringExpression,
        FacetStage,
        FieldExpression,
        FilterExpression,
        FilterFieldReference,
        FunctionExpression,
        GeoNearStage,
        GraphLookupStage,
        GroupStage,
        GtFilter,
        IncludeFieldProjectSpecification,
        IndexOfArrayExpression,
        IndexOfBytesExpression,
        IndexOfCPExpression,
        IndexStatsStage,
        LetExpression,
        LimitStage,
        ListLocalSessionsStage,
        ListSessionsStage,
        LookupStage,
        LTrimExpression,
        MapExpression,
        MatchStage,
        MergeStage,
        NaryExpression,
        NotFilter,
        OrFilter,
        OutStage,
        Pipeline,
        PlanCacheStatsStage,
        ProjectStage,
        RangeExpression,
        RedactStage,
        ReduceExpression,
        RegexFindExpression,
        ReplaceAllExpression,
        ReplaceOneExpression,
        ReplaceRootStage,
        RTrimExpression,
        SetFieldProjectSpecification,
        SetStage,
        SliceExpression,
        SortStage,
        SwitchExpression,
        TernaryExpression,
        TrimExpression,
        UnaryExpression,
        UnionWithStage,
        UnsetStage,
        UnwindStage,
        ZipExpression
    }
}
