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
    internal static class AnyMethodWithConstantArrayAndFieldEqualsParameterInPredicateToFilterTranslator
    {
        public static bool CanTranslate(
            MethodCallExpression expression,
            out ConstantExpression arrayConstantExpression,
            out Expression fieldExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var sourceExpression = arguments[0];
                if (sourceExpression is ConstantExpression constantSourceExpression)
                {
                    arrayConstantExpression = constantSourceExpression;

                    var predicateLambda = (LambdaExpression)arguments[1];
                    var predicateParameter = predicateLambda.Parameters[0];
                    var predicateBody = predicateLambda.Body;

                    if (predicateBody is BinaryExpression predicateBinaryExpression &&
                        predicateBinaryExpression.NodeType == ExpressionType.Equal)
                    {
                        if (predicateBinaryExpression.Right == predicateParameter)
                        {
                            fieldExpression = predicateBinaryExpression.Left;
                            return true;
                        }

                        if (predicateBinaryExpression.Left == predicateParameter)
                        {
                            fieldExpression = predicateBinaryExpression.Right;
                            return true;
                        }
                    }
                }
            }

            arrayConstantExpression = null;
            fieldExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, ConstantExpression arrayConstantExpression, Expression fieldExpression)
        {
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var fieldSerializer = fieldTranslation.Serializer;

            var arrayValue = (IEnumerable)arrayConstantExpression.Value;
            var serializedArrayValue = SerializationHelper.SerializeValues(itemSerializer: fieldSerializer, arrayValue);

            return AstFilter.In(fieldTranslation, serializedArrayValue);
        }
    }
}
