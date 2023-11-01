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
    internal enum AstNaryOperator
    {
        Add,
        And,
        Avg,
        BitAnd,
        BitOr,
        BitXor,
        Concat,
        ConcatArrays,
        Max,
        MergeObjects,
        Min,
        Multiply,
        Or,
        SetEquals,
        SetIntersection,
        SetUnion,
        StdDevPop,
        StdDevSamp,
        Sum
    }

    internal static class AstNaryOperatorExtensions
    {
        public static string Render(this AstNaryOperator @operator)
        {
            return @operator switch
            {
                AstNaryOperator.Add => "$add",
                AstNaryOperator.And => "$and",
                AstNaryOperator.Avg => "$avg",
                AstNaryOperator.BitAnd => "$bitAnd",
                AstNaryOperator.BitOr => "$bitOr",
                AstNaryOperator.BitXor => "$bitXor",
                AstNaryOperator.Concat => "$concat",
                AstNaryOperator.ConcatArrays => "$concatArrays",
                AstNaryOperator.Max => "$max",
                AstNaryOperator.MergeObjects => "$mergeObjects",
                AstNaryOperator.Min => "$min",
                AstNaryOperator.Multiply => "$multiply",
                AstNaryOperator.Or => "$or",
                AstNaryOperator.SetEquals => "$setEquals",
                AstNaryOperator.SetIntersection => "$setIntersection",
                AstNaryOperator.SetUnion => "$setUnion",
                AstNaryOperator.StdDevPop => "$stdDevPop",
                AstNaryOperator.StdDevSamp => "$stdDevSamp",
                AstNaryOperator.Sum => "$sum",
                _ => throw new InvalidOperationException($"Unexpected n-ary operator: {@operator}.")
            };
        }
    }
}
