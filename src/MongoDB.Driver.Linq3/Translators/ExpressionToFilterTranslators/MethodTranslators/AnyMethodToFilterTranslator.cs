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
using MongoDB.Bson;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class AnyMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (AnyWithContainsInPredicateMethodToFilterTranslator.CanTranslate(expression, out var arrayFieldExpression, out var arrayConstantExpression))
            {
                return AnyWithContainsInPredicateMethodToFilterTranslator.Translate(context, arrayFieldExpression, arrayConstantExpression);
            }

            if (WhereFollowedByAnyMethodToFilterTranslator.CanTranslate(expression, out var whereExpression, out var anyExpression))
            {
                return WhereFollowedByAnyMethodToFilterTranslator.Translate(context, expression, whereExpression, anyExpression);
            }

            var sourceExpression = arguments[0];
            var sourceField = ExpressionToFilterFieldTranslator.Translate(context, sourceExpression);
            var elementSerializer = ArraySerializerHelper.GetItemSerializer(sourceField.Serializer);

            if (method.Is(EnumerableMethod.Any))
            {
                return new AstAndFilter(
                    new AstComparisonFilter(AstComparisonFilterOperator.Ne, sourceField, BsonNull.Value),
                    new AstNorFilter(new AstSizeFilter(sourceField, 0)));
            }

            if (method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var predicateLambda = (LambdaExpression)arguments[1];
                var parameterExpression = predicateLambda.Parameters[0];
                var parameterSymbol = new Symbol("$elem", elementSerializer);
                var predicateSymbolTable = new SymbolTable(parameterExpression, parameterSymbol); // only one symbol is visible inside an $elemMatch
                var predicateContext = new TranslationContext(predicateSymbolTable);
                var predicateFilter = ExpressionToFilterTranslator.Translate(predicateContext, predicateLambda.Body);
                return new AstElemMatchFilter(sourceField, predicateFilter);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
