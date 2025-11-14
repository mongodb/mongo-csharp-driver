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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewDictionaryExpressionToAggregationExpressionTranslator
    {
        public static bool CanTranslate(NewExpression expression)
            => DictionaryConstructor.IsWithIEnumerableKeyValuePairConstructor(expression.Constructor);

        public static TranslatedExpression Translate(TranslationContext context, NewExpression expression)
        {
            var arguments = expression.Arguments;

            var collectionExpression = arguments[0];
            var collectionTranslation = ExpressionToAggregationExpressionTranslator.TranslateEnumerable(context, collectionExpression);
            var itemSerializer = ArraySerializerHelper.GetItemSerializer(collectionTranslation.Serializer);

            AstExpression collectionTranslationAst;

            if (itemSerializer.IsKeyValuePairSerializer(out var keyElementName, out var valueElementName, out var keySerializer, out var valueSerializer))
            {
                if (keyElementName == "k" && valueElementName == "v")
                {
                    collectionTranslationAst = collectionTranslation.Ast;
                }
                else
                {
                    // map keyElementName and valueElementName to "k" and "v"
                    var pairVar = AstExpression.Var("pair");
                    var computedDocumentAst = AstExpression.ComputedDocument([
                        AstExpression.ComputedField("k", AstExpression.GetField(pairVar, keyElementName)),
                        AstExpression.ComputedField("v", AstExpression.GetField(pairVar, valueElementName))
                    ]);

                    collectionTranslationAst = AstExpression.Map(collectionTranslation.Ast, pairVar, computedDocumentAst);
                }
            }
            else
            {
                throw new ExpressionNotSupportedException(expression);
            }

            if (keySerializer is not IRepresentationConfigurable { Representation: BsonType.String })
            {
                throw new ExpressionNotSupportedException(expression, because: "key does not serialize as a string");
            }

            var ast = AstExpression.Unary(AstUnaryOperator.ArrayToObject, collectionTranslationAst);
            var resultSerializer = CreateResultSerializer(keySerializer, valueSerializer);
            return new TranslatedExpression(expression, ast, resultSerializer);
        }

        private static IBsonSerializer CreateResultSerializer(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
        {
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keySerializer.ValueType, valueSerializer.ValueType);
            var serializerType = typeof(DictionaryInterfaceImplementerSerializer<,,>).MakeGenericType(dictionaryType, keySerializer.ValueType, valueSerializer.ValueType);

            return (IBsonSerializer)Activator.CreateInstance(serializerType, DictionaryRepresentation.Document, keySerializer, valueSerializer);
        }
    }
}
