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
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class AnyMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (IsArrayFieldInArrayConstantExpression(context, expression, out var arrayFieldExpression, out var arrayConstantExpression))
            {
                return CreateArrayFieldInArrayConstantFilter(context, arrayFieldExpression, arrayConstantExpression);
            }

            var sourceExpression = arguments[0];
            var sourceField = ExpressionToFilterFieldTranslator.Translate(context, sourceExpression);
            var elementSerializer = ArraySerializerHelper.GetItemSerializer(sourceField.Serializer);

            if (method.Is(EnumerableMethod.Any))
            {
                throw new ExpressionNotSupportedException(expression); // TODO
            }

            if (method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var predicateLambda = (LambdaExpression)arguments[1];
                var parameterExpression = predicateLambda.Parameters[0];
                var parameterSymbol = new Symbol("$elem", elementSerializer);
                var predicateContext = context.WithSymbol(parameterExpression, parameterSymbol);
                var predicateFilter = ExpressionToFilterTranslator.Translate(predicateContext, predicateLambda.Body);
                return new AstElemMatchFilter(sourceField, predicateFilter);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsArrayFieldInArrayConstantExpression(TranslationContext context, MethodCallExpression expression, out Expression arrayFieldExpression, out ConstantExpression arrayConstantExpression)
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
                    // f.Any(i => a.Contains(i)) where f is an array field and as is an array constant
                    if (innerSourceExpression is ConstantExpression innerArrayConstantExpression)
                    {
                        arrayFieldExpression = outerSourceExpression;
                        arrayConstantExpression = innerArrayConstantExpression;
                        return true;
                    }

                    // a.Any(i => f.Contains(i)) where f is an array field and as is an array constant
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

        private static AstFilter CreateArrayFieldInArrayConstantFilter(TranslationContext context, Expression arrayFieldExpression, ConstantExpression arrayConstantExpression)
        {
            var arrayFieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, arrayFieldExpression);
            var itemSerializer = ArraySerializerHelper.GetItemSerializer(arrayFieldTranslation.Serializer);
            var values = (IEnumerable)arrayConstantExpression.Value;
            var serializedValues = SerializationHelper.SerializeValues(itemSerializer, values);
            return new AstInFilter(arrayFieldTranslation, serializedValues);
        }
    }
}
