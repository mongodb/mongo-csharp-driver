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
    internal enum AstUnaryAccumulatorOperator
    {
        AddToSet,
        Avg,
        First,
        Last,
        Max,
        MergeObjects,
        Min,
        Push,
        StdDevPop,
        StdDevSamp,
        Sum
    }

    internal static class AstUnaryAccumulatorOperatorExtensions
    {
        public static string Render(this AstUnaryAccumulatorOperator @operator)
        {
            return @operator switch
            {
                AstUnaryAccumulatorOperator.AddToSet => "$addToSet",
                AstUnaryAccumulatorOperator.Avg => "$avg",
                AstUnaryAccumulatorOperator.First => "$first",
                AstUnaryAccumulatorOperator.Last => "$last",
                AstUnaryAccumulatorOperator.Max => "$max",
                AstUnaryAccumulatorOperator.MergeObjects => "$mergeObjects",
                AstUnaryAccumulatorOperator.Min => "$min",
                AstUnaryAccumulatorOperator.Push => "$push",
                AstUnaryAccumulatorOperator.StdDevPop => "$stdDevPop",
                AstUnaryAccumulatorOperator.StdDevSamp => "$stdDevSamp",
                AstUnaryAccumulatorOperator.Sum => "$sum",
                _ => throw new InvalidOperationException($"Unexpected accumulator operator: {@operator}.")
            };
        }
    }
}
