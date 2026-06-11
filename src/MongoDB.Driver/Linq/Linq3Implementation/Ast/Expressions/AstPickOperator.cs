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
    internal enum AstPickOperator
    {
        BottomPlaceholder,
        BottomNPlaceholder,
        FirstNPlaceholder,
        FirstNArray,
        LastNPlaceholder,
        LastNArray,
        MaxNPlaceholder,
        MaxNArray,
        MinNPlaceholder,
        MinNArray,
        TopPlaceholder,
        TopNPlaceholder
    }

    internal static class AstPickOperatorExtensions
    {
        public static string Render(this AstPickOperator @operator)
        {
            return @operator switch
            {
                AstPickOperator.BottomPlaceholder => "$bottom(placeholder)",
                AstPickOperator.BottomNPlaceholder => "$bottomN(placeholder)",
                AstPickOperator.FirstNPlaceholder => "$firstN(placeholder)",
                AstPickOperator.FirstNArray => "$firstN",
                AstPickOperator.LastNPlaceholder => "$lastN(placeholder)",
                AstPickOperator.LastNArray => "$lastN",
                AstPickOperator.MaxNPlaceholder => "$maxN(placeholder)",
                AstPickOperator.MaxNArray => "$maxN",
                AstPickOperator.MinNPlaceholder => "$minN(placeholder)",
                AstPickOperator.MinNArray => "$minN",
                AstPickOperator.TopPlaceholder => "$top(placeholder)",
                AstPickOperator.TopNPlaceholder => "$topN(placeholder)",
                _ => throw new InvalidOperationException($"Invalid operator: {@operator}.")
            };
        }

        public static AstPickAccumulatorOperator ToAccumulatorOperator(this AstPickOperator @operator)
        {
            return @operator switch
            {
                AstPickOperator.BottomPlaceholder => AstPickAccumulatorOperator.Bottom,
                AstPickOperator.BottomNPlaceholder => AstPickAccumulatorOperator.BottomN,
                AstPickOperator.FirstNPlaceholder => AstPickAccumulatorOperator.FirstN,
                AstPickOperator.LastNPlaceholder => AstPickAccumulatorOperator.LastN,
                AstPickOperator.MaxNPlaceholder => AstPickAccumulatorOperator.MaxN,
                AstPickOperator.MinNPlaceholder => AstPickAccumulatorOperator.MinN,
                AstPickOperator.TopPlaceholder => AstPickAccumulatorOperator.Top,
                AstPickOperator.TopNPlaceholder => AstPickAccumulatorOperator.TopN,
                _ => throw new InvalidOperationException($"Invalid operator: {@operator}.")
            };
        }
    }
}
