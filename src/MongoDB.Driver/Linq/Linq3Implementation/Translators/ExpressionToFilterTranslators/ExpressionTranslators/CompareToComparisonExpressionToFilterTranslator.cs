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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class CompareToComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression)
        {
            return
                leftExpression is MethodCallExpression leftMethodCallExpression &&
                IsCompareToMethod(leftMethodCallExpression.Method);
        }

        // caller is responsible for ensuring constant is on the right
        public static AstFilter Translate(
            TranslationContext context,
            Expression expression,
            Expression leftExpression,
            AstComparisonFilterOperator comparisonOperator,
            Expression rightExpression)
        {
            if (leftExpression is MethodCallExpression leftMethodCallExpression &&
                IsCompareToMethod(leftMethodCallExpression.Method))
            {
                var fieldExpression = leftMethodCallExpression.Object;
                var field = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                var valueExpression = leftMethodCallExpression.Arguments[0];
                var value = valueExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedValue = SerializationHelper.SerializeValue(field.Serializer, value);

                var rightValue = rightExpression.GetConstantValue<int>(containingExpression: expression);
                if (rightValue == 0)
                {
                    return AstFilter.Compare(field, comparisonOperator, serializedValue);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsCompareToMethod(MethodInfo method)
        {
            ParameterInfo[] parameters;
            return
                method.IsPublic == true &&
                method.IsStatic == false &&
                method.ReturnType == typeof(int) &&
                method.Name == "CompareTo" &&
                (parameters = method.GetParameters()).Length == 1 &&
                (parameters[0].ParameterType == typeof(object) || parameters[0].ParameterType == method.DeclaringType);
        }
    }
}
