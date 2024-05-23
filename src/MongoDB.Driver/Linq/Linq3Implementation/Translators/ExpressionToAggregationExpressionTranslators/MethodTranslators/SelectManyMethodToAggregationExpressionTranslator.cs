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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class SelectManyMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __selectManyMethods =
        {
            EnumerableMethod.SelectMany,
            QueryableMethod.SelectMany
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__selectManyMethods))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);
                var selectorLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var selectorParameter = selectorLambda.Parameters[0];
                var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var selectorParameterSymbol = context.CreateSymbol(selectorParameter, selectorParameterSerializer);
                var selectorContext = context.WithSymbol(selectorParameterSymbol);
                var selectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(selectorContext, selectorLambda.Body);
                var asVar = selectorParameterSymbol.Var;
                var valueVar = AstExpression.Var("value");
                var thisVar = AstExpression.Var("this");
                var ast = AstExpression.Reduce(
                    input: AstExpression.Map(
                        input: sourceTranslation.Ast,
                        @as: asVar,
                        @in: selectorTranslation.Ast),
                    initialValue: new BsonArray(),
                    @in: AstExpression.ConcatArrays(valueVar, thisVar));
                return new AggregationExpression(expression, ast, selectorTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
