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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class RangeMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Range))
            {
                var startExpression = expression.Arguments[0];
                var countExpression = expression.Arguments[1];

                var startTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, startExpression);
                var countTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, countExpression);
                var (startVar, startSimpleAst) = AstExpression.UseVarIfNotSimple("start", startTranslation.Ast);
                var (countVar, countSimpleAst) = AstExpression.UseVarIfNotSimple("count", countTranslation.Ast);
                var ast = AstExpression.Let(
                    startVar,
                    countVar,
                    AstExpression.Range(startSimpleAst, end: AstExpression.Add(startSimpleAst, countSimpleAst)));
                var serializer = IEnumerableSerializer.Create(new Int32Serializer());

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsSimple(AggregationExpression translation)
        {
            var ast = translation.Ast;
            return ast.NodeType == AstNodeType.ConstantExpression || ast.NodeType == AstNodeType.FieldExpression;
        }
    }
}
