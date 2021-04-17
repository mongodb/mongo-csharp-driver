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
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class SubstringMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(StringMethod.Substring, StringMethod.SubstringWithLength))
            {
                var stringExpression = expression.Object;
                var startIndexExpression = arguments[0];
                var lengthExpression = arguments.Count == 2 ? arguments[1] : null;
                return TranslateHelper(context, expression, stringExpression, startIndexExpression, lengthExpression, AstTernaryOperator.SubstrCP);
            }

            if (method.Is(MongoDBLinqExtensionsMethod.SubstrBytes))
            {
                var stringExpression = arguments[0];
                var startIndexExpression = arguments[1];
                var lengthExpression = arguments[2];
                return TranslateHelper(context, expression, stringExpression, startIndexExpression, lengthExpression, AstTernaryOperator.SubstrBytes);
            }

            throw new ExpressionNotSupportedException(expression);

        }

        private static bool IsSimple(AstExpression expression)
        {
            return
                expression.NodeType == AstNodeType.ConstantExpression ||
                expression.NodeType == AstNodeType.FieldExpression;
        }

        private static AggregationExpression TranslateHelper(TranslationContext context, Expression expression, Expression stringExpression, Expression startIndexExpression, Expression lengthExpression, AstTernaryOperator substrOperator)
        {
            var stringTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, stringExpression);
            var startIndexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, startIndexExpression);

            AstExpression ast;
            if (lengthExpression == null)
            {
                var strlenOperator = substrOperator == AstTernaryOperator.SubstrCP ? AstUnaryOperator.StrLenCP : AstUnaryOperator.StrLenBytes;
                if (IsSimple(stringTranslation.Ast) && IsSimple(startIndexTranslation.Ast))
                {
                    var lengthAst = AstExpression.StrLen(strlenOperator, stringTranslation.Ast);
                    var countAst = AstExpression.Subtract(lengthAst, startIndexTranslation.Ast);
                    ast = AstExpression.Substr(substrOperator, stringTranslation.Ast, startIndexTranslation.Ast, countAst);
                }
                else
                {
                    var stringVar = AstExpression.Var("string", stringTranslation.Ast);
                    var indexVar = AstExpression.Var("index", startIndexTranslation.Ast);
                    var stringField = AstExpression.Field("$string");
                    var indexField = AstExpression.Field("$index");
                    var lengthAst = AstExpression.StrLen(strlenOperator, stringField);
                    var countAst = AstExpression.Subtract(lengthAst, indexField);
                    var inAst = AstExpression.Substr(substrOperator, stringField, indexField, countAst);
                    ast = AstExpression.Let(stringVar, indexVar, inAst);
                }
            }
            else
            {
                var lengthTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, lengthExpression);
                ast = AstExpression.Substr(substrOperator, stringTranslation.Ast, startIndexTranslation.Ast, lengthTranslation.Ast);
            }

            return new AggregationExpression(expression, ast, new StringSerializer());
        }
    }
}
