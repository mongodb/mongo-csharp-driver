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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class IndexOfAnyMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __indexOfAnyMethods =
        {
            StringMethod.IndexOfAny,
            StringMethod.IndexOfAnyWithStartIndex,
            StringMethod.IndexOfAnyWithStartIndexAndCount,
       };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__indexOfAnyMethods))
            {
                var (stringVar, stringAst) = TranslateObject(expression.Object);
                var anyOf = TranslateAnyOf(arguments);
                var (startIndexVar, startIndexAst) = TranslateStartIndex(arguments);
                var (countVar, countAst) = TranslateCount(arguments);
                var (endVar, endAst) = ComputeEnd(startIndexAst, countAst);

                AstExpression ast;
                if (anyOf.Length == 0)
                {
                    ast = new AstConstantExpression(0);
                }
                else
                {
                    // indexOfChar => { $indexOfCP : ['$$string', { $substrCP : [<anyOf>, '$$anyOfIndex', 1] }, '$$startIndex', '$$end' }
                    var charAst = new AstTernaryExpression(AstTernaryOperator.SubstrCP, anyOf, new AstFieldExpression("$anyOfIndex"), 1);
                    ast = new AstIndexOfCPExpression(stringAst, charAst, startIndexAst, endAst);

                    // computeIndexes => { $map : { input : { $range : [0, anyOf.Length] }, as : 'anyOfIndex', in : <indexOfChar> } }
                    ast = new AstMapExpression(
                        input: new AstRangeExpression(0, anyOf.Length),
                        @as: "anyOfIndex",
                        @in: ast);

                    // minResult => { $min : { $filter : { input : <computeIndexes>, as : 'result', cond : { $gte : ['$$result', 0] } } } }
                    ast = new AstUnaryExpression(
                        AstUnaryOperator.Min,
                        new AstFilterExpression(
                            input: ast,
                            cond: new AstBinaryExpression(AstBinaryOperator.Gte, new AstFieldExpression("$result"), 0),
                            @as: "result"));

                    // topLevel => { $cond : [{ $eq : ['$$string', null] }, null, { $ifNull : [<minResult>, -1] }] }
                    ast = new AstCondExpression(
                        @if: new AstBinaryExpression(AstBinaryOperator.Eq, stringAst, BsonNull.Value),
                        then: BsonNull.Value,
                        @else: new AstBinaryExpression(AstBinaryOperator.IfNull, ast, -1));

                    // computeEnd => { $let : { vars : { end : { $add : ['$$startIndex', '$$count'] } }, in : <topLevel> } }
                    if (endVar != null)
                    {
                        ast = new AstLetExpression(
                            vars: new[] { endVar },
                            @in: ast);
                    }

                    // s.IndexOfAny(anyOf, startIndex, count) => { $let : { vars : { string : <s>, startIndex : <startIndex>, count : <count> }, in : <computeEnd> } }
                    var vars = new List<AstComputedField>();
                    vars.Add(stringVar);
                    if (startIndexVar != null)
                    {
                        vars.Add(startIndexVar);
                    }
                    if (countVar != null)
                    {
                        vars.Add(countVar);
                    }
                    ast = new AstLetExpression(vars, @in: ast);
                }

                return new AggregationExpression(expression, ast, new Int32Serializer());
            }

            throw new ExpressionNotSupportedException(expression);

            (AstComputedField, AstExpression) TranslateObject(Expression objectExpression)
            {
                var stringTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var stringVar = new AstComputedField("string", stringTranslation.Ast);
                var stringAst = new AstFieldExpression("$string");
                return (stringVar, stringAst);
            }

            string TranslateAnyOf(ReadOnlyCollection<Expression> arguments)
            {
                var anyOfExpression = arguments[0];
                if (anyOfExpression is ConstantExpression anyOfConstantExpression)
                {
                    var anyOfChars = (char[])anyOfConstantExpression.Value;
                    return new string(anyOfChars);
                }

                throw new ExpressionNotSupportedException(expression);
            }

            (AstComputedField, AstExpression) TranslateStartIndex(ReadOnlyCollection<Expression> arguments)
            {
                if (arguments.Count < 2)
                {
                    return (null, null);
                }

                var startIndexExpression = arguments[1];
                var startIndexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, startIndexExpression);
                var startIndexAst = startIndexTranslation.Ast;

                if (startIndexAst.NodeType == AstNodeType.ConstantExpression)
                {
                    return (null, startIndexAst);
                }
                else
                {
                    var startIndexVar = new AstComputedField("startIndex", startIndexAst);
                    startIndexAst = new AstFieldExpression("$startIndex");
                    return (startIndexVar, startIndexAst);
                }
            }

            (AstComputedField, AstExpression) TranslateCount(ReadOnlyCollection<Expression> arguments)
            {
                if (arguments.Count < 3)
                {
                    return (null, null);
                }

                var countExpression = arguments[2];
                var countTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, countExpression);
                var countAst = countTranslation.Ast;

                if (countAst.NodeType == AstNodeType.ConstantExpression)
                {
                    return (null, countAst);
                }
                else
                {
                    var countVar = new AstComputedField("count", countAst);
                    countAst = new AstFieldExpression("$count");
                    return (countVar, countAst);
                }
            }

            (AstComputedField, AstExpression) ComputeEnd(AstExpression startIndexAst, AstExpression countAst)
            {
                if (countAst == null)
                {
                    return (null, null);
                }

                if (startIndexAst is AstConstantExpression startIndexConstantExpression &&
                    countAst is AstConstantExpression countConstantExpression)
                {
                    var startIndex = startIndexConstantExpression.Value.AsInt32;
                    var count = countConstantExpression.Value.AsInt32;
                    var endAst = new AstConstantExpression(startIndex + count);
                    return (null, endAst);
                }
                else
                {
                    var endVar = new AstComputedField("end", new AstNaryExpression(AstNaryOperator.Add, startIndexAst, countAst));
                    var endAst = new AstFieldExpression("$end");
                    return (endVar, endAst);
                }
            }
        }
    }
}
