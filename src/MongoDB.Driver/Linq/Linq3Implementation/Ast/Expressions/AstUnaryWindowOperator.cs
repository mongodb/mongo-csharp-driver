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
    internal enum AstUnaryWindowOperator
    {
        AddToSet,
        Average,
        First,
        Last,
        Locf,
        Max,
        Min,
        Push,
        StandardDeviationPopulation,
        StandardDeviationSample,
        Sum
    }

    internal static class AstUnaryWindowOperatorExtensions
    {
        public static string Render(this AstUnaryWindowOperator @operator)
        {
            return @operator switch
            {
                AstUnaryWindowOperator.AddToSet => "$addToSet",
                AstUnaryWindowOperator.Average => "$avg",
                AstUnaryWindowOperator.First => "$first",
                AstUnaryWindowOperator.Last => "$last",
                AstUnaryWindowOperator.Locf => "$locf",
                AstUnaryWindowOperator.Max => "$max",
                AstUnaryWindowOperator.Min => "$min",
                AstUnaryWindowOperator.Push => "$push",
                AstUnaryWindowOperator.StandardDeviationPopulation => "$stdDevPop",
                AstUnaryWindowOperator.StandardDeviationSample => "$stdDevSamp",
                AstUnaryWindowOperator.Sum => "$sum",
                _ => throw new InvalidOperationException($"Unexpected unary window operator: {@operator}.")
            };
        }
    }
}
