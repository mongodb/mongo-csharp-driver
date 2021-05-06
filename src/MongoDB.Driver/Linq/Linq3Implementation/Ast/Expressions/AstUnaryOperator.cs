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
        public static string Render(this AstUnaryOperator @operator)
        {
            switch (@operator)
            {
                case AstUnaryOperator.Abs: return "$abs";
                case AstUnaryOperator.ACos: return "$acos";
                case AstUnaryOperator.ACosh: return "$acosh";
                case AstUnaryOperator.AddToSet: return "$addToSet";
                case AstUnaryOperator.AllElementsTrue: return "$allElementsTrue";
                case AstUnaryOperator.AnyElementTrue: return "$anyElementTrue";
                case AstUnaryOperator.ArrayToObject: return "$arrayToObject";
                case AstUnaryOperator.ASin: return "$asin";
                case AstUnaryOperator.ASinh: return "$asinh";
                case AstUnaryOperator.ATan: return "$atan";
                case AstUnaryOperator.ATanh: return "$atanh";
                case AstUnaryOperator.Avg: return "$avg";
                case AstUnaryOperator.BinarySize: return "$binarySize";
                case AstUnaryOperator.BsonSize: return "$bsonSize";
                case AstUnaryOperator.Ceil: return "$ceil";
                case AstUnaryOperator.Cos: return "$cos";
                case AstUnaryOperator.DegreesToRadians: return "$degreesToRadians";
                case AstUnaryOperator.Exp: return "$exp";
                case AstUnaryOperator.First: return "$first";
                case AstUnaryOperator.Floor: return "$floor";
                case AstUnaryOperator.IsArray: return "$isArray";
                case AstUnaryOperator.IsNumber: return "$isNumber";
                case AstUnaryOperator.Last: return "$last";
                case AstUnaryOperator.Literal: return "$literal";
                case AstUnaryOperator.Ln: return "$ln";
                case AstUnaryOperator.Log10: return "$log10";
                case AstUnaryOperator.Max: return "$max";
                case AstUnaryOperator.MergeObjects: return "$mergeObjects";
                case AstUnaryOperator.Meta: return "$meta";
                case AstUnaryOperator.Min: return "$min";
                case AstUnaryOperator.Not: return "$not";
                case AstUnaryOperator.ObjectToArray:return "$objectToArray";
                case AstUnaryOperator.Push: return "$push";
                case AstUnaryOperator.RadiansToDegrees: return "$radiansToDegrees";
                case AstUnaryOperator.ReverseArray: return "$reverseArray";
                case AstUnaryOperator.Round: return "$round";
                case AstUnaryOperator.Sin: return "$sin";
                case AstUnaryOperator.Size: return "$size";
                case AstUnaryOperator.Sqrt: return "$sqrt";
                case AstUnaryOperator.StdDevPop: return "$stdDevPop";
                case AstUnaryOperator.StdDevSamp: return "$stdDevSamp";
                case AstUnaryOperator.StrLenBytes: return "$strLenBytes";
                case AstUnaryOperator.StrLenCP: return "$strLenCP";
                case AstUnaryOperator.Sum: return "$sum";
                case AstUnaryOperator.Tan: return "$tan";
                case AstUnaryOperator.ToBool: return "$toBool";
                case AstUnaryOperator.ToDate: return "$toDate";
                case AstUnaryOperator.ToDecimal: return "$toDecimal";
                case AstUnaryOperator.ToDouble: return "$toDouble";
                case AstUnaryOperator.ToInt: return "$toInt";
                case AstUnaryOperator.ToLong: return "$toLong";
                case AstUnaryOperator.ToLower: return "$toLower";
                case AstUnaryOperator.ToObjectId: return "$toObjectId";
                case AstUnaryOperator.ToString: return "$toString";
                case AstUnaryOperator.ToUpper: return "$toUpper";
                case AstUnaryOperator.Trunc: return "$trunc";
                case AstUnaryOperator.Type: return "$type";
                default: throw new InvalidOperationException($"Unexpected unary operator: {@operator}.");
            }
        }
    }
}
