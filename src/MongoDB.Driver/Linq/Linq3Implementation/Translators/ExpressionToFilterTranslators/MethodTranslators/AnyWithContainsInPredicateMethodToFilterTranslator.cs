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

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class AnyWithContainsInPredicateMethodToFilterTranslator
    {
        public static bool CanTranslate(MethodCallExpression expression, out Expression arrayFieldExpression, out ConstantExpression arrayConstantExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var outerSourceExpression = arguments[0];
                var predicateLambda = (LambdaExpression)arguments[1];
                var predicateParameter = predicateLambda.Parameters[0];

                if (IsContainsParameterExpression(predicateLambda.Body, predicateParameter, out var innerSourceExpression))
                {
                    // f.Any(i => a.Contains(i)) where f is an array field and a is an array constant
                    if (innerSourceExpression is ConstantExpression innerArrayConstantExpression)
                    {
                        arrayFieldExpression = outerSourceExpression;
                        arrayConstantExpression = innerArrayConstantExpression;
                        return true;
                    }

                    // a.Any(i => f.Contains(i)) where f is an array field and a is an array constant
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
            return AstFilter.In(arrayFieldTranslation, serializedValues);
        }

        private static bool IsContainsParameterExpression(Expression predicateBody, ParameterExpression predicateParameter, out Expression innerSourceExpression)
        {
            if (predicateBody is MethodCallExpression methodCallExpression &&
                EnumerableMethod.IsContainsMethod(methodCallExpression, out innerSourceExpression, out var valueExpression) &&
                valueExpression == predicateParameter)
            {
                return true;
            }

            innerSourceExpression = null;
            return false;
        }
    }
}
