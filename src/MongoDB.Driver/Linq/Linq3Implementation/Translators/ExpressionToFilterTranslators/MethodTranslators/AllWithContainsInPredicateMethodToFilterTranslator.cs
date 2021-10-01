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

using System.Collections;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class AllWithContainsInPredicateMethodToFilterTranslator
    {
        public static bool CanTranslate(MethodCallExpression expression, out Expression arrayFieldExpression, out ConstantExpression arrayConstantExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.All))
            {
                var outerSourceExpression = arguments[0];
                var predicateLambda = (LambdaExpression)arguments[1];
                var predicateParameter = predicateLambda.Parameters[0];

                if (IsContainsParameterExpression(predicateLambda.Body, predicateParameter, out var innerSourceExpression))
                {
                    // a.All(i => f.Contains(i)) where f is an array field and a is an array constant
                    if (outerSourceExpression is ConstantExpression outerArrayConstantExpression)
                    {
                        arrayFieldExpression = innerSourceExpression;
                        arrayConstantExpression = outerArrayConstantExpression;
                        return true;
                    }
                }
            }

            arrayFieldExpression = null;
            arrayConstantExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, Expression arrayFieldExpression, ConstantExpression arrayConstantExpression)
        {
            var arrayFieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, arrayFieldExpression);
            var itemSerializer = ArraySerializerHelper.GetItemSerializer(arrayFieldTranslation.Serializer);
            var values = (IEnumerable)arrayConstantExpression.Value;
            var serializedValues = SerializationHelper.SerializeValues(itemSerializer, values);
            return AstFilter.All(arrayFieldTranslation, serializedValues);
        }

        private static bool IsContainsParameterExpression(Expression predicateBody, ParameterExpression predicateParameter, out Expression innerSourceExpression)
        {
            if (predicateBody is MethodCallExpression methodCallExpression &&
                methodCallExpression.Method.Is(EnumerableMethod.Contains) &&
                methodCallExpression.Arguments[1] == predicateParameter)
            {
                innerSourceExpression = methodCallExpression.Arguments[0];
                return true;
            }

            innerSourceExpression = null;
            return false;
        }
    }
}
