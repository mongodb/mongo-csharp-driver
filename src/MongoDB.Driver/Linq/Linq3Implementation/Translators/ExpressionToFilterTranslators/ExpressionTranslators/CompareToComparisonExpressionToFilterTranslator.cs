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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class CompareToComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression)
        {
            return
                leftExpression is MethodCallExpression leftMethodCallExpression &&
                leftMethodCallExpression.Method is var method &&
                (IComparableMethod.IsCompareToMethod(method) || IsStaticCompareMethod(method));
        }

        // caller is responsible for ensuring constant is on the right
        public static AstFilter Translate(
            TranslationContext context,
            Expression expression,
            Expression leftExpression,
            AstComparisonFilterOperator comparisonOperator,
            Expression rightExpression)
        {
            if (CanTranslate(leftExpression))
            {
                var leftMethodCallExpression = (MethodCallExpression)leftExpression;
                var method= leftMethodCallExpression.Method;
                var arguments = leftMethodCallExpression.Arguments;

                Expression fieldExpression;
                Expression valueExpression;
                if (method.IsStatic)
                {
                    fieldExpression = arguments[0];
                    valueExpression = arguments[1];
                }
                else
                {
                    fieldExpression = leftMethodCallExpression.Object;
                    valueExpression = arguments[0];
                }

                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                var value = valueExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedValue = SerializationHelper.SerializeValue(fieldTranslation.Serializer, value);

                var rightValue = rightExpression.GetConstantValue<int>(containingExpression: expression);
                return (comparisonOperator, rightValue) switch
                {
                    (AstComparisonFilterOperator.Eq, -1) => AstFilter.Lt(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Ne, -1) => AstFilter.Gte(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Gt, -1) => AstFilter.Gte(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Eq, 0) => AstFilter.Eq(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Ne, 0) => AstFilter.Ne(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Lt, 0) => AstFilter.Lt(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Lte, 0) => AstFilter.Lte(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Gt, 0) => AstFilter.Gt(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Gte, 0) => AstFilter.Gte(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Eq, 1) => AstFilter.Gt(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Ne, 1) => AstFilter.Lte(fieldTranslation.Ast, serializedValue),
                    (AstComparisonFilterOperator.Lt, 1) => AstFilter.Lte(fieldTranslation.Ast, serializedValue),
                    _ => throw new ExpressionNotSupportedException(expression)
                };
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsStaticCompareMethod(MethodInfo method)
        {
            return
                method.IsStatic &&
                method.IsPublic &&
                method.ReturnType == typeof(int) &&
                method.GetParameters() is var parameters &&
                parameters.Length == 2 &&
                parameters[0].ParameterType == method.DeclaringType &&
                parameters[1].ParameterType == parameters[0].ParameterType;
        }
    }
}
