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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class CompareComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression)
        {
            return
                leftExpression is MethodCallExpression leftMethodCallExpression &&
                leftMethodCallExpression.Method is var method &&
                (method.IsStaticCompareMethod() || method.IsInstanceCompareToMethod() || method.Is(StringMethod.CompareWithIgnoreCase));
        }

        // caller is responsible for ensuring constant is on the right
        public static AstFilter Translate(
            TranslationContext context,
            Expression expression,
            Expression leftExpression,
            AstComparisonFilterOperator outerComparisonOperator,
            Expression rightExpression)
        {
            if (CanTranslate(leftExpression))
            {
                var compareMethodCallExpression = (MethodCallExpression)leftExpression;
                var compareMethod = compareMethodCallExpression.Method;
                var compareArguments = compareMethodCallExpression.Arguments;
                var outerValue = rightExpression.GetConstantValue<int>(containingExpression: expression);

                Expression fieldExpression;
                Expression innerValueExpression;
                if (compareMethod.IsStatic)
                {
                    fieldExpression = compareArguments[0];
                    innerValueExpression = compareArguments[1];
                }
                else
                {
                    fieldExpression = compareMethodCallExpression.Object;
                    innerValueExpression = compareArguments[0];
                }

                if (compareMethod.Is(StringMethod.CompareWithIgnoreCase))
                {
                    var ignoreCaseExpression = compareArguments[2];
                    var ignoreCase = ignoreCaseExpression.GetConstantValue<bool>(containingExpression: compareMethodCallExpression);
                    if (ignoreCase)
                    {
                        throw new ExpressionNotSupportedException(compareMethodCallExpression, because: "ignoreCase must be false");
                    }
                }

                var fieldComparisonOperator = (outerComparisonOperator, outerValue) switch
                {
                    (AstComparisonFilterOperator.Eq, -1) => AstComparisonFilterOperator.Lt,
                    (AstComparisonFilterOperator.Ne, -1) => AstComparisonFilterOperator.Gte,
                    (AstComparisonFilterOperator.Gt, -1) => AstComparisonFilterOperator.Gte,
                    (AstComparisonFilterOperator.Eq, 0) => AstComparisonFilterOperator.Eq,
                    (AstComparisonFilterOperator.Ne, 0) => AstComparisonFilterOperator.Ne,
                    (AstComparisonFilterOperator.Lt, 0) => AstComparisonFilterOperator.Lt,
                    (AstComparisonFilterOperator.Lte, 0) => AstComparisonFilterOperator.Lte,
                    (AstComparisonFilterOperator.Gt, 0) => AstComparisonFilterOperator.Gt,
                    (AstComparisonFilterOperator.Gte, 0) => AstComparisonFilterOperator.Gte,
                    (AstComparisonFilterOperator.Eq, 1) => AstComparisonFilterOperator.Gt,
                    (AstComparisonFilterOperator.Ne, 1) => AstComparisonFilterOperator.Lte,
                    (AstComparisonFilterOperator.Lt, 1) => AstComparisonFilterOperator.Lte,
                    _ => throw new ExpressionNotSupportedException(expression)
                };

                if (fieldExpression.NodeType == ExpressionType.Constant && innerValueExpression.NodeType != ExpressionType.Constant)
                {
                    (fieldExpression, innerValueExpression) = (innerValueExpression, fieldExpression);
                    fieldComparisonOperator = fieldComparisonOperator.GetComparisonOperatorForSwappedLeftAndRight();
                }

                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
                var value = innerValueExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedValue = SerializationHelper.SerializeValue(fieldTranslation.Serializer, value);

                return AstFilter.Compare(fieldTranslation.Ast, fieldComparisonOperator, serializedValue);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
