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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class WhereFollowedByAnyMethodToFilterTranslator
    {
        public static bool CanTranslate(MethodCallExpression expression, out MethodCallExpression whereExpression, out MethodCallExpression anyExpression)
        {
            if (expression.Method.Is(EnumerableMethod.Any))
            {
                anyExpression = expression;
                var anySourceExpression = anyExpression.Arguments[0];
                if (anySourceExpression is MethodCallExpression anySourceMethodCallExpression &&
                    anySourceMethodCallExpression.Method.Is(EnumerableMethod.Where))
                {
                    whereExpression = anySourceMethodCallExpression;
                    return true;
                }
            }

            whereExpression = null;
            anyExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, Expression expression, MethodCallExpression whereExpression, MethodCallExpression anyExpression)
        {
            if (whereExpression.Method.Is(EnumerableMethod.Where) &&
                anyExpression.Method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var combinedAnyExpression = CreateCombinedAnyExpression(whereExpression, anyExpression);
                return AnyMethodToFilterTranslator.Translate(context, combinedAnyExpression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static MethodCallExpression CreateCombinedAnyExpression(MethodCallExpression whereExpression, MethodCallExpression anyExpression)
        {
            // transform s.Where(x => p1(x)).Any(y => p2(y)) to s.Any(x => p1(x) && p2(x))

            var whereSourceExpression = whereExpression.Arguments[0];
            var wherePredicateLambda = (LambdaExpression)whereExpression.Arguments[1];
            var wherePredicateParameter = wherePredicateLambda.Parameters.Single();
            var anyPredicateLambda = (LambdaExpression)anyExpression.Arguments[1];
            var anyPredicateParameter = anyPredicateLambda.Parameters.Single();
            var modifiedAnyBody = ExpressionReplacer.Replace(anyPredicateLambda.Body, anyPredicateParameter, wherePredicateParameter);

            return Expression.Call(
                EnumerableMethod.AnyWithPredicate.MakeGenericMethod(anyPredicateParameter.Type),
                whereSourceExpression,
                Expression.Lambda(
                    Expression.MakeBinary(
                        ExpressionType.AndAlso,
                        wherePredicateLambda.Body,
                        modifiedAnyBody),
                    wherePredicateParameter));
        }
    }
}
