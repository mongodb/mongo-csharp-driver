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
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodCallTranslators
{
    public static class StandardDeviationTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            if (IsStandardDeviationMethod(method, out var @operator))
            {
                var arguments = expression.Arguments;
                if (arguments.Count == 1 || arguments.Count == 2)
                {
                    var source = arguments[0];
                    var translatedSource = ExpressionTranslator.Translate(context, source);

                    if (arguments.Count == 2)
                    {
                        var selector = (LambdaExpression)arguments[1];
                        var parameter = selector.Parameters[0];
                        var selectMethod = EnumerableMethod.MakeSelect(parameter.Type, selector.ReturnType);
                        var selectExpression = Expression.Call(selectMethod, source, selector);
                        translatedSource = ExpressionTranslator.Translate(context, selectExpression);
                    }

                    var translation = new AstUnaryExpression(@operator, translatedSource.Translation);
                    var serializer = BsonSerializer.LookupSerializer(expression.Type);
                    return new TranslatedExpression(expression, translation, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsStandardDeviationMethod(MethodInfo methodInfo, out AstUnaryOperator @operator)
        {
            @operator = default;

            if (methodInfo.DeclaringType == typeof(EnumerableExtensions))
            {
                switch (methodInfo.Name)
                {
                    case "StandardDeviationPopulation": @operator = AstUnaryOperator.StdDevPop; return true;
                    case "StandardDeviationSample": @operator = AstUnaryOperator.StdDevSamp; return true;
                }
            }

            return false;
        }
    }
}
