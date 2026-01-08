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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class WhereMethodToAggregationExpressionTranslator
    {
        private static readonly IReadOnlyMethodInfoSet __whereOverloads = MethodInfoSet.Create(
        [
            EnumerableMethod.Where,
            MongoEnumerableMethod.WhereWithLimit,
            QueryableMethod.Where
        ]);

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__whereOverloads))
            {
                var sourceExpression = arguments[0];
                var sourceTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, sourceExpression);
                NestedAsQueryableHelper.EnsureQueryableMethodHasNestedAsQueryableSource(expression, sourceTranslation);

                var sourceAst = sourceTranslation.Ast;
                var sourceSerializer = sourceTranslation.Serializer;
                if (sourceSerializer is IWrappedValueSerializer wrappedValueSerializer)
                {
                    sourceAst = AstExpression.GetField(sourceAst, wrappedValueSerializer.FieldName);
                    sourceSerializer = wrappedValueSerializer.ValueSerializer;
                }
                var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceSerializer);

                var predicateLambda = ExpressionHelper.UnquoteLambdaIfQueryableMethod(method, arguments[1]);
                var predicateParameter = predicateLambda.Parameters[0];
                var predicateSymbol = context.CreateSymbol(predicateParameter, itemSerializer);
                var predicateContext = context.WithSymbol(predicateSymbol);
                var predicateTranslation = ExpressionToAggregationExpressionTranslator.Translate(predicateContext, predicateLambda.Body);

                TranslatedExpression limitTranslation = null;
                if (method.Is(MongoEnumerableMethod.WhereWithLimit))
                {
                    var limitExpression = arguments[2];
                    limitTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, limitExpression);
                }

                var ast = AstExpression.Filter(
                    sourceAst,
                    predicateTranslation.Ast,
                    @as: predicateSymbol.Var.Name,
                    limitTranslation?.Ast);

                var resultSerializer = context.NodeSerializers.GetSerializer(expression);
                return new TranslatedExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
