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

namespace MongoDB.Driver.Linq3.Ast.Expressions
{
    public abstract class AstExpression : AstNode
    {
        // public implicit conversions
        public static implicit operator AstExpression(BsonValue value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(bool value)
        {
            return new AstConstantExpression(value);
        }

        public static implicit operator AstExpression(int value)
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

        public static AstExpression Add(params AstExpression[] args)
        {
            if (AllArgsAreConstantInt32s(args, out var values))
            {
                var value = values.Sum();
                return new AstConstantExpression(value);
            }

            if (args.Any(arg => arg is AstNaryExpression naryExpression && naryExpression.Operator == AstNaryOperator.Add))
            {
                var flattenedArgs = new List<AstExpression>();
                foreach (var arg in args)
                {
                    if (arg is AstNaryExpression naryExpression && naryExpression.Operator == AstNaryOperator.Add)
                    {
                        flattenedArgs.AddRange(naryExpression.Args);
                    }
                    else
                    {
                        flattenedArgs.Add(arg);
                    }
                }
                return new AstNaryExpression(AstNaryOperator.Add, flattenedArgs);
            }

            return new AstNaryExpression(AstNaryOperator.Add, args);
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

            if (args.Any(arg => arg.NodeType == AstNodeType.AndExpression))
            {
                var flattenedArgs = new List<AstExpression>();
                foreach (var arg in args)
                {
                    if (arg is AstAndExpression andExpression)
                    {
                        flattenedArgs.AddRange(andExpression.Args);
                    }
                    else
                    {
                        flattenedArgs.Add(arg);
                    }
                }
                return new AstAndExpression(flattenedArgs);
            }

            return new AstAndExpression(args);
        }

        public static AstExpression ArrayElemAt(AstExpression array, AstExpression index)
        {
            return new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, array, index);
        }

        public static AstExpression Avg(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Avg, array);
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

        public static AstExpression Concat(IEnumerable<AstExpression> args)
        {
            return new AstNaryExpression(AstNaryOperator.Concat, args);
        }

        public static AstExpression Concat(params AstExpression[] args)
        {
            return new AstNaryExpression(AstNaryOperator.Concat, args);
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
            return new AstConvertExpression(input, to, onError, onNull);
        }

        public static AstExpression Convert(AstExpression input, Type toType, AstExpression onError = null, AstExpression onNull = null)
        {
            Ensure.IsNotNull(toType, nameof(toType));
            var to = toType.FullName switch
            {
                "MongoDB.Bson.ObjectId" => "objectId",
                "System.Boolean" => "bool",
                "System.DateTime" => "date",
                "System.Decimal" => "decimal",
                "System.Double" => "double",
                "System.Int32" => "int",
                "System.Int64" => "long",
                "System.String" => "string",
                _ => throw new ArgumentException($"Invalid toType: {toType.FullName}.", nameof(toType))
            };

            return new AstConvertExpression(input, to, onError, onNull);
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

        public static AstExpression DateToString(AstExpression date, AstExpression format = null, AstExpression timezone = null, AstExpression onNull = null)
        {
            return new AstDateToStringExpression(date, format, timezone, onNull);
        }

        public static AstExpression Divide(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Divide, arg1, arg2);
        }

        public static AstExpression Eq(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Eq, arg1, arg2);
        }

