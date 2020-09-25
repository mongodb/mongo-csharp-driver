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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class EqualsMethodTranslator
    {
        private static readonly IBsonSerializer<bool> __boolSerializer = new BooleanSerializer();

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsStringEqualsMethod(expression.Method))
            {
                return TranslateStringEqualsMethod(context, expression);
            }

            if (IsInstanceEqualsMethod(expression.Method))
            {
                var lhs = expression.Object;
                var rhs = expression.Arguments[0];
                var translatedLhs = ExpressionTranslator.Translate(context, lhs);
                var translatedRhs = ExpressionTranslator.Translate(context, rhs);

                var translation = new AstBinaryExpression(AstBinaryOperator.Eq, translatedLhs.Translation, translatedRhs.Translation);
                return new TranslatedExpression(expression, translation, __boolSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsInstanceEqualsMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return
                !method.IsStatic &&
                method.ReturnParameter.ParameterType == typeof(bool) &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == method.DeclaringType;
        }

        private static bool IsStringEqualsMethod(MethodInfo method)
        {
            return method.DeclaringType == typeof(string);
        }

        private static TranslatedExpression TranslateStringEqualsMethod(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            Expression a;
            Expression b;
            Expression comparisonType = null;
            if (method.IsStatic)
            {
                a = arguments[0];
                b = arguments[1];
                if (arguments.Count == 3)
                {
                    comparisonType = arguments[2];
                }
            }
            else
            {
                a = expression.Object;
                b = arguments[0];
                if (arguments.Count == 2)
                {
                    comparisonType = arguments[1];
                }
            }

            var translatedA = ExpressionTranslator.Translate(context, a);
            var translatedB = ExpressionTranslator.Translate(context, b);

            StringComparison comparisonTypeValue = StringComparison.Ordinal;
            if (comparisonType != null)
            {
                var constantExpression = comparisonType as ConstantExpression;
                if (constantExpression == null)
                {
                    goto notSupported;
                }

                comparisonTypeValue = (StringComparison)constantExpression.Value;
            }

            AstExpression translation;
            switch (comparisonTypeValue)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.Ordinal:
                    translation = new AstBinaryExpression(AstBinaryOperator.Eq, translatedA.Translation, translatedB.Translation);
                    break;

                case StringComparison.CurrentCultureIgnoreCase:
                case StringComparison.OrdinalIgnoreCase:
                    translation = new AstBinaryExpression(
                        AstBinaryOperator.Eq,
                        new AstBinaryExpression(AstBinaryOperator.StrCaseCmp, translatedA.Translation, translatedB.Translation),
                        0);
                    break;

                default:
                    goto notSupported;
            }

            var serializer = new BooleanSerializer();
            return new TranslatedExpression(expression, translation, serializer);

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
