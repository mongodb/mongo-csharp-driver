﻿/* Copyright 2010-present MongoDB Inc.
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
    public static class ToStringMethodTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsInstanceToStringMethodWithNoArguments(expression.Method))
            {
                var source = expression.Object;
                var translatedSource = ExpressionTranslator.Translate(context, source);

                var translation = new AstUnaryExpression(AstUnaryOperator.ToString, translatedSource.Translation);
                var stringSerializer = new StringSerializer();
                return new TranslatedExpression(expression, translation, stringSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsInstanceToStringMethodWithNoArguments(MethodInfo methodInfo)
        {
            return
                !methodInfo.IsStatic &&
                methodInfo.ReturnParameter.ParameterType == typeof(string) &&
                methodInfo.GetParameters().Length == 0;
        }
    }
}
