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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal enum AstPickAccumulatorOperator
    {
        Bottom,
        BottomN,
        FirstN,
        LastN,
        MaxN,
        MinN,
        Top,
        TopN
    }

    internal static class AstPickAccumulatorOperatorExtensions
    {
        public static AstExpression EnsureNIsValid(this AstPickAccumulatorOperator @operator, AstExpression n)
        {
            switch (@operator)
            {
                case AstPickAccumulatorOperator.Bottom:
                case AstPickAccumulatorOperator.Top:
                    return Ensure.IsNull(n, nameof(n));

                case AstPickAccumulatorOperator.BottomN:
                case AstPickAccumulatorOperator.FirstN:
                case AstPickAccumulatorOperator.LastN:
                case AstPickAccumulatorOperator.MaxN:
                case AstPickAccumulatorOperator.MinN:
                case AstPickAccumulatorOperator.TopN:
                    return Ensure.IsNotNull(n, nameof(n));

                default:
                    throw new InvalidOperationException($"Invalid operator: {@operator}.");
            }
        }

        public static AstSortFields EnsureSortByIsValid(this AstPickAccumulatorOperator @operator, AstSortFields sortBy)
        {
            switch (@operator)
            {
                case AstPickAccumulatorOperator.Bottom:
                case AstPickAccumulatorOperator.BottomN:
                case AstPickAccumulatorOperator.Top:
                case AstPickAccumulatorOperator.TopN:
                    return Ensure.IsNotNull(sortBy, nameof(sortBy));

                case AstPickAccumulatorOperator.FirstN:
                case AstPickAccumulatorOperator.LastN:
                case AstPickAccumulatorOperator.MaxN:
                case AstPickAccumulatorOperator.MinN:
                    return Ensure.IsNull(sortBy, nameof(sortBy));

                default:
                    throw new InvalidOperationException($"Invalid operator: {@operator}.");
            }
        }

        public static string Render(this AstPickAccumulatorOperator @operator)
        {
            return @operator switch
            {
                AstPickAccumulatorOperator.Bottom => "$bottom",
                AstPickAccumulatorOperator.BottomN => "$bottomN",
                AstPickAccumulatorOperator.FirstN => "$firstN",
                AstPickAccumulatorOperator.LastN => "$lastN",
                AstPickAccumulatorOperator.MaxN => "$maxN",
                AstPickAccumulatorOperator.MinN => "$minN",
                AstPickAccumulatorOperator.Top => "$top",
                AstPickAccumulatorOperator.TopN => "$topN",
                _ => throw new InvalidOperationException($"Invalid operator: {@operator}.")
            };
        }
    }
}
