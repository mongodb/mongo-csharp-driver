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
    internal enum AstBinaryOperator
    {
        ArrayElemAt,
        ATan2,
        Cmp,
        Divide,
        Eq,
        Gt,
        Gte,
        IfNull,
        In,
        Log,
        Lt,
        Lte,
        Mod,
        Ne,
        Pow,
        Round,
        SetDifference,
        SetIsSubset,
        Split,
        StrCaseCmp,
        Subtract,
        Trunc
    }

    internal static class AstBinaryOperatorExtensions
    {
        public static string Render(this AstBinaryOperator @operator)
        {
            return @operator switch
            {
                AstBinaryOperator.ArrayElemAt => "$arrayElemAt",
                AstBinaryOperator.ATan2 => "$atan2",
                AstBinaryOperator.Cmp => "$cmp",
                AstBinaryOperator.Divide => "$divide",
                AstBinaryOperator.Eq => "$eq",
                AstBinaryOperator.Gt => "$gt",
                AstBinaryOperator.Gte => "$gte",
                AstBinaryOperator.IfNull => "$ifNull",
                AstBinaryOperator.In => "$in",
                AstBinaryOperator.Log => "$log",
                AstBinaryOperator.Lt => "$lt",
                AstBinaryOperator.Lte => "$lte",
                AstBinaryOperator.Mod => "$mod",
                AstBinaryOperator.Ne => "$ne",
                AstBinaryOperator.Pow => "$pow",
                AstBinaryOperator.Round => "$round",
                AstBinaryOperator.SetDifference => "$setDifference",
                AstBinaryOperator.SetIsSubset => "$setIsSubset",
                AstBinaryOperator.Split => "$split",
                AstBinaryOperator.StrCaseCmp => "$strcasecmp",
                AstBinaryOperator.Subtract => "$subtract",
                AstBinaryOperator.Trunc => "$trunc",
                _ => throw new InvalidOperationException($"Unexpected binary operator: {@operator}.")
            };
        }
    }
}
