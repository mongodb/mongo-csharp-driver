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

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class DivideExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            var arg1 = expression.Left;
            var arg2 = expression.Right;

            var serverType = GetServerType(arg1.Type, arg2.Type);
            arg1 = RemoveImpliedConversion(arg1, serverType);
            arg2 = RemoveImpliedConversion(arg2, serverType);

            var translatedArg1 = ExpressionTranslator.Translate(context, arg1);
            var translatedArg2 = ExpressionTranslator.Translate(context, arg2);
            var translation = (AstExpression)new AstBinaryExpression(AstBinaryOperator.Divide, translatedArg1.Translation, translatedArg2.Translation);

            if (expression.Type != serverType)
            {
                var toExpressionType = GetTo(expression.Type);
                translation = new AstConvertExpression(translation, toExpressionType);
            }

            var serializer = BsonSerializer.LookupSerializer(expression.Type);
            return new TranslatedExpression(expression, translation, serializer);
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

        private static AstExpression GetTo(Type type)
        {
            string to;
            switch (type.FullName)
            {
                case "System.Decimal": to = "decimal"; break;
                case "System.Double": to = "double"; break;
                case "System.Int32": to = "int"; break;
                case "System.Int64": to = "long"; break;
                default: throw new InvalidOperationException($"Unexpected type: {type.FullName}");
            }

            return new AstConstantExpression(to);
        }

        private static Expression RemoveImpliedConversion(Expression expression, Type serverType)
        {
            if (expression.NodeType == ExpressionType.Convert && expression.Type == serverType)
            {
                return ((UnaryExpression)expression).Operand;
            }
            else
            {
                return expression;
            }
        }
    }
}
