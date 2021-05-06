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
        Avg,
        Concat,
        ConcatArrays,
        Max,
        MergeObjects,
        Min,
        Multiply,
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
            switch (@operator)
            {
                case AstNaryOperator.Add: return "$add";
                case AstNaryOperator.Avg: return "$avg";
                case AstNaryOperator.Concat: return "$concat";
                case AstNaryOperator.ConcatArrays: return "$concatArrays";
                case AstNaryOperator.Max: return "$max";
                case AstNaryOperator.MergeObjects: return "$mergeObjects";
                case AstNaryOperator.Min: return "$min";
                case AstNaryOperator.Multiply: return "$multiply";
                case AstNaryOperator.SetEquals: return "$setEquals";
                case AstNaryOperator.SetIntersection: return "$setIntersection";
                case AstNaryOperator.SetUnion: return "$setUnion";
                case AstNaryOperator.StdDevPop: return "$stdDevPop";
                case AstNaryOperator.StdDevSamp: return "$stdDevSamp";
                case AstNaryOperator.Sum: return "$sum";
                default: throw new InvalidOperationException($"Unexpected n-ary operator: {@operator}.");
            }
        }
    }
}