        public static AstExpression Exp(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Exp, arg);
        }

        public static AstExpression Field(string path)
        {
            return new AstFieldExpression(path);
        }

        public static AstExpression Filter(AstExpression input, AstExpression cond, string @as)
        {
            return new AstFilterExpression(input, cond, @as);
        }

        public static AstExpression Floor(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Floor, arg);
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

        public static AstExpression Last(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Last, array);
        }

        public static AstExpression Let(AstComputedField var, AstExpression @in)
        {
            if (var == null)
            {
                return @in;
            }
            else
            {
                return new AstLetExpression(new[] { var }, @in);
            }
        }

        public static AstExpression Let(AstComputedField var1, AstComputedField var2, AstExpression @in)
        {
            if (var1 == null && var2 == null)
            {
                return @in;
            }
            else
            {
                var vars = new List<AstComputedField>(2);
                if (var1 != null) { vars.Add(var1); }
                if (var2 != null) { vars.Add(var2); }
                return new AstLetExpression(vars, @in);
            }
        }

        public static AstExpression Let(AstComputedField var1, AstComputedField var2, AstComputedField var3, AstExpression @in)
        {
            if (var1 == null && var2 == null && var3 == null)
            {
                return @in;
            }
            else
            {
                var vars = new List<AstComputedField>(2);
                if (var1 != null) { vars.Add(var1); }
                if (var2 != null) { vars.Add(var2); }
                if (var3 != null) { vars.Add(var3); }
                return new AstLetExpression(vars, @in);
            }
        }

        public static AstExpression Let(IEnumerable<AstComputedField> vars, AstExpression @in)
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

        public static AstExpression Map(AstExpression input, string @as, AstExpression @in)
        {
            var prefix = "$" + @as + ".";
            if (input is AstFieldExpression inputField && @in is AstFieldExpression inField && inField.Path.StartsWith(prefix))
            {
                var subFieldName = inField.Path.Substring(prefix.Length);
                return AstExpression.SubField(inputField, subFieldName);
            }

            return new AstMapExpression(input, @as, @in);
        }

        public static AstExpression Max(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Max, array);
        }

        public static AstExpression Min(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.Min, array);
        }

        public static AstExpression Mod(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Mod, arg1, arg2);
        }

        public static AstExpression Multiply(params AstExpression[] args)
        {
            if (args.Any(arg => arg is AstNaryExpression naryExpression && naryExpression.Operator == AstNaryOperator.Multiply))
            {
                var flattenedArgs = new List<AstExpression>();
                foreach (var arg in args)
                {
                    if (arg is AstNaryExpression naryExpression && naryExpression.Operator == AstNaryOperator.Multiply)
                    {
                        flattenedArgs.AddRange(naryExpression.Args);
                    }
                    else
                    {
                        flattenedArgs.Add(arg);
                    }
                }
                return new AstNaryExpression(AstNaryOperator.Multiply, flattenedArgs);
            }


            return new AstNaryExpression(AstNaryOperator.Multiply, args);
        }

        public static AstExpression Ne(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.Ne, arg1, arg2);
        }

        public static AstExpression Not(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Not, arg);
        }

        public static AstExpression Or(params AstExpression[] args)
        {
            Ensure.IsNotNull(args, nameof(args));
            Ensure.That(args.Length > 0, "args cannot be empty.", nameof(args));
            Ensure.That(!args.Contains(null), "args cannot contain null.", nameof(args));

            if (args.Any(a => a.NodeType == AstNodeType.OrExpression))
            {
                var flattenedArgs = new List<AstExpression>();
                foreach (var arg in args)
                {
                    if (arg is AstOrExpression orExpression)
                    {
                        flattenedArgs.AddRange(orExpression.Args);
                    }
                    else
                    {
                        flattenedArgs.Add(arg);
                    }
                }
                return new AstOrExpression(flattenedArgs);
            }

            return new AstOrExpression(args);
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

        public static AstExpression ReverseArray(AstExpression array)
        {
            return new AstUnaryExpression(AstUnaryOperator.ReverseArray, array);
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

        public static AstExpression SetIntersection(params AstExpression[] args)
        {
            return new AstNaryExpression(AstNaryOperator.SetIntersection, args);
        }

        public static AstExpression SetIsSubset(AstExpression arg1, AstExpression arg2)
        {
            return new AstBinaryExpression(AstBinaryOperator.SetIsSubset, arg1, arg2);
        }

        public static AstExpression SetUnion(params AstExpression[] args)
        {
            return new AstNaryExpression(AstNaryOperator.SetUnion, args);
        }

        public static AstExpression Size(AstExpression arg)
        {
            return new AstUnaryExpression(AstUnaryOperator.Size, arg);
        }

        public static AstExpression Slice(AstExpression array, AstExpression n)
        {
            return new AstSliceExpression(array, n);
        }

        public static AstExpression Slice(AstExpression array, AstExpression position, AstExpression n)
        {
            return new AstSliceExpression(array, position, n);
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
                var value = constantExpression.Value.AsString;
                return value;
            }
            return new AstUnaryExpression(AstUnaryOperator.StrLenCP, arg);
        }

        public static AstExpression SubField(AstExpression expression, string subFieldName)
        {
            Ensure.IsNotNull(expression, nameof(expression));
            Ensure.IsNotNull(subFieldName, nameof(subFieldName));

            if (expression is AstFieldExpression fieldExpression)
            {
                return fieldExpression.SubField(subFieldName);
            }
            else
            {
                return AstExpression.Let(AstExpression.ComputedField("this", expression), AstExpression.Field($"$this.{subFieldName}"));
            }
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
                var value = constantExpression.Value.AsString;
                return value.ToLowerInvariant();
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
                var value = constantExpression.Value.AsString;
                return value.ToUpperInvariant();
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

        public static (AstComputedField, AstExpression) UseVarIfNotSimple(string name, AstExpression expression)
        {
            if (IsSimple(expression))
            {
                return (null, expression);
            }
            else
            {
                var var = AstExpression.ComputedField(name, expression);
                var simpleAst = AstExpression.Field("$" + name);
                return (var, simpleAst);
            }

            static bool IsSimple(AstExpression expression)
            {
                return
                    expression == null ||
                    expression.NodeType == AstNodeType.ConstantExpression ||
                    expression.NodeType == AstNodeType.FieldExpression;
            }
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
    }
}
