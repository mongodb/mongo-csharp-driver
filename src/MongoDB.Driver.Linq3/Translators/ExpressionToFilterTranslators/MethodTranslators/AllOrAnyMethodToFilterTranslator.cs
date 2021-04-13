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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class AllOrAnyMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (AllWithContainsInPredicateMethodToFilterTranslator.CanTranslate(expression, out var arrayFieldExpression, out var arrayConstantExpression))
            {
                return AllWithContainsInPredicateMethodToFilterTranslator.Translate(context, arrayFieldExpression, arrayConstantExpression);
            }

            if (AnyWithContainsInPredicateMethodToFilterTranslator.CanTranslate(expression, out arrayFieldExpression, out arrayConstantExpression))
            {
                return AnyWithContainsInPredicateMethodToFilterTranslator.Translate(context, arrayFieldExpression, arrayConstantExpression);
            }

            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableMethod.All, EnumerableMethod.Any, EnumerableMethod.AnyWithPredicate))
            {
                var sourceExpression = arguments[0];
                var (field, filter) = FilteredEnumerableFilterFieldTranslator.Translate(context, sourceExpression);

                if (method.IsOneOf(EnumerableMethod.All, EnumerableMethod.AnyWithPredicate))
                {
                    var predicateLambda = (LambdaExpression)arguments[1];
                    var parameterExpression = predicateLambda.Parameters.Single();
                    var elementSerializer = ArraySerializerHelper.GetItemSerializer(field.Serializer);
                    var parameterSymbol = new Symbol("$elem", elementSerializer);
                    var predicateSymbolTable = new SymbolTable(parameterExpression, parameterSymbol); // $elem is the only symbol visible inside an $elemMatch
                    var predicateContext = new TranslationContext(predicateSymbolTable);
                    var predicateFilter = ExpressionToFilterTranslator.Translate(predicateContext, predicateLambda.Body, exprOk: false);

                    filter = AstFilter.Combine(filter, predicateFilter);
                }

                if (method.Is(EnumerableMethod.All))
                {
                    return AstFilter.Not(AstFilter.ElemMatch(field, AstFilter.Not(filter)));
                }
                else
                {
                    if (filter == null)
                    {
                        return AstFilter.And(AstFilter.Ne(field, BsonNull.Value), AstFilter.Not(AstFilter.Size(field, 0)));
                    }
                    else
                    {
                        return AstFilter.ElemMatch(field, filter);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }

    public static class FilteredEnumerableFilterFieldTranslator
    {
        public static (AstFilterField, AstFilter) Translate(TranslationContext context, Expression sourceExpression)
        {
            if (sourceExpression is MethodCallExpression sourceMethodCallExpression)
            {
                var method = sourceMethodCallExpression.Method;
                var arguments = sourceMethodCallExpression.Arguments;

                if (method.Is(EnumerableMethod.OfType))
                {
                    var ofTypeSourceExpression = arguments[0];
                    var (sourceField, sourceFilter) = Translate(context, ofTypeSourceExpression);

                    var nominalType = ArraySerializerHelper.GetItemSerializer(sourceField.Serializer).ValueType;
                    var actualType = method.GetGenericArguments()[0];

                    var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(actualType);
                    var discriminatorField = AstFilter.Field(discriminatorConvention.ElementName, BsonValueSerializer.Instance);
                    var discriminatorValue = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                    var ofTypeFilter = AstFilter.Eq(discriminatorField, discriminatorValue);

                    var actualTypeSerializer = BsonSerializer.LookupSerializer(actualType); // TODO: use known serializers
                    var enumerableActualTypeSerializer = IEnumerableSerializer.Create(actualTypeSerializer);
                    var actualTypeSourceField = AstFilter.Field(sourceField.Path, enumerableActualTypeSerializer);
                    var combinedFilter = AstFilter.Combine(sourceFilter, ofTypeFilter);

                    return (actualTypeSourceField, combinedFilter);
                }

                if (method.Is(EnumerableMethod.Where))
                {
                    var whereSourceExpression = arguments[0];
                    var (sourceField, sourceFilter) = Translate(context, whereSourceExpression);

                    var predicateLambda = (LambdaExpression)arguments[1];
                    var parameterExpression = predicateLambda.Parameters.Single();
                    var itemSerializer = ArraySerializerHelper.GetItemSerializer(sourceField.Serializer);
                    var parameterSymbol = new Symbol("$elem", itemSerializer);
                    var predicateSymbolTable = new SymbolTable(parameterExpression, parameterSymbol); // $elem is the only symbol visible inside an $elemMatch
                    var predicateContext = new TranslationContext(predicateSymbolTable);
                    var whereFilter = ExpressionToFilterTranslator.Translate(predicateContext, predicateLambda.Body, exprOk : false);
                    var combinedFilter = AstFilter.Combine(sourceFilter, whereFilter);

                    return (sourceField, combinedFilter);
                }
            }

            var field = ExpressionToFilterFieldTranslator.Translate(context, sourceExpression);
            return (field, null);
        }
    }
}
