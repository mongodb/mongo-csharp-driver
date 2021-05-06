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
            switch (@operator)
            {
                case AstBinaryOperator.ArrayElemAt: return "$arrayElemAt";
                case AstBinaryOperator.ATan2: return "$atan2";
                case AstBinaryOperator.Cmp: return "$cmp";
                case AstBinaryOperator.Divide: return "$divide";
                case AstBinaryOperator.Eq: return "$eq";
                case AstBinaryOperator.Gt: return "$gt";
                case AstBinaryOperator.Gte: return "$gte";
                case AstBinaryOperator.IfNull: return "$ifNull";
                case AstBinaryOperator.In: return "$in";
                case AstBinaryOperator.Log: return "$log";
                case AstBinaryOperator.Lt: return "$lt";
                case AstBinaryOperator.Lte: return "$lte";
                case AstBinaryOperator.Mod: return "$mod";
                case AstBinaryOperator.Ne: return "$ne";
                case AstBinaryOperator.Pow: return "$pow";
                case AstBinaryOperator.Round: return "$round";
                case AstBinaryOperator.SetDifference: return "$setDifference";
                case AstBinaryOperator.SetIsSubset: return "$setIsSubset";
                case AstBinaryOperator.Split: return "$split";
                case AstBinaryOperator.StrCaseCmp: return "$strcasecmp";
                case AstBinaryOperator.Subtract: return "$subtract";
                case AstBinaryOperator.Trunc: return "$trunc";
                default: throw new InvalidOperationException($"Unexpected binary operator: {@operator}.");
            }
        }
    }
}
