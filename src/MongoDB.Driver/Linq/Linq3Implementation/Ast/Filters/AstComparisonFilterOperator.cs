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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal enum AstComparisonFilterOperator
    {
        Eq,
        Gt,
        Gte,
        Lt,
        Lte,
        Ne
    }

    internal static class AstComparisonFilterOperatorExtensions
    {
        public static string Render(this AstComparisonFilterOperator @operator)
        {
            return @operator switch
            {
                AstComparisonFilterOperator.Eq => "$eq",
                AstComparisonFilterOperator.Gt => "$gt",
                AstComparisonFilterOperator.Gte => "$gte",
                AstComparisonFilterOperator.Lt => "$lt",
                AstComparisonFilterOperator.Lte => "$lte",
                AstComparisonFilterOperator.Ne => "$ne",
                _ => throw new InvalidOperationException($"Unexpected comparison filter operator: {@operator}.")
            };
        }
    }
}
