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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class OfTypeMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(EnumerableOrQueryableMethod.OfType))
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

                var nominalType = itemSerializer.ValueType;
                var nominalTypeSerializer = itemSerializer;
                var actualType = method.GetGenericArguments().Single();
                var actualTypeSerializer = BsonSerializer.LookupSerializer(actualType);

                AstExpression ast;
                if (nominalType == actualType)
                {
                    ast = sourceAst;
                }
                else
                {
                    var discriminatorConvention = nominalTypeSerializer.GetDiscriminatorConvention();
                    var itemVar = AstExpression.Var("item");
                    var discriminatorField = AstExpression.GetField(itemVar, discriminatorConvention.ElementName);

                    var ofTypeExpression = discriminatorConvention switch
                    {
                        IHierarchicalDiscriminatorConvention hierarchicalDiscriminatorConvention => DiscriminatorAstExpression.TypeIs(discriminatorField, hierarchicalDiscriminatorConvention, nominalType, actualType),
                        IScalarDiscriminatorConvention scalarDiscriminatorConvention => DiscriminatorAstExpression.TypeIs(discriminatorField, scalarDiscriminatorConvention, nominalType, actualType),
                        _ => throw new ExpressionNotSupportedException(expression, because: "OfType is not supported with the configured discriminator convention")
                    };

                    ast = AstExpression.Filter(
                        input: sourceAst,
                        cond: ofTypeExpression,
                        @as: "item");
                }

                var resultSerializer = NestedAsQueryableSerializer.CreateIEnumerableOrNestedAsQueryableSerializer(expression.Type, actualTypeSerializer);
                return new TranslatedExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
