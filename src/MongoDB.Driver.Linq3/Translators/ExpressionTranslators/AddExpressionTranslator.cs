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
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class AddExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, BinaryExpression expression)
        {
            switch (expression.Type.FullName)
            {
                case "System.Decimal":
                case "System.Double":
                case "System.Int32":
                case "System.Int64":
                case "System.Single":
                    return TranslateNumericAddition(context, expression);

                case "System.String":
                    return TranslateStringConcatenation(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
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

        private static ExpressionTranslation TranslateNumericAddition(TranslationContext context, BinaryExpression expression)
        {
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            var serverType = GetServerType(leftExpression.Type, rightExpression.Type);
            leftExpression = ConvertHelper.RemoveUnnecessaryConvert(leftExpression, impliedType: serverType);
            rightExpression = ConvertHelper.RemoveUnnecessaryConvert(rightExpression, impliedType: serverType);
            var leftTranslation = ExpressionTranslator.Translate(context, leftExpression);
            var rightTranslation = ExpressionTranslator.Translate(context, rightExpression);
            var ast = (AstExpression)new AstNaryExpression(AstNaryOperator.Add, leftTranslation.Ast, rightTranslation.Ast);
            var serializer = BsonSerializer.LookupSerializer(expression.Type);

            return new ExpressionTranslation(expression, ast, serializer);
        }

        private static ExpressionTranslation TranslateStringConcatenation(TranslationContext context, BinaryExpression expression)
        {
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            var leftTranslation = ExpressionTranslator.Translate(context, leftExpression);
            var rightTranslation = ExpressionTranslator.Translate(context, rightExpression);
            AstExpression ast;
            if (leftTranslation.Ast is AstNaryExpression naryExpression && naryExpression.Operator == AstNaryOperator.Concat)
            {
                var args = new List<AstExpression>();
                args.AddRange(naryExpression.Args);
                args.Add(rightTranslation.Ast);
                ast = new AstNaryExpression(AstNaryOperator.Concat, args);
            }
            else
            {
                ast = new AstNaryExpression(AstNaryOperator.Concat, leftTranslation.Ast, rightTranslation.Ast);
            }

            return new ExpressionTranslation(expression, ast, new StringSerializer());
        }
    }
}
