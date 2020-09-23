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

            bool IsInstanceEqualsMethod(MethodInfo method)
            {
                var parameters = method.GetParameters();
                return
                    !method.IsStatic &&
                    method.ReturnParameter.ParameterType == typeof(bool) &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == method.DeclaringType;
            }
        }
    }
}
