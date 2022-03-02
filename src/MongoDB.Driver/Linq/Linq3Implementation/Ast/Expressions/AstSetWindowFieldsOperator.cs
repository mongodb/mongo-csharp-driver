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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal enum AstSetWindowFieldsOperator
    {
        AddToSet,
        Average,
        Count,
        CovariancePop,
        CovarianceSamp,
        DenseRank,
        Derivative,
        DocumentNumber,
        ExpMovingAvgPlaceholder, // temporary placeholder until we know what weighting is used
        ExpMovingAvgWithAlphaWeighting,
        ExpMovingAvgWithPositionalWeighting,
        First,
        Integral,
        Last,
        Max,
        Min,
        Push,
        Rank,
        Shift,
        StdDevPop,
        StdDevSamp,
        Sum
    }

    internal static class AstSetWindowFieldsOperatorExtensions
    {
        public static string Render(this AstSetWindowFieldsOperator @operator)
        {
            return @operator switch
            {
                AstSetWindowFieldsOperator.AddToSet => "$addToSet",
                AstSetWindowFieldsOperator.Average => "$avg",
                AstSetWindowFieldsOperator.Count => "$count",
                AstSetWindowFieldsOperator.CovariancePop => "$covariancePop",
                AstSetWindowFieldsOperator.CovarianceSamp => "$covarianceSamp",
                AstSetWindowFieldsOperator.DenseRank => "$denseRank",
                AstSetWindowFieldsOperator.Derivative => "$derivative",
                AstSetWindowFieldsOperator.DocumentNumber => "$documentNumber",
                AstSetWindowFieldsOperator.ExpMovingAvgWithAlphaWeighting => "$expMovingAvg",
                AstSetWindowFieldsOperator.ExpMovingAvgWithPositionalWeighting => "$expMovingAvg",
                AstSetWindowFieldsOperator.First => "$first",
                AstSetWindowFieldsOperator.Integral => "$integral",
                AstSetWindowFieldsOperator.Last => "$last",
                AstSetWindowFieldsOperator.Max => "$max",
                AstSetWindowFieldsOperator.Min => "$min",
                AstSetWindowFieldsOperator.Push => "$push",
                AstSetWindowFieldsOperator.Rank => "$rank",
                AstSetWindowFieldsOperator.Shift => "$shift",
                AstSetWindowFieldsOperator.StdDevPop => "$stdDevPop",
                AstSetWindowFieldsOperator.StdDevSamp => "$stdDevSamp",
                AstSetWindowFieldsOperator.Sum => "$sum",
                _ => throw new InvalidOperationException($"Unexpected SetWindowFields operator: {@operator}.")
            };
        }
    }
}
