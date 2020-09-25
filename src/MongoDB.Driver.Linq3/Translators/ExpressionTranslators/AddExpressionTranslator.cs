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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class AddExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            if (expression.Type == typeof(string))
            {
                return TranslateStringConcatenation(context, expression);
            }

            var arg1 = expression.Left;
            var arg2 = expression.Right;

            var serverType = GetServerType(arg1.Type, arg2.Type);
            arg1 = RemoveUnnecessaryConvert(arg1, serverType);
            arg2 = RemoveUnnecessaryConvert(arg2, serverType);

            var translatedArg1 = ExpressionTranslator.Translate(context, arg1);
            var translatedArg2 = ExpressionTranslator.Translate(context, arg2);
            var translation = (AstExpression)new AstNaryExpression(AstNaryOperator.Add, translatedArg1.Translation, translatedArg2.Translation);

            if (expression.Type != serverType)
            {
                var to = GetTo(expression.Type);
                translation = new AstConvertExpression(translation, to);
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

        private static string GetTo(Type type)
        {
            switch (type.FullName)
            {
                case "System.Decimal": return "decimal";
                case "System.Double": return "double";
                case "System.Int32": return "int";
                case "System.Int64": return "long";
                default: throw new InvalidOperationException($"Unexpected type: {type.FullName}");
            }
        }

        private static Expression RemoveUnnecessaryConvert(Expression expression, Type serverType)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var unaryExpression = (UnaryExpression)expression;
                if (IsConvertUnnecessary(unaryExpression.Operand.Type, unaryExpression.Type))
                {
                    return unaryExpression.Operand;
                }
            }

            return expression;

            bool IsConvertUnnecessary(Type from, Type to)
            {
                return
                    to == serverType ||
                    (from == typeof(int) && to == typeof(long));
            }
        }

        private static TranslatedExpression TranslateStringConcatenation(TranslationContext context, BinaryExpression expression)
        {
            var translatedLeft = ExpressionTranslator.Translate(context, expression.Left);
            var translatedRight = ExpressionTranslator.Translate(context, expression.Right);

            AstExpression translation;
            if (translatedLeft.Translation is AstNaryExpression naryExpression && naryExpression.Operator == AstNaryOperator.Concat)
            {
                var args = new List<AstExpression>();
                args.AddRange(naryExpression.Args);
                args.Add(translatedRight.Translation);
                translation = new AstNaryExpression(AstNaryOperator.Concat, args);
            }
            else
            {
                translation = new AstNaryExpression(AstNaryOperator.Concat, translatedLeft.Translation, translatedRight.Translation);
            }

            var serializer = new StringSerializer(); // TODO: find correct serializer

            return new TranslatedExpression(expression, translation, serializer);
        }
    }
}
