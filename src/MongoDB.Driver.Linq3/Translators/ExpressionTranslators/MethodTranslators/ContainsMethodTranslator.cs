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
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class ContainsMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsContainsMethod(expression, out var sourceExpression, out var valueExpression))
            {
                var sourceTranslation = ExpressionTranslator.Translate(context, sourceExpression);
                var valueTranslation = ExpressionTranslator.Translate(context, valueExpression);
                var ast = new AstBinaryExpression(AstBinaryOperator.In, valueTranslation.Ast, sourceTranslation.Ast);

                return new ExpressionTranslation(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsContainsMethod(MethodCallExpression expression, out Expression sourceExpression, out Expression valueExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            sourceExpression = null;
            valueExpression = null;

            if (method.Is(EnumerableMethod.Contains))
            {
                sourceExpression = arguments[0];
                valueExpression = arguments[1];
                return true;
            }

            if (!method.IsStatic && method.ReturnType == typeof(bool) && arguments.Count == 1)
            {
                sourceExpression = expression.Object;
                valueExpression = arguments[0];

                var ienumerableInterface = sourceExpression.Type.GetIEnumerableGenericInterface();
                var itemType = ienumerableInterface.GetGenericArguments()[0];
                return itemType == valueExpression.Type;
            }

            return false;
        }
    }
}
