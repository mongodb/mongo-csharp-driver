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

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class ToUpperMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsStringInstanceMethodWithNoArguments(expression.Method))
            {
                var sourceExpression = expression.Object;

                var sourceTranslation = ExpressionTranslator.Translate(context, sourceExpression);
                var ast = new AstUnaryExpression(AstUnaryOperator.ToUpper, sourceTranslation.Ast);

                return new ExpressionTranslation(expression, ast, new StringSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsStringInstanceMethodWithNoArguments(MethodInfo methodInfo)
        {
            return
                methodInfo.DeclaringType == typeof(string) &&
                !methodInfo.IsStatic &&
                methodInfo.ReturnParameter.ParameterType == typeof(string) &&
                methodInfo.GetParameters().Length == 0;
        }
    }
}
