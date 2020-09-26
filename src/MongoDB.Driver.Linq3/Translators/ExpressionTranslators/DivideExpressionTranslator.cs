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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class DivideExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, BinaryExpression expression)
        {
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            var serverType = GetServerType(leftExpression.Type, rightExpression.Type);
            leftExpression = ConvertHelper.RemoveUnnecessaryConvert(leftExpression, impliedType: serverType);
            rightExpression = ConvertHelper.RemoveUnnecessaryConvert(rightExpression, impliedType: serverType);
            var leftTranslation = ExpressionTranslator.Translate(context, leftExpression);
            var rightTranslation = ExpressionTranslator.Translate(context, rightExpression);
            var ast = (AstExpression)new AstBinaryExpression(AstBinaryOperator.Divide, leftTranslation.Ast, rightTranslation.Ast);
            if (expression.Type != serverType)
            {
                ast = new AstConvertExpression(ast, expression.Type);
            }
            var serializer = BsonSerializer.LookupSerializer(expression.Type);

            return new ExpressionTranslation(expression, ast, serializer);
        }

        private static Type GetServerType(Type arg1Type, Type arg2Type)
        {
            if (arg1Type == typeof(decimal) || arg2Type == typeof(decimal))
            {
                return typeof(decimal);
            }
            else
            {
                return typeof(double);
            }
        }
    }
}
