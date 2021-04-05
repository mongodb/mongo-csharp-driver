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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class FirstOrLastMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableMethod.First, EnumerableMethod.FirstWithPredicate, EnumerableMethod.Last, EnumerableMethod.LastWithPredicate))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);

                var array = sourceTranslation.Ast;
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);

                if (method.IsOneOf(EnumerableMethod.FirstWithPredicate, EnumerableMethod.LastWithPredicate))
                {
                    var predicateLambda = (LambdaExpression)arguments[1];
                    var predicateTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, predicateLambda, itemSerializer);
                    var predicateParameter = predicateLambda.Parameters[0];
                    array = AstExpression.Filter(
                        input: array,
                        cond: predicateTranslation.Ast,
                        @as: predicateParameter.Name);
                }

                var index = method.Name == "First" ? 0 : -1;
                var ast = AstExpression.ArrayElemAt(array, index);

                return new AggregationExpression(expression, ast, itemSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
