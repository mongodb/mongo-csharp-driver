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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal abstract class AstExpression : AstNode
    {
        #region static
        // private static properties
        private static readonly AstExpression __rootVar = AstExpression.Var("ROOT", isCurrent: true);

        // public static properties
        public static AstExpression RootVar => __rootVar;

        // public implicit conversions
        public static implicit operator AstExpression(BsonValue value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(bool value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(double value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(int value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(long value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(string value)
        {
            return new AstConstantExpression(value);
        }

        // public static methods
        public static AstExpression Abs(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Abs, arg);
        }

        public static AstAccumulatorField AccumulatorField(string name, AstUnaryAccumulatorOperator @operator, AstExpression arg)
        {
            var value = new AstUnaryAccumulatorExpression(@operator, arg);
            return new AstAccumulatorField(name, value);
        }

        public static AstAccumulatorField AccumulatorField(string name, AstAccumulatorExpression value)
        {
            return new AstAccumulatorField(name, value);
        }

        public static AstExpression Add(params AstExpression[] args)
        {
            if (AllArgsAreConstantInt32s(args, out var values))
            {
                var value = values.Sum();
                return new AstConstantExpression(value);
            }

            if (args.Length == 2)
            {
                if (args[0].IsZero())
                {
                    return args[1];
                }

                if (args[1].IsZero())
                {
                    return args[0];
                }
            }

            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.Add);
            return new AstNaryExpression(AstNaryOperator.Add, flattenedArgs);
        }

        public static AstExpression AllElementsTrue(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.AllElementsTrue, array);
        }

        public static AstExpression AnyElementTrue(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.AnyElementTrue, array);
        }

        public static AstExpression And(params AstExpression[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!args.Contains(null), "args cannot contain null.", nameof(args));

            if (AllArgsAreConstantBools(args, out var values))
            {
                var value = values.All(value => value);
                return new AstConstantExpression(value);
            }

            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.And);
            return new AstNaryExpression(AstNaryOperator.And, flattenedArgs);
        }

        public static AstExpression ArrayElemAt(AstExpression array, AstExpression index)
        {
            return new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, array, index);
        }

        public static AstExpression Avg(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Avg, array);
        }

        public static AstExpression Binary(AstBinaryOperator @operator, AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(@operator, arg1, arg2);
        }

        public static AstExpression BinaryWindowExpression(AstBinaryWindowOperator @operator, AstExpression arg1, AstExpression arg2, AstWindow window)
        {
            return new AstBinaryWindowExpression(@operator, arg1, arg2, window);
        }

        public static AstExpression BitAnd(params AstExpression[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!args.Contains(null), "args cannot contain null.", nameof(args));

            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.BitAnd);
            return new AstNaryExpression(AstNaryOperator.BitAnd, flattenedArgs);
        }

        public static AstExpression BitNot(AstExpression arg)
        {
            Ensure.IsNotNull(arg, nameof(arg));
            return new AstUnaryExpression(AstUnaryOperator.BitNot, arg);
        }

        public static AstExpression BitOr(params AstExpression[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!args.Contains(null), "args cannot contain null.", nameof(args));

            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.BitOr);
            return new AstNaryExpression(AstNaryOperator.BitOr, flattenedArgs);
        }

        public static AstExpression BitXor(params AstExpression[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!args.Contains(null), "args cannot contain null.", nameof(args));

            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.BitXor);
            return new AstNaryExpression(AstNaryOperator.BitXor, flattenedArgs);
        }

        public static AstExpression Ceil(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Ceil, arg);
        }

        public static AstExpression Cmp(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Cmp, arg1, arg2);
        }

        public static AstExpression Comparison(AstBinaryOperator comparisonOperator, AstExpression arg1, AstExpression arg2)
        {
            return comparisonOperator switch
            {
                AstBinaryOperator.Eq => AstExpression.Eq(arg1, arg2),
                AstBinaryOperator.Gt => AstExpression.Gt(arg1, arg2),
                AstBinaryOperator.Gte => AstExpression.Gte(arg1, arg2),
                AstBinaryOperator.Lt => AstExpression.Lt(arg1, arg2),
                AstBinaryOperator.Lte => AstExpression.Lte(arg1, arg2),
                AstBinaryOperator.Ne => AstExpression.Ne(arg1, arg2),
                _ => throw new ArgumentException($"Unexpected comparison operator: {comparisonOperator}.", nameof(comparisonOperator))
            };
        }

        public static AstExpression ComputedArray(IEnumerable<AstExpression> items)
        {
            return new AstComputedArrayExpression(items);
        }

        public static AstExpression ComputedArray(params AstExpression[] items)
        {
            return ComputedArray((IEnumerable<AstExpression>)items);
        }

        public static AstExpression ComputedDocument(IEnumerable<AstComputedField> fields)
        {
            return new AstComputedDocumentExpression(fields);
        }

        public static AstComputedField ComputedField(string name, AstExpression value)
        {
            return new AstComputedField(name, value);
        }

        public static IEnumerable<AstComputedField> ComputedFields(params (string name, AstExpression value)[] fields)
        {
            return fields.Select(field => new AstComputedField(field.name, field.value));
        }

        public static AstExpression Concat(params AstExpression[] args)
        {
            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.Concat);
            return new AstNaryExpression(AstNaryOperator.Concat, flattenedArgs);
        }

        public static AstExpression ConcatArrays(params AstExpression[] arrays)
        {
            return new AstNaryExpression(AstNaryOperator.ConcatArrays, arrays);
        }

        public static AstExpression Cond(AstExpression @if, AstExpression then, AstExpression @else)
        {
            return new AstCondExpression(@if, then, @else);
        }

        public static AstExpression Constant(BsonValue value)
        {
            return new AstConstantExpression(value);
        }

        public static AstExpression Convert(AstExpression input, AstExpression to, AstExpression onError = null, AstExpression onNull = null)
        {
            Ensure.IsNotNull(input, nameof(input));
            Ensure.IsNotNull(to, nameof(to));

            if (to is AstConstantExpression toConstantExpression &&
                (toConstantExpression.Value as BsonString)?.Value is string toValue &&
                toValue != null &&
                onError == null &&
                onNull == null)
            {
                var unaryOperator = toValue switch
                {
                    "bool" => AstUnaryOperator.ToBool,
                    "date" => AstUnaryOperator.ToDate,
                    "decimal" => AstUnaryOperator.ToDecimal,
                    "double" => AstUnaryOperator.ToDouble,
                    "int" => AstUnaryOperator.ToInt,
                    "long" => AstUnaryOperator.ToLong,
                    "objectId" => AstUnaryOperator.ToObjectId,
                    "string" => AstUnaryOperator.ToString,
                    _ => (AstUnaryOperator?)null
                };
                if (unaryOperator.HasValue)
                {
                    return AstExpression.Unary(unaryOperator.Value, input);
                }
            }

            return new AstConvertExpression(input, to, onError, onNull);
        }

        public static AstExpression DateAdd(
            AstExpression startDate,
            AstExpression unit,
            AstExpression amount,
            AstExpression timezone = null)
        {
            return new AstDateAddExpression(startDate, unit, amount, timezone);
        }

        public static AstExpression DateDiff(
            AstExpression startDate,
            AstExpression endDate,
            AstExpression unit,
            AstExpression timezone = null,
            AstExpression startOfWeek = null)
        {
            return new AstDateDiffExpression(startDate, endDate, unit, timezone, startOfWeek);
        }

        public static AstExpression DateFromParts(
            AstExpression year,
            AstExpression month = null,
            AstExpression day = null,
            AstExpression hour = null,
            AstExpression minute = null,
            AstExpression second = null,
            AstExpression millisecond = null,
            AstExpression timezone = null)
        {
            return new AstDateFromPartsExpression(year, month, day, hour, minute, second, millisecond, timezone);
        }

        public static AstExpression DateFromString(
            AstExpression dateString,
            AstExpression format = null,
            AstExpression timezone = null,
            AstExpression onError = null,
            AstExpression onNull = null)
        {
            return new AstDateFromStringExpression(dateString, format, timezone, onError, onNull);
        }

        public static AstExpression DatePart(AstDatePart part, AstExpression date, AstExpression timezone = null)
        {
            return new AstDatePartExpression(part, date, timezone);
        }

        public static AstExpression DateSubtract(
            AstExpression startDate,
            AstExpression unit,
            AstExpression amount,
            AstExpression timezone = null)
        {
            return new AstDateSubtractExpression(startDate, unit, amount, timezone);
        }

        public static AstExpression DateToString(AstExpression date, AstExpression format = null, AstExpression timezone = null, AstExpression onNull = null)
        {
            return new AstDateToStringExpression(date, format, timezone, onNull);
        }

        public static AstExpression DateTrunc(
           AstExpression date,
           AstExpression unit,
           AstExpression binSize = null,
           AstExpression timezone = null,
           AstExpression startOfWeek = null)
        {
            return new AstDateTruncExpression(date, unit, binSize, timezone, startOfWeek);
        }

        public static AstExpression DerivativeOrIntegralWindowExpression(AstDerivativeOrIntegralWindowOperator @operator, AstExpression arg, WindowTimeUnit? unit, AstWindow window)
        {
            return new AstDerivativeOrIntegralWindowExpression(@operator, arg, unit, window);
        }

        public static AstExpression Divide(AstExpression arg1, AstExpression arg2)
        {
            if (arg1 is AstConstantExpression constant1 && arg2 is AstConstantExpression constant2)
            {
                return Divide(constant1, constant2);
            }

            return new AstBinaryExpression(AstBinaryOperator.Divide, arg1, arg2);

            static AstExpression Divide(AstConstantExpression constant1, AstConstantExpression constant2)
            {
                return (constant1.Value.BsonType, constant2.Value.BsonType) switch
                {
                    (BsonType.Double, BsonType.Double) => constant1.Value.AsDouble / constant2.Value.AsDouble,
                    (BsonType.Double, BsonType.Int32) => constant1.Value.AsDouble / constant2.Value.AsInt32,
                    (BsonType.Double, BsonType.Int64) => constant1.Value.AsDouble / constant2.Value.AsInt64,
                    (BsonType.Int32, BsonType.Double) => constant1.Value.AsInt32 / constant2.Value.AsDouble,
                    (BsonType.Int32, BsonType.Int32) => (double)constant1.Value.AsInt32 / constant2.Value.AsInt32,
                    (BsonType.Int32, BsonType.Int64) => (double)constant1.Value.AsInt32 / constant2.Value.AsInt64,
                    (BsonType.Int64, BsonType.Double) => constant1.Value.AsInt64 / constant2.Value.AsDouble,
                    (BsonType.Int64, BsonType.Int32) => (double)constant1.Value.AsInt64 / constant2.Value.AsInt32,
                    (BsonType.Int64, BsonType.Int64) => (double)constant1.Value.AsInt64 / constant2.Value.AsInt64,
                    _ => new AstBinaryExpression(AstBinaryOperator.Divide, constant1, constant2)
                };
            }
        }

        public static AstExpression Eq(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Eq, arg1, arg2);
        }

        public static AstExpression Exp(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Exp, arg);
        }

        public static AstExpression ExponentialMovingAverageWindowExpression(AstExpression arg, ExponentialMovingAverageWeighting weighting, AstWindow window)
        {
            return new AstExponentialMovingAverageWindowExpression(arg, weighting, window);
        }

        public static AstFieldPathExpression FieldPath(string path)
        {
            return new AstFieldPathExpression(path);
        }

        public static AstExpression Filter(AstExpression input, AstExpression cond, string @as, AstExpression limit = null)
        {
            return new AstFilterExpression(input, cond, @as, limit);
        }

        public static AstExpression First(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.First, array);
        }

        public static AstExpression Floor(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Floor, arg);
        }

        public static AstGetFieldExpression GetField(AstExpression input, AstExpression fieldName)
        {
            return new AstGetFieldExpression(input, fieldName);
        }

        public static AstExpression Gt(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Gt, arg1, arg2);
        }

        public static AstExpression Gte(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Gte, arg1, arg2);
        }

        public static AstExpression IfNull(AstExpression arg, AstExpression replacement)
        {
            return new AstBinaryExpression(AstBinaryOperator.IfNull, arg, replacement);
        }

        public static AstExpression In(AstExpression value, AstExpression array)
        {
            return new AstBinaryExpression(AstBinaryOperator.In, value, array);
        }

        public static AstExpression IndexOfBytes(AstExpression @string, AstExpression value, AstExpression start = null, AstExpression end = null)
        {
            return new AstIndexOfBytesExpression(@string, value, start, end);
        }

        public static AstExpression IndexOfCP(AstExpression @string, AstExpression value, AstExpression start = null, AstExpression end = null)
        {
            return new AstIndexOfCPExpression(@string, value, start, end);
        }

        public static AstExpression IsArray(AstExpression value)
        {
            return new AstUnaryExpression(AstUnaryOperator.IsArray, value);
        }

        public static AstExpression IsMissing(AstGetFieldExpression field)
        {
            return AstExpression.Eq(AstExpression.Type(field), "missing");
        }

        public static AstExpression IsNotMissing(AstGetFieldExpression field)
        {
            return AstExpression.Ne(AstExpression.Type(field), "missing");
        }

        public static AstExpression IsNullOrMissing(AstGetFieldExpression field)
        {
            return AstExpression.In(AstExpression.Type(field), new BsonArray { "null", "missing" });
        }

        public static AstExpression Last(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Last, array);
        }

        public static AstExpression Let(AstVarBinding var, AstExpression @in)
        {
            if (var == null)
            {
                return @in;
            }
            else
            {
                return AstExpression.Let(new[] { var }, @in);
            }
        }

        public static AstExpression Let(AstVarBinding var1, AstVarBinding var2, AstExpression @in)
        {
            if (var1 == null && var2 == null)
            {
                return @in;
            }
            else
            {
                var vars = new List<AstVarBinding>(2);
                if (var1 != null) { vars.Add(var1); }
                if (var2 != null) { vars.Add(var2); }
                return AstExpression.Let(vars, @in);
            }
        }

        public static AstExpression Let(AstVarBinding var1, AstVarBinding var2, AstVarBinding var3, AstExpression @in)
        {
            if (var1 == null && var2 == null && var3 == null)
            {
                return @in;
            }
            else
            {
                var vars = new List<AstVarBinding>(3);
                if (var1 != null) { vars.Add(var1); }
                if (var2 != null) { vars.Add(var2); }
                if (var3 != null) { vars.Add(var3); }
                return AstExpression.Let(vars, @in);
            }
        }

        public static AstExpression Let(IEnumerable<AstVarBinding> vars, AstExpression @in)
        {
            return new AstLetExpression(vars, @in);
        }

        public static AstExpression Literal(AstExpression value)
        {
            return new AstUnaryExpression(AstUnaryOperator.Literal, value);
        }

        public static AstExpression Ln(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Ln, arg);
        }

        public static AstExpression Log(AstExpression arg, AstExpression @base)
        {
            return new AstBinaryExpression(AstBinaryOperator.Log, arg, @base);
        }

        public static AstExpression Log10(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Log10, arg);
        }

        public static AstExpression Lt(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Lt, arg1, arg2);
        }

        public static AstExpression Lte(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Lte, arg1, arg2);
        }

        public static AstExpression LTrim(AstExpression input, AstExpression chars = null)
        {
            return new AstLTrimExpression(input, chars);
        }

        public static AstExpression Map(AstExpression input, AstVarExpression @as, AstExpression @in)
        {
            return new AstMapExpression(input, @as, @in);
        }

        public static AstExpression Max(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Max, array);
        }

        public static AstExpression Max(AstExpression arg1, AstExpression arg2)
        {
            if (AllArgsAreConstantInt32s([arg1, arg2], out var values))
            {
                return values.Max();
            }

            return new AstNaryExpression(AstNaryOperator.Max, [arg1, arg2]);
        }

        public static AstExpression Min(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Min, array);
        }

        public static AstExpression Min(AstExpression arg1, AstExpression arg2)
        {
            if (AllArgsAreConstantInt32s([arg1, arg2], out var values))
            {
                return values.Min();
            }

            return new AstNaryExpression(AstNaryOperator.Min, [arg1, arg2]);
        }

        public static AstExpression Mod(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Mod, arg1, arg2);
        }

        public static AstExpression Multiply(params AstExpression[] args)
        {
            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.Multiply);
            return new AstNaryExpression(AstNaryOperator.Multiply, flattenedArgs);
        }

        public static AstExpression Ne(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Ne, arg1, arg2);
        }

        public static AstExpression Not(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Not, arg);
        }

        public static AstExpression NullaryWindowExpression(AstNullaryWindowOperator @operator, AstWindow window)
        {
            return new AstNullaryWindowExpression(@operator, window);
        }

        public static AstExpression ObjectToArray(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.ObjectToArray, arg);
        }

        public static AstExpression Or(params AstExpression[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!args.Contains(null), "args cannot contain null.", nameof(args));

            var flattenedArgs = FlattenNaryArgs(args, AstNaryOperator.Or);
            return new AstNaryExpression(AstNaryOperator.Or, flattenedArgs);
        }

        public static AstExpression PickExpression(AstPickOperator @operator, AstExpression source, AstSortFields sortBy, AstVarExpression @as, AstExpression selector, AstExpression n)
        {
            return new AstPickExpression(@operator, source, sortBy, @as, selector, n);
        }

        public static AstExpression PickAccumulatorExpression(AstPickAccumulatorOperator @operator, AstSortFields sortBy, AstExpression selector, AstExpression n)
        {
            return new AstPickAccumulatorExpression(@operator, sortBy, selector, n);
        }

        public static AstExpression Pow(AstExpression arg, AstExpression exponent)
        {
            return new AstBinaryExpression(AstBinaryOperator.Pow, arg, exponent);
        }

        public static AstExpression Push(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Push, arg);
        }

        public static AstExpression Range(AstExpression start, AstExpression end, AstExpression step = null)
        {
            return new AstRangeExpression(start, end, step);
        }

        public static AstExpression Reduce(AstExpression input, AstExpression initialValue, AstExpression @in)
        {
            return new AstReduceExpression(input, initialValue, @in);
        }

        public static AstExpression RegexMatch(AstExpression input, string pattern, string options)
            => new AstRegexExpression(AstRegexOperator.Match, input, pattern, options);

        public static AstExpression ReverseArray(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.ReverseArray, array);
        }

        public static AstExpression Round(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Round, arg);
        }

        public static AstExpression Round(AstExpression arg, AstExpression place)
        {
            return new AstBinaryExpression(AstBinaryOperator.Round, arg, place);
        }

        public static AstExpression RTrim(AstExpression input, AstExpression chars = null)
        {
            return new AstRTrimExpression(input, chars);
        }

        public static AstExpression SetDifference(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.SetDifference, arg1, arg2);
        }

        public static AstExpression SetEquals(AstExpression arg1, AstExpression arg2)
        {
            return new AstNaryExpression(AstNaryOperator.SetEquals, arg1, arg2);
        }

        public static AstExpression SetIntersection(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.SetIntersection, arg);
        }

        public static AstExpression SetIntersection(params AstExpression[] args)
        {
            return new AstNaryExpression(AstNaryOperator.SetIntersection, args);
        }

        public static AstExpression SetIsSubset(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.SetIsSubset, arg1, arg2);
        }

        public static AstExpression SetUnion(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.SetUnion, arg);
        }

        public static AstExpression SetUnion(params AstExpression[] args)
        {
            return new AstNaryExpression(AstNaryOperator.SetUnion, args);
        }

        public static AstExpression ShiftWindowExpression(AstExpression arg, int by, AstExpression defaultValue)
        {
            return new AstShiftWindowExpression(arg, by, defaultValue);
        }

        public static AstExpression Size(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Size, arg);
        }

        public static AstExpression Slice(AstExpression array, AstExpression n)
        {
            return new AstSliceExpression(array, position: null, n);
        }

        public static AstExpression Slice(AstExpression array, AstExpression position, AstExpression n)
        {
            return new AstSliceExpression(array, position, n);
        }

        public static AstExpression SortArray(AstExpression input, AstSortFields fields)
        {
            return new AstSortArrayExpression(input, fields);
        }

        public static AstExpression SortArray(AstExpression input, params AstSortField[] fields)
        {
            return new AstSortArrayExpression(input, new AstSortFields(fields));
        }

        public static AstExpression SortArray(AstExpression input, AstSortOrder order)
        {
            return new AstSortArrayExpression(input, order);
        }

        public static AstExpression Split(AstExpression arg, AstExpression delimiter)
        {
            return new AstBinaryExpression(AstBinaryOperator.Split, arg, delimiter);
        }

        public static AstExpression Sqrt(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Sqrt, arg);
        }

        public static AstExpression StdDev(AstUnaryOperator stdDevOperator, AstExpression array)
        {
            return stdDevOperator switch
            {
                AstUnaryOperator.StdDevPop => AstExpression.StdDevPop(array),
                AstUnaryOperator.StdDevSamp => AstExpression.StdDevSamp(array),
                _ => throw new ArgumentException($"Unexpected stddev operator: {stdDevOperator}.", nameof(stdDevOperator))
            };
        }

        public static AstExpression StdDevPop(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.StdDevPop, array);
        }

        public static AstExpression StdDevSamp(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.StdDevSamp, array);
        }

        public static AstExpression StrCaseCmp(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.StrCaseCmp, arg1, arg2);
        }

        public static AstExpression StrLen(AstUnaryOperator strlenOperator, AstExpression arg)
        {
            return strlenOperator switch
            {
                AstUnaryOperator.StrLenBytes => AstExpression.StrLenBytes(arg),
                AstUnaryOperator.StrLenCP => AstExpression.StrLenCP(arg),
                _ => throw new ArgumentException($"Unexpected strlen operator: {strlenOperator}.", nameof(strlenOperator))
            };
        }

        public static AstExpression StrLenBytes(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.StrLenBytes, arg);
        }

        public static AstExpression StrLenCP(AstExpression arg)
        {
            if (arg is AstConstantExpression constantExpression && constantExpression.Value.BsonType == BsonType.String)
            {
                var value = constantExpression.Value.AsString.Length;
                return new AstConstantExpression(value);
            }
            return new AstUnaryExpression(AstUnaryOperator.StrLenCP, arg);
        }

        public static AstExpression Substr(AstTernaryOperator substrOperator, AstExpression arg, AstExpression index, AstExpression count)
        {
            return substrOperator switch
            {
                AstTernaryOperator.SubstrBytes => AstExpression.SubstrBytes(arg, index, count),
                AstTernaryOperator.SubstrCP => AstExpression.SubstrCP(arg, index, count),
                _ => throw new ArgumentException($"Unexpected substr operator: {substrOperator}.", nameof(substrOperator))
            };
        }

        public static AstExpression SubstrBytes(AstExpression arg, AstExpression index, AstExpression count)
        {
            return new AstTernaryExpression(AstTernaryOperator.SubstrBytes, arg, index, count);
        }

        public static AstExpression SubstrCP(AstExpression arg, AstExpression index, AstExpression count)
        {
            return new AstTernaryExpression(AstTernaryOperator.SubstrCP, arg, index, count);
        }

        public static AstExpression Subtract(AstExpression arg1, AstExpression arg2)
        {
            if (AllArgsAreConstantInt32s([arg1, arg2], out var values))
            {
                var value = values[0] - values[1];
                return new AstConstantExpression(value);
            }

            if (arg2.IsZero())
            {
                return arg1;
            }

            return new AstBinaryExpression(AstBinaryOperator.Subtract, arg1, arg2);
        }

        public static AstExpression Sum(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Sum, array);
        }

        public static AstExpression ToLower(AstExpression arg)
        {
            if (arg is AstConstantExpression constantExpression && constantExpression.Value.BsonType == BsonType.String)
            {
                var value = constantExpression.Value.AsString.ToLowerInvariant();
                return new AstConstantExpression(value);
            }

            return new AstUnaryExpression(AstUnaryOperator.ToLower, arg);
        }

        public static AstExpression ToString(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.ToString, arg);
        }

        public static AstExpression ToUpper(AstExpression arg)
        {
            if (arg is AstConstantExpression constantExpression && constantExpression.Value.BsonType == BsonType.String)
            {
                var value = constantExpression.Value.AsString.ToUpperInvariant();
                return new AstConstantExpression(value);
            }

            return new AstUnaryExpression(AstUnaryOperator.ToUpper, arg);
        }

        public static AstExpression Trim(AstExpression input, AstExpression chars = null)
        {
            return new AstTrimExpression(input, chars);
        }

        public static AstExpression Trunc(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Trunc, arg);
        }

        public static AstExpression Type(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Type, arg);
        }

        public static AstExpression Unary(AstUnaryOperator @operator, AstExpression arg)
        {
            return new AstUnaryExpression(@operator, arg);
        }

        public static AstAccumulatorExpression UnaryAccumulator(AstUnaryAccumulatorOperator @operator, AstExpression arg)
        {
            return new AstUnaryAccumulatorExpression(@operator, arg);
        }

        public static AstExpression UnaryWindowExpression(AstUnaryWindowOperator @operator, AstExpression arg, AstWindow window)
        {
            return new AstUnaryWindowExpression(@operator, arg, window);
        }

        public static (AstVarBinding, AstExpression) UseVarIfNotSimple(string name, AstExpression expression)
        {
            if (IsSimple(expression))
            {
                return (null, expression);
            }
            else
            {
                var var = AstExpression.Var(name);
                var varBinding = AstExpression.VarBinding(var, expression);
                return (varBinding, var);
            }

            static bool IsSimple(AstExpression expression)
            {
                return
                    expression == null ||
                    expression.NodeType == AstNodeType.ConstantExpression ||
                    expression.CanBeConvertedToFieldPath();
            }
        }

        public static AstVarExpression Var(string name, bool isCurrent = false)
        {
            return new AstVarExpression(name, isCurrent);
        }

        public static AstVarBinding VarBinding(AstVarExpression var, AstExpression value)
        {
            return new AstVarBinding(var, value);
        }

        public static AstExpression Zip(IEnumerable<AstExpression> inputs, bool? useLongestLength = null, AstExpression defaults = null)
        {
            return new AstZipExpression(inputs, useLongestLength, defaults);
        }

        // private static methods
        private static bool AllArgsAreConstantBools(AstExpression[] args, out List<bool> values)
        {
            if (args.All(arg => arg is AstConstantExpression constantExpression && constantExpression.Value.BsonType == BsonType.Boolean))
            {
                values = args.Select(arg => ((AstConstantExpression)arg).Value.AsBoolean).ToList();
                return true;
            }

            values = null;
            return false;
        }

        private static bool AllArgsAreConstantInt32s(AstExpression[] args, out List<int> values)
        {
            if (args.All(arg => arg is AstConstantExpression constantExpression && constantExpression.Value.BsonType == BsonType.Int32))
            {
                values = args.Select(arg => ((AstConstantExpression)arg).Value.AsInt32).ToList();
                return true;
            }

            values = null;
            return false;
        }

        private static IEnumerable<AstExpression> FlattenNaryArgs(IEnumerable<AstExpression> args, AstNaryOperator naryOperator)
        {
            if (args.Any(arg => arg is AstNaryExpression naryExpression && naryExpression.Operator == naryOperator))
            {
                var flattenedArgs = new List<AstExpression>();
                foreach (var arg in args)
                {
                    if (arg is AstNaryExpression naryExpression && naryExpression.Operator == naryOperator)
                    {
                        flattenedArgs.AddRange(naryExpression.Args);
                    }
                    else
                    {
                        flattenedArgs.Add(arg);
                    }
                }
                return flattenedArgs;
            }

            return args;
        }
        #endregion static

        // public methods
        public virtual bool CanBeConvertedToFieldPath() => false;

        public virtual string ConvertToFieldPath()
        {
            throw new InvalidOperationException($"{this} cannot be converted to a field path.");
        }
    }
}
