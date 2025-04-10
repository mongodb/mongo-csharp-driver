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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewDictionaryExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, NewExpression expression)
        {
            var arguments = expression.Arguments;
            var collectionExpression = arguments.Single();
            var collectionTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, collectionExpression);

            if (collectionTranslation.Serializer is IBsonArraySerializer bsonArraySerializer &&
                bsonArraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
            {
                IBsonSerializer keySerializer = null;
                IBsonSerializer valueSerializer = null;
                AstExpression collectionTranslationAst;

                if (itemSerializationInfo.Serializer is IRepresentationConfigurable { Representation: BsonType.Array })
                {
                    collectionTranslationAst = collectionTranslation.Ast;
                }
                else if (itemSerializationInfo.Serializer is IBsonDocumentSerializer itemDocumentSerializer)
                {
                    if (!itemDocumentSerializer.TryGetMemberSerializationInfo("Key", out var keyMemberSerializationInfo) ||
                        !itemDocumentSerializer.TryGetMemberSerializationInfo("Value", out var valueMemberSerializationInfo))
                    {
                        throw new ExpressionNotSupportedException(expression, because: $"document serializer class {itemSerializationInfo.Serializer.GetType()} does not provide member serialization info for required fields.");
                    }

                    if (keyMemberSerializationInfo.ElementName == "k" && valueMemberSerializationInfo.ElementName == "v")
                    {
                        collectionTranslationAst = collectionTranslation.Ast;
                    }
                    else
                    {
                        keySerializer = keyMemberSerializationInfo.Serializer;
                        valueSerializer = valueMemberSerializationInfo.Serializer;

                        var pairVar = AstExpression.Var("pair");
                        var computedDocumentAst = AstExpression.ComputedDocument([
                            AstExpression.ComputedField("k", AstExpression.GetField(pairVar, keyMemberSerializationInfo.ElementName)),
                            AstExpression.ComputedField("v", AstExpression.GetField(pairVar, valueMemberSerializationInfo.ElementName))
                        ]);
                        collectionTranslationAst = AstExpression.Map(collectionTranslation.Ast, pairVar, computedDocumentAst);
                    }
                }
                else
                {
                    throw new ExpressionNotSupportedException(expression, because: $"document serializer class {itemSerializationInfo.Serializer.GetType()} does not implement {nameof(IBsonDocumentSerializer)}");
                }

                if (keySerializer is not IRepresentationConfigurable { Representation: BsonType.String })
                {
                    throw new ExpressionNotSupportedException(expression, because: "key did not serialize as a string");
                }

                var ast = AstExpression.Unary(AstUnaryOperator.ArrayToObject, collectionTranslationAst);
                var resultSerializer = CreateDictionarySerializer(keySerializer, valueSerializer);
                return new TranslatedExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static bool CanTranslate(NewExpression expression)
            => expression.Type.IsConstructedGenericType &&
               expression.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                DictionaryConstructor.IsIEnumerableKeyValuePairConstructor(expression.Constructor);

        private static IBsonSerializer CreateDictionarySerializer(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
        {
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keySerializer.ValueType, valueSerializer.ValueType);
            var serializerType = typeof(DictionaryInterfaceImplementerSerializer<,,>).MakeGenericType(dictionaryType, keySerializer.ValueType, valueSerializer.ValueType);

            return (IBsonSerializer)Activator.CreateInstance(serializerType, DictionaryRepresentation.Document, keySerializer, valueSerializer);
        }
    }
}
