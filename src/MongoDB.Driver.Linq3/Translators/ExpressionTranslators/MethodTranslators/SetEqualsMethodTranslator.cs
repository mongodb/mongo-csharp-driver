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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class SetEqualsMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsSetEqualsMethod(expression, out var objectExpression, out var otherExpression))
            {
                var objectTranslation = ExpressionTranslator.Translate(context, objectExpression);
                var otherTranslation = ExpressionTranslator.Translate(context, otherExpression);
                var ast = new AstNaryExpression(AstNaryOperator.SetEquals, objectTranslation.Ast, otherTranslation.Ast);

                return new ExpressionTranslation(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsSetEqualsMethod(MethodCallExpression expression, out Expression objectExpression, out Expression otherExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (!method.IsStatic &&
                method.ReturnType == typeof(bool) &&
                method.Name == "SetEquals" &&
                arguments.Count == 1)
            {
                objectExpression = expression.Object;
                otherExpression = arguments[0];

                if (objectExpression.Type.TryGetIEnumerableGenericInterface(out var objectEnumerableInterface) &&
                    otherExpression.Type.TryGetIEnumerableGenericInterface(out var otherEnumerableInterface))
                {
                    var objectItemType = objectEnumerableInterface.GetGenericArguments()[0];
                    var otherItemType = otherEnumerableInterface.GetGenericArguments()[0];
                    if (objectItemType == otherItemType)
                    {
                        return true;
                    }
                }
            }

            objectExpression = null;
            otherExpression = null;
            return false;
        }
    }
}
