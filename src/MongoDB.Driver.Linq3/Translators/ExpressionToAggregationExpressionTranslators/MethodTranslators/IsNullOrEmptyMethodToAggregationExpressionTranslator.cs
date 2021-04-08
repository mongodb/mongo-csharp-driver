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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class IsNullOrEmptyMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(StringMethod.IsNullOrEmpty))
            {
                var stringExpression = arguments[0];

                var stringTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, stringExpression);
                AstExpression ast;
                if (IsSimple(stringTranslation.Ast))
                {
                    ast = AstExpression.Or(
                        AstExpression.Eq(stringTranslation.Ast, BsonNull.Value),
                        AstExpression.Eq(stringTranslation.Ast, ""));
                }
                else
                {
                    ast = AstExpression.Let(
                        var: AstExpression.ComputedField("this", stringTranslation.Ast),
                        @in: AstExpression.Or(
                            AstExpression.Eq(AstExpression.Field("$this"), BsonNull.Value),
                            AstExpression.Eq(AstExpression.Field("$this"), "")));
                }

                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsSimple(AstExpression expression)
        {
            return
                expression.NodeType == AstNodeType.ConstantExpression ||
                expression.NodeType == AstNodeType.FieldExpression;
        }
    }
}
