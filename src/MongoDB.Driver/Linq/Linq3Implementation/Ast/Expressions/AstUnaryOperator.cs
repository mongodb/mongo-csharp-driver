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
    internal enum AstUnaryOperator
    {
        Abs,
        ACos,
        ACosh,
        AddToSet,
        AllElementsTrue,
        AnyElementTrue,
        ArrayToObject,
        ASin,
        ASinh,
        ATan,
        ATanh,
        Avg,
        BinarySize,
        BsonSize,
        Ceil,
        Cos,
        DegreesToRadians,
        Exp,
        First,
        Floor,
        IsArray,
        IsNumber,
        Last,
        Literal,
        Ln,
        Log10,
        Max,
        MergeObjects,
        Meta,
        Min,
        Not,
        ObjectToArray,
        Push,
        RadiansToDegrees,
        ReverseArray,
        Round,
        Sin,
        Size,
        Sqrt,
        StdDevPop,
        StdDevSamp,
        StrLenBytes,
        StrLenCP,
        Sum,
        Tan,
        ToBool,
        ToDate,
        ToDecimal,
        ToDouble,
        ToInt,
        ToLong,
        ToLower,
        ToObjectId,
        ToString,
        ToUpper,
        Trunc,
        Type
    }

    internal static class AstUnaryOperatorExtensions
    {
        public static bool IsAccumulator(this AstUnaryOperator @operator, out AstAccumulatorOperator accumulatorOperator)
        {
            switch (@operator)
            {
                case AstUnaryOperator.AddToSet: accumulatorOperator = AstAccumulatorOperator.AddToSet; return true;
                case AstUnaryOperator.Avg: accumulatorOperator = AstAccumulatorOperator.Avg; return true;
                case AstUnaryOperator.First: accumulatorOperator = AstAccumulatorOperator.First; return true;
                case AstUnaryOperator.Last: accumulatorOperator = AstAccumulatorOperator.Last; return true;
                case AstUnaryOperator.Max: accumulatorOperator = AstAccumulatorOperator.Max; return true;
                case AstUnaryOperator.Min: accumulatorOperator = AstAccumulatorOperator.Min; return true;
                case AstUnaryOperator.Push: accumulatorOperator = AstAccumulatorOperator.Push; return true;
                case AstUnaryOperator.StdDevPop: accumulatorOperator = AstAccumulatorOperator.StdDevPop; return true;
                case AstUnaryOperator.StdDevSamp: accumulatorOperator = AstAccumulatorOperator.StdDevSamp; return true;
                case AstUnaryOperator.Sum: accumulatorOperator = AstAccumulatorOperator.Sum; return true;
                default: accumulatorOperator = default; return false;
            }
        }

        public static string Render(this AstUnaryOperator @operator)
        {
            return @operator switch
            {
                AstUnaryOperator.Abs => "$abs",
                AstUnaryOperator.ACos => "$acos",
                AstUnaryOperator.ACosh => "$acosh",
                AstUnaryOperator.AddToSet => "$addToSet",
                AstUnaryOperator.AllElementsTrue => "$allElementsTrue",
                AstUnaryOperator.AnyElementTrue => "$anyElementTrue",
                AstUnaryOperator.ArrayToObject => "$arrayToObject",
                AstUnaryOperator.ASin => "$asin",
                AstUnaryOperator.ASinh => "$asinh",
                AstUnaryOperator.ATan => "$atan",
                AstUnaryOperator.ATanh => "$atanh",
                AstUnaryOperator.Avg => "$avg",
                AstUnaryOperator.BinarySize => "$binarySize",
                AstUnaryOperator.BsonSize => "$bsonSize",
                AstUnaryOperator.Ceil => "$ceil",
                AstUnaryOperator.Cos => "$cos",
                AstUnaryOperator.DegreesToRadians => "$degreesToRadians",
                AstUnaryOperator.Exp => "$exp",
                AstUnaryOperator.First => "$first",
                AstUnaryOperator.Floor => "$floor",
                AstUnaryOperator.IsArray => "$isArray",
                AstUnaryOperator.IsNumber => "$isNumber",
                AstUnaryOperator.Last => "$last",
                AstUnaryOperator.Literal => "$literal",
                AstUnaryOperator.Ln => "$ln",
                AstUnaryOperator.Log10 => "$log10",
                AstUnaryOperator.Max => "$max",
                AstUnaryOperator.MergeObjects => "$mergeObjects",
                AstUnaryOperator.Meta => "$meta",
                AstUnaryOperator.Min => "$min",
                AstUnaryOperator.Not => "$not",
                AstUnaryOperator.ObjectToArray => "$objectToArray",
                AstUnaryOperator.Push => "$push",
                AstUnaryOperator.RadiansToDegrees => "$radiansToDegrees",
                AstUnaryOperator.ReverseArray => "$reverseArray",
                AstUnaryOperator.Round => "$round",
                AstUnaryOperator.Sin => "$sin",
                AstUnaryOperator.Size => "$size",
                AstUnaryOperator.Sqrt => "$sqrt",
                AstUnaryOperator.StdDevPop => "$stdDevPop",
                AstUnaryOperator.StdDevSamp => "$stdDevSamp",
                AstUnaryOperator.StrLenBytes => "$strLenBytes",
                AstUnaryOperator.StrLenCP => "$strLenCP",
                AstUnaryOperator.Sum => "$sum",
                AstUnaryOperator.Tan => "$tan",
                AstUnaryOperator.ToBool => "$toBool",
                AstUnaryOperator.ToDate => "$toDate",
                AstUnaryOperator.ToDecimal => "$toDecimal",
                AstUnaryOperator.ToDouble => "$toDouble",
                AstUnaryOperator.ToInt => "$toInt",
                AstUnaryOperator.ToLong => "$toLong",
                AstUnaryOperator.ToLower => "$toLower",
                AstUnaryOperator.ToObjectId => "$toObjectId",
                AstUnaryOperator.ToString => "$toString",
                AstUnaryOperator.ToUpper => "$toUpper",
                AstUnaryOperator.Trunc => "$trunc",
                AstUnaryOperator.Type => "$type",
                _ => throw new InvalidOperationException($"Unexpected unary operator: {@operator}.")
            };
        }
    }
}
