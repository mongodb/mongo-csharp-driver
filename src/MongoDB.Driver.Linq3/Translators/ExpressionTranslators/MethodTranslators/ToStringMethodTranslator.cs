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
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class ToStringMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (TryTranslateInstanceToStringWithNoArguments(context, expression, out var translation) ||
                TryTranslateDateTimeToStringWithFormat(context, expression, out translation))
            {
                return translation;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool TryTranslateDateTimeToStringWithFormat(TranslationContext context, MethodCallExpression expression, out ExpressionTranslation translation)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            translation = null;

            if (method.DeclaringType != typeof(DateTime) || method.IsStatic || method.ReturnType != typeof(string) || method.Name != "ToString" || arguments.Count != 1 || arguments[0].Type != typeof(string))
            {
                return false;
            }

            var dateTimeExpression = expression.Object;
            var formatExpression = arguments[0];

            var dateTimeTranslation = ExpressionTranslator.Translate(context, dateTimeExpression);
            var formatTranslation = ExpressionTranslator.Translate(context, formatExpression);
            var ast = new AstDateToStringExpression(dateTimeTranslation.Ast, formatTranslation.Ast);

            translation = new ExpressionTranslation(expression, ast, new StringSerializer());
            return true;
        }

        private static bool TryTranslateInstanceToStringWithNoArguments(TranslationContext context, MethodCallExpression expression, out ExpressionTranslation translation)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            translation = null;

            if (method.IsStatic || method.ReturnType != typeof(string) || method.Name != "ToString" || arguments.Count != 0)
            {
                return false;
            }

            var objectExpression = expression.Object;

            var objectTranslation = ExpressionTranslator.Translate(context, objectExpression);
            var ast = new AstUnaryExpression(AstUnaryOperator.ToString, objectTranslation.Ast);

            translation = new ExpressionTranslation(expression, ast, new StringSerializer());
            return true;
        }
    }
}
