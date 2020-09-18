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

namespace MongoDB.Driver.Linq3.Ast.Filters
{
    public enum AstComparisonFilterOperator
    {
        Eq,
        Gt,
        Gte,
        Lt,
        Lte,
        Ne
    }

    public static class AstComparisonFilterOperatorExtensions
    {
        public static string Render(this AstComparisonFilterOperator @operator)
        {
            switch (@operator)
            {
                case AstComparisonFilterOperator.Eq: return "$eq";
                case AstComparisonFilterOperator.Gt: return "$gt";
                case AstComparisonFilterOperator.Gte: return "$gte";
                case AstComparisonFilterOperator.Lt: return "$lt";
                case AstComparisonFilterOperator.Lte: return "$lte";
                case AstComparisonFilterOperator.Ne: return "$ne";
                default: throw new InvalidOperationException($"Unexpected comparison filter operator: {@operator}.");
            }
        }
    }
}
